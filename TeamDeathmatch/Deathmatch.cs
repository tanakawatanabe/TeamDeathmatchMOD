using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using System.Xml.Linq;

namespace TeamDeathmatch
{
    public class Deathmatch : Script
    {
        //General
        Ped player = Game.Player.Character;
        bool isInMatch = false;
        float teamDistance = 100;
        public List<XDocument> locationSettings = new List<XDocument>();
        public XDocument currentLocation;
        int minPedAmmount = 20;
        public int matchPoint = 200;
        public bool spawnEnemyAsCops = false;
        public enum GameMode { TeamDeathmatch, Survival };
        public GameMode mode = GameMode.TeamDeathmatch;
        public List<dynamic> listofLocationNames = new List<dynamic>();
        public List<dynamic> listofWeaponsets = new List<dynamic>();
        public List<dynamic> listofMatchpoints = new List<dynamic>();

        //Survival
        int wave = 1;
        int currentWaveAmount = 20;
        int spawnedEnemies = 0;
        int wave1Amount = 20;
        int wave2_3Amount = 30;
        int wave3_5Amount = 40;

        //Team
        public Team team0 = new Team(); //Friendly
        public Team team1 = new Team(); //Enemy

        //Ped
        public List<string> pedNames = new List<string>();
        public List<int> pedHashkeyList = new List<int>();

        //Weapons
        List<string> weaponsType = new List<string>();
        public string currentWeaponsType = "Heavy";

        public WeaponHash[] weapons =
        {
        WeaponHash.CarbineRifle,
        WeaponHash.AssaultRifle,
        WeaponHash.AdvancedRifle,
        WeaponHash.BullpupRifle,
        WeaponHash.SpecialCarbine,
        WeaponHash.AssaultSMG,
        WeaponHash.CarbineRifleMk2,
        WeaponHash.SMG,
        WeaponHash.RPG,
        WeaponHash.MicroSMG,
        WeaponHash.PumpShotgun,
        WeaponHash.MarksmanRifle,
        WeaponHash.CombatMG,
        WeaponHash.HeavySniper,
        WeaponHash.MG
    };

        public WeaponHash[] weaponsNoHeavy =
        {
        WeaponHash.CarbineRifle,
        WeaponHash.AssaultRifle,
        WeaponHash.BullpupRifle,
        WeaponHash.AdvancedRifle,
        WeaponHash.SpecialCarbine,
        WeaponHash.AssaultSMG,
        WeaponHash.CarbineRifleMk2,
        WeaponHash.SMG,
        WeaponHash.MicroSMG,
        WeaponHash.PumpShotgun,
        WeaponHash.MarksmanRifle,
        WeaponHash.CombatMG,
        WeaponHash.HeavySniper,
        WeaponHash.MG
    };

        public WeaponHash[] WeaponsCQB =
        {
        WeaponHash.CarbineRifle,
        WeaponHash.AdvancedRifle,
        WeaponHash.SpecialCarbine,
        WeaponHash.SMG,
        WeaponHash.SMGMk2,
        WeaponHash.AssaultSMG,
        WeaponHash.MicroSMG,
        WeaponHash.PumpShotgun,
        WeaponHash.HeavyShotgun,
    };

        public WeaponHash[] WeaponsSmallArms =
        {
        WeaponHash.Pistol,
        WeaponHash.SNSPistol,
        WeaponHash.CombatPistol,
        WeaponHash.APPistol,
        WeaponHash.PumpShotgun,
        WeaponHash.MicroSMG,

    };

        //Settings
        XDocument xml;
        XElement configTable;
        XElement pedHashTable;
        Keys launchKey = Keys.F10;
        Keys showStatsKey = Keys.NumPad1;

        //Menu values
        MenuPool modMenuPool;
        UIMenu currentMenu;

        //Main menu
        UIMenu mainMenu;
        UIMenuItem maxHealth;
        UIMenuItem startMatchButton;
        UIMenuItem startSurvivalButton;
        UIMenuItem stopMatchButton;
        UIMenuItem teleport;
        UIMenuItem loadLocations;
        UIMenuItem saveCurrentPed;

        //Settings
        UIMenu settingMenu;
        UIMenuListItem locList;
        UIMenuListItem weaponsList;
        UIMenuListItem matchPointList;

        UIMenuListItem friendPedChoise;
        UIMenuListItem friendPed1Choise;
        UIMenuListItem enemyPedChoise;
        UIMenuListItem enemyPed1Choise;

        UIMenuCheckboxItem enemyGuntruckCheckbox;
        UIMenuCheckboxItem enemyTankCheckbox;
        UIMenuCheckboxItem enemyChopperCheckbox;
        UIMenuCheckboxItem enemyJuggernautCheckbox;

        UIMenuCheckboxItem friendlyGuntruckCheckbox;
        UIMenuCheckboxItem friendlyTankCheckbox;
        UIMenuCheckboxItem friendlyChopperCheckbox;
        UIMenuCheckboxItem friendlyJuggernautCheckbox;

        UIMenuCheckboxItem spawnAsCopToggle;

        List<Blip> friendPosBlips = new List<Blip>();
        List<Blip> enemyPosBlips = new List<Blip>();

        //Location Editor
        UIMenu locMenu;
        UIMenuItem friendlyPos;
        UIMenuItem enemyPos0;
        UIMenuItem enemyPos1;
        UIMenuItem enemyPos2;
        UIMenuItem enemyPos3;
        UIMenuItem carPos;
        UIMenuItem carDest;
        UIMenuItem heliPos;
        UIMenuItem heliDest;
        UIMenuItem friendCarPos;
        UIMenuItem friendCarDest;
        UIMenuItem friendHeliPos;
        UIMenuItem friendHeliDest;
        UIMenuItem saveLocation;

        Blip enemyCarPosBlip;
        Blip enemyCarDestBlip;
        Blip enemyChopperPosBlip;
        Blip enemyChopperDestBlip;
        Blip friendlyCarPosBlip;
        Blip friendlyCarDestBlip;
        Blip friendlyChopperPosBlip;
        Blip friendlyChopperDestBlip;

        public Deathmatch()
        {

            ReadSettings();
            LoadLocation();
            ReadCurrentLocation();
            TeamSetup();

            MainMenu();
            SettingMenu();
            LocationEditor();

            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        XDocument tes;

        void ReadSettings()
        {

            xml = XDocument.Load(@"scripts//TeamDeathmatch.xml");

            //Read Config
            configTable = xml.Element("CONFIG");
            foreach (var keys in configTable.Elements("KEYS"))
            {
                launchKey = (Keys)System.Enum.Parse(typeof(Keys), keys.Element("LAUNCHKEY").Value);
                showStatsKey = (Keys)System.Enum.Parse(typeof(Keys), keys.Element("SHOWSTATSKEY").Value);
            }

            //Read Pedhash
            pedHashTable = configTable.Element("PEDHASH");
            foreach (var ped in pedHashTable.Elements("PED"))
            {
                pedNames.Add(ped.Element("PEDNAME").Value);
                pedHashkeyList.Add(int.Parse(ped.Element("ID").Value));
                
            }

        }

        void TeamSetup()
        {
            team0.ChangeName("Friendly");
            team1.ChangeName("Enemy");

            team0.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "PLAYER");
            team1.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");

            team0.EnemyTeam = team1;
            team1.EnemyTeam = team0;

            team0.weaponSet = weapons;
            team1.weaponSet = weapons;

            team0.Ped0 = (PedHash)pedHashkeyList[0].GetHashCode();
            team0.Ped1 = (PedHash)pedHashkeyList[0].GetHashCode();
            team1.Ped0 = (PedHash)pedHashkeyList[1].GetHashCode();
            team1.Ped1 = (PedHash)pedHashkeyList[1].GetHashCode();
        }

        public List<dynamic> listOfPeds = new List<dynamic>();



        public void SaveCurrentPed()
        {
            int currentPedHashKey = Game.Player.Character.Model.GetHashCode();
            PedHash currentPedHash = (PedHash)Game.Player.Character.Model.Hash;
            string saveName = currentPedHash.ToString();

            Ped[] nearbypeds = World.GetNearbyPeds(Game.Player.Character.Position, 1);
            foreach (Ped ped in nearbypeds)
            {
                if (ped.Model.Hash != Game.Player.Character.Model.Hash)
                {
                    currentPedHashKey = nearbypeds[0].Model.GetHashCode();
                    currentPedHash = (PedHash)nearbypeds[0].Model.Hash;
                    saveName = currentPedHash.ToString();

                }
            }

            //pedNames.Add(saveName);
            //pedHashkeyList.Add(currentPedHashKey);
            //listOfPeds.Add(saveName);

            saveName = Game.GetUserInput(WindowTitle.PM_NAME_CHALL, 20);

            //Write
            var p = new XElement("PED",
                new XElement("PEDNAME", saveName),
                new XElement("ID", currentPedHashKey)
            );
            pedHashTable.Add(p);
            xml.Save(@"scripts//TeamDeathmatch.xml");

            UI.ShowSubtitle("Saved " + saveName + ". Available from next loading.");
        }


        

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == launchKey)
            {
                currentMenu.Visible = !currentMenu.Visible;
            }

            if (e.KeyCode == showStatsKey)
            {
                if (!showText)
                    showText = true;
                else
                    showText = false;
            }

            else return;

        }

        bool showText = false;

        private void OnTick(object sender, EventArgs e)
        {
            modMenuPool.ProcessMenus();


            if (showText)
                ShowStats();

            if (isInMatch)
                MatchControl();
        }

        void ShowStats()
        {
            int x = (int)Math.Round((double)UI.WIDTH / 2);
            int y = (int)Math.Round((double)UI.HEIGHT / 9.5);

            UIText text;
            text = new UIText(
                "Friendly:" + team0.members.Count +
                " Enemy:" + team1.members.Count +
                " Kills:" + team0.kills +
                " Deaths:" + team1.kills,
                //"\n" +

                //" Guntruck:" + team1.isGuntruckInField +
                //" Tank:" + team1.isTankInField +
                //" Chopper:" + team1.isChopperInField +
                //" Juggernaut:" + team1.isJuggernautInField +
                //"\n" +

                //" Guntruck:" + team0.isGuntruckInField +
                //" Tank:" + team0.isTankInField +
                //" Chopper:" + team0.isChopperInField +
                //" Juggernaut:" + team0.isJuggernautInField +
                //"\n" +

                //" InWave:" + isInWave +
                //" Wave:" + wave,
                new Point(x, y), 1f, Color.White, GTA.Font.ChaletComprimeCologne, true);
            text.Draw();
        }

        void MatchControl()
        {
            if (mode == GameMode.TeamDeathmatch)
            {
                CheckMatch();
                team0.CheckMembers();
                team1.Support();
                team0.Support();
            }
            else
            {
                SurvivalControl();
            }

            team1.CheckMembers();

            team1.TankControl();
            team1.GuntruckControl();
            team1.ChopperControl();
            team1.JuggernautControl();

            team0.TankControl();
            team0.GuntruckControl();
            team0.ChopperControl();
            team0.JuggernautControl();

            team1.AddEnemy(Game.Player.Character);

            //if (!team0.members.Contains(Game.Player.Character) && Game.Player.IsAlive)
            //    team0.members.Add(Game.Player.Character);
        }

        void CheckMatch()
        {
            if (team0.kills >= matchPoint || team1.kills >= matchPoint)
            {

                team0.Matched();
                team1.Matched();

                if (team0.members.Count == 0 || team1.members.Count == 0)
                {

                    if (team0.kills > team1.kills)
                    {
                        UI.ShowSubtitle("You WON! " + team0.kills + " - " + team1.kills, 10000);
                        Game.Player.WantedLevel = 0;
                        StopMatch();
                        return;
                    }
                    else
                    {
                        UI.ShowSubtitle("You LOST!" + team0.kills + " - " + team1.kills, 10000);
                        StopMatch();
                        return;
                    }
                }
            }
            else
            {
                spawnPedTimer++;
                if (spawnPedTimer > 30)
                {
                    spawnPedTimer = 0;
                    if (team0.members.Count < minPedAmmount)
                        team0.SpawnMember(1);
                    if (team1.members.Count < minPedAmmount)
                        team1.SpawnMember(1);
                }
            }
        }

        bool isInWave = false;
        int waveTimer = 100;

        void StartWave()
        {

            waveTimer = 0;
            isInWave = true;
            UI.Notify("Wave " + wave.ToString() + " started.");

            switch (wave)
            {
                case 1:
                currentWaveAmount = wave1Amount;
                    break;
                case 2:
                    currentWaveAmount = wave2_3Amount;
                    if (team1.enableGuntruck)
                        team1.SpawnGuntruck();
                    break;
                case 3:
                    currentWaveAmount = wave2_3Amount;
                    if (team1.enableTank)
                        team1.SpawnTank();
                    break;
                case 4:
                    currentWaveAmount = wave3_5Amount;
                    if (team1.enableChopper)
                        team1.SpawnChopper();
                    break;
                default:
                    currentWaveAmount = wave3_5Amount;
                    SpawnSupportRandomly();
                    break;
            }
        }

        enum Support
        {
            Guntruck,
            Chopper,
            Tank,
            Juggernaut,
        }

        void SpawnSupportRandomly()
        {
            List<Support> supportList = new List<Support>();

            if (team1.enableGuntruck)
            {
                supportList.Add(Support.Guntruck);
            }
            if (team1.enableTank)
            {
                supportList.Add(Support.Tank);
            }
            if (team1.enableChopper)
            {
                supportList.Add(Support.Chopper);
            }
            if (team1.enableJuggernaut)
            {
                supportList.Add(Support.Juggernaut);
            }

            Random rnd = new System.Random();
            Support selectedSupport = supportList[rnd.Next(supportList.Count)];

            switch (selectedSupport)
            {
                case Support.Guntruck:
                    team1.SpawnGuntruck();
                    break;
                case Support.Tank:
                    team1.SpawnTank();
                    break;
                case Support.Chopper:
                    team1.SpawnChopper();
                    break;
                case Support.Juggernaut:
                    team1.SpawnJuggernaut();
                    break;
                default:
                    break;
            }
            
            
        }

        void SurvivalControl()
        {
            if (Game.Player.IsAlive == false)
            {
                UI.ShowSubtitle("You survived " + wave + " waves, killed " + team0.kills + " enemies.", 10000);
                StopMatch();
            }

            if (!isInWave)
            {
                waveTimer++;

                //Start wave
                if (waveTimer > 600)
                    StartWave();
            }
            else
                InWave();
        }

        void InWave()
        {
            //In wave
            spawnPedTimer++;
            if (spawnPedTimer > 30 && spawnedEnemies < currentWaveAmount)
            {
                spawnPedTimer = 0;
                team1.SpawnMember(1);
                spawnedEnemies++;
            }

            //Clear wave
            if (spawnedEnemies >= currentWaveAmount && team1.members.Count == 0)
            {
                isInWave = false;
                UI.Notify("Wave " + wave.ToString() + " finished.");
                spawnedEnemies = 0;
                wave++;
            }
        }

        int spawnPedTimer = 0;

        Vector3 ReadPosition(string positionName)
        {
            float x = float.Parse(currentLocation.Element("LOCATION").Element(positionName).Attribute("X").Value);
            float y = float.Parse(currentLocation.Element("LOCATION").Element(positionName).Attribute("Y").Value);
            float z = float.Parse(currentLocation.Element("LOCATION").Element(positionName).Attribute("Z").Value);

            return new Vector3(x, y, z);
        }

        public void WritePosition(XElement table, string positionName, Vector3 pos)
        {

            XElement newElement = new XElement(positionName);
            XAttribute newX = new XAttribute("X", pos.X.ToString());
            XAttribute newY = new XAttribute("Y", pos.Y.ToString());
            XAttribute newZ = new XAttribute("Z", pos.Z.ToString());
            newElement.Add(newX);
            newElement.Add(newY);
            newElement.Add(newZ);
            table.Add(newElement);

        }


        string[] fileNames;
        string path = @"scripts\Locations\";

        public static string RevoveChars(string s, string[] removes)
        {
            System.Text.StringBuilder buf = new System.Text.StringBuilder(s);
            foreach (string rm in removes)
            {
                buf.Replace(rm, "");
            }
            return buf.ToString();
        }

        public void LoadLocation()
        {
            fileNames = Directory.GetFiles(path);



            //Load locationSettings
            locationSettings.Clear();
            listofLocationNames.Clear();

            for (int i = 0; i < fileNames.Length; i++)
            {
                locationSettings.Add(XDocument.Load(fileNames[i]));
            }
            currentLocation = locationSettings[0];

            string[] removeStrings = new string[2];
            removeStrings[0] = @"scripts\Locations\";
            removeStrings[1] = ".xml";
            for (int i = 0; i < fileNames.Length; i++)
            {
                listofLocationNames.Add(RevoveChars(fileNames[i], removeStrings));
            }
            UI.Notify(locationSettings.Count + " locations available.");
        }

        public void ReadCurrentLocation()
        {
            //Load locationSettings

            team0.spawnPositions.Clear();
            team1.spawnPositions.Clear();

            team0.spawnPositions.Add(ReadPosition("FRIENDPOS"));
            team1.spawnPositions.Add(ReadPosition("ENEMYPOS0"));
            team1.spawnPositions.Add(ReadPosition("ENEMYPOS1"));
            team1.spawnPositions.Add(ReadPosition("ENEMYPOS2"));
            team1.spawnPositions.Add(ReadPosition("ENEMYPOS3"));

            team1.vehSpawn = ReadPosition("VEHICLESPAWNS");
            team1.vehDest = ReadPosition("VEHICLEGOES");
            team1.chopperPos = ReadPosition("CHOPPERSPAWNS");
            team1.chopperDest = ReadPosition("CHOPPERGOES");

            team0.vehSpawn = ReadPosition("FRIENDVEHSPAWNS");
            team0.vehDest = ReadPosition("FRIENDVEHGOES");
            team0.chopperPos = ReadPosition("FRIENDCHOPPERSPAWNS");
            team0.chopperDest = ReadPosition("FRIENDCHOPPERGOES");

        }

        public void StartMatch()
        {
            if (!isInMatch)
            {

                team0.StartMatch();
                team1.StartMatch();

                isInMatch = true;

                World.CreatePickup(PickupType.Armour, team0.spawnPositions[0].Around(1), new Model("prop_armour_pickup"), 100);

                if (mode == GameMode.TeamDeathmatch)
                {
                    //Game.Player.Character.Position = team0.spawnPositions[0];
                }
                else
                {
                    wave = 1;
                    currentWaveAmount = wave1Amount;
                }

                UI.Notify("Match Started.");

            }

        }

        public void StopMatch()
        {
            if (isInMatch)
            {
                isInMatch = false;
                teamDistance = 100;

                isInWave = false;
                waveTimer = 0;
                wave = 1;
                spawnedEnemies = 0;

                team0.StopMatch();
                team1.StopMatch();

                Ped[] nearbypeds = World.GetNearbyPeds(team0.spawnPositions[0], 2000);
                for (int i = 0; i < nearbypeds.Length; i++)
                {
                    if (nearbypeds[i].IsDead)
                    {
                        nearbypeds[i].MarkAsNoLongerNeeded();
                    }
                }

                UI.Notify("Match Stopped.");
            }

        }


        void MainMenu()
        {
            modMenuPool = new MenuPool();
            mainMenu = new UIMenu("Mod Menu", "SELECT AN OPTION");
            modMenuPool.Add(mainMenu);
            currentMenu = mainMenu;

            startMatchButton = new UIMenuItem("Start Team Deathmatch");
            mainMenu.AddItem(startMatchButton);

            startSurvivalButton = new UIMenuItem("Start Survival");
            mainMenu.AddItem(startSurvivalButton);

            stopMatchButton = new UIMenuItem("Stop Match");
            mainMenu.AddItem(stopMatchButton);

            teleport = new UIMenuItem("Teleport to Friendly Spawn Point");
            mainMenu.AddItem(teleport);

            //loadLocations = new UIMenuItem("Load Locations");
            //mainMenu.AddItem(loadLocations);



            maxHealth = new UIMenuItem("Max Health & Armor");
            mainMenu.AddItem(maxHealth);

            saveCurrentPed = new UIMenuItem("Save current ped");
            mainMenu.AddItem(saveCurrentPed);

            mainMenu.OnItemSelect += OnMainMenuItemSelect;
            mainMenu.OnCheckboxChange += OnMainMenuCheckboxChange;
            mainMenu.OnListChange += OnMainMenuListChange;
            mainMenu.OnMenuChange += OnMenuChanged;
        }


        void OnMenuChanged(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            currentMenu = newMenu;
            if (newMenu == locMenu || newMenu == settingMenu)
                CreateBlips();
        }

        void CreateBlips()
        {
            for (int i = 0; i < team0.spawnPositions.Count; i++)
            {
                friendPosBlips.Add(World.CreateBlip(team0.spawnPositions[i]));
                friendPosBlips[i].Sprite = BlipSprite.Standard;
                if (i == 0)
                    friendPosBlips[i].Scale = 1.3f;
                friendPosBlips[i].Color = BlipColor.Blue;
                friendPosBlips[i].Name = "Friendly Position " + i;
            }

            for (int i = 0; i < team1.spawnPositions.Count; i++)
            {
                enemyPosBlips.Add(World.CreateBlip(team1.spawnPositions[i]));
                enemyPosBlips[i].Sprite = BlipSprite.Standard;
                if (i == 0)
                    enemyPosBlips[i].Scale = 1.3f;
                enemyPosBlips[i].Color = BlipColor.Red;
                enemyPosBlips[i].Name = "Enemy Position " + i;
            }

            enemyCarPosBlip = World.CreateBlip(team1.vehSpawn);
            enemyCarPosBlip.Sprite = BlipSprite.Standard;
            enemyCarPosBlip.Color = BlipColor.Yellow;
            enemyCarPosBlip.Name = "Enemy Vehicle Spawns";

            enemyCarDestBlip = World.CreateBlip(team1.vehDest);
            enemyCarDestBlip.Sprite = BlipSprite.Standard;
            enemyCarDestBlip.Color = BlipColor.Yellow;
            enemyCarDestBlip.Name = "Enemy Vehicle Destination";

            enemyChopperPosBlip = World.CreateBlip(team1.chopperPos);
            enemyChopperPosBlip.Sprite = BlipSprite.Standard;
            enemyChopperPosBlip.Color = BlipColor.Yellow;
            enemyChopperPosBlip.Name = "Enemy Chopper Spawns";

            enemyChopperDestBlip = World.CreateBlip(team1.chopperDest);
            enemyChopperDestBlip.Sprite = BlipSprite.Standard;
            enemyChopperDestBlip.Color = BlipColor.Yellow;
            enemyChopperDestBlip.Name = "Enemy Chopper Destination";

            friendlyCarPosBlip = World.CreateBlip(team0.vehSpawn);
            friendlyCarPosBlip.Sprite = BlipSprite.Standard;
            friendlyCarPosBlip.Color = BlipColor.Green;
            friendlyCarPosBlip.Name = "Friendly Vehicle Spawns";

            friendlyCarDestBlip = World.CreateBlip(team0.vehDest);
            friendlyCarDestBlip.Sprite = BlipSprite.Standard;
            friendlyCarDestBlip.Color = BlipColor.Green;
            friendlyCarDestBlip.Name = "Friendly Vehicle Destination";

            friendlyChopperPosBlip = World.CreateBlip(team0.chopperPos);
            friendlyChopperPosBlip.Sprite = BlipSprite.Standard;
            friendlyChopperPosBlip.Color = BlipColor.Green;
            friendlyChopperPosBlip.Name = "Friendly Chopper Spawns";

            friendlyChopperDestBlip = World.CreateBlip(team0.chopperDest);
            friendlyChopperDestBlip.Sprite = BlipSprite.Standard;
            friendlyChopperDestBlip.Color = BlipColor.Green;
            friendlyChopperDestBlip.Name = "Friendly Chopper Destination";
        }

        void DeleteBlips()
        {
            foreach(Blip blip in friendPosBlips)
                blip.Remove();
            friendPosBlips.Clear();

            foreach (Blip blip in enemyPosBlips)
                blip.Remove();
            enemyPosBlips.Clear();

            enemyCarPosBlip.Remove();

            enemyCarDestBlip.Remove();

            enemyChopperPosBlip.Remove();

            enemyChopperDestBlip.Remove();

            friendlyCarPosBlip.Remove();

            friendlyCarDestBlip.Remove();

            friendlyChopperPosBlip.Remove();

            friendlyChopperDestBlip.Remove();
        }

        void OnMainMenuItemSelect(UIMenu sender, UIMenuItem item, int index)
        {

            int listIndex = locList.Index;

            if (item == maxHealth)
            {
                Game.Player.Character.Health = 100;
                Game.Player.Character.Armor = 100;
            }
            if (item == loadLocations)
                LoadLocation();
            if (item == startMatchButton)
            {
                mode = Deathmatch.GameMode.TeamDeathmatch;
                StartMatch();
            }
            if (item == startSurvivalButton)
            {
                mode = Deathmatch.GameMode.Survival;
                StartMatch();
            }
            if (item == stopMatchButton)
                StopMatch();
            if (item == teleport)
                Game.Player.Character.Position = team0.spawnPositions[0];
            if (item == saveCurrentPed)
            {
                SaveCurrentPed();
            }

        }

        void OnMainMenuCheckboxChange(UIMenu sender, UIMenuItem item, bool checked_)
        {

        }

        void OnMainMenuListChange(UIMenu sender, UIMenuItem item, int index)
        {

        }

        public void SettingMenu()
        {
            settingMenu = modMenuPool.AddSubMenu(mainMenu, "Settings");

            locList = new UIMenuListItem("Location: ", listofLocationNames, 0);
            settingMenu.AddItem(locList);


            //Peds
            for (int i = 0; i < pedNames.Count; i++)
                listOfPeds.Add(pedNames[i]);

            friendPedChoise = new UIMenuListItem("Friendly: ", listOfPeds, 0);
            friendPed1Choise = new UIMenuListItem("Friendly: ", listOfPeds, 0);
            settingMenu.AddItem(friendPedChoise);
            settingMenu.AddItem(friendPed1Choise);

            enemyPedChoise = new UIMenuListItem("Enemy: ", listOfPeds, 1);
            enemyPed1Choise = new UIMenuListItem("Enemy: ", listOfPeds, 1);
            settingMenu.AddItem(enemyPedChoise);
            settingMenu.AddItem(enemyPed1Choise);


            //Weapons
            listofWeaponsets.Add("Heavy");
            listofWeaponsets.Add("NoRPG");
            listofWeaponsets.Add("CQB");
            listofWeaponsets.Add("Small");
            weaponsList = new UIMenuListItem("Weapons: ", listofWeaponsets, 0);
            settingMenu.AddItem(weaponsList);


            //Match Point
            listofMatchpoints.Add(50);
            listofMatchpoints.Add(100);
            listofMatchpoints.Add(200);
            listofMatchpoints.Add(300);
            listofMatchpoints.Add(500);
            matchPointList = new UIMenuListItem("MatchPoint: ", listofMatchpoints, 2);
            settingMenu.AddItem(matchPointList);


            //Enemy support
            enemyGuntruckCheckbox = new UIMenuCheckboxItem("Enemy Guntruck", false);
            settingMenu.AddItem(enemyGuntruckCheckbox);

            enemyTankCheckbox = new UIMenuCheckboxItem("Enemy Tank", false);
            settingMenu.AddItem(enemyTankCheckbox);

            enemyChopperCheckbox = new UIMenuCheckboxItem("Enemy Chopper", false);
            settingMenu.AddItem(enemyChopperCheckbox);

            enemyJuggernautCheckbox = new UIMenuCheckboxItem("Enemy Juggernaut", false);
            settingMenu.AddItem(enemyJuggernautCheckbox);


            //Friendly support
            friendlyGuntruckCheckbox = new UIMenuCheckboxItem("Friendly Guntruck", false);
            settingMenu.AddItem(friendlyGuntruckCheckbox);

            friendlyTankCheckbox = new UIMenuCheckboxItem("Friendly Tank", false);
            settingMenu.AddItem(friendlyTankCheckbox);

            friendlyChopperCheckbox = new UIMenuCheckboxItem("Friendly Chopper", false);
            settingMenu.AddItem(friendlyChopperCheckbox);

            friendlyJuggernautCheckbox = new UIMenuCheckboxItem("Friendly Juggernaut", false);
            settingMenu.AddItem(friendlyJuggernautCheckbox);

            spawnAsCopToggle = new UIMenuCheckboxItem("Spawn Enemy As Cop", false);
            settingMenu.AddItem(spawnAsCopToggle);


            settingMenu.OnItemSelect += OnSettingMenuItemSelect;
            settingMenu.OnCheckboxChange += OnSettingMenuCheckboxChange;
            settingMenu.OnListChange += OnSettingMenuListChange;
            settingMenu.OnMenuChange += OnMenuChanged;
            settingMenu.OnMenuClose += SettingMenu_OnMenuClose;
        }

        private void SettingMenu_OnMenuClose(UIMenu sender)
        {
            DeleteBlips();
        }

        void OnSettingMenuItemSelect(UIMenu sender, UIMenuItem item, int index)
        {

        }

        void OnSettingMenuCheckboxChange(UIMenu sender, UIMenuItem item, bool checked_)
        {
            if (item == enemyGuntruckCheckbox)
                team1.enableGuntruck = checked_;
            if (item == enemyTankCheckbox)
                team1.enableTank = checked_;
            if (item == enemyChopperCheckbox)
                team1.enableChopper = checked_;
            if (item == enemyJuggernautCheckbox)
                team1.enableJuggernaut = checked_;

            if (item == friendlyGuntruckCheckbox)
                team0.enableGuntruck = checked_;
            if (item == friendlyTankCheckbox)
                team0.enableTank = checked_;
            if (item == friendlyChopperCheckbox)
                team0.enableChopper = checked_;
            if (item == friendlyJuggernautCheckbox)
                team0.enableJuggernaut = checked_;

            if (item == spawnAsCopToggle)
            {
                if (checked_)
                    team1.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "COPS");
                else
                    team1.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");

            }
        }

        void OnSettingMenuListChange(UIMenu sender, UIMenuItem item, int index)
        {
            if (item == locList)
                OnLocationEditorOnListChange(sender, item, index);
            if (item == friendPedChoise)
                team0.Ped0 = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == friendPed1Choise)
                team0.Ped1 = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == enemyPedChoise)
                team1.Ped0 = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == enemyPed1Choise)
                team1.Ped1 = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == weaponsList)
            {
                currentWeaponsType = listofWeaponsets[index];
                WeaponHash[] selected = weapons;
                switch (index)
                {
                    case 0:
                        selected = weapons;
                        break;
                    case 1:
                        selected = weaponsNoHeavy;
                        break;
                    case 2:
                        selected = WeaponsCQB;
                        break;
                    case 3:
                        selected = WeaponsSmallArms;
                        break;
                    default:
                        break;
                }
                team0.weaponSet = selected;
                team1.weaponSet = selected;
            }
            if (item == matchPointList)
                matchPoint = listofMatchpoints[index];
        }

        public void LocationEditor()
        {
            locMenu = modMenuPool.AddSubMenu(mainMenu, "Location Editor");
            locMenu.AddItem(locList);



            friendlyPos = new UIMenuItem("Friendly Spawn Position");
            locMenu.AddItem(friendlyPos);

            enemyPos0 = new UIMenuItem("Enemy Spawn Position 0");
            locMenu.AddItem(enemyPos0);
            enemyPos1 = new UIMenuItem("Enemy Spawn Position 1");
            locMenu.AddItem(enemyPos1);
            enemyPos2 = new UIMenuItem("Enemy Spawn Position 2");
            locMenu.AddItem(enemyPos2);
            enemyPos3 = new UIMenuItem("Enemy Spawn Position 3");
            locMenu.AddItem(enemyPos3);

            carPos = new UIMenuItem("Enemy Ground Vehicle Spawn");
            locMenu.AddItem(carPos);
            carDest = new UIMenuItem("Enemy Ground Vehicle Goto");
            locMenu.AddItem(carDest);
            heliPos = new UIMenuItem("Enemy Chopper Spawn");
            locMenu.AddItem(heliPos);
            heliDest = new UIMenuItem("Enemy Chopper Goto");
            locMenu.AddItem(heliDest);

            friendCarPos = new UIMenuItem("Friendly Ground Vehicle Spawn");
            locMenu.AddItem(friendCarPos);
            friendCarDest = new UIMenuItem("Friendly Ground Vehicle Goto");
            locMenu.AddItem(friendCarDest);
            friendHeliPos = new UIMenuItem("Friendly Chopper Spawn");
            locMenu.AddItem(friendHeliPos);
            friendHeliDest = new UIMenuItem("Friendly Chopper Goto");
            locMenu.AddItem(friendHeliDest);

            saveLocation = new UIMenuItem("Save This Location");
            locMenu.AddItem(saveLocation);

            locMenu.OnItemSelect += OnLocationEditorItemSelect;
            locMenu.OnListChange += OnLocationEditorOnListChange;
            locMenu.OnMenuChange += OnMenuChanged;
            locMenu.OnMenuClose += LocMenu_OnMenuClose;
        }

        private void LocMenu_OnMenuClose(UIMenu sender)
        {
            DeleteBlips();
            //throw new NotImplementedException();
        }

        void OnLocationEditorOnListChange(UIMenu sender, UIMenuItem item, int index)
        {

            currentLocation = locationSettings[index];
            ReadCurrentLocation();

            for (int i = 0; i < team0.spawnPositions.Count; i++)
            {
                friendPosBlips[i].Position = team0.spawnPositions[i];
            }

            for (int i = 0; i < team1.spawnPositions.Count; i++)
            {
                enemyPosBlips[i].Position = team1.spawnPositions[i];
            }

            enemyCarPosBlip.Position = team1.vehSpawn;
            enemyCarDestBlip.Position = team1.vehDest;
            enemyChopperPosBlip.Position = team1.chopperPos;
            enemyChopperDestBlip.Position = team1.chopperDest;

            friendlyCarPosBlip.Position = team0.vehSpawn;
            friendlyCarDestBlip.Position = team0.vehDest;
            friendlyChopperPosBlip.Position = team0.chopperPos;
            friendlyChopperDestBlip.Position = team0.chopperDest;

        }

        void OnLocationEditorItemSelect(UIMenu sender, UIMenuItem item, int index)
        {
            int listIndex = locList.Index;
            XDocument currentEditing = locationSettings[listIndex];
            Vector3 newPos = Game.Player.Character.Position;

            if (item == friendlyPos)
            {
                friendPosBlips[0].Position = newPos;
            }
            if (item == enemyPos0)
            {
                enemyPosBlips[0].Position = newPos;
            }
            if (item == enemyPos1)
            {
                enemyPosBlips[1].Position = newPos;

            }
            if (item == enemyPos2)
            {
                enemyPosBlips[2].Position = newPos;

            }
            if (item == enemyPos3)
            {
                enemyPosBlips[3].Position = newPos;
            }
            if (item == carPos)
            {
                enemyCarPosBlip.Position = newPos;
            }
            if (item == carDest)
            {
                enemyCarDestBlip.Position = newPos;
            }
            if (item == heliPos)
            {
                enemyChopperPosBlip.Position = newPos;
            }
            if (item == heliDest)
            {
                enemyChopperDestBlip.Position = newPos;
            }
            if (item == friendCarPos)
            {
                friendlyCarPosBlip.Position = newPos;
            }
            if (item == friendCarDest)
            {
                friendlyCarDestBlip.Position = newPos;
            }
            if (item == friendHeliPos)
            {
                friendlyChopperPosBlip.Position = newPos;
            }
            if (item == friendHeliDest)
            {
                friendlyChopperDestBlip.Position = newPos;
            }
            if (item == saveLocation)
            {
                //currentEditing.Save();
                SaveLocation();

                //UI.Notify("Location saved.");
            }
        }

        void SaveLocation()
        {

            string saveName = Game.GetUserInput(WindowTitle.PM_NAME_CHALL, listofLocationNames[locList.Index], 20);

            XDocument newXml = new XDocument();
            XElement locationTable = new XElement("LOCATION");

            //Write
            WritePosition(locationTable, "FRIENDPOS", friendPosBlips[0].Position);
            WritePosition(locationTable, "ENEMYPOS0", enemyPosBlips[0].Position);
            WritePosition(locationTable, "ENEMYPOS1", enemyPosBlips[1].Position);
            WritePosition(locationTable, "ENEMYPOS2", enemyPosBlips[2].Position);
            WritePosition(locationTable, "ENEMYPOS3", enemyPosBlips[3].Position);
            WritePosition(locationTable, "VEHICLESPAWNS", enemyCarPosBlip.Position);
            WritePosition(locationTable, "VEHICLEGOES", enemyCarDestBlip.Position);
            WritePosition(locationTable, "CHOPPERSPAWNS", enemyChopperPosBlip.Position);
            WritePosition(locationTable, "CHOPPERGOES", enemyChopperDestBlip.Position);
            WritePosition(locationTable, "FRIENDVEHSPAWNS", friendlyCarPosBlip.Position);
            WritePosition(locationTable, "FRIENDVEHGOES", friendlyCarDestBlip.Position);
            WritePosition(locationTable, "FRIENDCHOPPERSPAWNS", friendlyChopperPosBlip.Position);
            WritePosition(locationTable, "FRIENDCHOPPERGOES", friendlyChopperDestBlip.Position);

            newXml.Add(locationTable);

            newXml.Save(@"scripts//Locations//" + saveName + ".xml");

            UI.ShowSubtitle("Saved " + saveName + ". Available from next loading.");

        }
    }
}


