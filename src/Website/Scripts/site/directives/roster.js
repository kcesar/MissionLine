angular.module('missionlineApp').directive('roster', ['eventsService', 'EditModalService', function (eventsService, EditModalService) {
  return {
    restrict: 'E',
    templateUrl: 'roster.html',
    scope: {
      event: '=',
      sort: '=',
      sortDir: '=',
      eventOrderBy: '='
    },
    bindToController: true,
    controllerAs: 'rosterCtrl',
    controller: ['eventsService', 'toasterService', '$scope', function (eventsService, toasterService, $scope) {
      $scope.event = this.event;
      $.extend(this, {
        events: eventsService.list,
        moveToEvent: function (responder, otherEvent) {
          eventsService.moveResponder(responder, this.event, otherEvent);
        },
        signout: function (responder) {
          console.log('should sign out' + responder.name);
        },
        undoSignout: function (responder) {
          console.log('should undo signout for ' + responder.name);
        },
        startEdit: function () { EditModalService.edit('editDialogTemplate.html', 'Edit Event', new EventModel(this.event.getData()), eventsService.save); },
        startClose: function () {
          eventsService.close(this.event)
            .catch(function (error) { toasterService.toast('Error', 'danger', error); });
        },
        reopen: function () {
          eventsService.repoen(this.event)
            .catch(function (error) { toasterService.toast('Error', 'danger', error); })
        },
        startMerge: function (otherEvent) {
          EditModalService.edit('mergeDialogTemplate.html', 'Merge Event', { from: this.event, available: eventsService.list, into: null, eventOrderBy: this.eventOrderBy }, eventsService.merge, 'Merge');
        }
      })
    }]
  }
}]);