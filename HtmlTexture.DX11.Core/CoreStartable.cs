using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using VVVV.DX11.Nodes;
using VVVV.PluginInterfaces.V2;

namespace VVVV.HtmlTexture.DX11.Core
{
    public static class HtmlTextureStartable
    {
        private static void app_OnBeforeCommandLineProcessing(object sender, Chromium.Event.CfxOnBeforeCommandLineProcessingEventArgs e)
        {
            // speech api
            e.CommandLine.AppendSwitch("enable-speech-input");
            e.CommandLine.AppendSwitch("ignore-gpu-blacklist");
            e.CommandLine.AppendSwitch("enable-experimental-canvas-features");
            e.CommandLine.AppendSwitch("smooth-scrolling");
            e.CommandLine.AppendSwitchWithValue("enable-features", "OverlayScrollbar");
            e.CommandLine.AppendSwitchWithValue("touch-events", "enabled");

            // enable pepper flash or system Flash
            if (Directory.Exists(Path.Combine(Globals.AssemblyDir, @"cef\PepperFlash")))
            {
                e.CommandLine.AppendSwitchWithValue("ppapi-flash-version", "19.0.0.201");
                e.CommandLine.AppendSwitchWithValue("ppapi-flash-path", Path.Combine(Globals.AssemblyDir, @"cef\PepperFlash\pepflashplayer.dll"));
            }
            else
            {
                e.CommandLine.AppendSwitch("enable-system-flash");
            }

            // MessageBox.Show(e.CommandLine.CommandLineString);
        }

        // Main entry point when called by vvvv
        public static void Start()
        {
            CfxRuntime.LibCefDirPath = Globals.AssemblyDir;

            var app = new CfxApp();
            app.OnBeforeCommandLineProcessing += app_OnBeforeCommandLineProcessing;

            var settings = new CfxSettings
            {
                //PackLoadingDisabled = true,
                WindowlessRenderingEnabled = true,
                NoSandbox = true,
                UserDataPath = Path.Combine(Globals.AssemblyDir, "userdata"),
                CachePath = Path.Combine(Globals.AssemblyDir, "cache"),
                BrowserSubprocessPath = Globals.AssemblyLocation,
                LogSeverity = CfxLogSeverity.Disable,
                SingleProcess = false, // DEBUG
                MultiThreadedMessageLoop = true, // false
                IgnoreCertificateErrors = true
            };

            CfxRuntime.Initialize(settings, app, RenderProcess.RenderProcessMain);
        }
        public static void Shutdown()
        {
            CfxRuntime.Shutdown();
        }
    }
}
