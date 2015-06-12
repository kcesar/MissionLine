var PageModel = function () {
  var self = this;
  var callsClient = $.connection.callsHub.client;

  function getUpdateRowHandler(model, searchProperty, rowGetter, fixup) {
    return function (row) {
      console.log(row);
      var list = model();
      var test = rowGetter(row);
      model.remove(function (item) { return item[searchProperty] == test; });
      var newModel = fixup(row);
      if (newModel) {
        model.push(newModel);
        if (model.lastSort) model.sort(model.lastSort);
      }
    }
  }

  var fixupRow = function (row) {
    row.timeIn = moment(row.timeIn);
    if (row.timeOut) { row.timeOut = moment(row.timeOut); }
    row.timeOutText = row.timeOut ? row.timeOut.fromNow() : '';
    row.timeOutSort = row.timeOut ? row.timeOut : moment(row.timeIn).add(100, 'years');
    return row;
  }

  var fixupCall = function (row) {
    row.time = moment(row.time);
    row.timeText = row.time.format('HHmm ddd D');
    row.name = row.name || '';
    return row;
  }


  this.roster = ko.observableArray([]);
  this.showRoster = ko.observable(true);

  this.calls = ko.observableArray([]);
  this.showCalls = ko.observable(false);


  function createRosterComputed(eventId) {
    return ko.computed(function () {
      return ko.utils.arrayFilter(self.roster(), function (r) {
        return r.eventId == ko.unwrap(eventId);
      })
    });
  }

  var fixupEvent = function fixupEvent(row) {
    var opened = moment(row.opened);
    row.name = ko.observable(row.name);
    row.opened = ko.computed(function () {
      return moment(new Date(row.opened.date() + ' ' + row.opened.time()));
    }, row, { deferEvaluation: true });
    row.opened.date = ko.observable(opened.format('YYYY-MM-DD'));
    row.opened.time = ko.observable(opened.format('HH:mm'));
    row.opened.isValid = ko.computed(function () {
      return row.opened().isValid();
    })
    row.roster = row.roster || createRosterComputed(row.id);
    row.isUnassignedEvent = row.isUnassignedEvent || false;
    return row;
  }


  this.events = ko.observableArray([]);
  this.events.lastSort = function (l, r) { return l.opened() == r.opened() ? 0 : l.opened() < r.opened() ? 1 : -1 };
  this.currentEvent = ko.observable(null);
  this.editingEvent = forms.extendForErrors(fixupEvent({
  }));



  this.unassignedEvent = {
    id: null,
    name: 'Unassigned',
    roster: createRosterComputed(null),
    isUnassignedEvent: true,
    closed: true
  }
  this.getRosterClass = function (evt) {
    return evt.isUnassignedEvent
            ? 'panel-warning'
            : evt.closed
              ? 'panel-default'
              : 'panel-primary';
  }
  this.createEvent = function () {
    var dialog = BootstrapDialog.show({
      title: 'Create New Event',
      message: $('<div data-bind="template: { name: \'edit-dialog-template\', data: $root }"></div>'),
      buttons: [{
        label: 'Save',
        action: function (dialogRef) {
          $.ajax({ url: window.appRoot + 'api/events', method: 'POST', contentType: 'application/json', data: ko.toJSON(self.editingEvent) })
          .done(function (r) {
            if (r['errors'] && r['errors'].length > 0) {
              forms.applyErrors(self.editingEvent, r.errors);
              //self.editingEvent.working(false);
              return;
            }
            dialogRef.close();
          })
          .fail(function (err) { forms.handleServiceError(err, self.editingEvent); })
        }
      }, {
        label: 'Cancel',
        action: function (dialogRef) {
          dialogRef.close();
        }
      }]
    });
    ko.applyBindings(self, dialog.getModalBody()[0]);
  }

  this.startMerge = function (evtModel) {
    new MergeModel(evtModel, self.events).start();
  }

  this.load = function () {
    $.ajax(window.appRoot + 'api/events')
    .done(function (data) {
      for (var i = 0; i < data.length; i++) {
        fixupEvent(data[i]);
        //data[i].roster = createRosterComputed(data[i].id);
      }
      self.events(data);

      $.ajax(window.appRoot + 'api/roster')
      .done(function (d) {
        for (var i = 0; i < d.length; i++) {
          fixupRow(d[i]);
        }
        self.roster(d);
      });
      $.ajax(window.appRoot + 'api/calls')
      .done(function (d) {
        for (var i = 0; i < d.length; i++) {
          fixupCall(d[i]);
        }
        self.calls(d);
      });

    });
  };
  callsClient.updatedRoster = getUpdateRowHandler(self.roster,'memberId', function (row) { return row.memberId; }, fixupRow);
  callsClient.updatedCall = getUpdateRowHandler(self.calls, 'id', function (row) { return row.id; }, fixupCall);
  callsClient.updatedEvent = getUpdateRowHandler(self.events, 'id', function (row) { return row.id; }, fixupEvent);
  callsClient.removedEvent = getUpdateRowHandler(self.events, 'id', function (row) { return row; }, function () { return null; });
}

var MergeModel = function (fromEvent, events) {
  var self = this;

  this.events = events;
  this.availableEvents = ko.utils.arrayFilter(self.events(), function (r) {
    return r.id != ko.unwrap(fromEvent.id);
  })

  this.fromEvent = fromEvent;
  this.targetEvent = ko.observable();

  this.apply = function () {
    alert('apply');
  };

  this.start = function () {
    var dialog = BootstrapDialog.show({
      title: 'Merge Event Into Another',
      message: $('<div data-bind="template: { name: \'merge-dialog-template\', data: $root }"></div>'),
      buttons: [{
        label: 'Merge',
        action: function (dialogRef) {
          $.ajax({ url: window.appRoot + 'api/events/' + self.fromEvent.id + '/merge/' + self.targetEvent().id, method: 'POST', contentType: 'application/json'})
          .done(function (r) {
            if (r['errors']&& r['errors'].length > 0) {
              forms.applyErrors(self.editingEvent, r.errors);
              //self.editingEvent.working(false);
              return ;
            }
            dialogRef.close();
          })
          .fail(function (err) { forms.handleServiceError(err, self.editingEvent); })
        }
      }, {
        label: 'Cancel',
        action: function (dialogRef) {
          dialogRef.close();
        }
      }]
    });
    ko.applyBindings(self, dialog.getModalBody()[0]);
  }
}

var model = new PageModel();
window.hasHubs = true;
$(document).ready(function () {
  model.load();
  ko.applyBindings(model);
  $('.selectpicker').selectpicker();
})