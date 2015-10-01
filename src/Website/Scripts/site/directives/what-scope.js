angular.module('missionlineApp').directive('whatScope', function () {
  return {
    restrict: 'E',
    bindToController: true,
    controllerAs: 'fooCtrl',
    controller: ['$scope', function ($scope) {
      debugger;
    }]
  }
});