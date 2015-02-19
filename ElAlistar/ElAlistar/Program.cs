using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace ElAlistar
{
    internal class Program
    {
        private static String hero = "Alistar";
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _w, _e, _r;
        private static SpellSlot _ignite;

        #region Main

        private static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        #endregion

        #region Gameloaded 

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (!Player.ChampionName.Equals(hero, StringComparison.CurrentCultureIgnoreCase))
                return;

            AddNotification("ElAlistar by jQuery v1.0.0.6");
            AddNotification("Do you like mexican because I'll wrap you in my arms and make you my baerito.");

            #region Spell Data

            // set spells
            _q = new Spell(SpellSlot.Q, 365);
            _w = new Spell(SpellSlot.W, 650);
            _e = new Spell(SpellSlot.E, 575);
            _r = new Spell(SpellSlot.R, 0);

            // init ignite
            _ignite = Player.GetSpellSlot("summonerdot");

            #endregion

            //subscribe to event
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            try
            {
                InitializeMenu();
            }
            catch (Exception ex) {}
        }


        #endregion

        #region OnGameUpdate

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (_menu.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }

            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            if (Interrupter2.IsCastingInterruptableSpell(target) &&
                Interrupter2.GetInterruptableTargetData(target).DangerLevel == Interrupter2.DangerLevel.High &&
                target.IsValidTarget(_w.Range))
            {
                _w.Cast();
            }

            SelfHealing();
            HealAlly();
        }

        #endregion

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel != Interrupter2.DangerLevel.High || sender.Distance(ObjectManager.Player) > _w.Range)
            {
                return;
            }

            if (sender.IsValidTarget(_w.Range) && args.DangerLevel == Interrupter2.DangerLevel.High && _q.IsReady())
            {
                _q.Cast();
                _q.Cast(ObjectManager.Player);
            }
            else if (sender.IsValidTarget(_w.Range) && args.DangerLevel == Interrupter2.DangerLevel.High && _w.IsReady() &&
                     !_q.IsReady())
            {
                _w.Cast();
                _w.Cast(ObjectManager.Player);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsValidTarget(_w.Range))
            {
                return;
            }

            if (gapcloser.Sender.Distance(ObjectManager.Player) > _w.Range)
            {
                return;
            }

            if (gapcloser.Sender.IsValidTarget(_w.Range))
            {
                if (_menu.Item("Interrupt").GetValue<bool>() && _w.IsReady())
                {
                    _w.Cast(ObjectManager.Player);
                    _w.Cast(gapcloser.Sender);
                }

                if (_menu.Item("Interrupt").GetValue<bool>() && !_w.IsReady())
                {
                    _q.Cast(ObjectManager.Player);
                    _q.Cast(gapcloser.Sender);
                }
            }
        }

        #region Harass

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            if (_menu.Item("HarassQ").GetValue<bool>() && _q.IsReady())
            {
                _q.CastOnUnit(target);
            }

            /*var turrets = (from tower in ObjectManager.Get<Obj_Turret>()
                           where tower.IsAlly && !tower.IsDead && target.Distance(tower.Position) < 1500 && tower.Health > 0
                           select tower).ToList();

            if (turrets.Any())
            {
                _w.CastOnUnit(target);
                Console.WriteLine("Can be in tower");
            }*/
        }

        #endregion

        #region Combo

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            // Check mana before combo
            SpellDataInst Qmana = Player.Spellbook.GetSpell(SpellSlot.Q);
            SpellDataInst Wmana = Player.Spellbook.GetSpell(SpellSlot.W);

            if (_q.IsReady() && _w.IsReady() && Qmana.ManaCost + Wmana.ManaCost <= Player.Mana)
            {
                _w.CastOnUnit(target);
                var comboTime = Math.Max(0, Player.Distance(target) - 500) * 10 / 25 + 25;
                Utility.DelayAction.Add((int) comboTime, () => _q.Cast());
            }


            // if killable with just W
            if (!_q.IsReady() && _w.IsReady() && _w.IsKillable(target, 1) &&
                ObjectManager.Player.Distance(target, false) < _w.Range + target.BoundingRadius)
            {
                _w.CastOnUnit(target, true);
            }

            if (_menu.Item("SelfHeal").GetValue<bool>() &&
                (Player.Health / Player.MaxHealth) * 100 <= _menu.Item("SelfHperc").GetValue<Slider>().Value &&
                _e.IsReady())
            {
                _e.Cast(Player);
            }

            if (Player.CountEnemiesInRange(_w.Range) >= _menu.Item("rcount").GetValue<Slider>().Value &&
                _menu.Item("RCombo").GetValue<bool>() &&
                (Player.Health / Player.MaxHealth) * 100 <= _menu.Item("UltHP").GetValue<Slider>().Value)
            {
                _r.Cast();
            }

            // if w is on CD and in Q range cast q
            if (_q.IsReady() && !_w.IsReady())
            {
                _q.CastOnUnit(target);
            }

            //ignite when killable
            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health &&
                _menu.Item("UseIgnite").GetValue<bool>())
            {
                Player.Spellbook.CastSpell(_ignite, target);
            }
        }

        #endregion

        #region SelfHealing

        private static void SelfHealing()
        {
            SpellDataInst Emana = Player.Spellbook.GetSpell(SpellSlot.E);
            
            if (Player.HasBuff("Recall") || Utility.InFountain(Player)) return;
            if (_menu.Item("SelfHeal").GetValue<bool>() &&
                (Player.Health / Player.MaxHealth) * 100 <= _menu.Item("SelfHperc").GetValue<Slider>().Value && Player.ManaPercentage() >= _menu.Item("minmanaE").GetValue<Slider>().Value  &&
                _e.IsReady())
            {
                _e.Cast(Player);
            }
        }

        #endregion

        #region HealAlly

        private static void HealAlly()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
            {
                if (Player.HasBuff("Recall") || Utility.InFountain(Player)) return;
                if (_menu.Item("HealAlly").GetValue<bool>() &&
                    (hero.Health / hero.MaxHealth) * 100 <= _menu.Item("HealAllyHP").GetValue<Slider>().Value && Player.ManaPercentage() >= _menu.Item("minmanaE").GetValue<Slider>().Value &&
                    _e.IsReady() &&
                    hero.Distance(Player.ServerPosition) <= _e.Range)
                    _e.Cast(hero);
            }
        }

        #endregion

        #region GetComboDamage   

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (_q.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (_w.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (_ignite != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(_ignite) == SpellState.Ready)
            {
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            }

            return (float) damage;
        }

        #endregion

        #region Ignite

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (_ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(_ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float) Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        #endregion

        #region Menu Config

        private static void InitializeMenu()
        {
            _menu = new Menu("ElAlistar", hero, true);

            //Orbwalker
            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);

            //TargetSelector
            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);
            _menu.AddSubMenu(targetSelector);

            //Combo
            var comboMenu = _menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("QCombo", "[Combo] Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("WCombo", "[Combo] Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("RCombo", "[Combo] Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("UltHP", "Ult when health >= ").SetValue(new Slider(50, 1, 100)));
            comboMenu.AddItem(new MenuItem("rcount", "Use ult in combo if enemies >= ")).SetValue(new Slider(2, 1, 5));
            comboMenu.AddItem(new MenuItem("UseIgnite", "Use Ignite in combo when killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            //Harass
            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "H"));
            harassMenu.AddItem(new MenuItem("HarassQ", "[Harass] Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            //self healing
            var healMenu = _menu.AddSubMenu(new Menu("Heal settings", "SH"));
            healMenu.AddItem(new MenuItem("SelfHeal", "Auto heal yourself").SetValue(true));
            healMenu.AddItem(new MenuItem("SelfHperc", "Self heal at >= ").SetValue(new Slider(25, 1, 100)));

            healMenu.AddItem(new MenuItem("HealAlly", "Auto heal ally's").SetValue(true));
            healMenu.AddItem(new MenuItem("HealAllyHP", "Heal ally at >= ").SetValue(new Slider(25, 1, 100)));
            healMenu.AddItem(new MenuItem("minmanaE", "Min % mana for heal")).SetValue(new Slider(55));

            //Misc

            var miscMenu = _menu.AddSubMenu(new Menu("Drawings", "Misc"));
            miscMenu.AddItem(new MenuItem("Drawingsoff", "[Drawing] Drawings off").SetValue(false));
            miscMenu.AddItem(new MenuItem("DrawQ", "[Drawing] Draw Q").SetValue(true));
            miscMenu.AddItem(new MenuItem("DrawW", "[Drawing] Draw W").SetValue(true));
            miscMenu.AddItem(new MenuItem("DrawE", "[Drawing] Draw E").SetValue(true));


            //Interupt
            var interruptMenu = _menu.AddSubMenu(new Menu("Interrupt", "I"));
            interruptMenu.AddItem(new MenuItem("Interrupt", "Interrupt spells").SetValue(true));

            //nigga who made this
            var credits = _menu.AddSubMenu(new Menu("Credits", "jQuery"));
            credits.AddItem(new MenuItem("Thanks", "By jQuery"));

            _menu.AddToMainMenu();
        }

        #endregion

        #region Drawings

        private static void Drawing_OnDraw(EventArgs args)
        {

            if (_menu.Item("Drawingsoff").GetValue<bool>())
                return;

            if (_menu.Item("DrawQ").GetValue<bool>())
                if (_q.Level > 0)
                    Utility.DrawCircle(Player.Position, _q.Range, _q.IsReady() ? Color.Green : Color.Red);

            if (_menu.Item("DrawW").GetValue<bool>())
                if (_w.Level > 0)
                    Utility.DrawCircle(Player.Position, _w.Range, _w.IsReady() ? Color.Green : Color.Red);

            if (_menu.Item("DrawE").GetValue<bool>())
                if (_e.Level > 0)
                    Utility.DrawCircle(Player.Position, _e.Range, _e.IsReady() ? Color.Green : Color.Red);
        }

        #endregion

        #region L33tIsGod

        /* maybe I should create a new class for this instead of writting it all down in the same damn file, yes maybe. But NO I WONT. */
        public static void AddNotification(String text)
        {
            var notification = new Notification(text, 10000);
            Notifications.AddNotification(notification);
        }

        #endregion
    }
}