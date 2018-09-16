/**
 * Hack for autofilled input-fields by Chrome.
 * The placeholder label will overlap the autofilled text.
 * Add the active class to the associated label to prevent overlapping text.
 */
document.addEventListener("DOMContentLoaded",
    function() {
        setTimeout(function() {
                $("input:-webkit-autofill").each(function(index, element) {

                    var labels = element.labels;
                    for (var i = labels.length; i-- > 0;) {
                        labels[i].classList.add("active");
                    }
                });
            },
            1);
    });