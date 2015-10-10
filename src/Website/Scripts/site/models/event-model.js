var EventModel = function (data) {
  $.extend(this, {
    opened: new Date(),
    roster: []
  }, data);
  this.opened = moment(this.opened);

  var self = this;
  $.extend(this, {
    datePart: this.opened.format("YYYY-MM-DD"),
    timePart: this.opened.format("HH:mm"),
    jsOpened: this.opened.toDate(),
    getData: function () {
      return {
        id: self.id,
        name: self.name,
        opened: moment(self.datePart + 'T' + ("00:00".substring(0, 5 - self.timePart.length) + self.timePart)),
        closed: self.closed
      }
    }
  })
};