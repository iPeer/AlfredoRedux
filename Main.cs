using AlfredoRedux.Extensions;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlfredoRedux
{
    public class Main : Plugin
    {

        private InitializationFile Settings;
        private string WeaponPool = string.Empty;
        private bool ApplyOnGoOnDuty;
        private bool CleanPlayer;
        private bool FillHealth;
        private bool FillArmour;
        private bool InfiniteAmmo;
        private bool RepairVehicle;
        private bool CleanVehicle;
        private bool OnlyRepairPoliceVehicles;
        private bool GiveFlashlight;

        Keys Keybind;

        private short AmmoCount;

        private bool IS_ON_DUTY = false;

        GameFiber CheckThread;

        public override void Initialize()
        {
            Settings = new InitializationFile(@"Plugins\LSPDFR\AlfredoRedux.ini");
            WeaponPool = Settings.ReadString("WEAPONS", "WeaponList", "WEAPON_PISTOL|COMPONENT_AT_PI_FLSH,WEAPON_CARBINERIFLE|COMPONENT_AT_AR_FLSH|COMPONENT_AT_AR_AFGRIP,WEAPON_STUNGUN,WEAPON_PUMPSHOTGUN,WEAPON_FLASHLIGHT,WEAPON_NIGHTSTICK,WEAPON_FIREEXTINGUISHER");
            ApplyOnGoOnDuty = Settings.ReadBoolean("WEAPONS", "RunWhenGoingOnDuty", true);
            CleanPlayer = Settings.ReadBoolean("PLAYER", "CleanPlayer", true);
            FillHealth = Settings.ReadBoolean("PLAYER", "FillHealth", true);
            FillArmour = Settings.ReadBoolean("PLAYER", "FillArmour", true);
            InfiniteAmmo = Settings.ReadBoolean("AMMO", "InfiniteAmmo", false);
            RepairVehicle = Settings.ReadBoolean("VEHICLES", "RepairVehicle", true);
            CleanVehicle = Settings.ReadBoolean("VEHICLES", "CleanVehicle", false);
            GiveFlashlight = Settings.ReadBoolean("WEAPONS", "GiveFlashlight", true);
            OnlyRepairPoliceVehicles = Settings.ReadBoolean("VEHICLES", "OnlyRepairPoliceVehicles", true);
            AmmoCount = Settings.ReadInt16("AMMO", "AmmoCount", 1000);
            KeysConverter kc = new KeysConverter();
            Keybind = (Keys)kc.ConvertFromString(Settings.ReadString("GENERAL", "Keybind", "F7"));

            Functions.OnOnDutyStateChanged += OnOnDutyStateChanged;

            CheckThread = new GameFiber(DoMagicThread);

            Game.DisplayNotification("~g~AlfredoRedux~w~ loaded!");

        }

        private void OnOnDutyStateChanged(bool onDuty)
        {
            IS_ON_DUTY = onDuty;
            if (onDuty)
            {
                CheckThread.Resume();
                if (ApplyOnGoOnDuty)
                    DoMagic();
            }
        }

        private void DoMagic()
        {

            Player player = Game.LocalPlayer;
            Ped pc = player.Character;
            if (CleanPlayer)
                pc.ClearBlood();
            if (FillHealth)
                pc.Health = pc.MaxHealth;
            if (FillArmour)
                pc.Armor = 100;

            if ((!OnlyRepairPoliceVehicles && pc.IsInAnyVehicle(false)) || (OnlyRepairPoliceVehicles && pc.IsInAnyPoliceVehicle)) 
            {
                if (RepairVehicle)
                    pc.CurrentVehicle.Repair();
                if (CleanVehicle)
                    pc.CurrentVehicle.DirtLevel = 0f;
            }

            // Add weapons

            pc.Inventory.Weapons.Clear();
            if (GiveFlashlight)
                pc.Inventory.GiveFlashlight();

            foreach (string s in WeaponPool.Split(','))
            {
                WeaponAsset wep = new WeaponAsset();
                string[] weaponData = s.Split('|');
                for (int x = 0; x < weaponData.Length; x++)
                {
                    string w = weaponData[x];
                    /*
                     * WEAPON_UNARMED WEAPON_ANIMAL WEAPON_COUGAR WEAPON_KNIFE WEAPON_NIGHTSTICK WEAPON_HAMMER WEAPON_BAT WEAPON_GOLFCLUB WEAPON_CROWBAR WEAPON_PISTOL WEAPON_COMBATPISTOL WEAPON_APPISTOL WEAPON_PISTOL50 
                     * WEAPON_MICROSMG WEAPON_SMG WEAPON_ASSAULTSMG WEAPON_ASSAULTRIFLE WEAPON_CARBINERIFLE WEAPON_ADVANCEDRIFLE WEAPON_MG WEAPON_COMBATMG WEAPON_PUMPSHOTGUN WEAPON_SAWNOFFSHOTGUN WEAPON_ASSAULTSHOTGUN 
                     * WEAPON_BULLPUPSHOTGUN WEAPON_STUNGUN WEAPON_SNIPERRIFLE WEAPON_HEAVYSNIPER WEAPON_REMOTESNIPER WEAPON_GRENADELAUNCHER WEAPON_GRENADELAUNCHER_SMOKE WEAPON_RPG WEAPON_PASSENGER_ROCKET 
                     * WEAPON_AIRSTRIKE_ROCKET WEAPON_STINGER WEAPON_MINIGUN WEAPON_GRENADE WEAPON_STICKYBOMB WEAPON_SMOKEGRENADE WEAPON_BZGAS WEAPON_MOLOTOV WEAPON_FIREEXTINGUISHER WEAPON_PETROLCAN WEAPON_DIGISCANNER 
                     * WEAPON_BRIEFCASE WEAPON_BRIEFCASE_02 WEAPON_BALL WEAPON_FLARE WEAPON_VEHICLE_ROCKET WEAPON_BARBED_WIRE WEAPON_DROWNING WEAPON_DROWNING_IN_VEHICLE WEAPON_BLEEDING WEAPON_ELECTRIC_FENCE 
                     * WEAPON_EXPLOSION WEAPON_FALL WEAPON_EXHAUSTION WEAPON_HIT_BY_WATER_CANNON WEAPON_RAMMED_BY_CAR WEAPON_RUN_OVER_BY_CAR WEAPON_HELI_CRASH WEAPON_FIRE WEAPON_ANIMAL_RETRIEVER WEAPON_SMALL_DOG 
                     * WEAPON_TIGER_SHARK WEAPON_HAMMERHEAD_SHARK WEAPON_KILLER_WHALE WEAPON_BOAR WEAPON_PIG WEAPON_COYOTE WEAPON_DEER WEAPON_HEN WEAPON_RABBIT WEAPON_CAT WEAPON_COW WEAPON_BIRD_CRAP
                     */
                    if (x == 0)
                    {
                        Game.Console.Print($"Attempting to add weapon {w} to player");
                        wep = new WeaponAsset(w);
#if DEBUG
                        Game.Console.Print($"{wep.Hash} // {wep.IsValid} // {wep.IsLoaded}");
#endif
                        if (!wep.IsValid) continue;
                        if (!wep.IsLoaded)
                            wep.LoadAndWait();
                        WeaponDescriptor wd = new WeaponDescriptor(wep);
                        pc.Inventory.GiveNewWeapon(wep, (InfiniteAmmo ? short.MaxValue : /*(short)(wd.MagazineSize * (short)15)*/AmmoCount), false);
                        continue;
                    }
                    Game.Console.Print($"Attempting to add component {w} to weapon");
                    pc.Inventory.AddComponentToWeapon(wep, w);
                }
            }

            Game.DisplayNotification("AlfredoRedux has completed tasks.");

        }

        private void DoMagicThread()
        {
            do
            {
                if (Game.IsKeyDown(Keybind))
                {

                    DoMagic();
                    GameFiber.Sleep(250);

                }
                GameFiber.Yield();
            } while (IS_ON_DUTY);
        }

        public override void Finally()
        {
            if (CheckThread.IsRunning())
                CheckThread.Abort();
        }

    }
}
