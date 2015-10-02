angular.module('missionlineApp').service('eventsService', ['$sce', '$http', '$q', '$rootScope', 'pushService',
  function ($sce, $http, $q, $rootScope, pushService) {
    var self = this;
    $.extend(this, {
      list: [],
      calls: [
          { number: '206-776-2036', timeText: 'yesterday', name: 'Matt', recording: $sce.trustAsResourceUrl('//example.com/foo.mp3') },
          { number: '206-776-0055', timeText: 'yesterday', name: 'Amber', recording: $sce.trustAsResourceUrl('//example.com/foo.mp3') }
      ],
      load: function () {
        self.list.length = 0;
        self.list.loading = true;
        $http({
          method: 'GET',
          url: window.appRoot + 'api/events',
        }).success(function (data) {
          $.each(data, function (idx, event) {
            self.list.push(new EventModel(event));
          });
          delete self.list.loading;
        })
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
      }
    });
    pushService.listenTo('updatedEvent', function (data) {
      console.log(data);
      var event = new EventModel(data);
      var found = false;
      for (var i = 0; i < self.list.length; i++) {
        if (self.list[i].id == data.id) {
          self.list[i] = event;
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
        if (item.id == data.id) { index = idx; }
        return index === -1;
      })
      self.list.splice(index, 1);
    })
  }]);
