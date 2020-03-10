﻿using AutoMapper;
using Newtonsoft.Json;
using Shrooms.DataLayer.EntityModels.Models.Events;
using Shrooms.Premium.DataTransferObjects.Models.Events;
using Shrooms.Premium.DataTransferObjects.Models.OfficeMap;
using Shrooms.Premium.Presentation.WebViewModels.Events;

namespace Shrooms.Premium.Presentation.ModelMappings.Profiles
{
    public class EventsProfile : Profile
    {
        protected override void Configure()
        {
            CreateDtoToViewModelMappings();
            CreateViewModelToDtoMappings();
            CreateEventsModelMappings();
        }

        private void CreateDtoToViewModelMappings()
        {
            CreateMap<OfficeDTO, EventOfficeViewModel>();
            CreateMap<EventTypeDTO, EventTypeViewModel>();
            CreateMap<EventListItemDTO, EventListItemViewModel>()
                .ForMember(dest => dest.OfficeIds, opt => opt.MapFrom(u => JsonConvert.DeserializeObject<string[]>(u.Offices.Value)));
            CreateMap<EventOptionDTO, EventOptionViewModel>();
            CreateMap<EventDetailsDTO, EventDetailsViewModel>()
                .Ignore(x => x.Comments)
                .Ignore(x => x.IsForAllOffices)
                .ForMember(dest => dest.OfficesName, opt => opt.MapFrom(u => u.Offices.OfficeNames));

            CreateMap<NewEventOptionDTO, NewEventOptionViewModel>();
            CreateMap<NewEventOptionViewModel, NewEventOptionDTO>();

            CreateMap<EventDetailsOptionDTO, EventDetailsOptionViewModel>();
            CreateMap<EventDetailsParticipantDTO, EventDetailsParticipantViewModel>();
            CreateMap<EventEditDTO, EventEditViewModel>()
                .ForMember(dest => dest.OfficeIds, opt => opt.MapFrom(u => JsonConvert.DeserializeObject<string[]>(u.Offices.Value)));
            CreateMap<EventOptionsDTO, EventOptionsViewModel>();

            CreateMap<EventChangeOptionViewModel, EventChangeOptionsDTO>()
                .Ignore(x => x.OrganizationId)
                .Ignore(x => x.UserId);
        }

        private void CreateViewModelToDtoMappings()
        {
            CreateMap<CreateEventViewModel, CreateEventDto>()
                .Ignore(d => d.ResetParticipantList)
                .Ignore(d => d.Id)
                .Ignore(d => d.Offices)
                .IgnoreUserOrgDto();
            CreateMap<UpdateEventViewModel, EditEventDTO>()
                .IgnoreUserOrgDto()
                .Ignore(d => d.Offices);
            CreateMap<MyEventsOptionsViewModel, MyEventsOptionsDTO>()
                .IgnoreUserOrgDto();
            CreateMap<EventJoinViewModel, EventJoinDTO>()
                .Ignore(d => d.ParticipantIds)
                .IgnoreUserOrgDto();
            CreateMap<EventJoinMultipleViewModel, EventJoinDTO>()
                .Ignore(d => d.AttendStatus)
                .Ignore(d => d.AttendComment)
                .IgnoreUserOrgDto();
            CreateMap<EventOptionViewModel, EventOptionDTO>();

            CreateMap<UpdateAttendStatusViewModel, UpdateAttendStatusDTO>()
                .IgnoreUserOrgDto();

            CreateMap<EventQueueViewModel, EventQueueDTO>();

            CreateMap<CreateEventTypeViewModel, CreateEventTypeDTO>().IgnoreUserOrgDto();
            CreateMap<UpdateEventTypeViewModel, UpdateEventTypeDTO>().IgnoreUserOrgDto();
        }

        private void CreateEventsModelMappings()
        {
            CreateMap<Event, Event>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EventParticipants, opt => opt.Ignore());
        }
    }
}
