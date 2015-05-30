var PageModel = function () {
  var self = this;
  var callsClient = $.connection.callsHub.client;

  callsClient.updatedRoster = function (row) {
    console.log(row);
    var list = self.rows();
    self.rows.remove(function (item) { return item.memberId == row.memberId; });
    self.rows.push(fixupRow(row));
    if (self.rows.lastSort) self.rows.sort(self.rows.lastSort);
  }

  this.rows = ko.observableArray([]);

  var fixupRow = function (row) {
    row.timeIn = moment(row.timeIn);
    if (row.timeOut) { row.timeOut = moment(row.timeOut); }
    row.timeOutText = row.timeOut ? row.timeOut.fromNow() : '';
    row.timeOutSort = row.timeOut ? row.timeOut : moment(row.timeIn).add(100, 'years');
    return row;
  }

  this.load = function () {
    $.ajax(window.appRoot + 'api/roster')
    .done(function (data) {
      for (var i = 0; i < data.length; i++) {
        fixupRow(data[i]);
      }
      self.rows(data);
    });
  };
}

var model = new PageModel();
window.hasHubs = true;
$(document).ready(function () {
  model.load();
  ko.applyBindings(model);
})