angular.module('missionlineApp').service('carpoolingService',
  ['$http', '$q', '$rootScope', 'pushService',
  function ($http, $q, $rootScope, pushService) {
    var self = this;
    $.extend(this, {
      getCarpoolers: function (eventId) {
        var deferred = $q.defer();
        $http({
          method: 'GET',
          url: window.appRoot + 'api/events/' + eventId + '/carpoolers'
        }).success(function (data) {
          var carpoolers = [];
          $.each(data, function (idx, carpooler) {
            carpoolers.push(new CarpoolerModel(carpooler));
          });
          deferred.resolve(carpoolers);
        })
        .error(function (response) { deferred.reject(response); });
        return deferred.promise;
      },
      updateCarpoolerInfo: function (eventId, currentMemberId, carpoolerInfo) {
        var deferred = $q.defer();
        $http({
          method: 'POST',
          url: window.appRoot + 'api/events/' + eventId + '/carpoolers/' + currentMemberId,
          data: carpoolerInfo,
          headers: { 'Content-Type': 'application/json' }
        }).success(function (data) {
          deferred.resolve(data);
        }).error(function (response) {
          deferred.reject(response);
        });
        return deferred.promise;
      },
      getUpdateInfo: function (eventId, memberId) {
        var deferred = $q.defer();
        $http({
          method: 'GET',
          url: window.appRoot + 'api/events/' + eventId + '/carpoolers/' + memberId + '/updateinfo'
        }).success(function (data) {
          deferred.resolve(data);
        }).error(function (response) {
          deferred.reject(response);
        });
        return deferred.promise;
      },
      removeCarpooler: function (eventId, memberId) {
        var deferred = $q.defer();
        $http({
          method: 'DELETE',
          url: window.appRoot + 'api/events/' + eventId + '/carpoolers/' + memberId,
          headers: { 'Content-Type': 'application/json' }
        }).success(function (data) {
          deferred.resolve(data);
        }).error(function (response) {
          deferred.reject(response);
        });
        return deferred.promise;
      },
      getCarpooler: function (eventId, memberId) {
        var deferred = $q.defer();
        $http({
          method: 'GET',
          url: window.appRoot + 'api/events/' + eventId + '/carpoolers/' + memberId
        }).success(function (data) {
          deferred.resolve(data);
        }).error(function (response) {
          deferred.reject(response);
        });
        return deferred.promise;
      },
      getPreviousLocation: function (memberId) {
        var deferred = $q.defer();
        $http({
          method: 'GET',
          url: window.appRoot + 'api/carpoolers/' + memberId + '/previouslocation'
        }).success(function (data) {
          deferred.resolve(data);
        }).error(function (response) {
          deferred.reject(response);
        });
        return deferred.promise;
      }
    });
  }]);
