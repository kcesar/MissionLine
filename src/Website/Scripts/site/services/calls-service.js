angular.module('missionlineApp').service('callsService', ['$sce', '$http', '$q', '$rootScope', 'pushService',
  function ($sce, $http, $q, $rootScope, pushService) {
    //{"id":2076,"number":"+12067550085","name":"Matt Cosand","time":"2015-10-02T01:21:49.653","recording":null,"eventId":null}
    var CallModel = function (data) {
      $.extend(this, {
        name: 'Unknown',
        number: null,
        recording: null,
        time: new Date()
      }, data);

      if (this.recording) { this.recording = $sce.trustAsResourceUrl(this.recording); }
      if (this.time) { this.time = moment(this.time); }
    };

    var self = this;
    $.extend(this, {
      calls: [],
      load: function () {
        self.calls.length = 0;
        self.calls.loading = true;
        return $http({
          method: 'GET',
          url: window.appRoot + 'api/calls',
        }).success(function (data) {
          $.each(data, function (idx, aCall) {
            self.calls.push(new CallModel(aCall));
          });
          delete self.calls.loading;
        })
      },
      newCall: function (data) {
        return new CallModel(data);
      }
    });
    pushService.listenTo('updatedCall', function (data) {
      var call = self.newCall(data);
      var found = false;
      for (var i = 0; i < self.calls.length; i++) {
        if (self.calls[i].id == data.id) {
          self.calls[i] = call;
          found = true;
          break;
        }
      }
      if (!found) { self.calls.push(call); }
      $rootScope.$digest();
    });
  }]);
