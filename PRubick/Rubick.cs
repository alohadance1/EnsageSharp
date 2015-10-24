using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;

using SharpDX;
using SharpDX.Direct3D9;

using EZGUI;

namespace PRubick
{
    internal class Rubick
    {

        #region Fields

        private static string VERSION = "v2.0.0.2";
        private static bool menuOn = true;
        private static bool loaded = false;

        private static EzElement stealIfHave;
        private static EzElement enabled;
        private static EzElement lastSpell;

        private static EzGUI gui;
        private static EzElement spcat = null;
        private static EzElement heroes = null;

        private static Hero myHero = null;
        private static Ability spellSteal = null;
        private static int[] castRange = new int[] {
			1000, 1400
		};

        private static List<Hero> uiHeroes = new List<Hero>();
        private static List<Hero> lastSpellIsChecked = new List<Hero>();
        private static Dictionary<string, string> abilitiesFix = new Dictionary<string, string>();
        private static List<string> includedAbilities = new List<string>();

        #endregion

        #region Init

        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            
            // abilitiesFix
            abilitiesFix.Add("ancient_apparition_ice_blast_release", "ancient_apparition_ice_blast");
        }

        #endregion

        #region Update

        static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame) { loaded = false; return; }
            if (Game.GameState == GameState.Picking || Game.GameState == GameState.WaitingForLoaders || Game.GameState == GameState.Scoreboard) return;
            #region If assembly not loaded
            if (loaded == false)
            {
                gui = new EzGUI(Drawing.Width - 350, 60, "PRubick " + VERSION);
                enabled = new EzElement(ElementType.CHECKBOX, "Enabled / Активен", true);
                spcat = new EzElement(ElementType.CATEGORY, "Spell Steal / Кража скиллов", false);
                stealIfHave = new EzElement(ElementType.CHECKBOX, "Steal if no cd / Красть если нет кд [if selected]", false);
                lastSpell = new EzElement(ElementType.CHECKBOX, "Steal if last spell selected [unstable]", false);
                gui.AddMainElement(new EzElement(ElementType.TEXT, "Main / Главная", false));
                gui.AddMainElement(enabled);
                gui.AddMainElement(stealIfHave);
                gui.AddMainElement(lastSpell);
                gui.AddMainElement(new EzElement(ElementType.TEXT, "Rubick / Рубик (heroes appear gradually)", false));
                gui.AddMainElement(spcat);
                uiHeroes.Clear();
                myHero = ObjectMgr.LocalHero;
                spellSteal = myHero.Spellbook.SpellR;
                loaded = true;
            }
            #endregion
            if (myHero.ClassID != ClassID.CDOTA_Unit_Hero_Rubick) return;
            //
            if (enabled.isActive)
            {
                Hero[] enemies = ObjectMgr.GetEntities<Hero>().Where(x => x.Team != myHero.Team && !x.IsIllusion() && x.IsAlive && x.IsVisible ).ToArray();
                #region GUI Checks
                if (Utils.SleepCheck("GUI_ABILITIES") && heroes != null)
                {
                    foreach (EzElement hero in heroes.GetElements())
                    {
                        foreach (EzElement spell in hero.GetElements())
                        {
                            if (spell.isActive && !includedAbilities.Contains(spell.Content)) includedAbilities.Add(spell.Content);
                            if (!spell.isActive && includedAbilities.Contains(spell.Content)) includedAbilities.Remove(spell.Content);
                        }
                    }
                    Utils.Sleep(1000, "GUI_ABILITIES");
                }

                if (Utils.SleepCheck("uiheroesupdate"))
                {
                    if (heroes == null) { heroes = new EzElement(ElementType.CATEGORY, "Heroes / Герои", false); spcat.AddElement(heroes); }
                    List<Hero> _heroes = ObjectMgr.GetEntities<Hero>().Where(p => !p.IsIllusion && p.Team != myHero.Team).ToList();
                    foreach (Hero enemy in _heroes)
                    {
                        if (!uiHeroes.Contains(enemy))
                        {
                            var hero = new EzElement(ElementType.CATEGORY, enemy.Name.Replace("_", "").Replace("npcdotahero", ""), false);
                            foreach (Ability ability in enemy.Spellbook.Spells)
                            {
                                if (ability.AbilityBehavior == AbilityBehavior.Passive || ability.AbilityType == AbilityType.Attribute) continue;
                                bool ac = false;
                                if (ability.AbilityType == AbilityType.Ultimate) { ac = true; includedAbilities.Add(ability.Name); }
                                hero.AddElement(new EzElement(ElementType.CHECKBOX, ability.Name, ac));
                            }
                            heroes.AddElement(hero);
                            uiHeroes.Add(enemy);
                        }
                    }
                    Utils.Sleep(2000, "uiheroesupdate");
                }
                #endregion
                foreach (Hero enemy in enemies)
                {
                    if (Utils.SleepCheck(enemy.ClassID.ToString()))
                    {
                        foreach (Ability ability in enemy.Spellbook.Spells)
                        {
                            if ( (lastSpellIsChecked.Contains(enemy) && ( isCasted(ability) && !includedAbilities.Contains(ability.Name) )) && lastSpell.isActive ) lastSpellIsChecked.Remove(enemy);
                            if (includedAbilities.Contains(ability.Name) && ( isCasted(ability) || lastSpellIsChecked.Contains(enemy) ) && !spellOnCooldown(ability.Name) && iCanSteal(enemy) && myHero.Spellbook.SpellD.Name != ability.Name && ability.CooldownLength != 0)
                            {
                                if (stealIfHave.isActive == false && myHero.Spellbook.SpellD.Cooldown == 0 && includedAbilities.Contains(myHero.Spellbook.SpellD.Name)) continue;
                                if (spellSteal.CanBeCasted()) spellSteal.UseAbility(enemy);
                                else if (lastSpell.isActive) lastSpellIsChecked.Add(enemy); 
                            }
                        }
                        Utils.Sleep(125, enemy.ClassID.ToString());
                    }
                }
            }
        }

        #endregion

        #region Methods

        private static bool isCasted(Ability ability)
        {
            return (ability.CooldownLength - ability.Cooldown < (float)0.7 + (Game.Ping / 1000));
        }

        private static bool iCanSteal(Hero hero)
        {
            switch (myHero.AghanimState())
            {
                case true:
                    if (myHero.Distance2D(hero) <= castRange[1]) return true;
                    break;
                case false:
                    if (myHero.Distance2D(hero) <= castRange[0]) return true;
                    break;
            }
            return false;
        }

        private static bool spellOnCooldown(string abilityName)
        {
            if (abilitiesFix.ContainsKey(abilityName)) abilityName = abilitiesFix[abilityName];
            Ability[] Spells = myHero.Spellbook.Spells.ToArray();
            Ability[] SpellsF = Spells.Where(x => x.Name == abilityName).ToArray();
            if (SpellsF.Length > 0)
            {
                Ability SpellF = SpellsF.First();
                if (SpellF.Cooldown > 10) return true;
                return false;
            }
            else return false;
        }

        #endregion
    }


}