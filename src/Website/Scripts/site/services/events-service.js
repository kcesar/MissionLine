angular.module('missionlineApp').service('eventsService', ['$sce', '$http', '$q', '$rootScope', 'pushService', 'rosterService',
  function ($sce, $http, $q, $rootScope, pushService, rosterService) {
    var self = this;
    $.extend(this, {
      list: [],
      _lookup: {},
      unassigned: new EventModel({ 'name': 'Unassigned', roster: [], isUnassigned: true }),
      load: function () {
        var deferred = $q.defer();
        var rosterPromise = rosterService.load();
        self.list.length = 0;
        self.list.loading = true;
        $http({
          method: 'GET',
          url: window.appRoot + 'api/events',
        }).success(function (data) {
          $.each(data, function (idx, event) {
            var model = new EventModel(event);
            self.list.push(model);
            self._lookup[event.id] = model;
          });
          delete self.list.loading;
          deferred.resolve(data);
        })
        .error(function (response) { deferred.reject(response); });
        
        return $q.all([rosterPromise, deferred.promise]).then(function () { self.populateRosters(); });
      },
      save: function (eventModel) {
        var deferred = $q.defer();
        $http({
          method: eventModel.id ? 'PUT' : 'POST',
          url: window.appRoot + 'api/events',
          data: eventModel.getData(),
          headers: { 'Content-Type': 'application/json' }
        }).success(function (data) {
          deferred.resolve(data);
        })
        .error(function (response) {
          deferred.reject(response);
        });
        return deferred.promise;
      },
      _updateCloseTime: function (eventModel, newClosed) {
        var deferred = $q.defer();
        if ($.grep(eventModel.roster, function (item) { return item.timeOut }, true).length > 0) {
          deferred.reject("Can\t close event until everyone is signed out");
        }
        var shadow = new EventModel(eventModel.getData());
        shadow.closed = newClosed;
        self.save(shadow)
        .then(
          function (result) { deferred.resolve(result) },
          function (error) { deferred.reject(result); }
        );
        return deferred.promise;
      },
      close: function (eventModel) { return self._updateCloseTime(eventModel, moment()); },
      reopen: function (eventModel) { return self._updateCloseTime(eventModel, null); },
      merge: function (mergeModel) {
        var deferred = $q.defer();
        $http({
          method: 'POST',
          url: window.appRoot + 'api/events/' + mergeModel.from.id + '/merge/' + mergeModel.into.id
        })
        .success(function (data) { deferred.resolve(data); })
        .error(function (response) { deferred.reject(response); })
        return deferred.promise;
      },
      populateRosters: function () {
        self.unassigned.roster.length = 0;
        for (var i = 0; i < self.list.length; i++) { self.list[i].roster.length = 0; }
        $.each(rosterService.signins, function (idx, item) {
          item.event = self._lookup[item.eventId];
          if (!item.event) {
            console.log('no event found for '); console.log(item);
          }
          item.eventId ? item.event.roster.push(item) : self.unassigned.roster.push(item);
        })
      }
    });

    $rootScope.$on('roster-updated', self.populateRosters);

    pushService.listenTo('updatedEvent', function (data) {
      var event = new EventModel(data);
      var found = false;
      for (var i = 0; i < self.list.length; i++) {
        if (self.list[i].id == data.id) {
          self.list[i] = event;
          self._lookup[event.id] = event;
          found = true;
          break;
        }
      }
      if (!found) { self.list.push(event); }
      $rootScope.$digest();
    });
    pushService.listenTo('removedEvent', function (data) {
      var index = -1;
      $.each(self.list, function (idx, item) {
        if (item.id == data.id) { index = idx; delete self._lookup[data.id]; }
        return index === -1;
      })
      self.list.splice(index, 1);
    });
  }]);
