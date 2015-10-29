using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;

using SharpDX;
using SharpDX.Direct3D9;


namespace Kikabuz
{
    internal class Kikabuz
    {
        #region Fields

        private static Hero myHero;

        private static string[] gItems = new string[] { "item_arcane_boots", "item_soul_ring", "item_bottle", "item_magic_stick", "item_magic_wand" };

        private static int[] queue = new int[] { 0, 0, 0, 0 };

        private static List<Item> droppedUItems = new List<Item>();
        private static List<Item> droppedItems = new List<Item>();

        #endregion

        #region Init

        public static void Init()
        {
            Game.OnWndProc += Game_OnWndProc;
            Game.OnUpdate += Game_OnUpdate;
        }

        #endregion

        #region Update

        static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame) { myHero = null; return; }
            if (myHero == null) { myHero = ObjectMgr.LocalHero; }
            CheckQueue();
        }

        #endregion

        #region Wnd

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsInGame) { myHero = null; return; }
            if (myHero == null) myHero = ObjectMgr.LocalHero;
            if (!Game.IsChatOpen)
            {
                switch (args.Msg)
                {
                    case (uint)Utils.WindowsMessages.WM_KEYUP:
                        switch (args.WParam)
                        {
                            case 'B':
                                GoAbuze();
                                break;
                        }
                        break;
                }
            }
        }

        #endregion

        #region Methods
        private static void GoAbuze()
        {
            var uItems = myHero.Inventory.Items.Where(x => gItems.Contains(x.Name));
            if (uItems.Any())
            {
                DropItems();
                foreach (Item item in uItems)
                {
                    if (item.CanBeCasted())
                    {
                        switch (item.Name)
                        {
                            case "item_bottle":
                                queue[0] += 3;
                                break;
                            case "item_arcane_boots":
                                droppedUItems.Add(item);
                                myHero.DropItem(item, myHero.Position);
                                queue[3]++;
                                break;
                            case "item_magic_stick":
                                droppedUItems.Add(item);
                                myHero.DropItem(item, myHero.Position);
                                queue[3]++;
                                break;
                            case "item_magic_wand":
                                droppedUItems.Add(item);
                                myHero.DropItem(item, myHero.Position);
                                queue[3]++;
                                break;
                            case "item_soul_ring":
                                queue[1] += 1;
                                break;
                            default:
                                item.UseAbility();
                                break;
                        }
                    }
                }
                if (DurWPing(0) == 0) Utils.Sleep(300, "abuze");
                else Utils.Sleep(DurWPing(500), "abuze");
                queue[2]++;
            }
        }

        private static void CheckQueue()
        {
            if (queue[0] == 0 && queue[1] == 0 && queue[2] == 0) return;
            if (queue[0] > 0)
            {
                if (Utils.SleepCheck("bottle"))
                {
                    myHero.FindItem(gItems[2]).UseAbility();
                    queue[0]--;
                    Utils.Sleep(DurWPing(3000), "bottle");
                }
            }
            if (Utils.SleepCheck("bottle"))
            {
                if (queue[0] == 0 && queue[1] > 0)
                {
                    myHero.FindItem(gItems[1]).UseAbility();
                    queue[1]--;
                }
                if (queue[0] == 0 && queue[1] == 0 && queue[2] > 0 && Utils.SleepCheck("abuze"))
                {
                    PickupUItems();
                    PickupItems();
                    queue[2]--;
                }
            }
        }

        private static float DurWPing(float num)
        {
            return num+(Game.Ping / 1000);
        }

        private static void DropItems()
        {
            var pItems = myHero.Inventory.Items.Where(x => !gItems.Contains(x.Name));
            foreach (Item item in pItems)
            {
                droppedItems.Add(item);
                myHero.DropItem(item, myHero.Position);
            }
        }

        private static void PickupUItems()
        {
            PhysicalItem[] items = ObjectMgr.GetEntities<PhysicalItem>().Where(x => x.Distance2D(myHero.Position) < 50).ToArray();
            foreach (PhysicalItem physicalItem in items)
            {
                if (droppedUItems.Contains(physicalItem.Item))
                {
                    myHero.PickUpItem(physicalItem);
                    physicalItem.Item.UseAbility();
                    droppedUItems.Remove(physicalItem.Item);
                }
            }
        }

        private static void PickupItems()
        {
            PhysicalItem[] items = ObjectMgr.GetEntities<PhysicalItem>().Where(x => x.Distance2D(myHero.Position) < 50).ToArray();
            foreach (PhysicalItem physicalItem in items)
            {
                if (droppedItems.Contains(physicalItem.Item))
                {
                    myHero.PickUpItem(physicalItem);
                    droppedItems.Remove(physicalItem.Item);
                }
            }
        }

        #endregion
    }
}
