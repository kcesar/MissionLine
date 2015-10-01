angular.module('missionlineApp').animation('.animate-card', function () {
  return {
    enter: function (element, done) {
      element.css({
        position: 'relative',
      });
      element.hide().slideDown(done)
    }
  };
});