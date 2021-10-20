$(function () {
    $.ajaxSetup({ cache: false });
    $(".popup").click(function (e) {

        e.preventDefault();
        $.get(this.href, function (data) {
            $('#dialogContent').html(data);
            $('#modDialog').modal('show');
        });
    });
})
$(function () {
    $.ajaxSetup({ cache: false });
    $(".popup"). (function (e) {

        e.preventDefault();
        $.get(this.href, function (data) {
            $('#dialogContent').html(data);
            $('#modDialog').modal('show');
        });
    });
})
