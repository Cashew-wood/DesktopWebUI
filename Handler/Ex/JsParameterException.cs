using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUI.Handler.Ex
{
    public class JsParameterException : JsException
    {
        public JsParameterException(string message) : base(message)
        {
        }
    }
}
