var PageModel = function () {
  var self = this;
  var callsClient = $.connection.callsHub.client;

  callsClient.updatedRoster = function (row) {
    console.log(row);
    var list = self.roster();
    self.roster.remove(function (item) { return item.memberId == row.memberId; });
    self.roster.push(fixupRow(row));
    if (self.roster.lastSort) self.roster.sort(self.roster.lastSort);
  }

  callsClient.updatedCall = function (row) {
    console.log(row);
    var list = self.calls();
    self.calls.remove(function (item) { return item.id == row.id; });
    var r = fixupCall(row);
    self.calls.push(r);
    if (self.calls.lastSort) self.calls.sort(self.calls.lastSort);
  }

  this.roster = ko.observableArray([]);
  this.showRoster = ko.observable(true);
  this.toggleRoster = function () {
    self.showRoster(!self.showRoster());
  };

  this.calls = ko.observableArray([]);
  this.showCalls = ko.observable(false);
  this.toggleCalls = function () {
    self.showCalls(!self.showCalls());
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

  this.load = function () {
    $.ajax(window.appRoot + 'api/roster')
    .done(function (data) {
      for (var i = 0; i < data.length; i++) {
        fixupRow(data[i]);
      }
      self.roster(data);
    });
    $.ajax(window.appRoot + 'api/calls')
    .done(function (data) {
      for (var i = 0; i < data.length; i++) {
        fixupCall(data[i]);
      }
      self.calls(data);
    });
  };
}

var model = new PageModel();
window.hasHubs = true;
$(document).ready(function () {
  model.load();
  ko.applyBindings(model);
})