using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dev.D3.Core.UI
{
    class UIClick
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        public static void ClickUI(string name, int click = 1)
        {
            ////var resaults = Engine.Current.ObjectManager.x984_UI.x0000_Controls.x10_Map[name].Dereference<UIControl>();
            //var addpointX = 0;
            //var addpointY = 0;
            //var Point = GetUIPos.GetUIPosition(name);
            //while (Point[0] == 0 && Point[1] == 0)
            //{
            //    Point = GetUIPos.GetUIPosition(name);
            //    Thread.Sleep(250);
            //}
            //var Point2 = UIRectAngle.GetUIRectAngle((int)Point[0], (int)Point[1], (int)Point[2], (int)Point[3]);
            //if (Point2[2] < 0)
            //{
            //    //Console.WriteLine(Point2[2]);
            //    Point2[2] = 0;
            //    addpointX = 20;
            //}
            //if (Point2[3] < 0)
            //{
            //    //Console.WriteLine(Point2[3]);
            //    Point2[3] = 0;
            //    addpointY = 20;
            //}
            //if (click == 0)
            //{
            //    Cursor.Position = new Point(((int)Point2[0] + (int)Point2[2] / 2) + addpointX, ((int)Point2[1] + (int)Point2[3] / 2) + addpointY);
            //}
            //else
            //{
            //    Cursor.Position = new Point(((int)Point2[0] + (int)Point2[2] / 2) + addpointX, ((int)Point2[1] + (int)Point2[3] / 2) + addpointY);
            //    mouse_event((int)0x02 | 0x04, ((int)Point2[0] + (int)Point2[2] / 2) + addpointX, ((int)Point2[1] + (int)Point2[3] / 2) + addpointY, 0, 0);
            //}


        }

        //public static bool Click(string name)
        //{
        //    UIControl control = new UIControl(Engine.Current.Memory, Engine.Current.ObjectManager.x984_UI.x0000_Controls.x10_Map[name].Value.Address);
        //    UIRect rect = control.x4D8_UIRect.TranslateToClientRect(1920, 1080);

        //    if (IsVisible(name))
        //    {
        //        Console.WriteLine("Clicking " + control.x030_Self);
        //        Thread.Sleep(200);
        //        Util.Click((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        //        return true;
        //    }
        //    return false;
        //}


    }
}
