var nav = document.getElementById("nav-trans"),
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
    nav.style["-webkit-transition"] = "background-color 0.5s linear, border-color 0.5s linear";
    nav.style["-moz-transition"]    = "background-color 0.5s linear, border-color 0.5s linear";
    nav.style["-ms-transition"]     = "background-color 0.5s linear, border-color 0.5s linear";
    nav.style["-o-transition"]      = "background-color 0.5s linear, border-color 0.5s linear";
    nav.style["transition"]         = "background-color 0.5s linear, border-color 0.5s linear";
    nav.style["background-color"]   = rgba + ", 0";
    nav.style["border-color"]       = rgba2 + " , 0";
}

function transition() {
    scrolled = true;
    nav.style["background-color"] = rgba + ", 1";
    nav.style["border-color"]     = rgba2 + ", 1";
}

function transitionBack() {
    scrolled = false;
    nav.style["background-color"] = rgba + ", 0";
    nav.style["border-color"]     = rgba2 + ", 0";
}

firstRun();
window.onscroll = function() {scroll = true};
setInterval(function() {
        if(scroll === true) {
            scroll = false;
            var pos = window.scrollY || window.scrollTop;
            if (pos > 50) {
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