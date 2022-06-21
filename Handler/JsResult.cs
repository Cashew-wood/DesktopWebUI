using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUI.Handler
{
    public class JsResult
    {
        public int Type;
        public long Id;
        public string ObjectName;
        public object Data;

        public JsResult(int type, long id, string objectName, object data)
        {
            Type = type;
            Id = id;
            ObjectName = objectName;
            Data = data;
        }
    }
}
