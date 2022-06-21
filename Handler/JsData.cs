using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUI.Handler
{
    public class JsData
    {
        //0:Object describe,1:Invoke
        public int Type { set; get; }
        public long Id { set; get; }
        public string Name { set; get; }
        public string Member { set; get; }
        public bool IsField { set; get; }
        public object[] Args { set; get; }
    }
}
