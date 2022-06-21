using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WebUI.Handler.Ex;
using WebUI.JsObject;

namespace WebUI.Handler
{
    public class JsMessageHandler
    {
        private WebUIWindow Window;
        public JsBaseObject jsObject { get; }
        public string Name { get; set; }

        internal readonly List<JsMessageHandler> ChidHandlers;
        public JsMessageHandler(WebUIWindow window, JsBaseObject jsObject)
        {
            this.Window = window;
            this.jsObject = jsObject;
            this.ChidHandlers = new List<JsMessageHandler>();
        }

        public JsMessageHandler(WebUIWindow window, JsBaseObject jsObject, List<JsMessageHandler> chidHandlers) : this(window, jsObject)
        {
            ChidHandlers = chidHandlers;
        }

        public JsMessageHandler Clone(string name)
        {
            return new JsMessageHandler(this.Window, this.jsObject, this.ChidHandlers) { Name = name };
        }
        public object Invoke(JsData jsInvoke, WebUIWindow current)
        {
            if (jsInvoke.IsField)
            {
                var property = this.jsObject.GetType().GetProperty(jsInvoke.Member, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (property == null) return null;
                if (jsInvoke.Args.Length == 0)
                {
                    return property.GetValue(this.jsObject);
                }
                else
                {
                    property.SetValue(this.jsObject, jsInvoke.Args[0]);
                    return null;
                }
            }
            else
            {
                var method = this.jsObject.GetType().GetMethods().First(e =>
                {
                    return e.Name == jsInvoke.Member;
                });
                if (method == null) return null;
                var parameters = method.GetParameters();

                var list = new List<object>();
                var len = 0;
                if (jsInvoke.Args != null)
                {
                    len = jsInvoke.Args.Length;
                }
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    if (parameter.ParameterType.IsPrimitive && len <= i)
                    {
                        list.Add(Activator.CreateInstance(parameters[i].ParameterType));
                    }
                    else if (parameter.ParameterType == typeof(JsFunction) && len > i && jsInvoke.Args[i] != null)
                    {
                        var c = jsInvoke.Args[i].GetType();
                        if (jsInvoke.Args[i].GetType() == typeof(object[]))
                        {
                            object[] objects = (object[])jsInvoke.Args[i];
                            var a = objects[0].GetType();
                            if (objects.Length == 2 && objects[0]?.ToString() == "__function__")
                            {
                                string id = objects[1].ToString();
                                list.Add(new JsFunction(current.WebView, id));
                            }
                            else
                                throw new JsParameterException("parameter function format error");
                        }
                        else
                            throw new JsParameterException("parameter function format error");
                    }
                    else if (len > i)
                    {
                        object value = jsInvoke.Args[i];
                        int now = list.Count;
                        if (value != null)
                        {
                            Type sourceType = value.GetType();
                            if (parameter.ParameterType == typeof(int))
                            {
                                if (sourceType == typeof(long))
                                {
                                    list.Add((int)(long)value);
                                }
                            }
                            else if (parameter.ParameterType == typeof(long))
                            {
                                if (sourceType == typeof(int))
                                {
                                    list.Add((long)(int)value);
                                }
                            }

                        }
                        if (now == list.Count)
                            list.Add(jsInvoke.Args[i]);

                    }
                    else
                    {
                        list.Add(null);
                    }
                }
                jsInvoke.Args = list.ToArray();
                return method.Invoke(this.jsObject, jsInvoke.Args);
            }
        }


        public List<ObjDescribe> GetObjDescribes(WebUIWindow window)
        {
            JsBaseObject obj = jsObject;
            List<ObjDescribe> objDescribes = new List<ObjDescribe>();
            foreach (MemberInfo info in obj.GetType().GetMembers(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            {
                if (info.Name.StartsWith("get_") || info.Name.StartsWith("set_")) continue;
                if (info.MemberType == MemberTypes.Property || info.MemberType == MemberTypes.Method)
                {
                    objDescribes.Add(new ObjDescribe()
                    {
                        Field = info.MemberType == MemberTypes.Property,
                        Type = 0,
                        Name = info.Name
                    });
                }
            }
            if (window != this.Window)
            {
                foreach (KeyValuePair<string, string> memberDesc in obj.__describe)
                {
                    objDescribes.Add(new ObjDescribe()
                    {
                        Field = memberDesc.Value != "function",
                        Type = 1,
                        Name = memberDesc.Key
                    });
                }
            }
            return objDescribes;
        }
        public void AddObjDescribes(JsData jsData, WebUIWindow current)
        {
            if (current != this.Window) return;
            JsBaseObject obj = jsObject;
            if (!obj.__describe.ContainsKey(jsData.Member))
            {
                obj.__describe.Add(jsData.Member, (string)jsData.Args[0]);
                foreach (JsMessageHandler item in this.ChidHandlers)
                {
                    if (item.Window != current)
                    {
                        item.Window.WebView.CoreWebView2?.PostWebMessageAsJson(new JsResult(0, -1, item.Name, this.GetObjDescribes(item.Window)).ToJson());
                    }
                }
            }
        }
        public void InvokeJs(JsData jsData, Action<object> action)
        {
            string fullName = "native." + this.jsObject.__name;
            if (jsData.IsField && jsData.Args.Length > 0)
            {
                this.Window.WebView.CoreWebView2.ExecuteScriptAsync(fullName + "." + jsData.Member + "=JSON.parse('" + jsData.Args[0].ToJson() + "')");
            }
            else
            {
                Task<string> task = this.Window.WebView.CoreWebView2.ExecuteScriptAsync("(()=>{_args_='" + jsData.Args.ToJson() + "';return " + fullName + "." + jsData.Member + (jsData.IsField ? "" : ".apply(null,JSON.parse(_args_))") + "})()");
                task.ContinueWith((t) =>
                {
                    this.Window.Dispatcher.Invoke(() =>
                    {
                        action.Invoke(Json.ParseValue(typeof(object), task.Result));
                    });
                });
            }

        }
    }
}
