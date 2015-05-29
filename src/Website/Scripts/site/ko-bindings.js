ko.bindingHandlers.sort = {
  init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
    var asc = false;

    var prop = valueAccessor().prop;
    var mySort = function (left, right) {
      var rec1 = left;
      var rec2 = right;

      if (!asc) {
        rec1 = right;
        rec2 = left;
      }

      var props = prop.split('.');
      for (var i in props) {
        var propName = props[i];
        var parenIndex = propName.indexOf('()');
        if (parenIndex > 0) {
          propName = propName.substring(0, parenIndex);
          rec1 = rec1[propName]();
          rec2 = rec2[propName]();
        } else {
          rec1 = rec1[propName];
          rec2 = rec2[propName];
        }
      }

      return rec1 == rec2 ? 0 : rec1 < rec2 ? -1 : 1;
    }

    if (valueAccessor().default) {
      valueAccessor().arr.lastSort = mySort;
    }

    element.style.cursor = 'pointer';

    element.onclick = function () {
      var value = valueAccessor();
      var prop = value.prop;
      var data = value.arr;
      asc = !asc;

      data.lastSort = mySort;
      data.sort(data.lastSort);
    };
  }
};

ko.bindingHandlers.foreachWithHighlight = {
  init: function (element, valueAccessor, allBindingsAccessor, viewModel, context) {
    var flashReset = valueAccessor().data.initReset,
      key = "forEachWithHightlight_initialized";
    if (flashReset) {
      flashReset.subscribe(function () { ko.utils.domData.set(element, key, false) });
    }
    return ko.bindingHandlers.foreach.init(element, valueAccessor, allBindingsAccessor, viewModel, context);
  },
  update: function (element, valueAccessor, allBindingsAccessor, viewModel, context) {
    var args = valueAccessor();
    var value = ko.unwrap(args.data),
        duration = ko.unwrap(args.duration) || 800,
        key = "forEachWithHightlight_initialized";

    var newValue = function () {
      return {
        data: value,
        afterAdd: function (el, index, data) {
          if (ko.utils.domData.get(element, key)) {
            $(el).addClass('info');
            window.setTimeout(function () { $(el).removeClass('info') }, duration);
          };
        }
      };
    };

    ko.bindingHandlers.foreach.update(element, newValue, allBindingsAccessor, viewModel, context);

    //if we have not previously marked this as initialized and there are currently items in the array, then cache on the element that it has been initialized
    if (!ko.utils.domData.get(element, key) && value.length) {
      ko.utils.domData.set(element, key, true);
    }

    return { controlsDescendantBindings: true };
  }
};