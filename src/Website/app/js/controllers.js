var missionlineApp = angular.module('missionlineApp', ['angularModalService', 'ngAnimate']);

missionlineApp.animation('.animate-card', function () {
  return {
    enter: function (element, done) {
      element.css({
        position: 'relative',
      });
      element.hide().slideDown(done)
    }
  };
});

var EventModel = function (name, mmnt) {
  mmnt = mmnt || moment();
  var self = this;
  $.extend(this, {
    name: name,
    datePart: mmnt.format("YYYY-MM-DD"),
    timePart: mmnt.format("HH:mm"),
    getData: function () {
      return { name: self.name, opened: moment() }
    }
  })
};
EventModel.fromServer = function (data) {
  if (data.opened) { data.opened = moment(data.opened); }
  data.roster = data.roster || [];
  return data;
}

//==============================================
missionlineApp.service('eventsService', ['$sce', '$http', '$q', '$rootScope', 'pushService',
  function ($sce, $http, $q, $rootScope, pushService) {
  var self = this;
  $.extend(this, {
    list: [
      {
        id: 1, name: 'Mailbox missing hiker', roster: [
          { name: 'Matthew', timeIn: moment(new Date(2015, 09-1, 28, 5, 46)) },
          { name: 'Amber', timeIn: moment(new Date(2015, 09-1, 28, 5, 47)), isMember: true, memberId: 'asdfasdf' }
        ], opened: moment(new Date(2015, 09-1, 27, 8))
      },
      { id: 2, 'name': 'Something else', roster: [], opened: moment(new Date(2015, 09-1, 26, 20, 45)) },
      { id: 3, 'name': 'closed', roster: [], opened: moment(new Date(2015, 09-1,27)), closed: moment(new Date(2015,09-1,28)) }
    ],
    calls: [
        { number: '206-776-2036', timeText: 'yesterday', name: 'Matt', recording: $sce.trustAsResourceUrl('//example.com/foo.mp3') },
        { number: '206-776-0055', timeText: 'yesterday', name: 'Amber', recording: $sce.trustAsResourceUrl('//example.com/foo.mp3') }
    ],
    load: function() {
      self.list.length = 0;
      self.list.loading = true;
      $http({
        method: 'GET',
        url: window.appRoot + 'api/events',
      }).success(function (data) {
        $.each(data, function (idx, event) {
          self.list.push(EventModel.fromServer(event));
        });
        delete self.list.loading;
      })
    },
    create: function (eventModel) {
      var deferred = $q.defer();
      $http({
        method: 'POST',
        url: window.appRoot + 'api/events',
        data: eventModel.getData(),
        headers: { 'Content-Type': 'application/json' }
      }).success(function (data) {
        deferred.resolve();
      })
      .error(function (response) {
        deferred.resolve();
      });
      return deferred.promise;
    },
  });
  pushService.listenTo('updatedEvent', function (data) {
    console.log(data);
    self.list.push(EventModel.fromServer(data));
    $rootScope.$digest();
  });
  pushService.listenTo('removedEvent', function (data) {
    debugger;
  })
}]);

//==============================================
missionlineApp.service('toasterService', function () {
  this.toast = function (title, priority, message) {
    $.toaster({ title: title, priority: priority, message: message });
  }
});

//==============================================
missionlineApp.service('pushService', ['toasterService', function (toaster) {
  var handlers = {};

  var hubClient = $.connection.callsHub.client;
  $.connection.hub.connectionSlow(function () { toaster.toast('Connection Slow', 'info', 'Slow connection detected. May not get all updates') });
  $.connection.hub.reconnecting(function () { toaster.toast('Reconnecting...', 'warning', 'Reconnecting to update stream'); });
  $.connection.hub.reconnected(function () { toaster.toast('Reconnected', 'success', 'Reconnected to update stream'); });
  $.connection.hub.disconnected(function () { toaster.toast('Disconnected', 'danger', 'Disconnected from update stream. Refresh the page.') });

  this.listenTo = function (actionName, handler) {
    if (handlers[actionName] === undefined) {
      handlers[actionName] = [handler];
      hubClient[actionName] = function (data) { $.each(handlers[actionName], function (idx, h) { h(data); })}
    } else {
      handlers[actionName].push(handler);
    }
  }
}])

//==============================================
missionlineApp.controller('ModalController', [
  '$scope', '$element', '$q', 'title', 'model', 'save', 'close',
    function ($scope, $element, $q, title, model, save, close) {
      var self = this;

      $scope.close = function () {
        self.close();
      }

      var bootstrapDialog = new BootstrapDialog({
        title: title,
        message: $element,
        buttons: [{
          label: 'Save',
          action: function (dialogRef) {
            var btn = this;
            btn.disable().spin();
            save(model)
              .then($scope.close)
              .finally(function () { btn.enable().stopSpin(); });
          }
        }, {
          label: 'Cancel',
          action: $scope.close
        }]
      });

      self.close = function () {
        bootstrapDialog.close();
        close(false, 500);
      }

      self.open = bootstrapDialog.open.bind(bootstrapDialog);

      $scope.model = model;
    }]);

//==============================================
missionlineApp.controller('IndexCtrl', ['$scope', '$q', 'ModalService', 'eventsService', function ($scope, $q, ModalService, eventsService) {
  this.createHandler = function () {
    ModalService.showModal({
      templateUrl: "editDialogTemplate.html",
      controller: "ModalController",
      inputs: {
        title: 'Create New Event',
        model: new EventModel(),
        save: function (model) {
          console.log(model);
          return eventsService.create(model);
          var saveDeferred = $q.defer();
          if (model.name == "fred") {
            saveDeferred.reject();
          } else { saveDeferred.resolve(); }
          return saveDeferred.promise;
        }
      }
    }).then(function (modal) {
      modal.controller.open();
    });
  }

  $.extend($scope, {
    showRoster: true,
    showCalls: false,
    unassignedEvent: { 'name': 'Unassigned', roster: [{ name: 'Matt' }], isUnassigned: true },
    callsSort: 'time',
    callsSortDesc: true,
    rosterSort: 'timeOut',
    rosterSortDesc: true,
    events: eventsService.list,
    eventsSortPredicate: function (event) {
      var v = -event.opened.unix() + (event.closed ? 100000000 : 0);
      console.log({ n: event.name, t: event.opened.format(), v: v });
      return v;
    },
    calls: eventsService.calls,
    createEvent: this.createHandler
  })

  eventsService.load();
}]);

//==============================================
missionlineApp.directive('roster', ['eventsService', function (eventsService) {
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
      this.startEdit = function () {
        console.log('start edit');
      };
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

//==============================================
missionlineApp.directive('whatScope', function () {
  return {
    restrict: 'E',
    bindToController: true,
    controllerAs: 'fooCtrl',
    controller: ['$scope', function ($scope) {
      debugger;
    }]
  }
});

//==============================================
missionlineApp.filter('eventTime', function () {
  return function (input, fromDate) {
    if (!input || !input.isValid()) { return '' };
    var me = input.clone().startOf('day'), reference = fromDate.clone().startOf('day');
    var days = Math.floor(me.diff(reference, 'days', true));
    return (days ? (days + '+') : '') + input.format("HHmm");
  }
});

//==============================================
missionlineApp.filter('exceptId', function () {
  return function (items, item) {
    var filtered = [];
    angular.forEach(items, function (it) {
      if (it.id != item.id) {
        filtered.push(it);
      }
    });
    return filtered;
  }
});