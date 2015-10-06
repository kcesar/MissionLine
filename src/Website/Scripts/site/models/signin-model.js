var SigninModel = function (data) {
  $.extend(this, {
    hours: null,
    isMember: false,
    memberId: null,
    miles: null,
    name: "Unknown",
    state: "SignedIn",
    timeIn: moment(),
    timeOut: null,
    eventId: null,
    highlight: false
  }, data);
  if (this.timeIn) { this.timeIn = moment(this.timeIn); }
  if (this.timeOut) { this.timeOut = moment(this.timeOut); }

  var self = this;
  this.inDatePart = self.timeIn.format("YYYY-MM-DD");
  this.inTimePart = self.timeIn.format("HH:mm");
  this.outDatePart = self.timeOut ? self.timeOut.format("YYYY-MM-DD") : null;
  this.outTimePart = self.timeOut ? self.timeOut.format("HH:mm") : null;
  this.getData = function () {
    var data = {
      id: self.id,
      name: self.name,
      eventId: self.eventId,
      miles: self.miles
    };
    if (self.inDatePart && self.inTimePart) {
      data.timeIn = moment(self.inDatePart + 'T' + ("00:00".substring(0, 5 - self.inTimePart.length) + self.inTimePart))
    }
    if (self.outDatePart && self.outTimePart) {
      data.timeOut = moment(self.outDatePart + 'T' + ("00:00".substring(0, 5 - self.outTimePart.length) + self.outTimePart))
    }
    return data;
  }
};