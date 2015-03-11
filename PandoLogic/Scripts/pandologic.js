$(function () {

    $(".iCheck-helper").click(function () {
        var wrapper = $(this).parent();
        var check = $(this).prev();
        var value = wrapper.hasClass("checked");
        var id = check.attr("data-task-id");

        if (value) {
            check.parent().parent().parent().addClass("done");
            $.post("/Tasks/Complete/" + id, function (data) { });
        } else {
            check.parent().parent().parent().removeClass("done");
            $.post("/Tasks/Uncomplete/" + id, function (data) { });
        }
    });
});

function markIndices($container) {
    // Mark each child with their new index
    var index = 1;
    $container.children().each(function () {
        var $this = $(this);
        if ($this.is(":hidden"))
            return;

        $(this).find(".index").html(index);
        ++index;
    });
}

function addNestedForm(container, counter, ticks, content) {

    // Setup the new form
    var nextIndex = $(counter).length;
    var pattern = new RegExp(ticks, "gi");
    content = content.replace(pattern, nextIndex);
    var $container = $(container);
    $container.append(content);

    markIndices($container);
}

function removeNestedForm(element, container, deleteElement) {
    $container = $(element).parents(container);
    $container.find(deleteElement).val('True');
    $container.hide();

    markIndices($container.parent());
}