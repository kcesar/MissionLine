angular.module('missionlineApp').filter('eventTime', function () {
  return function (input, fromDate) {
    if (!input || !input.isValid()) { return '' };
    var me = input.clone().startOf('day'), reference = fromDate.clone().startOf('day');
    var days = Math.floor(me.diff(reference, 'days', true));
    return (days ? (days + '+') : '') + input.format("HHmm");
  }
});
