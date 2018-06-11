angular.module('missionlineApp').service('carpoolingModelService', ['$sce', '$http', '$q', '$rootScope', '$timeout', 'pushService', 'carpoolingService',
  function ($sce, $http, $q, $rootScope, $timeout, pushService, carpoolingService) {
    var self = this;
    var currentLocationLoadedDeferral = $q.defer();
    var personModalId = 'personModal';
    var updateInfoModalId = 'updateInfoModal';
    var changeLocationId = 'changeLocation';
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
      personModalId: personModalId,
      updateInfoModalId: updateInfoModalId,
      changeLocationId: changeLocationId,
      modalIds: [personModalId, updateInfoModalId, changeLocationId],
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
        window.location.hash = changeLocationId;
      },
      cancelChangeLocation: function () {
        self.returnHome();
      },
      saveChangedLocation: function () {
        self.model.savingLocation = true;
        carpoolingService.updateCarpoolerInfo(self.model.eventId, self.model.memberId, {
          LocationLatitude: changeLocationMap.getCenter().lat(),
          LocationLongitude: changeLocationMap.getCenter().lng()
        }).then(function () {
          self.model.savingLocation = false;
          self.reload();
          self.returnHome();
        }, function () {
          self.model.savingLocation = false;
        });
      },
      addSelf: function () {
        window.location.hash = "updateInfoModal";
      },
      editSelf: function () {
        window.location.hash = "updateInfoModal";
      },
      getModalIdBasedOnHash: function () {
        if (window.location.hash.length <= 1) {
          return null;
        }

        var foundModalId = null;
        $.each(self.modalIds, function (idx, modalId) {
          if (modalId === window.location.hash.substr(1)) {
            foundModalId = modalId;
          }
        });

        if (foundModalId != null) {
          return foundModalId;
        }

        // Otherwise, must be viewing person info about the carpooler
        return self.personModalId;
      }
    });

    function updateBasedOnHashChange() {
      self.model.isChangingLocation = self.getModalIdBasedOnHash() === changeLocationId;
    }

    window.addEventListener('hashchange', function () {
      $rootScope.$apply(updateBasedOnHashChange);
    }, false);
  }]);

angular.module('missionlineApp').service('carpoolingPersonModelService',
  ['$sce', '$http', '$q', '$rootScope', '$timeout', 'pushService', 'carpoolingService', 'carpoolingModelService',
  function ($sce, $http, $q, $rootScope, $timeout, pushService, carpoolingService, carpoolingModelService) {
    var self = this;
    var eventId = carpoolingModelService.model.eventId;
    $.extend(this, {
      model: {
        carpooler: null,
        loading: true,
        carpoolerTypeText: ''
      }
    });

    function loadCarpooler(memberId) {
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

    window.addEventListener('hashchange', function () {
      if (carpoolingModelService.getModalIdBasedOnHash() === carpoolingModelService.personModalId) {
        var memberId = window.location.hash.substr(1);
        loadCarpooler(memberId);
      }
    }, false);
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
              self.model.saving = false;
              carpoolingModelService.reload();
              carpoolingModelService.returnHome();
            }, function () {
              self.model.saving = false;
            });
          };

          if (self.model.alreadyHasLocation) {
            finishSave();
          } else {
            carpoolingModelService.getCurrentLocation().then(function (position) {
              if (position == null) {
                alert('Geolocation not supported');
              } else {
                updatedInfo.LocationLatitude = position.latitude;
                updatedInfo.LocationLongitude = position.longitude;
                finishSave();
              }
            });
          }
        }
      }
    });

    window.addEventListener('hashchange', function () {
      if (carpoolingModelService.getModalIdBasedOnHash() === carpoolingModelService.updateInfoModalId) {
        self.load();
      }
    }, false);
  }]);