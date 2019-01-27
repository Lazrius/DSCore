var nav = $("#nav-trans"),
    scroll = false,
    scrolled = false,
    color = "#161717",
    rgba = "rgba(" + parseInt(color.slice(-6,-4),16)
        + "," + parseInt(color.slice(-4,-2),16)
        + "," + parseInt(color.slice(-2),16),
    color2 = "#2d3139",
    rgba2 = "rgba(" + parseInt(color2.slice(-6,-4),16)
        + "," + parseInt(color2.slice(-4,-2),16)
        + "," + parseInt(color2.slice(-2),16);

function firstRun() {
    nav.css({
        "-webkit-transition": "background-color 0.5s linear, border-color 0.5s linear",
        "-moz-transition": "background-color 0.5s linear, border-color 0.5s linear",
        "-ms-transition": "background-color 0.5s linear, border-color 0.5s linear",
        "-o-transition": "background-color 0.5s linear, border-color 0.5s linear",
        "transition": "background-color 0.5s linear, border-color 0.5s linear",
        "background-color": rgba + ", 0",
        "border-color": rgba2 + " , 0",
    });
}

function transition() {
    scrolled = true;
    nav.css({
        "background-color": rgba + ", 1",
        "border-color": rgba2 + ", 1",
    })
}

function transitionBack() {
    scrolled = false;
    nav.css({
        "background-color": rgba + ", 0",
        "border-color": rgba2 + ", 0",
    })
}

firstRun();
$(window).scroll(function() {scroll = true});
setInterval(function() {
        if(scroll === true) {
            scroll = false;
            if ($(window).scrollTop() > 50) {
                if (!scrolled) {
                    transition();
                }
            } else {
                if(scrolled) {
                    transitionBack();
                }
            }
        }
    }, 50)