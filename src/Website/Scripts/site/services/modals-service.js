angular.module('missionlineApp').controller('ModalController', [
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
            var invalid = false;
            $element.find('form[name]').addBack('form[name]').each(function (idx, item) {
              var ngForm = $scope[item.getAttribute('name')];
              ngForm.$setSubmitted();
              invalid = invalid || ngForm.$invalid;
            })
            if (invalid) {
              $scope.$digest();
              return;
            }
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
      $scope.isInvalid = function (form, name) {
        return form[name].$invalid && (form[name].$dirty || form.$submitted);
      }
    }]);

angular.module('missionlineApp').service('EditModalService', ['ModalService', function (ModalService) {
  this.edit = function (title, model, saveAction) {
    ModalService.showModal({
      templateUrl: "editDialogTemplate.html",
      controller: "ModalController",
      inputs: {
        title: title,
        model: model,
        save: function (model) {
          console.log(model);
          return saveAction(model);
        }
      }
    }).then(function (modal) {
      modal.controller.open();
    });
  }
}]);
