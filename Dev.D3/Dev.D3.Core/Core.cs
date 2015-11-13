using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.D3;
using Enigma.D3.Helpers;
using Enigma.Memory;
using Enigma.D3.UI;
using Enigma.D3.DataTypes;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Automation;

namespace Dev.D3.Core
{
    public class Core
    {

        private static Enigma.D3.Engine engine;

        public bool IsD3Ready()
        {
            engine = Enigma.D3.Engine.Create();

            if (engine == null)
                return false;
            else
                return true;
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

        private void Teste()
        {
            

        }



 


        //private static bool IsValidMonster(ActorCommonData acd)
        //{
        //    return acd.x188_Hitpoints > 0.00001 && // seems to be lower limit for what D3 considers dead
        //        (acd.x194_Flags_Is_Trail_Proxy_Etc & 1) == 0 && // this removes most weird actors
        //        acd.x190_TeamId == 10; // = hostile
        //}

        public void GetAllMonsters()
        {
            var engine = Engine.Create();
            var acds = new ActorCommonData[0];
            var pt = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                acds = ActorCommonDataHelper.EnumerateMonsters().ToArray();
            }
            pt.Stop();
            Console.WriteLine("Getting all monster ACDs 1000 times took " + pt.Elapsed.TotalMilliseconds.ToString("0.00") + "ms");
            Console.WriteLine("Max update frequency: " + (1000d / (pt.Elapsed.TotalMilliseconds / 1000)).ToString("0") + "Hz");
            Console.WriteLine("Number of ACDs: " + ActorCommonDataHelper.Enumerate(a => true).Count());
            Console.ReadLine();
        }


        private void DumpAttributeDescriptors()
        {
            var attribDescriptors = Engine.Current.AttributeDescriptors;
            var acd = ActorCommonDataHelper.GetLocalAcd();
            var q = acd.EnumerateAttributes().Select(a => new { Id = a.x04_Key & 0xFFF, Key = a.x04_Key, Mod = (a.x04_Key >> 12), Value = a.x08_Value, Descriptor = attribDescriptors.Single(d => d.x00_Id == (a.x04_Key & 0xFFF)) });
            var str = string.Join(Environment.NewLine, q.Select(a => string.Join("\t",
                                                                              a.Id.ToString().PadRight(5),
                                                                              "0x" + a.Key.ToString("X8"),
                                                                              (a.Mod == 0xFFFFF ? "-1" : a.Mod.ToString()).PadRight(5),
                                                                              "0x" + a.Mod.ToString("X8").Substring(3),
                                                                              a.Descriptor.x1C_Name,
                                                                              a.Descriptor.x10_DataType == 1 ? a.Value.Int32 : a.Value.Single)));

        }


    }
}
