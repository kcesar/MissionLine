var forms = {};

forms.handleServiceError = function handleServiceError(err, model) {
  if (err.status == 0) return;
  if (err.status == 401) {
    // If you want to get the user to re-authenticate and retry the request, use utils.getJSONRetriable
    $.toaster({ title: 'Error', priority: 'danger', message: 'Permission denied' })
  } else {
    if (console.log) { console.log(err); }
    if (err.responseJSON) {
      if (model && ko.isObservable(model.error)) {
        var val = "";
        for (var p in err.responseJSON) {
          if (model[p] && ko.isObservable(model[p]['error'])) {
            model[p].error(err.responseJSON[p]);
          } else {
            val += err.responseJSON[p] + "\n";
          }
        }
        model.error(val);
        return;
      }
    }
    $.toaster({ title: 'Error', priority: 'danger', message: 'An error occured talking to server' })
  }
};

forms.extendForErrors = function extendForErrors(model) {
  if (model == null) return model;
  for (var p in model) {
    if (ko.isObservable(model[p]) && model[p]['error'] === undefined) { model[p].error = ko.observable(); }
  }
  if (model.error === undefined) model.error = ko.observable();
  return model;
};

forms.applyErrors = function applyErrors(model, errs) {
  for (var i = 0; i < errs.length; i++) {

    if (errs[i].property === undefined || errs[i].property === null || errs[i].property === '') {
      model.error(errs[i].text);
    }
    else {
      model[errs[i].property].error(errs[i].text);
    }
  }
};