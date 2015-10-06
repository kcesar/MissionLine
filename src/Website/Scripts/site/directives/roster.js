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
    controller: ['rosterService', 'eventsService', 'toasterService', '$scope', function (rosterService, eventsService, toasterService, $scope) {
      $scope.event = this.event;
      $.extend(this, {
        events: eventsService.list,
        moveToEvent: function (responder, otherEvent) {
          eventsService.moveResponder(responder, this.event, otherEvent);
        },
        signout: function (signin) {
          var data = signin.getData();
          data.timeOut = moment();
          EditModalService.edit('signoutDialog.html', 'Sign Out', new SigninModel(data), rosterService.signout, { event: this.event });
        },
        undoSignout: function (signin) {
          var data = signin.getData();
          data.timeOut = null;
          rosterService.signout(new SigninModel(data));
        },
        startEdit: function () { EditModalService.edit('editDialog.html', 'Edit Event', new EventModel(this.event.getData()), eventsService.save); },
        startClose: function () {
          eventsService.close(this.event)
            .catch(function (error) { toasterService.toast('Error', 'danger', error); });
        },
        reopen: function () {
          eventsService.repoen(this.event)
            .catch(function (error) { toasterService.toast('Error', 'danger', error); })
        },
        startMerge: function (otherEvent) {
          EditModalService.edit('mergeDialog.html', 'Merge Event', { from: this.event, available: eventsService.list, into: null, eventOrderBy: this.eventOrderBy }, eventsService.merge, { saveText: 'Merge' });
        }
      })
    }]
  }
}]);