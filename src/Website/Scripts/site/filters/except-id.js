angular.module('missionlineApp').filter('exceptId', function () {
  return function (items, item) {
    var filtered = [];
    angular.forEach(items, function (it) {
      if (it.id != item.id) {
        filtered.push(it);
      }
    });
    return filtered;
  }
});