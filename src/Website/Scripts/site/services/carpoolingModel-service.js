angular.module('missionlineApp').service('carpoolingModelService', ['$sce', '$http', '$q', '$rootScope', '$timeout', 'pushService', 'carpoolingService',
  function ($sce, $http, $q, $rootScope, $timeout, pushService, carpoolingService) {
    var self = this;
    var currentLocationLoadedDeferral = $q.defer();
    $.extend(this, {
      model: {
        eventId: 0,
        memberId: "",
        carpoolers: null,
        ownCarpoolerEntry: null,
        isChangingLocation: false,
        currentLocationCoords: null,
        loading: true
      },
      load: function (eventId, memberId) {
        self.model.eventId = eventId;
        self.model.memberId = memberId;

        self.reload();
      },
      reload: function () {
        self.model.loading = true;
        self.model.carpoolers = null;
        self.model.ownCarpoolerEntry = null;

        carpoolingService.getCarpoolers(self.model.eventId).then(function (carpoolers) {
          self.model.carpoolers = carpoolers;
          $.each(carpoolers, function (idx, carpooler) {
            if (carpooler.member.id === self.model.memberId) {
              self.model.ownCarpoolerEntry = new CarpoolerModel(carpooler);
            }
          });
          self.model.loading = false;
        });
      },
      getCurrentLocation: function () {
        if (navigator.geolocation) {
          navigator.geolocation.getCurrentPosition(function (position) {
            self.model.currentLocationCoords = position.coords;
            currentLocationLoadedDeferral.resolve(position.coords);
          }, function () {
            currentLocationLoadedDeferral.resolve(null);
          });
        } else {
          currentLocationLoadedDeferral.resolve(null);
        }
        return currentLocationLoadedDeferral.promise;
      },
      returnHome: function () {
        if (window.location.hash.length > 1) {
          window.history.back();
        }
      },
      changeLocation: function () {
        self.model.isChangingLocation = true;
      },
      cancelChangeLocation: function () {
        self.model.isChangingLocation = false;
      },
      saveChangedLocation: function () {
        self.model.savingLocation = true;
        carpoolingService.updateCarpoolerInfo(self.model.eventId, self.model.memberId, {
          LocationLatitude: changeLocationMap.getCenter().lat(),
          LocationLongitude: changeLocationMap.getCenter().lng()
        }).then(function () {
          self.model.savingLocation = false;
          self.model.isChangingLocation = false;
          self.reload();
        }, function () {
          self.model.savingLocation = false;
        });
      }
    });
  }]);



angular.module('missionlineApp').service('carpoolingPersonModelService', ['$sce', '$http', '$q', '$rootScope', '$timeout', 'pushService', 'carpoolingService',
  function ($sce, $http, $q, $rootScope, $timeout, pushService, carpoolingService) {
    var self = this;
    $.extend(this, {
      model: {
        carpooler: null,
        loading: true,
        carpoolerTypeText: ''
      },
      loadCarpooler: function (eventId, memberId) {
        self.model.loading = true;
        self.model.carpooler = null;
        self.model.carpoolerTypeText = '';
        carpoolingService.getCarpooler(eventId, memberId).then(function (carpooler) {
          self.model.carpooler = carpooler;
          var typeText;
          if (carpooler.canBeDriver && carpooler.canBePassenger) {
            typeText = 'Can drive or be passenger!';
          }
          else if (carpooler.canBeDriver) {
            typeText = 'Can drive!';
          }
          else if (carpooler.canBePassenger) {
            typeText = 'Looking for a ride!';
          }
          else {
            typeText = 'Not carpooling';
          }
          self.model.carpoolerTypeText = typeText;
          self.model.loading = false;
        });
      }
    });
  }]);



angular.module('missionlineApp').service('carpoolingUpdateInfoModelService',
  ['$sce', '$http', '$q', '$rootScope', '$timeout', 'pushService', 'carpoolingService', 'carpoolingModelService',
  function ($sce, $http, $q, $rootScope, $timeout, pushService, carpoolingService, carpoolingModelService) {
    var self = this;
    var eventId = carpoolingModelService.model.eventId;
    var memberId = carpoolingModelService.model.memberId;
    $.extend(this, {
      model: {
        canBeDriver: false,
        canBePassenger: false,
        vehicleDescription: '',
        message: '',
        alreadyHasLocation: false,
        loading: true,
        saving: false
      },
      load: function () {
        self.model.loading = true;
        carpoolingService.getUpdateInfo(eventId, memberId).then(function (carpooler) {
          self.model.canBeDriver = carpooler.canBeDriver;
          self.model.canBePassenger = carpooler.canBePassenger;
          self.model.vehicleDescription = carpooler.vehicleDescription;
          self.model.message = carpooler.message;
          self.model.alreadyHasLocation = carpooler.locationLatitude && carpooler.locationLongitude;
          self.model.loading = false;
        });
      },
      save: function () {
        self.model.saving = true;
        if (!self.model.canBeDriver && !self.model.canBePassenger) {
          carpoolingService.removeCarpooler(eventId, memberId).then(function () {
            carpoolingModelService.reload();
            carpoolingModelService.returnHome();
          }, function () {
            self.model.saving = false;
          });
        }
        else {
          var updatedInfo = {
            CanBeDriver: self.model.canBeDriver,
            CanBePassenger: self.model.canBePassenger,
            VehicleDescription: self.model.vehicleDescription,
            Message: self.model.message
          };

          var finishSave = function () {
            carpoolingService.updateCarpoolerInfo(eventId, memberId, updatedInfo).then(function () {
              carpoolingModelService.reload();
              carpoolingModelService.returnHome();
            }, function () {
              self.model.saving = false;
            });
          };

          if (self.model.alreadyHasLocation) {
            finishSave();
          } else {
            if (navigator.geolocation) {
              navigator.geolocation.getCurrentPosition(function (position) {
                updatedInfo.LocationLatitude = postion.coords.latitude;
                updatedInfo.LocationLongitude = position.coords.longitude;
                finishSave();
              });
            } else {
              alert('Geolocation not supported');
            }
          }
        }
      }
    });
  }]);