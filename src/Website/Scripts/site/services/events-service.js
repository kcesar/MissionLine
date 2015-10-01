angular.module('missionlineApp').service('eventsService', ['$sce', '$http', '$q', '$rootScope', 'pushService',
  function ($sce, $http, $q, $rootScope, pushService) {
    var self = this;
    $.extend(this, {
      list: [
        {
          id: 1, name: 'Mailbox missing hiker', roster: [
            { name: 'Matthew', timeIn: moment(new Date(2015, 09 - 1, 28, 5, 46)) },
            { name: 'Amber', timeIn: moment(new Date(2015, 09 - 1, 28, 5, 47)), isMember: true, memberId: 'asdfasdf' }
          ], opened: moment(new Date(2015, 09 - 1, 27, 8))
        },
        { id: 2, 'name': 'Something else', roster: [], opened: moment(new Date(2015, 09 - 1, 26, 20, 45)) },
        { id: 3, 'name': 'closed', roster: [], opened: moment(new Date(2015, 09 - 1, 27)), closed: moment(new Date(2015, 09 - 1, 28)) }
      ],
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
          deferred.resolve();
        })
        .error(function (response) {
          deferred.resolve();
        });
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
      debugger;
    })
  }]);
