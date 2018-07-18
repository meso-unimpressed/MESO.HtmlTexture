
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

    vvvvUtils.prevDocW = 0;
    vvvvUtils.prevDocH = 0;

    window.requestAnimationFrame(() => {
        if ("customSizeReport" in vvvvUtils) {
            vvvvUtils.customSizeReport();
        } else {
            var el = document.querySelector(vvvvUtils.docSizeBaseSelector());
            var mw = Math.max(el.scrollWidth, document.body.scrollWidth);
            var mh = Math.max(el.scrollHeight, document.body.scrollHeight);
            var changed = vvvvUtils.prevDocW !== mw || vvvvUtils.prevDocH !== mh;

            if (changed) {
                vvvvUtils.docResizeNotification(mw, mh);
            }

            vvvvUtils.prevDocW = mw;
            vvvvUtils.prevDocH = mh;
        }
    });
}
