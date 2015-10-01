angular.module('missionlineApp').directive('roster', ['eventsService', 'EditModalService', function (eventsService, EditModalService) {
  return {
    restrict: 'E',
    templateUrl: 'roster.html',
    scope: {
      event: '=',
      sort: '=',
      sortDir: '='
    },
    bindToController: true,
    controllerAs: 'rosterCtrl',
    controller: ['eventsService', '$scope', function (eventsService, $scope) {
      $scope.event = this.event;
      this.moveToEvent = function (responder, otherEvent) {
        eventsService.moveResponder(responder, this.event, otherEvent);
      };
      this.signout = function (responder) {
        console.log('should sign out' + responder.name);
      };
      this.undoSignout = function (responder) {
        console.log('should undo signout for ' + responder.name);
      };
      this.startEdit = function () { EditModalService.edit('Edit Event', new EventModel(this.event.getData()), eventsService.save); };

      this.startClose = function () {
        console.log('start close');
      };
      this.reopen = function () {
        console.log('reopen');
      };
      this.startMerge = function () {
        console.log('start merge');
      };
      this.events = eventsService.list;
    }]
  }
}]);