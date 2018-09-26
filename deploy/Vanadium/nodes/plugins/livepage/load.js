
  
    function loadJs(filename) {
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = filename;
                document.head.appendChild(script);
            }
   
         var $livePageConfig = {
                monitor_css: true,
                monitor_js: true,
                monitor_html: true,
                monitor_custom: true,
                hosts_session: false,
                skip_external: true,
                entire_hosts: false,
                ignore_anchors: true,
                use_only_get: false,
                tidy_html: true,
                tidy_inline_html: false,
                refresh_rate: 200
            };

            var $livePage = false;