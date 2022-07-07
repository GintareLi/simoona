(function() {
    'use strict';

    angular
        .module('simoonaApp.Events')
        .component('aceEventParticipants', {
            bindings: {
                event: '=',
                isAdmin: '=',
                isLoading: '='
            },
            templateUrl: 'app/events/content/participants/participants.html',
            controller: eventParticipantsController,
            controllerAs: 'vm'
        });

    eventParticipantsController.$inject = [
        'eventRepository',
        'authService',
        'eventParticipantsService',
        'eventStatusService',
        'eventStatus',
        'errorHandler',
        'lodash',
        'Analytics',
        'attendStatus'
    ];

    function eventParticipantsController(eventRepository, authService, eventParticipantsService,
        eventStatusService, eventStatus, errorHandler, lodash, Analytics, attendStatus) {
        /* jshint validthis: true */
        var vm = this;

        vm.isParticipantsLoading = false;
        vm.isMainParticipantList = true;

        vm.eventStatus = eventStatus;
        vm.eventStatusService = eventStatusService;
        vm.participantsTabs = [{
            name: 'ParticipantsList',
            isOpen: true
        }, {
            name: 'OptionsList',
            isOpen: false
        }];

        vm.goToTab = goToTab;
        vm.expelUserFromEvent = expelUserFromEvent;
        vm.isDeleteVisible = isDeleteVisible;
        vm.isActiveTab = isActiveTab;
        vm.isExportVisible = isExportVisible;

        /////////

        function goToTab(tab) {
            vm.participantsTabs.forEach(function(item) {
                if (tab === item.name) {
                    item.isOpen = true;
                } else {
                    item.isOpen = false;
                }
            });
        }

        function isActiveTab(tab) {
            return !!lodash.find(vm.participantsTabs, function(obj) {
                return !!obj.isOpen && obj.name === tab;
            });
        }

        function isDeleteVisible() {
            return vm.isAdmin && eventStatusService.getEventStatus(vm.event) !== eventStatus.Finished;
        }

        function isExportVisible() {
            return vm.event.goingCount > 0;
        }

        function expelUserFromEvent(participant) {
            Analytics.trackEvent('Events', 'expelUserFromEvent: ' + participant.userId, 'Event: ' + vm.event.id);
            if (!participant.isLoading) {
                participant.isLoading = true;

                eventRepository.expelUserFromEvent(vm.event.id, participant.userId).then(function() {
                    participant.isLoading = false;

                    eventParticipantsService.removeParticipant(vm.event.participants, participant.userId);
                    eventParticipantsService.removeParticipantFromOptions(vm.event.options, participant.userId);

                    if (authService.identity.userId === participant.userId) {
                        vm.event.participatingStatus = attendStatus.Idle;
                    }

                    if (vm.event.maxParticipants > vm.event.participants.length) {
                        vm.event.isFull = false;
                    }

                    vm.event.participantsCount = vm.event.participants.length;

                    if(participant.attendStatus === attendStatus.Attending)
                    {
                        vm.event.goingCount -= 1;
                    }
                }, function(response) {
                    participant.isLoading = false;

                    errorHandler.handleErrorMessage(response, 'expelParticipant');
                });
            }
        }
    }
})();
