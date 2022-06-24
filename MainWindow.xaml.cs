
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using WebUI.Handler;
using WebUI.JsObject;

namespace WebUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WebUIWindow : System.Windows.Window
    {
        public readonly static Brush DefaultBackground = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        public readonly static Brush PenetrateBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public string Url { get; private set; }
        private string Html;
        public bool MainWindow { get; private set; } = true;
        private WebUIWindow ParentWindow;
        public IntPtr Handle;
        public readonly JsMessageHandler jsMessageHandler;
        public readonly MultipleJsMessageHandler multipleJsMessageHandler;
        public bool SetShowInTaskbar = true;
        public event Action WebViewPageLoadComplete;
        public event Action DragWindowEnd;
        public WebUIWindow()
        {
            InitializeComponent();
            this.Background = DefaultBackground;
            this.jsMessageHandler = new JsMessageHandler(this, new JsObject.Window(this));
            this.WebView.EnsureCoreWebView2Async(null);
            this.multipleJsMessageHandler = new MultipleJsMessageHandler();
            this.multipleJsMessageHandler.Register(this.jsMessageHandler);
            this.multipleJsMessageHandler.Register(new JsMessageHandler(this, new Device()));


        }
        public WebUIWindow(WebUIWindow parent, string source, bool isHtml) : this()
        {
            if (isHtml) this.Html = source;
            else this.Url = source;
            this.MainWindow = false;
            this.ParentWindow = parent;
            this.ShowInTaskbar = true;
            this.SetShowInTaskbar = false;
            this.Visibility = Visibility.Hidden;
            this.multipleJsMessageHandler.Register("parent", this.ParentWindow.jsMessageHandler, false, this.jsMessageHandler);
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.MainWindow)
            {
                if (Global.StartArgs.Length == 0)
                {
                    this.Close();
                    return;
                }
                this.Url = Global.StartArgs[0];
            }
            //NativeWindow nativeWindow = new WebUIWindowWndProc(this);
            this.Handle = new WindowInteropHelper(this).Handle;
            this.WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

        }

        private void CoreWebView2_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            //this.WebView.CoreWebView2.PostWebMessageAsString("hello js" + );
            JsData jsInvoke = Json.FromJson<JsData>(e.WebMessageAsJson);
            this.JsMessageDistribute(jsInvoke);

        }
        private void JsMessageDistribute(JsData jsData)
        {
            if (jsData.Type == -2)
            {
                this.WebViewPageLoadComplete?.Invoke();
            }
            else if (jsData.Type == 0)
            {
                object value = this.multipleJsMessageHandler.GetHander(jsData.Name)?.GetObjDescribes(this);
                this.WebView.CoreWebView2.PostWebMessageAsJson(new JsResult(jsData.Type, jsData.Id, jsData.Name, value).ToJson());
            }
            else if (jsData.Type == 1)
            {
                object value = this.multipleJsMessageHandler.GetHander(jsData.Name)?.Invoke(jsData, this);
                this.WebView.CoreWebView2.PostWebMessageAsJson(new JsResult(jsData.Type, jsData.Id, jsData.Name, value).ToJson());
            }
            else if (jsData.Type == 3)
            {
                this.multipleJsMessageHandler.GetHander(jsData.Name)?.AddObjDescribes(jsData, this);
            }
            else if (jsData.Type == 10001)//parent call function or get property,parent set property
            {
                this.multipleJsMessageHandler.GetHander(jsData.Name)?.InvokeJs(jsData, (value) =>
                {
                    this.WebView.CoreWebView2.PostWebMessageAsJson(new JsResult(1, jsData.Id, jsData.Name, value).ToJson());
                });
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (this.MainWindow)
            {
                if (Global.StartArgs.Length > 1 && Global.StartArgs[1] == "1")
                {
                    this.SetShowInTaskbar = false;
                    this.ShowInTaskbar = true;
                }
                else if (Global.StartArgs.Length <= 1 || Global.StartArgs[1] != "1") this.Hide();
            }
            this.WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(Properties.Resources.web_ui_sdk_min);
            if (this.Url == null) this.WebView.CoreWebView2.NavigateToString(this.Html);
            else this.WebView.Source = new Uri(this.Url);
            this.WebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            this.WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            this.WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            this.WebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
            this.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            this.WebView.CoreWebView2.Settings.HiddenPdfToolbarItems = Microsoft.Web.WebView2.Core.CoreWebView2PdfToolbarItems.None;
            this.WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
#if DEBUG
            this.WebView.CoreWebView2.OpenDevToolsWindow();
#endif
            this.WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;

        }
        private void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e)
        {
            this.WebView.CoreWebView2.ExecuteScriptAsync($"var st= document.createElement('style');document.body.appendChild(st);st.innerHTML = `{Properties.Resources.style_min}`;");
            Debug.WriteLine(this.Width);
        }

        private void Main_Closed(object sender, EventArgs e)
        {
            this.multipleJsMessageHandler.Clear();
            if (this.MainWindow)
            {
                Process.GetCurrentProcess().Kill();
            }
        }
        public class WebUIWindowWndProc : NativeWindow
        {
            private WebUIWindow window;
            public WebUIWindowWndProc(WebUIWindow window)
            {
                this.window = window;
                this.AssignHandle(window.Handle);
            }
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == 562)
                {
                    this.window.DragWindowEnd?.Invoke();
                }
            }
        }

    }


}
