
function stickyTooltip(id, text, position) {
    $('#' + id).tooltip({ trigger: "manual", title: text, placement: position, html: true, animation: false })
        .on("mouseenter", function () {
            var _this = this;
            $(this).tooltip("show");
            $(".tooltip").on("mouseleave", function () {
                $(_this).tooltip('hide');
            });
        }).on("mouseleave", function () {
            var _this = this;
            setTimeout(function () {
                if (!$(".tooltip:hover").length) {
                    $(_this).tooltip("hide");
                }
            }, 300);
        });
}