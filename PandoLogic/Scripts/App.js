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
})