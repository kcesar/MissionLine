angular.module('missionlineApp').service('carpoolingModelService', ['$q', '$rootScope', 'pushService', 'carpoolingService',
  function ($q, $rootScope, pushService, carpoolingService) {
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
      driverIconPath: 'm-4.66667,10.08767c0.7,0.099 -4.34794,0.099 -3.64894,0.19899c-0.1,0.49999 -0.1,0.99998 -0.5,1.09999c-0.49899,0.19999 -1.09899,-0.10001 -1.69899,0c0,-0.39999 5.74794,-0.79999 5.84793,-1.29898zm15.24005,-0.20001c0,0.399 0.1,0.89899 0.1,1.29899c-0.59901,-0.1 -1.29901,0.2 -1.799,-0.1c-0.39901,-0.2 -0.39901,-0.59998 -0.499,-1.09898c0.799,0.1 1.49899,0 2.198,-0.10001zm4.59799,-2.99893c-0.09999,1.39996 -0.09999,2.79894 -0.2,4.29792c0,0.49999 -0.1,0.99899 -0.39999,1.09897c-0.30001,0.1 -1.09901,0 -1.39901,0c-0.4,0 -1.09899,0.1 -1.399,0c-0.3,-0.09998 -0.3,-0.69898 -0.4,-1.09897c0,-0.49999 -0.1,-0.89999 -0.1,-1.39898c1.799,-0.4 3.198,-1.29998 3.898,-2.89894zm-30.18297,0c0.799,1.49997 2.099,2.49894 3.998,2.89894c0,0.49899 -0.09999,0.89899 -0.09999,1.39898c0,0.39999 0,0.99899 -0.40001,1.09897c-0.3,0.1 -1.09899,0 -1.499,0c-0.4,0 -1.09999,0.1 -1.399,0c-0.5,-0.19998 -0.4,-1.79895 -0.5,-2.49795c0,-0.49999 0,-0.99998 -0.1,-1.39897l0,-1.49997zm25.18598,-7.49486c-1.10001,0 -1.799,0.09999 -2.59901,0.399c-0.59899,0.19999 -1.49899,0.59998 -1.59899,1.39997c0,0.49898 0.30001,1.19898 0.7,1.49896c0.39899,0.3 1.09901,0.499 1.79899,0.499c1.69901,0 3.398,-0.499 4.397,-1.39898c0.3,-0.19999 0.8,-0.69899 0.7,-1.29897c0,-0.3 -0.3,-0.49999 -0.5,-0.59999c-0.6,-0.399 -1.599,-0.49899 -2.49899,-0.49899l-0.399,0zm-20.48898,0c-0.699,0 -1.29899,0 -1.899,0.19899c-0.499,0.1 -1.099,0.4 -1.199,0.79999c-0.1,0.49999 0.4,0.99899 0.7,1.29897c0.99901,0.89999 2.798,1.39898 4.497,1.39898c0.39999,0 1,-0.099 1.29899,-0.19899c0.60001,-0.20001 1.3,-1.09999 1.2,-1.89897c-0.09999,-0.3 -0.4,-0.59999 -0.7,-0.79999c-0.899,-0.59999 -2.099,-0.79898 -3.39799,-0.79898l-0.5,0zm10.894,-9.89483c-0.89901,0 -1.79902,0 -2.69801,0.10001c-2.499,0.09998 -4.69799,0.09998 -6.99599,0.39998c-1.1,1.69898 -1.799,3.79794 -2.49899,5.8969c7.69598,0.099 15.59097,0.099 23.38696,0c-0.5,-1.69897 -0.99999,-3.09894 -1.699,-4.69791c-0.2,-0.20001 -0.59999,-1.19899 -0.89999,-1.29899c-0.10001,-0.09999 -0.4,0 -0.69901,-0.09999c-2.39899,-0.10001 -5.19699,-0.3 -7.89597,-0.3zm0.19998,-1.49898c1.599,0 3.198,0.20001 4.697,0.3c1.699,0.19999 3.498,0.4 4.498,1.39897c0.3,0.3 0.599,0.79999 0.799,1.29898c0.8,1.29998 1.4,2.79896 1.99899,4.39793c0.1,-1.39898 3.099,-1.49898 3.298,-0.1c0.10001,0.49999 -0.399,0.79898 -0.799,0.99899c-0.4,0.09999 -1,0.19999 -1.399,0.09999c-0.3,-0.09999 -0.7,-0.39999 -0.8,-0.2c-0.1,0.10001 0.2,0.3 0.3,0.39999c0.4,0.4 0.7,0.7 1.099,1.09999c0.4,0.49898 1,1.09898 1.2,1.69897c0.2,0.69898 0,1.89897 -0.09999,2.59795c-0.10001,0.79999 -0.10001,1.69897 -0.20001,2.49896c-0.2,2.49796 -1.499,3.99691 -3.598,4.59691c-0.69999,0.19999 -1.49899,0.19999 -2.399,0.29998c-1.49899,0.10001 -2.99799,0.20001 -4.59699,0.20001l-0.1,0l-8.89499,0l-0.1,0c-1.599,0 -3.29799,-0.1 -4.79699,-0.20001c-0.9,-0.09999 -1.699,-0.09999 -2.399,-0.29998c-2.09899,-0.6 -3.398,-1.99897 -3.598,-4.59691c0,-0.79999 -0.2,-1.69897 -0.2,-2.49896c-0.1,-0.79898 -0.2,-1.79897 0,-2.49796c0.2,-0.69998 0.7,-1.19998 1.099,-1.69997c0.5,-0.49898 1,-0.89898 1.4,-1.39897c-0.29999,-0.2 -0.5,0 -0.79999,0.1c-0.99901,0 -2.39901,-0.3 -2.29901,-1.29897c0.2,-1.19899 3.099,-1.19899 3.29901,0.09999c0.59899,-1.49897 1.09899,-3.09794 1.99799,-4.39792c0.2,-0.399 0.5,-0.89899 0.8,-1.19899c0.99899,-0.99897 2.49899,-1.19898 4.19799,-1.29897c0.899,-0.1 1.699,-0.19999 2.598,-0.19999c0.9,0 1.79899,-0.10001 2.699,-0.10001c0.39901,-0.1 0.69899,-0.1 1.09899,-0.1z',
      passengerIconPath: 'm-7.31061,2.36309c2.01498,1.51696 4.51798,2.41601 7.22898,2.41601c2.71002,0 5.21403,-0.89905 7.22902,-2.41601c4.71002,0.59295 8.354,4.60998 8.354,9.48096l0,4.15601l-31.16801,0l0,-4.15601c0,-4.87097 3.646,-8.88801 8.35601,-9.48096zm7.22898,-18.36305c4.81901,0 8.72602,3.90802 8.72602,8.72802c0,4.82001 -3.90702,8.72602 -8.72602,8.72602c-4.81998,0 -8.727,-3.90601 -8.727,-8.72602c0,-4.82001 3.90701,-8.72802 8.727,-8.72802z',
      driverOrPassengerIconPath: 'M-19.43,8.65c.57.08,1.14.08,1.71.16-.08.41-.08.81-.41.89s-.89-.08-1.38,0A5,5,0,0,1-19.43,8.65Zm17.05-.16c0,.32.08.73.08,1.06-.49-.08-1.06.16-1.46-.08s-.32-.49-.41-.89A6.44,6.44,0,0,0-2.38,8.49ZM1.36,6.05c-.08,1.14-.08,2.27-.16,3.49,0,.41-.08.81-.32.89a5.32,5.32,0,0,1-1.14,0,5.3,5.3,0,0,1-1.14,0c-.24-.08-.24-.57-.33-.89s-.08-.73-.08-1.14A4.19,4.19,0,0,0,1.36,6.05Zm-24.52,0a4.53,4.53,0,0,0,3.25,2.36c0,.41-.08.73-.08,1.14s0,.81-.33.89a6.09,6.09,0,0,1-1.22,0,5.3,5.3,0,0,1-1.14,0c-.41-.16-.33-1.46-.41-2a5,5,0,0,0-.08-1.14ZM-2.7,0A5.51,5.51,0,0,0-4.81.29C-5.3.45-6,.78-6.11,1.43a1.73,1.73,0,0,0,.57,1.22A2.59,2.59,0,0,0-4.08,3,5.53,5.53,0,0,0-.51,1.91,1.16,1.16,0,0,0,.06.86C.06.61-.18.45-.35.37a4,4,0,0,0-2-.41ZM-19.35,0a4.77,4.77,0,0,0-1.54.16c-.41.08-.89.33-1,.65s.32.81.57,1.06A5.77,5.77,0,0,0-17.64,3a4.11,4.11,0,0,0,1.06-.16,1.64,1.64,0,0,0,1-1.54,1.45,1.45,0,0,0-.57-.65A5,5,0,0,0-18.94,0Zm8.85-8A19.77,19.77,0,0,0-12.69-8c-2,.08-3.82.08-5.68.32a21.25,21.25,0,0,0-2,4.79c6.25.08,12.67.08,19,0A30.6,30.6,0,0,0-2.78-6.69c-.16-.16-.49-1-.73-1.06-.08-.08-.33,0-.57-.08C-6-7.91-8.3-8.07-10.5-8.07Zm.16-1.22c1.3,0,2.6.16,3.82.24,1.38.16,2.84.33,3.65,1.14a3.34,3.34,0,0,1,.65,1.06A20.64,20.64,0,0,1-.59-3.28c.08-1.14,2.52-1.22,2.68-.08.08.41-.32.65-.65.81A2.7,2.7,0,0,1,.3-2.47c-.24-.08-.57-.32-.65-.16s.16.24.24.32l.89.89A5.32,5.32,0,0,1,1.77,0a6.15,6.15,0,0,1-.08,2.11c-.08.65-.08,1.38-.16,2A3.86,3.86,0,0,1-1.4,7.84a12.74,12.74,0,0,1-1.95.24c-1.22.08-2.44.16-3.74.16h-7.39c-1.3,0-2.68-.08-3.9-.16a12.74,12.74,0,0,1-1.95-.24,3.79,3.79,0,0,1-2.92-3.73c0-.65-.16-1.38-.16-2a5.67,5.67,0,0,1,0-2,4.07,4.07,0,0,1,.89-1.38,14.86,14.86,0,0,0,1.14-1.14c-.24-.16-.41,0-.65.08-.81,0-1.95-.24-1.87-1.06.16-1,2.52-1,2.68.08a17.83,17.83,0,0,1,1.62-3.57,4.09,4.09,0,0,1,.65-1A5,5,0,0,1-15.53-9a18.46,18.46,0,0,1,2.11-.16c.73,0,1.46-.08,2.19-.08A3.32,3.32,0,0,1-10.33-9.29ZM9.81,2.05a7.4,7.4,0,0,0,4.47,1.49,7.4,7.4,0,0,0,4.47-1.49A5.9,5.9,0,0,1,23.9,7.91v2.57H4.65V7.91A5.91,5.91,0,0,1,9.81,2.05ZM14.27-9.29A5.39,5.39,0,0,1,19.66-3.9a5.39,5.39,0,0,1-5.39,5.39A5.39,5.39,0,0,1,8.88-3.9,5.39,5.39,0,0,1,14.27-9.29Z',
      selfIconPath: 'm-37.32048,-8.43691l28.5475,0l8.82082,-27.12011l8.82186,27.12011l28.54635,0l-23.09425,16.75981l8.82137,27.12004l-23.09533,-16.76142l-23.09428,16.76142l8.82137,-27.12004l-23.09542,-16.75981l0,0z',
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
      expectReload: function () {
        self.model.loading = true;
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
          self.expectReload();
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

    pushService.listenTo('carpoolersChanged', function (eventId) {
      if (eventId === self.model.eventId) {
        self.reload();
      }
    });
  }]);

angular.module('missionlineApp').service('carpoolingPersonModelService',
  ['$rootScope', 'carpoolingService', 'carpoolingModelService',
  function ($rootScope, carpoolingService, carpoolingModelService) {
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
  ['$rootScope', 'carpoolingService', 'carpoolingModelService',
  function ($rootScope, carpoolingService, carpoolingModelService) {
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
            carpoolingModelService.expectReload();
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
              carpoolingModelService.expectReload();
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