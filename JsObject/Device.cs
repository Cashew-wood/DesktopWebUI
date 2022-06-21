using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebUI.JsObject
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class Device : JsBaseObject
    {
        public int screenWidth
        {
            get
            {
                return Screen.PrimaryScreen.Bounds.Width;
            }
        }
        public int screenHeight
        {
            get
            {
                return Screen.PrimaryScreen.Bounds.Height;
            }
        }

        public override string __name { get; set; } = "device";
    }
}
