using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUI.JsObject
{
    public abstract class JsBaseObject
    {
        public Dictionary<string, string> __describe = new Dictionary<string, string>();
        public abstract string __name { set; get; }
    }
}
