using Enigma.D3;
using Enigma.D3.DataTypes;
using Enigma.D3.Enums;
using Enigma.D3.Helpers;
using Enigma.D3.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Vector3 = SharpDX.Vector3;
using Vector2 = SharpDX.Vector2;
using System.Windows.Forms;
using Dev.D3.Core.Hook;
using Dev.D3.Core.Util;

namespace Dev.D3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        
        private static bool IsBotRunning { get; set; }

        private GlobalKeyboardHook globalKeyboardHook;


        Nav.ExploreEngine.Nearest exploreEngine;

        public MainWindow()
        {
            InitializeComponent();


            this.KeyboardHook();


            Engine.Create();

            WindowHook.SetD3WindowText("Dev Stopped");

            this.LoadLocalData();
            this.LoadBuffManager();
            this.LoadUIHandlers();
            this.LoadActors();

            //new System.Threading.Thread(() =>
            //{
            //    while (true)
            //    {
            //        this.RefreshDebugInfo();
            //        System.Threading.Thread.Sleep(10000000);
            //    }
            //}).Start();


            //new System.Threading.Thread(() =>
            //{
            //    while (true)
            //    {
            //        if (IsBotRunning)
            //            StartSimpleRun();
            //        System.Threading.Thread.Sleep(1000);
            //    }
            //}).Start();

            

        }

        private void KeyboardHook()
        {
            globalKeyboardHook = new GlobalKeyboardHook(); // Create a new GlobalKeyboardHook
                                                           // Declare a KeyDown Event
            globalKeyboardHook.KeyDown += GHook_KeyDown;
            // Add the keys you want to hook to the HookedKeys list
            //foreach (Keys key in Enum.GetValues(typeof(Keys)))
            //    globalKeyboardHook.HookedKeys.Add(key);111111111

            globalKeyboardHook.HookedKeys.Add(Keys.NumPad1);
            globalKeyboardHook.HookedKeys.Add(Keys.NumPad2);

            globalKeyboardHook.hook();
        }

        private void GHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.NumPad1)
            {
                IsBotRunning = true;
                WindowHook.SetD3WindowText( "Dev Running");
                StartSimpleRun();
            }

            if (e.KeyCode == Keys.NumPad2)
            {
                IsBotRunning = false;
                WindowHook.SetD3WindowText("Dev Stopped");

            }
        }



        private void Exploration()
        {
            exploreEngine = new Nav.ExploreEngine.Nearest();

            exploreEngine.Enabled = true;
        }



        private async void StartSimpleRun()
        {



            //var acd = ActorCommonDataHelper.EnumerateMonsters().Where(x => x.x004_Name.StartsWith("Templar"));
            //var localAcd = acd.Last();


            while (IsBotRunning)
            {
                var target = Targeting.GetTarget();
                if (target != null)
                {
                    Vector3 currentCharGameLoc = new Vector3() { X = target.x0D0_WorldPosX, Y = target.x0D4_WorldPosY, Z = target.x0D8_WorldPosZ };
                    var moveToResult = await Core.Util.MoveTo.MoveToPosWithNavMeshAsync(currentCharGameLoc);

                }

            }

            //foreach (var acd in ActorCommonDataHelper.EnumerateMonsters().Where(x => x.x0D0_WorldPosX > 0 && x.x188_Hitpoints > 00001 && x.x190_TeamId == 10))
            //{

            //    Vector3 currentCharGameLoc = new Vector3() { X = acd.x0D0_WorldPosX, Y = acd.x0D4_WorldPosY, Z = acd.x0D8_WorldPosZ };


            //    var moveToResult = await Core.Util.MoveTo.MoveToPosWithNavMeshAsync(currentCharGameLoc);

            //   // await Core.Util.Attack.AttackAcdAsync(acd);

            //    if (!IsBotRunning)
            //        return;

            //}


        }


        private void btnStart_Copy_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshDebugInfo();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
