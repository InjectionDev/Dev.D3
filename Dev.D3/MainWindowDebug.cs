using Enigma.D3;
using Enigma.D3.Helpers;
using Enigma.D3.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dev.D3
{
    public partial class MainWindow
    {



        private void RefreshDebugInfo()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (Engine.Current == null)
                    return;

                var LocalDataIsExpanded = ((TreeViewItem)this.treeView.Items[0]).IsExpanded;
                var BuffManageIsExpanded = ((TreeViewItem)this.treeView.Items[1]).IsExpanded;
                var UIHandlersIsExpanded = ((TreeViewItem)this.treeView.Items[2]).IsExpanded;
                var ActorsIsExpanded = ((TreeViewItem)this.treeView.Items[3]).IsExpanded;

                this.treeView.Items.Clear();

                this.LoadLocalData();
                this.LoadBuffManager();
                this.LoadUIHandlers();
                this.LoadActors();

                ((TreeViewItem)this.treeView.Items[0]).IsExpanded = LocalDataIsExpanded;
                ((TreeViewItem)this.treeView.Items[1]).IsExpanded = BuffManageIsExpanded;
                ((TreeViewItem)this.treeView.Items[2]).IsExpanded = UIHandlersIsExpanded;
                ((TreeViewItem)this.treeView.Items[3]).IsExpanded = ActorsIsExpanded;

            });

        }

        private void LoadActors()
        {
            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = "Actors";
            treeViewItem.Items.Add("x958_Ptr_RActors: " + Engine.Current.ObjectManager.x958_Ptr_RActors);

            TreeViewItem treeViewItemMonsters = new TreeViewItem();
            treeViewItemMonsters.Header = "Actors x098_MonsterSnoId";
            var AcdMonsterList = Engine.TryGet(a => ActorCommonData.Container).Where(a => a.x098_MonsterSnoId != -1);
            foreach (var acd in AcdMonsterList.OrderByDescending(x => x.x0B8_MonsterQuality))
            {
                treeViewItemMonsters.Items.Add(string.Format("UI x004_Name: {0} x0B8_MonsterQuality: {1} (x08C_ActorId: {2} WorldPos:{3}/{4}/{5})", acd.x004_Name, acd.x0B8_MonsterQuality, acd.x08C_ActorId, acd.x0D0_WorldPosX, acd.x0D4_WorldPosY, acd.x0D8_WorldPosZ));
            }

            treeViewItem.Items.Add(treeViewItemMonsters);

            this.treeView.Items.Add(treeViewItem);
        }

        private void LoadUIHandlers()
        {
            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = "UIHandlers";
            treeViewItem.Items.Add("UIHandlers: " + Engine.Current.UIHandlers.Count());
            foreach (var ui in Engine.Current.UIHandlers.Where(x => x.x00_Name != "-1"))
            {
                treeViewItem.Items.Add(string.Format("UI x00_Name: {0}  (x04_Hash: {1})", ui.x00_Name, ui.x04_Hash));


            }

            foreach (var ui in Engine.Current.UIReferences.Where(x => x.x008_Name != "-1"))
            {
                treeViewItem.Items.Add(string.Format("UI x00_Name: {0}  (x04_Hash: {1})", ui.x008_Name, ui.x000_Hash));

                //try
                //{
                //    var uiControl = UXHelper.GetControl(ui.x008_Name);



                //    MessageBox.Show("OK");
                //}
                //catch
                //{

                //}


            }

            var uiMap = UXHelper.GetUIMap();

            // Get all the ui map objects
            List<UIMap.Pair> uiMapList = uiMap.ToList();

            // For each control in the map get the reference control
            foreach (UIMap.Pair itemmap in uiMapList)
            {
                //uiControls.Add(uiMap[itemmap.x08_Hash].Dereference<UIControl>());
            }

            this.treeView.Items.Add(treeViewItem);
        }

        private void LoadBuffManager()
        {
            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = "BuffManager";
            treeViewItem.Items.Add("x1C_Buffs: " + Engine.Current.BuffManager.x1C_Buffs);
            treeViewItem.Items.Add("x30_Debuffs: " + Engine.Current.BuffManager.x30_Debuffs);
            //treeViewItem.Items.Add("x1C_Buffs: " + Engine.Current.BuffManager.x44_BuffList);
            //treeViewItem.Items.Add("x1C_Buffs: " + Engine.Current.BuffManager.x58_BuffList);

            if (Engine.Current.BuffManager.x1C_Buffs.Any())
                treeViewItem.Items.Add("x1C_Buffs List");
            foreach (var buff in Engine.Current.BuffManager.x1C_Buffs)
            {
                treeViewItem.Items.Add(string.Format("x000_PowerSnoId: {0}   x010_DurationInTicks:{1}", buff.x000_PowerSnoId, buff.x010_DurationInTicks));
            }

            if (Engine.Current.BuffManager.x30_Debuffs.Any())
                treeViewItem.Items.Add("x30_Debuffs List");
            foreach (var buff in Engine.Current.BuffManager.x30_Debuffs)
            {
                treeViewItem.Items.Add(string.Format("x000_PowerSnoId: {0}   x010_DurationInTicks:{1}", buff.x000_PowerSnoId, buff.x010_DurationInTicks));
            }


            this.treeView.Items.Add(treeViewItem);
        }

        private void LoadLocalData()
        {
            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = "LocalData";
            treeViewItem.Items.Add("x24_WorldPosX: " + Engine.Current.LocalData.x24_WorldPosX);
            treeViewItem.Items.Add("x28_WorldPosY: " + Engine.Current.LocalData.x28_WorldPosY);
            treeViewItem.Items.Add("x2C_WorldPosZ: " + Engine.Current.LocalData.x2C_WorldPosZ);
            treeViewItem.Items.Add("x00_IsActorCreated: " + Engine.Current.LocalData.x00_IsActorCreated);
            treeViewItem.Items.Add("x08_SceneSnoId: " + Engine.Current.LocalData.x08_SceneSnoId);
            treeViewItem.Items.Add("x0C_WorldSnoId: " + Engine.Current.LocalData.x0C_WorldSnoId);
            treeViewItem.Items.Add("x10_ActorSnoId: " + Engine.Current.LocalData.x10_ActorSnoId);
            treeViewItem.Items.Add("x14_ActId: " + Engine.Current.LocalData.x14_ActId);


            this.treeView.Items.Add(treeViewItem);
        }


    }
}
