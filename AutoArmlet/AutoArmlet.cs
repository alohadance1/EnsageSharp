using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common.Extensions;
using Ensage.Common;
using Ensage.Heroes;
using Ensage.Items;

using SharpDX;
using SharpDX.Direct3D9;

using EZGUI;

namespace AutoArmlet
{
    class AutoArmlet
    {
        #region Fields
        private static string VERSION = "v1.0.0.0";

        private static EzGUI ui;
        private static EzElement enabled;
        private static EzElement enabledAuto;

        private static EzElement keys;
        private static Dictionary<int, EzElement> keyDic = new Dictionary<int, EzElement>();

        private static bool loaded = false;

        private static Hero myHero;
        private static Item armlet;
        #endregion

        #region Init
        public static void Init()
        {
            ui = new EzGUI(Drawing.Width - 350, 60, "AutoArmlet " + VERSION);
            enabled = new EzElement(ElementType.CHECKBOX, "Enabled / Активен", true);
            enabledAuto = new EzElement(ElementType.CHECKBOX, "Auto Toggle / Авто армлет [without key]", false);
            keys = new EzElement(ElementType.CATEGORY, "Key / Клавиша", false);
            for (int i = 65; (i >= 65 && i <= 91); i++ )
            {
                if (i == 91) i = 32;
                EzElement element = new EzElement(ElementType.CHECKBOX, ((char)i).ToString(), false);
                if (i == 32) element.Content = "SPACE";
                if ((char)i == 'F') element.isActive = true;
                keys.AddElement(element);
                keyDic.Add(i, element);
            }
            ui.AddMainElement(enabled);
            ui.AddMainElement(enabledAuto);
            ui.AddMainElement(new EzElement(ElementType.TEXT, "Other / Прочее", true));
            ui.AddMainElement(keys);
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
        }
        #endregion

        #region WND
        static void Game_OnWndProc(WndEventArgs args)
        {
            if (Game.IsInGame && enabled.isActive == true)
            {
                switch (args.Msg)
                {
                    case (uint)Utils.WindowsMessages.WM_KEYDOWN:
                        int keyNum = (int)args.WParam;
                        if (!keyDic.ContainsKey(keyNum)) return;
                        EzElement key = keyDic[keyNum];
                        if (key != null && key.isActive) ArmletToggle(false);
                        break;
                }
            }
        }
        #endregion

        #region Update
        static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame || enabled.isActive == false) 
            {
                if (loaded == true) Console.WriteLine("AutoArmlet succesfully unloaded.");
                loaded = false; 
                return; 
            }

            if (loaded == false)
            {
                myHero = ObjectMgr.LocalHero;
                loaded = true;
                Console.WriteLine("AutoArmlet succesfully loaded.");
            }

            if (myHero != null && Utils.SleepCheck("armlet_finding"))
            {
                if (!myHero.Inventory.Items.Contains(armlet)) armlet = myHero.FindItem("item_armlet");
                Utils.Sleep(500, "armlet_finding");
            }

            if (armlet == null) return;

            if (Utils.SleepCheck("auto_armlet") && CanToggleArmlet() && enabledAuto.isActive && myHero.Health < 475)
            {
                switch(armlet.IsToggled)
                { 
                    case false:
                        ArmletToggle(true);
                        break;
                    case true:
                        if (myHero.Health < ( 475 * ( 1.6 / 2 ) ) ) ArmletToggle(true);
                        break;
                }
                Utils.Sleep(200, "auto_armlet");
            }
        }
        #endregion

        #region Methods
        private static void ArmletToggle(bool isAuto)
        {
            if (!CanToggleArmlet()) return;
            if (isAuto && !Utils.SleepCheck("armlet_toggle")) return;
            switch(armlet.IsToggled)
            { 
                case true:
                    for (int i = 1 ; i <= 2; i++ )
                        armlet.ToggleAbility();
                    break;
                case false:
                    armlet.ToggleAbility();
                    break;
            }
            Utils.Sleep(900, "armlet_toggle");
        }

        private static bool CanToggleArmlet()
        {
            return !(armlet == null || myHero.IsStunned() || myHero.IsAlive == false);
        }
        #endregion
    }
}
