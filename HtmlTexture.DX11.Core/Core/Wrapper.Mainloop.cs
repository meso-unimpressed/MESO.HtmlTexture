using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using md.stdl.Coding;
using md.stdl.Interfaces;

namespace VVVV.HtmlTexture.DX11.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public void Mainloop(float deltatime)
        {
            OnMainLoopBegin?.Invoke(this, EventArgs.Empty);
            if (Closed) return;

            if (CreatedFrame) CreatedFrame = false;
            if (_browserReadyFrame)
            {
                OnBrowserReady?.Invoke(this, EventArgs.Empty);
                CreatedFrame = true;
                _browserReadyFrame = false;
            }

            if (Browser == null) return;

            if (LoadedFrame)
            {
                LoadedFrame = false;
                IsDocumentReady = true;
            }
            Loading = Browser.IsLoading;
            if (_loadedFrame)
            {
                LoadedFrame = true;
                _loadedFrame = false;
            }

            if (_invalidateAccFrame)
            {
                _newAccFrameReady.Clear();
            }

            if (!TextureSettings.Equals(_prevTextureSettings)) UpdateSize();

            //if (BrowserSettings.ZoomLevel != Browser.Host.ZoomLevel)
            //{
            //    Browser.Host.ZoomLevel = Math.Abs(BrowserSettings.ZoomLevel) <= 0.0001 ? 0.0001 : BrowserSettings.ZoomLevel;
            //    //Browser.Host.ZoomLevel = BrowserSettings.ZoomLevel;
            //}

            Browser.Host.ZoomLevel = BrowserSettings.ZoomLevel;

            if (DocumentSizeBaseSelector != null)
                DocumentSizeBaseSelector.Selector = BrowserSettings.DocumentSizeElementSelector;

            if (LivePageActive != BrowserSettings.UseLivePage)
            {
                ExecuteJavascript(BrowserSettings.UseLivePage ? Globals.LivePageLoader : Globals.LivePageUnloader);
                LivePageActive = BrowserSettings.UseLivePage;
            }

            SubmittedTouches.Clear();

            if (Enabled)
            {
                try
                {
                    Operations?.InvokeRecursive(this);
                }
                catch (Exception e)
                {
                    LogError(e);
                }

                if (InitSettings.FrameRequestFromVvvv && AllowFrameRequest)
                {
                    Browser.Host.SendExternalBeginFrame();
                }
            }

            ProcessTouches();

            _prevTextureSettings = TextureSettings;
            _prevBrowserSettings = BrowserSettings;

            OnMainLoopEnd?.Invoke((object)this, EventArgs.Empty);
        }

        public void UpdateDX11Resources(DX11RenderContext context)
        {
            var invalidate = !_newAccFrameReady.ContainsKey(context) || !DX11Texture.Contains(context);
            if (invalidate)
            {
                UpdateSize();
            }
            if (!invalidate) invalidate = _newAccFrameReady[context];

            if (_newAccFramePtr != IntPtr.Zero && invalidate)
            {
                if (DX11Texture.Contains(context)) DX11Texture.Dispose(context);
                try
                {
                    DX11Texture[context] = DX11Texture2D.FromSharedHandle(context, _newAccFramePtr);
                    IsTextureValid = true;
                }
                catch
                {
                    IsTextureValid = false;
                }
            }

            if (invalidate)
            {
                _newAccFrameReady.UpdateGeneric(context, false);
                //Browser.Host.SendExternalBeginFrame();
            }
        }
    }
}
