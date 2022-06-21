using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebUI.JsObject;

namespace WebUI.Handler
{
    public class MultipleJsMessageHandler
    {
        private Dictionary<string, JsMessageHandler> handlers = new Dictionary<string, JsMessageHandler>();
        private List<KeyValuePair<JsMessageHandler, JsMessageHandler>> relation = new List<KeyValuePair<JsMessageHandler, JsMessageHandler>>();
        public string Register(JsMessageHandler handler)
        {
            handler.Name = handler.jsObject.__name;
            handlers.Add(handler.jsObject.__name, handler);
            return handler.jsObject.__name;
        }
        public string Register(string name, JsMessageHandler handler)
        {
            string key = getRegisterName(name, handler);
            if (handler.Name == null)
            {
                handler.Name = key;
            }else if (handler.Name != null && handler.Name != key)
            {
                handler = handler.Clone(key);
            }
            handlers.Add(key, handler);
            return key;
        }
        public string Register(string name, JsMessageHandler handler, bool parent, JsMessageHandler target)
        {
            if (parent)
            {
                target.ChidHandlers.Add(handler);
                this.relation.Add(new KeyValuePair<JsMessageHandler, JsMessageHandler>(target, handler));
            }
            else
            {
                handler.ChidHandlers.Add(target);
                this.relation.Add(new KeyValuePair<JsMessageHandler, JsMessageHandler>(handler, target));
            }
            return this.Register(name, handler);
        }
        public JsMessageHandler GetHander(string name)
        {
            return this.handlers.ContainsKey(name) ? this.handlers[name] : null;
        }
        public bool Exists(string name)
        {
            return this.handlers.ContainsKey(name);
        }
        public void Clear()
        {
            foreach (KeyValuePair<JsMessageHandler, JsMessageHandler> item in this.relation)
            {
                item.Key.ChidHandlers.Remove(item.Value);
            }
        }
        public static string getRegisterName(string name, JsMessageHandler handler)
        {
            return handler.jsObject.__name + "." + name;
        }
    }
}
