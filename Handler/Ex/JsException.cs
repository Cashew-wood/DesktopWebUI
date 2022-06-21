using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUI.Handler.Ex
{
    public class JsException : Exception
    {
        public JsException(string message) : base(message)
        {
        }
    }
}
