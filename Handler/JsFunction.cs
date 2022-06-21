using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUI.Handler
{
    public class JsFunction
    {
        private string Id;
        private WebView WebView;
        public JsFunction(WebView webView, string id)
        {
            this.Id = id;
            this.WebView = webView;
        }
        public void Invoke(params object[] args)
        {
            this.WebView.CoreWebView2.PostWebMessageAsJson(new Dictionary<string, object>() {
                {"Type",2 },
                {"Id" , this.Id },
                {"Data",args }
            }.ToJson());
        }
    }
}
