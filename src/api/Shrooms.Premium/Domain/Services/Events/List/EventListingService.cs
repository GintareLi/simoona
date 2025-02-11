﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Shrooms.Contracts.DAL;
using Shrooms.Contracts.DataTransferObjects;
using Shrooms.Contracts.Enums;
using Shrooms.DataLayer.EntityModels.Models.Events;
using Shrooms.Premium.Constants;
using Shrooms.Premium.DataTransferObjects.Models.Events;
using Shrooms.Premium.Domain.DomainServiceValidators.Events;
using Shrooms.Premium.Domain.Services.Args;

namespace Shrooms.Premium.Domain.Services.Events.List
{
    public class EventListingService : IEventListingService
    {
        private const string OutsideOffice = "[]";

        private static readonly Dictionary<MyEventsOptions, Func<string, Expression<Func<Event, bool>>>>
            _eventFilters = new Dictionary<MyEventsOptions, Func<string, Expression<Func<Event, bool>>>>
            {
                { MyEventsOptions.Host, MyEventsAsMasterFilter },
                { MyEventsOptions.Participant, MyEventsAsParticipantFilter }
            };

        private readonly IEventValidationService _eventValidationService;

        private readonly IDbSet<Event> _eventsDbSet;

        public EventListingService(IUnitOfWork2 uow, IEventValidationService eventValidationService)
        {
            _eventValidationService = eventValidationService;
            _eventsDbSet = uow.GetDbSet<Event>();
        }

        public async Task<EventOptionsDto> GetEventOptionsAsync(Guid eventId, UserAndOrganizationDto userOrg)
        {
            var eventOptionsDto = await _eventsDbSet
                .Include(e => e.EventOptions)
                .Where(e => e.Id == eventId && e.OrganizationId == userOrg.OrganizationId)
                .Select(MapOptionsToDto())
                .SingleOrDefaultAsync();

            _eventValidationService.CheckIfEventExists(eventOptionsDto);
            return eventOptionsDto;
        }

        public async Task<IEnumerable<EventListItemDto>> GetEventsFilteredAsync(EventsListingFilterArgs args, UserAndOrganizationDto userOrganization)
        {
            var officeSearchString = OfficeIdToString(args.OfficeId);

            var query = _eventsDbSet
                .Include(x => x.EventParticipants)
                .Include(x => x.EventType)
                .Where(e => e.OrganizationId == userOrganization.OrganizationId)
                .Where(EventTypeFilter(args.TypeId, args.IsOnlyMainEvents))
                .Where(EventOfficeFilter(officeSearchString));

            if (args.StartDate is null || args.EndDate is null)
            {
                query = query.Where(e => e.EndDate > DateTime.UtcNow);
            }
            else
            {
                _eventValidationService.CheckIfDateRangeExceededLimitOrNull(args.StartDate, args.EndDate);
                query = query.Where(e => e.StartDate >= args.StartDate && e.EndDate <= args.EndDate);
            }

            var events = query
                .OrderByDescending(e => e.IsPinned)
                .ThenBy(e => e.StartDate)
                .Skip((args.Page - 1) * EventsConstants.EventsDefaultPageSize)
                .Take(EventsConstants.EventsDefaultPageSize)
                .Select(MapEventToListItemDto(userOrganization.UserId));

            return await events.ToListAsync();
        }

        public async Task<IEnumerable<EventListItemDto>> GetMyEventsAsync(MyEventsOptionsDto options, int page, int? officeId = null)
        {
            var officeSearchString = OfficeIdToString(officeId);
            var myEventFilter = _eventFilters[options.Filter](options.UserId);
            var events = await _eventsDbSet
                .Include(x => x.EventParticipants)
                .Include(x => x.Offices)
                .Where(t => t.OrganizationId == options.OrganizationId)
                .Where(SearchFilter(options.SearchString))
                .Where(myEventFilter)
                .Where(EventOfficeFilter(officeSearchString))
                .OrderByDescending(e => e.StartDate)
                .Skip((page - 1) * EventsConstants.EventsDefaultPageSize)
                .Take(EventsConstants.EventsDefaultPageSize)
                .Select(MapEventToListItemDto(options.UserId))
                .ToListAsync();

            var orderedEvents = OrderEvents(events);
            return orderedEvents;
        }

        private static Expression<Func<Event, bool>> MyEventsAsMasterFilter(string userId)
        {
            return x => x.ResponsibleUserId == userId;
        }

        private static Expression<Func<Event, bool>> MyEventsAsParticipantFilter(string userId)
        {
            return x => x.EventParticipants.Any(p => p.ApplicationUserId == userId);
        }

        private static IEnumerable<EventListItemDto> OrderEvents(List<EventListItemDto> events)
        {
            var orderedFutureEvents = events
                .Where(e => e.StartDate > DateTime.UtcNow)
                .OrderByDescending(e => e.IsPinned)
                .ThenBy(e => e.StartDate);

            var orderedPastEvents = events
                .Where(e => e.StartDate < DateTime.UtcNow)
                .OrderByDescending(e => e.StartDate);
            return orderedFutureEvents.Concat(orderedPastEvents);
        }

        private static Expression<Func<Event, EventListItemDto>> MapEventToListItemDto(string userId)
        {
            return e => new EventListItemDto
            {
                Id = e.Id,
                ImageName = e.ImageName,
                Offices = new EventOfficesDto { Value = e.Offices },
                MaxParticipants = e.MaxParticipants,
                IsPinned = e.IsPinned,
                Name = e.Name,
                Place = e.Place,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                RegistrationDeadlineDate = e.RegistrationDeadline,
                ParticipantsCount = e.EventParticipants.Count(p => p.AttendStatus == (int)AttendingStatus.Attending),
                IsCreator = e.ResponsibleUserId == userId,
                ParticipatingStatus = e.EventParticipants.FirstOrDefault(p => p.ApplicationUserId == userId) != null
                    ? e.EventParticipants.FirstOrDefault(p => p.ApplicationUserId == userId).AttendStatus
                    : (int)AttendingStatus.Idle,
                MaxChoices = e.MaxChoices
            };
        }

        private static Expression<Func<Event, EventOptionsDto>> MapOptionsToDto()
        {
            return e => new EventOptionsDto
            {
                MaxOptions = e.MaxChoices,
                Options = e.EventOptions.Select(o => new EventOptionDto
                    {
                        Id = o.Id,
                        Option = o.Option,
                        Rule = o.Rule
                    })
                    .OrderByDescending(o => o.Rule == OptionRules.Default)
            };
        }

        private static Expression<Func<Event, bool>> EventTypeFilter(int? typeId, bool includeOnlyMain = false)
        {
            if (includeOnlyMain)
            {
                return x => x.EventType.IsShownWithMainEvents;
            }

            if (typeId == null || typeId == 0)
            {
                return x => true;
            }

            return x => x.EventTypeId == typeId;
        }

        private static Expression<Func<Event, bool>> EventOfficeFilter(string office)
        {
            if (office == OutsideOffice || office == null)
            {
                return x => true;
            }

            return x => x.Offices.Contains(office) || x.Offices == OutsideOffice;
        }

        private static Expression<Func<Event, bool>> SearchFilter(string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return e => true;
            }

            return e => e.Name.Contains(searchString) || e.Place.Contains(searchString);
        }

        private static string OfficeIdToString(int? officeId) => officeId != null ? $@"""{officeId.ToString()}""" : OutsideOffice;
    }
}
