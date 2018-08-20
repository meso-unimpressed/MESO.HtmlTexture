# HTML Texture by MESO

Originally forked from Gumilastik's implementation of ChromiumFX in vvvv

Yet another Html Texture for VVVV which is using a custom built CEF and the rebuilt ChromiumFX bindings for that. It's using CEF branch 3497 which is Chromium 69.0.3497.32 as basis and merged 2 pull requests into it which were crucial for our purposes.

* Accessing directly the resulting Chromium framebuffer as a shared D3D11 texture
  https://bitbucket.org/chromiumembedded/cef/pull-requests/158/support-external-textures-in-osr-mode
  This makes it possible to render high resolution (4K+) HTML content without the VRAM - System RAM back-and-forth copying enormous bottleneck
* Touch input and gestures implementation in offscreen-rendering
  https://bitbucket.org/chromiumembedded/cef/pull-requests/104/touch-processing-for-osr-different-api
  With this we can provide real multi-touch events to the HTML content.

At the time of writing these features are not part of the official CEF repository and we couldn't wait around until they become part of it. We only provide the modified CEF and its ChromiumFX bindings as binary blobs. We might attempt to contribute proper source-code back to the respective repository in the future, but currently we don't have time for that. This way redistribution and automated building is just incomparably faster to us.

### Additional features for VVVV

* Compiled with proprietary codec support
* Spreadable, binsizable
* Finer control of mouse and keyboard input
* Separate chainable Operation nodes
  * Bind Objects with callable functions
  * Execute Javascript code
  * Navigate history (forward/backward)
  * Scroll window or elements
  * Send Touches
* Arbitrary CLR object serialization to Javascript object

Licensed under BSD-3 Clause  
  Copyright @ MESO Digital Interiors GmbH, 2018