
if ("vvvvUtils" in window) {
    vvvvUtils.windowScroll = (h, v, normalized) => {
        if (normalized) {
            window.scrollTo(h * document.body.scrollWidth, v * document.body.scrollHeight);
        } else {
            window.scrollTo(h, v);
        }
    };

    vvvvUtils.elementScroll = (h, v, selector, normalized) => {
        document.querySelectorAll(selector).forEach(el => {
            if (normalized) {
                el.scrollLeft = h * el.scrollWidth;
                el.scrollTop = v * el.scrollHeight;
            } else {
                el.scrollLeft = h;
                el.scrollTop = v;
            }
        });
    };

    vvvvUtils.prevDocW = -1;
    vvvvUtils.prevDocH = -1;

    setInterval(() => {
        if ("customSizeReport" in vvvvUtils) {
            vvvvUtils.customSizeReport();
        } else {
            var selector = vvvvUtils.docSizeBaseSelector();
            var el = document.querySelector(selector);
            if(el != null && el !== undefined) {
                var mw = Math.max(el.scrollWidth, document.body.scrollWidth);
                var mh = Math.max(el.scrollHeight, document.body.scrollHeight);
            } else {
                var mw = document.body.scrollWidth;
                var mh = document.body.scrollHeight;
            }

            if (vvvvUtils.prevDocW != mw || vvvvUtils.prevDocH != mh) {
                console.log(selector + " size changed");
                
                vvvvUtils.docResizeNotification(mw, mh);
            }

            vvvvUtils.prevDocW = mw;
            vvvvUtils.prevDocH = mh;
        }
    }, 25);
}
