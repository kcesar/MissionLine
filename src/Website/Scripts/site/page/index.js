var PageModel = function () {
  var self = this;
  var callsClient = $.connection.callsHub.client;

  $.connection.hub.connectionSlow(function () {
    $.toaster({ title: 'Connection Slow', priority: 'info', message: 'Slow connection detected. May not get all updates.' });
  })

  $.connection.hub.reconnecting(function () {
    $.toaster({ title: 'Reconnecting ...', priority: 'warning', message: 'Reconnecting to update stream.' });
  })

  $.connection.hub.reconnecting(function () {
    $.toaster({ title: 'Reconnected', priority: 'success', message: 'Reconnected to update stream.' });
  })

  $.connection.hub.disconnected(function () {
    $.toaster({ title: 'Disconnected', priority: 'danger', message: 'Disconnected from update stream. Refresh the page.' });
  })


  function getUpdateRowHandler(model, removeFilter, fixup) {
    return function (row) {
      console.log(row);
      var list = model();
      model.remove(removeFilter.bind(row));
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

    row.moveToEvent = function (sarEvent) {
      $.ajax({ url: window.appRoot + 'api/roster/' + row.id + '/reassign/' + sarEvent.id, method: 'POST' })
        .done(function () {
        })
        .fail(function (err) { forms.handleServiceError(err, null); })
    }
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
    row.closed = ko.observable(row.closed ? moment(row.closed) : null);
    row.roster = row.roster || createRosterComputed(row.id);
    row.isUnassignedEvent = row.isUnassignedEvent || false;
    row.otherEvents = row.otherEvents || ko.observable([]);

    return row;
  }


  this.events = ko.observableArray([]);
  this.events.lastSort = function (l, r) {
    var result = 1;
    var lc = l.closed() != null;
    var rc = r.closed() != null;
    if (lc == rc) {
      result = l.opened().isSame(r.opened()) ? 0 : l.opened().isBefore(r.opened()) ? 1 : -1;
    } else if (lc) {
      result = 1;
    } else {
      result = -1;
    }
    return result;
  };
  this.currentEvent = ko.observable(null);
  this.editingEvent = forms.extendForErrors(fixupEvent({
  }));

  this.unassignedEvent = {
    id: null,
    name: 'Unassigned',
    roster: createRosterComputed(null),
    isUnassignedEvent: true,
    opened: new Date(),
    closed: new Date()
  }
  this.unassignedEvent.otherEvents = ko.computed(function () {
    console.log({ row: this.id, ev: this.eventId, evs: self.events() });
    var eventId = this.eventId;
    return ko.utils.arrayFilter(self.events(), function (item) { return item.id != eventId });
  }, this.unassignedEvent, { deferEvaluation: true })

  this.getRosterClass = function (evt) {
    return evt.isUnassignedEvent
            ? 'panel-warning'
            : (evt.closed() != null)
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
          var btn = this;
          btn.disable().spin();
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
          .always(function () {
            btn.enable().stopSpin();
          })
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
    var model = new MergeModel(evtModel, self.events);
    ko.applyBindings(model, model.buildDialog());
  }

  this.startClose = function (evtModel) {
    var stillSignedIn = ko.utils.arrayFilter(self.roster(), function (r) {
      return r.eventId == evtModel.id && r.timeOut == null;
    });
    if (stillSignedIn == 0) {
      $.ajax({ url: window.appRoot + 'api/events/' + evtModel.id + '/close', method: 'POST' })
      .done(function () {
        // don't do anything for now
      })
      .fail(function (err) { forms.handleServiceError(err, null); })
    }
    else {
      $.toaster({ title: 'Error', priority: 'danger', message: 'Can\'t close event until everyone is signed out.' });
    }
  }

  this.reopen = function (evtModel) {
    $.ajax({ url: window.appRoot + 'api/events/' + evtModel.id + '/reopen', method: 'POST' })
    .done(function () {
      // don't do anything for now
    })
    .fail(function (err) { forms.handleServiceError(err, null); })
  }

  this.signout = function (sarEvent, roster) {
    sarEvent = ko.unwrap(sarEvent);
    roster = ko.unwrap(roster);
    var model = new SignoutModel(roster, sarEvent);
    ko.applyBindings(model, model.buildDialog());
  }

  this.undoSignout = function (sarEvent, roster) {
    sarEvent = ko.unwrap(sarEvent);
    roster = ko.unwrap(roster);
    $.ajax({ url: window.appRoot + 'api/roster/' + roster.id + '/undoSignout', method: 'POST' })
    .fail(function (err) { forms.handleServiceError(err, null); })
  }

  this.formatTime = function(reference, time) {
    time = ko.unwrap(time);
    if (time == null) return '';
    var days = ~~moment.duration(time.diff(ko.unwrap(reference))).asDays();
    return (days == 0 ? '' : (days + '+')) + time.format("HHmm");
  }

  this.load = function () {
    $.ajax(window.appRoot + 'api/events')
    .done(function (data) {
      for (var i = 0; i < data.length; i++) {
        fixupEvent(data[i]);
        //data[i].roster = createRosterComputed(data[i].id);
      }
      self.events(data);
      self.events.sort(self.events.lastSort);

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

  callsClient.updatedRoster = getUpdateRowHandler(self.roster, function (item) { return item.id == this.id || (item.memberId == this.memberId && item.eventId == this.eventId) }, fixupRow);
  callsClient.updatedCall = getUpdateRowHandler(self.calls, function (item) { return item.id == this.id }, fixupCall);
  callsClient.updatedEvent = getUpdateRowHandler(self.events, function (item) { return item.id == this.id }, fixupEvent);
  callsClient.removedEvent = getUpdateRowHandler(self.events, function (item) { return item.id == this.id }, function () { return null });

}

var SignoutModel = function (roster, fromEvent) {
  var self = this;

  this.timeOut = ko.computed(function () {
    return moment(new Date(self.timeOut.date() + ' ' + self.timeOut.time()));
  }, this, { deferEvaluation: true });
  this.timeOut.date = ko.observable(moment().format('YYYY-MM-DD'));
  this.timeOut.time = ko.observable(moment().format('HH:mm'));
  this.timeOut.isValid = ko.computed(function () {
    return self.timeOut().isValid();
  })
  this.miles = ko.observable(null);

  this.rosterName = roster.name;
  this.eventName = fromEvent.name;

  this.id = roster.id;

  this.buildDialog = function () {
    var dialog = BootstrapDialog.show({
      title: 'Sign Out',
      message: $('<div data-bind="template: { name: \'signout-template\', data: $root }"></div>'),
      buttons: [{
        label: 'Sign Out',
        action: function (dialogRef) {
          var btn = this;
          btn.disable().spin();
          $.ajax({
            url: window.appRoot + 'api/roster/' + self.id + '/signout?when=' + self.timeOut().format() + '&miles=' + self.miles() || '',
            method: 'POST',
            contentType: 'application/json',
            data: ko.toJSON(self)
          })
          .done(function (r) {
            if (r['errors'] && r['errors'].length > 0) {
              forms.applyErrors(self, r.errors);
              return;
            }
            dialogRef.close();
          })
          .fail(function (err) {
            if (err.state() == "rejected") {
              $.toaster({ title: 'Error', priority: 'danger', message: 'Request was rejected. You may have been signed out. Refresh the page and try again.' })
            } else {
              forms.handleServiceError(err, self.editingEvent);
            }
          })
          .always(function () { btn.enable().stopSpin(); })
        }
      },
      {
        label: 'Cancel',
        action: function (dialogRef) {
          dialogRef.close();
        }
      }]
    })
    return dialog.getModalBody()[0];
  }
}

var MergeModel = function (fromEvent, events) {
  var self = this;

  this.events = events;
  this.availableEvents = ko.utils.arrayFilter(self.events(), function (r) {
    return r.id != ko.unwrap(fromEvent.id);
  })

  this.fromEvent = fromEvent;
  this.targetEvent = ko.observable();

  this.buildDialog = function () {
    var dialog = BootstrapDialog.show({
      title: 'Merge Event Into Another',
      message: $('<div data-bind="template: { name: \'merge-dialog-template\', data: $root }"></div>'),
      buttons: [{
        label: 'Merge',
        action: function (dialogRef) {
          var btn = this;
          btn.disable().spin();
          $.ajax({ url: window.appRoot + 'api/events/' + self.fromEvent.id + '/merge/' + self.targetEvent().id, method: 'POST', contentType: 'application/json' })
          .done(function (r) {
            if (r['errors'] && r['errors'].length > 0) {
              forms.applyErrors(self.editingEvent, r.errors);
              //self.editingEvent.working(false);
              return;
            }
            dialogRef.close();
          })
          .fail(function (err) { forms.handleServiceError(err, self.editingEvent); })
          .always(function () { btn.enable().stopSpin(); })
        }
      }, {
        label: 'Cancel',
        action: function (dialogRef) {
          dialogRef.close();
        }
      }]
    });
    return dialog.getModalBody()[0];
  }
}

var model = new PageModel();
window.hasHubs = true;
$(document).ready(function () {
  model.load();
  ko.applyBindings(model);
  $('.selectpicker').selectpicker();
})