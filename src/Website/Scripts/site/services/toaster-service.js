angular.module('missionlineApp').service('toasterService', function () {
  this.toast = function (title, priority, message) {
    $.toaster({ title: title, priority: priority, message: message });
  }
});