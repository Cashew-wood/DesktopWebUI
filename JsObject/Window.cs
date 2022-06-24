
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WebUI.Handler;
using WebUI.Win32;

namespace WebUI.JsObject
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class Window : JsBaseObject
    {
        private readonly WebUIWindow _window;

        public override string __name { set; get; } = "window";
        public string title
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Title = value);
            get => this._window.Dispatcher.Invoke(() => this._window.Title);
        }
        public string icon
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Icon = new BitmapImage(new Uri(new Uri(this._window.Url), value)));
        }
        public double width
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Width = value);
            get => this._window.Dispatcher.Invoke(() => this._window.Width);
        }
        public double height
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Height = value);
            get => this._window.Dispatcher.Invoke(() => this._window.Height);
        }
        public double left
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Left = value);
            get => this._window.Dispatcher.Invoke(() => this._window.Left);
        }
        public double top
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Top = value);
            get => this._window.Dispatcher.Invoke(() => this._window.Top);
        }
        public string state
        {
            get
            {
                switch (this._window.Dispatcher.Invoke(() => this._window.WindowState))
                {
                    case System.Windows.WindowState.Maximized:
                        return "max";
                    case System.Windows.WindowState.Minimized:
                        return "min";
                    case System.Windows.WindowState.Normal:
                        return "normal";
                    default:
                        return null;
                }
            }
            set
            {
                this._window.Dispatcher.Invoke(() =>
                {
                    switch (value)
                    {
                        case "max":
                            this._window.WindowState = System.Windows.WindowState.Maximized;
                            break;
                        case "min":
                            this._window.WindowState = System.Windows.WindowState.Minimized;
                            break;
                        case "normal":
                            this._window.WindowState = System.Windows.WindowState.Normal;
                            break;
                        default:
                            break;
                    }
                });
                
            }
        }
        public bool topmost
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Topmost = value);
            get => this._window.Dispatcher.Invoke(() => this._window.Topmost);
        }
        public bool penetrate
        {
            set => this._window.Dispatcher.Invoke(() => this._window.Background = value ? WebUIWindow.PenetrateBackground : WebUIWindow.DefaultBackground);
            get => this._window.Dispatcher.Invoke(() => ((SolidColorBrush)this._window.Background).Color.A == 0);
        }
        public bool showInTaskbar
        {
            set => this._window.Dispatcher.Invoke(() =>
            {
                this._window.ShowInTaskbar = value;
                if (this._window.SetShowInTaskbar && !value)
                    this._window.SetShowInTaskbar = value;
});
            get => this._window.Dispatcher.Invoke(() => this._window.ShowInTaskbar);
        }
        public bool disableDevTool
        {
            set => this._window.Dispatcher.Invoke(() => this._window.WebView.CoreWebView2.Settings.AreDevToolsEnabled = value);
            get => this._window.Dispatcher.Invoke(() => this._window.WebView.CoreWebView2.Settings.AreDevToolsEnabled);
        }
        public bool allowClose { set; get; }
        private List<Rectangle> DragAreaList = new List<Rectangle>();
        private List<Rectangle> DragNotAreaList = new List<Rectangle>();
        private Dictionary<string, object> _dataMap = new Dictionary<string, object>();
        public Window(WebUIWindow window)
        {
            this._window = window;
        }
        public void move(int offsetX, int offsetY)
        {
            this._window.Dispatcher.Invoke(() =>
            {
                this._window.Left += offsetX;
                this._window.Top += offsetY;
            });
        }
        public void size(int w, int h)
        {
            this._window.Dispatcher.Invoke(() =>
            {
                this._window.Width += w;
                this._window.Height += h;
            });
        }
        
        public void addDragMoveArea(int x, int y, int w, int h, bool exclude)
        {
            if (exclude)
                this.DragNotAreaList.Add(new System.Drawing.Rectangle(x, y, w, h));
            else
                this.DragAreaList.Add(new System.Drawing.Rectangle(x, y, w, h));
        }
        public void clearDragMoveArea()
        {
            this.DragNotAreaList.Clear();
            this.DragAreaList.Clear();
        }
        public void close()
        {
            if (this._window.MainWindow)
                Process.GetCurrentProcess().Kill();
            else
                this._window.Dispatcher.Invoke(() => this._window.Close());
        }
        public void startCenter()
        {
            this._window.Dispatcher.Invoke(() =>
            {
                this._window.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this._window.Width / 2;
                this._window.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this._window.Height / 2;
            });
        }
        public void showDevTool()
        {
            this._window.Dispatcher.Invoke(() =>
            {
                this._window.WebView.CoreWebView2.OpenDevToolsWindow();
            });
        }
        public void show(bool dialog, JsFunction callback)
        {

            if (dialog)
            {
                SynchronizationContext.Current.Post((_) =>
                {
                    callback?.Invoke(this._window.ShowDialog());
                }, null);
            }
            else
            {
                this._window.Dispatcher.Invoke(() =>
                {
                    this._window.Show();
                    if (this._window.SetShowInTaskbar)
                        this._window.ShowInTaskbar = true;
                    void WaitCompleted()
                    {
                        this._window.WebViewPageLoadComplete -= WaitCompleted;
                        callback?.Invoke();
                    }
                    this._window.WebViewPageLoadComplete += WaitCompleted;
                });
            }

        }


        public void hide()
        {
            this._window.Dispatcher.Invoke(() => this._window.Hide());
        }
        public string _createWindow(string name, string source, bool isHtml)
        {
            string windowObjKey = "subwindows." + (name ?? ("window" + (++Global.WindowCount)));
            SynchronizationContext.Current.Post((_) =>
            {
                var window = this._window.Dispatcher.Invoke(() => new WebUIWindow(this._window, source, isHtml));
                this._window.multipleJsMessageHandler.Register(windowObjKey, window.jsMessageHandler, false,
                    this._window.jsMessageHandler.Clone(MultipleJsMessageHandler.getRegisterName(windowObjKey, this._window.jsMessageHandler)));
            }, null);
            return MultipleJsMessageHandler.getRegisterName(windowObjKey, this._window.jsMessageHandler);
        }
        public void dragWindow(int x, int y)
        {
            SynchronizationContext.Current.Post((_) =>
            {
                foreach (Rectangle rect in this.DragAreaList)
                {
                    if (rect.Contains(x, y))
                    {
                        bool exclude = false;
                        foreach (Rectangle er in this.DragNotAreaList)
                        {
                            if (er.Contains(x, y))
                            {
                                exclude = true;
                                break;
                            }
                        }
                        if (!exclude)
                        {
                            /* void DrawWindowEnd()
                             {
                                 this._window.DragWindowEnd -= DrawWindowEnd;
                                 this._window.WebView.ExecuteScriptAsync("document.dispatchEvent(new Event('mouseup'))");
                             }
                             this._window.DragWindowEnd += DrawWindowEnd;*/
                            User32.ReleaseCapture();
                            User32.SendMessage(this._window.Handle, 0x0112, 0xF010 + 0x0002, 0);
                            break;
                        }
                    }
                }
            }, null);

        }
        public void hideInTaskView()
        {
            User32.SetWindowLong(this._window.Handle, User32.GWL_EX_STYLE, (User32.GetWindowLong(this._window.Handle, User32.GWL_EX_STYLE) | User32.WS_EX_TOOLWINDOW) & ~User32.WS_EX_APPWINDOW);
        }

        public void setData(string key, object value)
        {
            if (this._dataMap.ContainsKey(key))
            {
                this._dataMap[key] = value;
            }
            else
            {
                this._dataMap.Add(key, value);
            }
        }
        public object getData(string key)
        {
            if (this._dataMap.ContainsKey(key))
            {
                return this._dataMap[key];
            }
            else
            {
                return null;
            }
        }
        public void onClose(JsFunction callback)
        {
            this._window.Closing += (sender, e) =>
            {
                 callback?.Invoke();
                e.Cancel = !this.allowClose;
            };
        }
        public void onActivated(JsFunction callback)
        {
            this._window.Activated += (sender, e) =>
            {
                callback?.Invoke();
            };
        }
        public void onDeactivated(JsFunction callback)
        {
            this._window.Deactivated += (sender, e) =>
            {
                callback?.Invoke();
            };
        }
        public void onLocationChange(JsFunction callback)
        {
            this._window.LocationChanged += (sender, e) =>
            {
                callback?.Invoke(this._window.Left,this._window.Top);
            };

        }
        public void onResize(JsFunction callback)
        {
            this._window.SizeChanged += (o, e) =>
            {
                callback?.Invoke(e.NewSize.Width, e.NewSize.Height);
            };
        }
    }
}
