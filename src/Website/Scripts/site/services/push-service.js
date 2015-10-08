angular.module('missionlineApp').service('pushService', ['toasterService', function (toaster) {
  var handlers = {};

  var hubClient = $.connection.callsHub.client;
  $.connection.hub.connectionSlow(function () { toaster.toast('Connection Slow', 'info', 'Slow connection detected. May not get all updates') });
  $.connection.hub.reconnecting(function () { toaster.toast('Reconnecting...', 'warning', 'Reconnecting to update stream'); });
  $.connection.hub.reconnected(function () { toaster.toast('Reconnected', 'success', 'Reconnected to update stream'); });
  $.connection.hub.disconnected(function () { toaster.toast('Disconnected', 'danger', 'Disconnected from update stream. Refresh the page.') });

  this.listenTo = function (actionName, handler) {
    if (handlers[actionName] === undefined) {
      handlers[actionName] = [handler];
      hubClient[actionName] = function () {
        var args = arguments;
        var _this = this;
        $.each(handlers[actionName], function (idx, h) {
          h.apply(_this, args);
        })
      }
    } else {
      handlers[actionName].push(handler);
    }
  }
}])