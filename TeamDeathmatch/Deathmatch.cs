using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

public class Deathmatch : Script
{
    //General values
    Ped player = Game.Player.Character;
    bool isWave = false;
    int waveAmount = 20;
    float teamDistance = 100;
    List<int> deadpeds = new List<int>();
    List<ScriptSettings> locations = new List<ScriptSettings>();
    ScriptSettings currentLocation;
    int minPedAmmount = 20;
    int matchPoint = 200;
    bool spawnEnemyAsCops = false;

    //Friendly values
    PedHash friendPed = PedHash.Marine03SMY;
    PedHash friendPed1 = PedHash.Marine03SMY;
    int EnemyRelationShipGroup;
    int friendlyKills = 0;
    Vector3 friendPos = new Vector3(-197.735f, -642.9965f, 48.65916f);
    List<Ped> friendPeds = new List<Ped>();
    List<Vector3> friendlyPositions = new List<Vector3>();

    //Enemy values
    PedHash enemyPed = PedHash.Blackops03SMY;
    PedHash enemyPed1 = PedHash.Blackops03SMY;
    int freindRelationShipGroup;
    int enemyKills = 0;
    Vector3 enemyPos = new Vector3(-255.067f, -848.71F, 31.2456F);
    List<Ped> enemyPeds = new List<Ped>();
    List<Vector3> enemyPositions = new List<Vector3>();

    //Ped variations
    List <string> pedNames = new List<string>();
    List<int> pedHashkeyList = new List<int>();

    WeaponHash[] weaponset;

    

    //Weapon variations
    List<string> weaponsType = new List<string>();
    string currentWeaponsType = "Heavy";

    WeaponHash[] weapons =
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

    WeaponHash[] weaponsNoHeavy =
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

    WeaponHash[] WeaponsCQB =
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

    bool enableHeavyWeapons = true;
    bool enableCQB = true;

    //Guntruck values
    bool isGuntruckAvaileble = true;
    bool isGuntruckInField = false;
    bool carArrived = false;
    Vector3 vehSpawn;
    Vector3 vehDest;
    Ped enemyDriver;
    Ped enemyGunner;
    Vehicle ins;
    Vehicle guntruckWreck;
    float timeSinceGuntruckSpawned = 0;

    //Chopper values
    bool isChopperAvailable = true;
    bool isChopperInField = false;
    bool chopperArrived = false;
    Vector3 chopperPos;
    Vector3 chopperDest;
    Ped pilot;
    Ped chopperGunner;
    Ped heliGunner;
    Ped heliGunner1;
    Vehicle chopper;
    Vehicle chopperWreck;
    float timeSinceChopperSpawned = 0;

    //Tank values
    bool isGroundSupportAvailable = true;
    bool isTankInField = false;
    bool tankArrived = false;
    Ped tankDriver;
    Vehicle tank;
    Vehicle tankWreck;
    float timeSinceTankSpawned = 0;

    //Juggernaut values
    bool isJuggernautInField = false;
    bool juggernautArrived = false;
    PedHash juggernaut = PedHash.Juggernaut01M;
    Ped jug;

    //Vehicle support values 
    bool vehicleSupport = true;
    int timeSinceVehicleCalled = 0;
    int vehicleSupportInterval = 500;
    int timeSinceFired = 0;
    enum supportVehicles { Insurgent, Valkyrie, Tank };
    supportVehicles vehicle;


    //Menu values
    MenuPool modMenuPool;
    UIMenu mainMenu;
    UIMenuItem maxHealth;
    UIMenuItem startMatchButton;
    UIMenuItem stopMatchButton;
    UIMenuItem teleport;
    UIMenuItem loadLocations;
    UIMenuListItem locList;
    UIMenuListItem weaponsList;
    UIMenuListItem matchPointList;
    UIMenuItem saveCurrentPed;
    UIMenuCheckboxItem enableHeavyToggle;
    UIMenuCheckboxItem enableCQBToggle;
    UIMenuCheckboxItem vehicleSupportCheckbox;
    UIMenuCheckboxItem spawnAsCopToggle;
    ScriptSettings setting;


    public Deathmatch()
    {
        setting = ScriptSettings.Load(@"scripts//TeamDeathmatch.ini");

        string[] pedvalues = setting.GetAllValues("PEDNAME", "PEDNAME");

        for(int i=0; i < pedvalues.Length; i++)
        {
            pedNames.Add(pedvalues[i]);
            pedHashkeyList.Add(setting.GetValue<int>("PEDHASH", pedvalues[i], 452351020));
        }


        LoadLocation();
        ReadCurrentLocation();
        Setup();
        WeaponSelectorMenu();
        LocationEditor();


        Tick += OnTick;
        KeyDown += OnKeyDown;
        freindRelationShipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "PLAYER");
        EnemyRelationShipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
    }

    List<dynamic> listOfPeds = new List<dynamic>();

    void Setup()
    {
        modMenuPool = new MenuPool();
        mainMenu = new UIMenu("Mod Menu", "SELECT AN OPTION");
        modMenuPool.Add(mainMenu);

        for (int i = 0; i < pedNames.Count; i++)
        {
            listOfPeds.Add(pedNames[i]);
        }

        listofWeaponsets.Add("Heavy");
        listofWeaponsets.Add("NoRPG");
        listofWeaponsets.Add("CQB");

        listofMatchpoints.Add(50);
        listofMatchpoints.Add(100);
        listofMatchpoints.Add(200);
        listofMatchpoints.Add(300);
        listofMatchpoints.Add(500);

        UIMenuListItem friendPedChoise = new UIMenuListItem("Friendly: ", listOfPeds, 0);
        UIMenuListItem friendPed1Choise = new UIMenuListItem("Friendly: ", listOfPeds, 0);
        mainMenu.AddItem(friendPedChoise);
        mainMenu.AddItem(friendPed1Choise);
        //friendPed = listOfPeds[0];
        //friendPed1 = listOfPeds[0];

        UIMenuListItem enemyPedChoise = new UIMenuListItem("Enemy: ", listOfPeds, 1);
        UIMenuListItem enemyPed1Choise = new UIMenuListItem("Enemy: ", listOfPeds, 1);
        mainMenu.AddItem(enemyPedChoise);
        mainMenu.AddItem(enemyPed1Choise);
        //enemyPed = listOfPeds[1];
        //enemyPed = listOfPeds[1];

        saveCurrentPed = new UIMenuItem("Save current ped");
        mainMenu.AddItem(saveCurrentPed);


        startMatchButton = new UIMenuItem("Start Match");
        mainMenu.AddItem(startMatchButton);

        stopMatchButton = new UIMenuItem("Stop Match");
        mainMenu.AddItem(stopMatchButton);

        teleport = new UIMenuItem("Teleport to Friendly Spawn Point");
        mainMenu.AddItem(teleport);

        loadLocations = new UIMenuItem("Load Locations");
        mainMenu.AddItem(loadLocations);

        //enableHeavyToggle = new UIMenuCheckboxItem("Enable RPGs", true);
        //mainMenu.AddItem(enableHeavyToggle);

        //enableCQBToggle = new UIMenuCheckboxItem("Enable CQB", true);
        //mainMenu.AddItem(enableCQBToggle);

        vehicleSupportCheckbox = new UIMenuCheckboxItem("Enable Enemy Vehicle Support", true);
        mainMenu.AddItem(vehicleSupportCheckbox);

        spawnAsCopToggle = new UIMenuCheckboxItem("Spawn Enemy As Cop", false);
        mainMenu.AddItem(spawnAsCopToggle);

        locList = new UIMenuListItem("Location: ", listoflocations, 0);
        mainMenu.AddItem(locList);

        weaponsList = new UIMenuListItem("Weapons: ", listofWeaponsets, 0);
        mainMenu.AddItem(weaponsList);

        matchPointList = new UIMenuListItem("MatchPoint: ", listofMatchpoints, 2);
        mainMenu.AddItem(matchPointList);
        //matchPoint = listofMatchpoints[2];

        maxHealth = new UIMenuItem("Max health");
        mainMenu.AddItem(maxHealth);

        mainMenu.OnItemSelect += onMainMenuItemSelect;

        mainMenu.OnCheckboxChange += (sender, item, checked_) =>
        {
            if (item == enableHeavyToggle)
                enableHeavyWeapons = checked_;
            if (item == enableCQBToggle)
                enableCQB = checked_;
            if (item == vehicleSupportCheckbox)
                vehicleSupport = checked_;
            if(item == spawnAsCopToggle)
            {
                if (checked_)
                    EnemyRelationShipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "COPS");
                else
                    EnemyRelationShipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");

            }
        };

        mainMenu.OnListChange += (sender, item, index) =>
        {
            if(item == locList)
            {
                currentLocation = locations[index];
                ReadCurrentLocation();
            }
            if (item == friendPedChoise)
                friendPed = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == friendPed1Choise)
                friendPed1 = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == enemyPedChoise)
                enemyPed = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == enemyPed1Choise)
                enemyPed1 = (PedHash)pedHashkeyList[index].GetHashCode();
            if (item == weaponsList)
                currentWeaponsType = listofWeaponsets[index];
            if (item == matchPointList)
                matchPoint = listofMatchpoints[index];

        };

    }


    void onMainMenuItemSelect(UIMenu sender, UIMenuItem item, int index)
    {

        int listIndex = locList.Index;

        if (item == maxHealth)
            Game.Player.Character.Health = 100;
        if (item == loadLocations)
            LoadLocation();
        if (item == startMatchButton)
            StartMatch();
        if (item == stopMatchButton)
            StopMatch();
        if(item == teleport)
            Game.Player.Character.Position = friendlyPositions[0];
        if (item == saveCurrentPed)
        {
            SaveCurrentPed();
        }

    }

    void SaveCurrentPed()
    {
        int currentPedHashKey = Game.Player.Character.Model.GetHashCode();
        PedHash currentPedHash = (PedHash)Game.Player.Character.Model.Hash;
        string saveName = currentPedHash.ToString();

        Ped[] nearbypeds = World.GetNearbyPeds(Game.Player.Character.Position, 1);
        foreach (Ped ped in nearbypeds)
        {
            if (ped.Model.Hash != player.Model.Hash)
            {
                currentPedHashKey = nearbypeds[0].Model.GetHashCode();
                currentPedHash = (PedHash)nearbypeds[0].Model.Hash;
                saveName = currentPedHash.ToString();

            }
        }

        
        setting.SetValue<string>("PEDNAME", "PEDNAME//" + pedNames.Count, saveName);
        setting.SetValue<int>("PEDHASH", saveName, currentPedHashKey);
        setting.Save();

        pedNames.Add(saveName);
        pedHashkeyList.Add(currentPedHashKey);
        listOfPeds.Add(saveName);

        UI.ShowSubtitle("Saved "+ saveName+ " "+ currentPedHashKey, 2000);
    }

    void WeaponSelectorMenu()
    {
        UIMenu submenu = modMenuPool.AddSubMenu(mainMenu, "Weapon Selector Menu");

        List<dynamic> listOfWeapons = new List<dynamic>();
        WeaponHash[] allWeaponHashes = (WeaponHash[])Enum.GetValues(typeof(WeaponHash));
        for (int i = 0; i < allWeaponHashes.Length; i++)
        {
            listOfWeapons.Add(allWeaponHashes[i]);
        }

        UIMenuListItem list = new UIMenuListItem("Weapons: ", listOfWeapons, 0);
        submenu.AddItem(list);

        UIMenuItem getWeapon = new UIMenuItem("Get Weapon");
        submenu.AddItem(getWeapon);

        submenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == getWeapon)
            {
                int listIndex = list.Index;
                WeaponHash currentHash = allWeaponHashes[listIndex];
                Game.Player.Character.Weapons.Give(currentHash, 9999, true, true);
            }
        };
    }
    List<dynamic> listoflocations = new List<dynamic>();
    List<dynamic> listofWeaponsets= new List<dynamic>();
    List<dynamic> listofMatchpoints = new List<dynamic>();
    List<Blip> friendPosBlips = new List<Blip>();
    List<Blip> enemyPosBlips = new List<Blip>();
    Blip enemyCarPosBlip;
    Blip enemyCarDestBlip;
    Blip enemyChopperPosBlip;
    Blip enemyChopperDestBlip;

    void LocationEditor()
    {
        UIMenu locMenu = modMenuPool.AddSubMenu(mainMenu, "Location Editor");
        locMenu.AddItem(locList);

        for (int i = 0; i < friendlyPositions.Count; i++)
        {
            friendPosBlips.Add(World.CreateBlip(friendlyPositions[i]));
            friendPosBlips[i].Sprite = BlipSprite.Standard;
            if (i == 0)
                friendPosBlips[i].Scale = 1.3f;
            friendPosBlips[i].Color = BlipColor.Blue;
            friendPosBlips[i].Name = "Friendly Position "+i;
        }

        for (int i = 0; i < enemyPositions.Count; i++)
        {
            enemyPosBlips.Add(World.CreateBlip(enemyPositions[i]));
            enemyPosBlips[i].Sprite = BlipSprite.Standard;
            if (i == 0)
                enemyPosBlips[i].Scale = 1.3f;
            enemyPosBlips[i].Color = BlipColor.Red;
            enemyPosBlips[i].Name = "Enemy Position "+i;
        }

        enemyCarPosBlip = World.CreateBlip(vehSpawn);
        enemyCarPosBlip.Sprite = BlipSprite.Standard;
        enemyCarPosBlip.Color = BlipColor.Yellow;
        enemyCarPosBlip.Name = "Enemy Position";

        enemyCarDestBlip = World.CreateBlip(vehDest);
        enemyCarDestBlip.Sprite = BlipSprite.Standard;
        enemyCarDestBlip.Color = BlipColor.Yellow;
        enemyCarDestBlip.Name = "Enemy Position";

        enemyChopperPosBlip = World.CreateBlip(chopperPos);
        enemyChopperPosBlip.Sprite = BlipSprite.Standard;
        enemyChopperPosBlip.Color = BlipColor.Yellow;
        enemyChopperPosBlip.Name = "Enemy Position";

        enemyChopperDestBlip = World.CreateBlip(chopperDest);
        enemyChopperDestBlip.Sprite = BlipSprite.Standard;
        enemyChopperDestBlip.Color = BlipColor.Yellow;
        enemyChopperDestBlip.Name = "Enemy Position";

        UIMenuItem friendlyPos = new UIMenuItem("Friendly Spawn Position");
        locMenu.AddItem(friendlyPos);
        UIMenuItem enemyPos0 = new UIMenuItem("Enemy Spawn Position 0");
        locMenu.AddItem(enemyPos0);
        UIMenuItem enemyPos1 = new UIMenuItem("Enemy Spawn Position 1");
        locMenu.AddItem(enemyPos1);
        UIMenuItem enemyPos2 = new UIMenuItem("Enemy Spawn Position 2");
        locMenu.AddItem(enemyPos2);
        UIMenuItem enemyPos3 = new UIMenuItem("Enemy Spawn Position 3");
        locMenu.AddItem(enemyPos3);
        UIMenuItem carPos = new UIMenuItem("Enemy Ground Vehicle Spawn");
        locMenu.AddItem(carPos);
        UIMenuItem carDest = new UIMenuItem("Enemy Ground Vehicle Goto");
        locMenu.AddItem(carDest);
        UIMenuItem heliPos = new UIMenuItem("Enemy Chopper Spawn");
        locMenu.AddItem(heliPos);
        UIMenuItem heliDest = new UIMenuItem("Enemy Chopper Goto");
        locMenu.AddItem(heliDest);

        UIMenuItem saveLocation = new UIMenuItem("Save This Location");
        locMenu.AddItem(saveLocation);


        locMenu.OnListChange += (sender, item, index) =>
        {
            currentLocation = locations[index];
            ReadCurrentLocation();


            for (int i = 0; i < friendlyPositions.Count; i++)
            {
                friendPosBlips[i].Position = friendlyPositions[i];
            }
            for (int i = 0; i < enemyPositions.Count; i++)
            {
                enemyPosBlips[i].Position = enemyPositions[i];
            }
            enemyCarPosBlip.Position = vehSpawn;
            enemyCarDestBlip.Position = vehDest;
            enemyChopperPosBlip.Position = chopperPos;
            enemyChopperDestBlip.Position = chopperDest;
        };

        locMenu.OnItemSelect += (sender, item, index) =>
        {
            int listIndex = locList.Index;
            ScriptSettings currentEditing = locations[listIndex];
            Vector3 newPos = Game.Player.Character.Position;

            if (item == friendlyPos)
                WritePosition(currentEditing, "FriendPos", newPos.X, newPos.Y, newPos.Z);
            if (item == enemyPos0)
                WritePosition(currentEditing, "EnemyPos0", newPos.X, newPos.Y, newPos.Z);
            if (item == enemyPos1)
                WritePosition(currentEditing, "EnemyPos1", newPos.X, newPos.Y, newPos.Z);
            if (item == enemyPos2)
                WritePosition(currentEditing, "EnemyPos2", newPos.X, newPos.Y, newPos.Z);
            if (item == enemyPos3)
                WritePosition(currentEditing, "EnemyPos3", newPos.X, newPos.Y, newPos.Z);
            if (item == carPos)
                WritePosition(currentEditing, "VehicleSpawns", newPos.X, newPos.Y, newPos.Z);
            if (item == carDest)
                WritePosition(currentEditing, "VehicleGoes", newPos.X, newPos.Y, newPos.Z);
            if (item == heliPos)
                WritePosition(currentEditing, "ChopperSpawns", newPos.X, newPos.Y, newPos.Z);
            if (item == heliDest)
                WritePosition(currentEditing, "ChopperGoes", newPos.X, newPos.Y, newPos.Z);
            if (item == saveLocation)
            {
                currentEditing.Save();
                UI.Notify("Location saved.");
            }
        };


    }


    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F10)
        {
            mainMenu.Visible = !mainMenu.Visible;
        }
        
        if(e.KeyCode == Keys.NumPad1)
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

        if (modMenuPool != null)
            modMenuPool.ProcessMenus();

        Random rnd = new System.Random();

        int x = (int)Math.Round((double)UI.WIDTH / 2);
        int y = (int)Math.Round((double)UI.HEIGHT / 9.5);


        if (showText)
        {
            UIText text;
            text = new UIText("Friendly:" + friendPeds.Count + " Enemy:" + enemyPeds.Count + " Kills:" + friendlyKills + " Deaths:" + enemyKills + " tank:" + isTankInField + " ins:" + isGuntruckInField, new Point(x, y), 1f, Color.White, GTA.Font.ChaletComprimeCologne, true);
            text.Draw();
        }



        if (isWave)
        {

            CheckMatch();
            CheckEnemies();
            CheckFriendlies();

            SpawnVehicles();

            TankControll();
            GuntruckControll();
            ChopperControll();
            JuggernautControll();

            

            //if (!friendPeds.Contains(player))
            //{
            //    friendPeds.Add(player);
            //}

        }

    }

    void CheckEnemies()
    {
        //check enemies
        if(enemyPeds.Count != 0)
        {
            for (int i = 0; i < enemyPeds.Count; i++)
            {
                if (Vector3.Distance(enemyPeds[i].Position, enemyPositions[0]) > 300)
                {
                    enemyPeds[i].MarkAsNoLongerNeeded();
                    enemyPeds.Remove(enemyPeds[i]);
                    enemyPeds[i].Health = 0;
                }

                //if (enemyPeds[i].IsInCombat)
                //{
                //    Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, enemyPeds[i], true);
                //}
                if (enemyPeds[i].IsFleeing)
                {
                    enemyPeds[i].Task.ShootAt(friendPeds[new Random().Next(friendPeds.Count - 1)], 10000);

                    //enemyPeds[i].MarkAsNoLongerNeeded();
                    //enemyPeds.Remove(enemyPeds[i]);

                }

                //Free dead peds
                if (enemyPeds[i].IsDead)
                {
                    int deadHash = enemyPeds[i].GetHashCode();
                    if (!deadpeds.Contains(deadHash))
                    {
                        enemyPeds[i].MarkAsNoLongerNeeded();
                        deadpeds.Add(enemyPeds[i].GetHashCode());
                        enemyPeds.Remove(enemyPeds[i]);
                        friendlyKills++;
                    }
                }
            }
        }
        
    }

    void AvoidFleeing(Ped ped)
    {
        if (ped.IsFleeing)
        {
            //ped.Task.ShootAt(enemyPeds[0], 10000);
            //friendPeds[i].Task.ShootAt(enemyPeds[new Random().Next(enemyPeds.Count - 1)], 10000);

        }
    }

    void FreeDeadEnemy(Ped ped)
    {
        //Free dead peds
        if (ped.IsDead)
        {
            int deadHash = ped.GetHashCode();
            if (!deadpeds.Contains(deadHash))
            {
                ped.MarkAsNoLongerNeeded();
                deadpeds.Add(ped.GetHashCode());
                friendPeds.Remove(ped);
                enemyKills++;
            }
        }
    }

    void DeleteFarAway(Ped ped)
    {
        if (Vector3.Distance(ped.Position, friendlyPositions[0]) > 300)
        {
            ped.MarkAsNoLongerNeeded();
            friendPeds.Remove(ped);
            ped.Health = 0;
        }
    }

    void CheckFriendlies()
    {
        //check friendlies
        for(int i = 0; i < friendPeds.Count; i++)
        {
            if (friendPeds[i].IsDead)
            {
                int deadHash = friendPeds[i].GetHashCode();
                if (!deadpeds.Contains(deadHash))
                {
                    friendPeds[i].MarkAsNoLongerNeeded();
                    deadpeds.Add(friendPeds[i].GetHashCode());
                    friendPeds.Remove(friendPeds[i]);
                    enemyKills++;
                }
            }

            //if (Vector3.Distance(friendPeds[i].Position, player.Position) > 300)
            //{
            //    friendPeds[i].MarkAsNoLongerNeeded();
            //    friendPeds.Remove(friendPeds[i]);
            //    //friendPeds[i].Health = 0;
            //}

            //AvoidFleeing(friendPeds[i]);

            //DeleteFarAway(friendPeds[i]);

            //FreeDeadEnemy(friendPeds[i]);
        }

            //for (int i = 0; i < friendPeds.Count; i++)
            //{

            //    AvoidFleeing(friendPeds[i]);

            //    DeleteFarAway(friendPeds[i]);

            //    FreeDeadEnemy(friendPeds[i]);
            //}

            //if (!friendPeds.Contains(Game.Player.Character) && Game.Player.IsAlive)
            //{
            //    friendPeds.Add(Game.Player.Character);
            //}
        
    }

    void SpawnVehicles()
    {
        Random rnd = new System.Random();

        //Support Vehicles
        if (timeSinceVehicleCalled > vehicleSupportInterval && vehicleSupport)
        {
            timeSinceVehicleCalled = 0;
            int intResult = rnd.Next(3);

            if (isGroundSupportAvailable)
            {
                if (intResult == 0)
                {
                    SpawnTank();
                }
                else if (intResult == 1)
                {
                    SpawnGuntruck();
                }
                else if (intResult == 2)
                {
                    SpawnJuggernaut();
                }
            }

            if (isChopperAvailable)
            {
                //SpawnChopper();
            }
        }

        if (vehicleSupport)
            timeSinceVehicleCalled++;
    }

    void CheckMatch()
    {
        if (friendlyKills >= matchPoint || enemyKills >= matchPoint)
        {
            timeSinceVehicleCalled = 0;
            timeSinceChopperSpawned = 0;
            timeSinceGuntruckSpawned = 0;
            timeSinceTankSpawned = 0;

            if (friendPeds.Count == 0 || enemyPeds.Count == 0)
            {

                if (friendlyKills > enemyKills)
                {
                    UI.ShowSubtitle("You WON! "+friendlyKills+" - "+enemyKills, 10000);
                    Game.Player.WantedLevel = 0;
                    StopMatch();
                    return;
                }
                else
                {
                    UI.ShowSubtitle("You LOST!" + friendlyKills + " - " + enemyKills, 10000);
                    StopMatch();
                    return;
                }
            }

        }
        else
        {
            spawnPedTimer++;
            if(spawnPedTimer > 30)
            {
                spawnPedTimer = 0;
                if (friendPeds.Count < minPedAmmount)
                    CreateFriendly(1);
                if (enemyPeds.Count < minPedAmmount)
                    CreateEnemy(1);
            }
        }
    }

    int spawnPedTimer = 0;

    Vector3 ReadPosition(string positionName)
    {
        float x = currentLocation.GetValue<float>(positionName, "X", 0);//(category, name, default value)
        float y = currentLocation.GetValue<float>(positionName, "Y", 0);//(category, name, default value)
        float z = currentLocation.GetValue<float>(positionName, "Z", 0);//(category, name, default value)

        return new Vector3(x, y, z);
    }

    void WritePosition(ScriptSettings location, string positionName, float x,float y,float z)
    {
        location.SetValue(positionName, "X", x);
        location.SetValue(positionName, "Y", y);
        location.SetValue(positionName, "Z", z);
    }


    string[] fileNames;
    string path = @"scripts\Locations\";

    void LoadLocation()
    {
        fileNames = Directory.GetFiles(path);

        //UI.Notify("fp x:"+currentLocation.GetValue<float>("FriendPos", "X", 0));

        //Load locations
        locations.Clear();
        for (int i = 0; i < fileNames.Length; i++)
        {
            locations.Add(ScriptSettings.Load(fileNames[i]));
        }
        currentLocation = locations[0];
        for (int i = 0; i < fileNames.Length; i++)
        {
            listoflocations.Add(fileNames[i]);
        }
        UI.Notify(locations.Count + " locations available.");
    }

    void ReadCurrentLocation()
    {
        //Load locations

        friendlyPositions.Clear();
        enemyPositions.Clear();
        friendlyPositions.Add(ReadPosition("FriendPos"));
        enemyPositions.Add(ReadPosition("EnemyPos0"));
        enemyPositions.Add(ReadPosition("EnemyPos1"));
        enemyPositions.Add(ReadPosition("EnemyPos2"));
        enemyPositions.Add(ReadPosition("EnemyPos3"));

        vehSpawn = ReadPosition("VehicleSpawns");
        vehDest = ReadPosition("VehicleGoes");
        chopperPos = ReadPosition("ChopperSpawns");
        chopperDest = ReadPosition("VehicleGoes");
    }

    void StartMatch()
    {
        if (!isWave)
        {

            ReadCurrentLocation();

            //Load locations
            chopperDest = new Vector3(chopperDest.X, chopperDest.Y, chopperDest.Z + 25);

            isWave = true;

            //CreateFriendly(waveAmount);
            //CreateEnemy(waveAmount);

            //World.CreatePickup(PickupType.Armour, friendlyPositions[0].Around(1),);
            World.CreatePickup(PickupType.Armour, friendlyPositions[0].Around(1), new Model("prop_armour_pickup"), 100);

            player.Position = friendlyPositions[0];
        }

    }

    void StopMatch()
    {
        if (isWave)
        {

            timeSinceVehicleCalled = 0;
            timeSinceTankSpawned = 0;
            timeSinceGuntruckSpawned = 0;
            timeSinceChopperSpawned = 0;

            isWave = false;
            deadpeds.Clear();
            teamDistance = 100;

            foreach (Ped ped in enemyPeds)
                ped.MarkAsNoLongerNeeded();

            enemyPeds.Clear();
            enemyKills = 0;

            foreach (Ped ped in friendPeds)
                ped.MarkAsNoLongerNeeded();

            friendPeds.Clear();
            friendlyKills = 0;

            Ped[] nearbypeds = World.GetNearbyPeds(friendlyPositions[0], 2000);
            for (int i = 0; i < nearbypeds.Length; i++)
            {
                if (nearbypeds[i].IsDead)
                {
                    nearbypeds[i].MarkAsNoLongerNeeded();
                }
            }

            isGuntruckAvaileble = true;
            isGuntruckInField = false;
            carArrived = false;
            if (ins != null)
                ins.Delete();

            isChopperAvailable = true;
            isChopperInField = false;
            chopperArrived = false;
            if (chopper != null)
                chopper.Delete();

            isGroundSupportAvailable = true;
            isTankInField = false;
            tankArrived = false;
            if (tank != null)
                tank.Delete();
        }

    }

    void CreateFriendly(int amount)
    {
        Random rnd = new System.Random();

        for (int i = 0; i < amount; i++)
        {
            PedHash selectedPed = friendPed;
            if (rnd.Next(0, 50) > 25)
                selectedPed = friendPed1;

            Ped Friendly = World.CreatePed(selectedPed, friendlyPositions[0].Around(0.5f));

            GiveWeapon(Friendly);

            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, Friendly, freindRelationShipGroup);
            //Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, Friendly, true);
            //Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, Friendly, 0, 0);
            //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Friendly, 5, true);
            //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Friendly, 46, true);
            Friendly.Task.RunTo(enemyPositions[0]);
            //Friendly.AlwaysKeepTask = true;
            friendPeds.Add(Friendly);
        }
    }


    void CreateEnemy(int amount)
    {
        Random rnd = new System.Random();

        for (int i = 0; i < amount; i++)
        {
            PedHash selectedPed = enemyPed;
            if (rnd.Next(0, 50) > 25)
                selectedPed = enemyPed1;

            Ped enemy = World.CreatePed(selectedPed, enemyPositions[rnd.Next(enemyPositions.Count - 1)].Around(0.5f));


            GiveWeapon(enemy);


            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, enemy, EnemyRelationShipGroup);
            //Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, enemy, true);
            //Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, enemy, 0, 0);
            //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, enemy, 5, true);
            //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, enemy, 46, true);
            enemy.Task.RunTo(friendlyPositions[0]);
            //enemy.AlwaysKeepTask = true;
            enemyPeds.Add(enemy);
        }
    }

    void GiveWeapon(Ped target)
    {
        Random rnd = new System.Random();

        if (currentWeaponsType == "Heavy")
        {
            target.Weapons.Give(weapons[rnd.Next(weapons.Length - 1)]/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
        }
        else if (currentWeaponsType == "NoRPG")
        {
            target.Weapons.Give(weaponsNoHeavy[rnd.Next(weaponsNoHeavy.Length - 1)]/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
        }
        else
        {
            target.Weapons.Give(WeaponsCQB[rnd.Next(WeaponsCQB.Length - 1)]/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
        }

    }



    void SpawnTank()
    {
        //spawn Tank
        isGroundSupportAvailable = false;
        isTankInField = true;

        tank = World.CreateVehicle(new Model(VehicleHash.Rhino), vehSpawn);
        tank.Health = 100;
        tank.LockStatus = VehicleLockStatus.Locked;
        tankDriver = tank.CreatePedOnSeat(VehicleSeat.Driver, enemyPed);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, tankDriver, EnemyRelationShipGroup);
        //Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH, tankDriver, tankDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 40f, 156, 2.5f);
        Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, tankDriver, tankDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 20f, 1, tankDriver.CurrentVehicle, DrivingStyle.AvoidTrafficExtremely.GetHashCode(), -1.0, -1);

        UI.Notify("Tank Spawned.");

        //Delete previous tank
        if (tankWreck != null)
            tankWreck.Delete();
    }

    void SpawnGuntruck()
    {
        //spawn insurgent
        isGroundSupportAvailable = false;
        isGuntruckInField = true;

        ins = World.CreateVehicle(new Model(VehicleHash.Insurgent), vehSpawn);
        ins.LockStatus = VehicleLockStatus.Locked;
        ins.CustomPrimaryColor = Color.Black;
        ins.CustomSecondaryColor = Color.Black;
        //ins = World.CreateVehicle(new Model(VehicleHash.Technical), vehSpawn);
        enemyDriver = ins.CreatePedOnSeat(VehicleSeat.Driver, enemyPed);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, enemyDriver, EnemyRelationShipGroup);
        Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH, enemyDriver, enemyDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 30f, 156, 2.5f);

        enemyGunner = ins.CreatePedOnSeat((VehicleSeat)7, enemyPed);
        //enemyGunner = ins.CreatePedOnSeat((VehicleSeat)1, enemyPed);
        enemyGunner.Weapons.Give(WeaponHash.MicroSMG/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, enemyGunner, EnemyRelationShipGroup);
        int turret = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_TURRET_INSURGENT");
        Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, enemyGunner, turret);

        UI.Notify("Guntruck Spawned.");

        //Delete previous truck
        if (guntruckWreck != null)
            guntruckWreck.Delete();
    }

    void SpawnChopper()
    {

        //spawn chopper
        isChopperAvailable = false;
        isChopperInField = true;

        //chopper = World.CreateVehicle(new Model(VehicleHash.Buzzard), new Vector3(chopperPos.X, chopperPos.Y, chopperPos.Z+50));
        chopper = World.CreateVehicle(new Model(VehicleHash.Valkyrie), new Vector3(chopperPos.X, chopperPos.Y, chopperPos.Z + 70));
        chopper.EngineRunning = true;
        pilot = chopper.CreatePedOnSeat(VehicleSeat.Driver, enemyPed);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, pilot, EnemyRelationShipGroup);
        Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, pilot, chopper, chopperDest.X, chopperDest.Y, chopperDest.Z, 50f, 1, pilot.CurrentVehicle, 1, -1.0, -1);
        pilot.Weapons.Give(WeaponHash.CombatMG/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);

        chopperGunner = chopper.CreatePedOnSeat(VehicleSeat.Passenger, enemyPed);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, chopperGunner, EnemyRelationShipGroup);
        int wephash = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_NOSE_TURRET_VALKYRIE");
        Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, chopperGunner, wephash);

        heliGunner = chopper.CreatePedOnSeat(VehicleSeat.LeftRear, enemyPed);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, heliGunner, EnemyRelationShipGroup);
        heliGunner.Accuracy = 100;
        heliGunner.Weapons.Give(WeaponHash.CombatMG/*whatever weapon you wanna give*/, 2000, true/*equip now*/, true);
        int valTurret = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_TURRET_VALKYRIE");
        Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, heliGunner, valTurret);

        heliGunner1 = chopper.CreatePedOnSeat(VehicleSeat.RightRear, enemyPed);
        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, heliGunner1, EnemyRelationShipGroup);
        heliGunner1.Accuracy = 100;
        heliGunner1.Weapons.Give(WeaponHash.CombatMG/*whatever weapon you wanna give*/, 2000, true/*equip now*/, true);
        Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, heliGunner1, valTurret);

        UI.Notify("Chopper Spawned.");


        //delete previous chopper
        if (chopperWreck != null)
            chopperWreck.Delete();

    }

    void SpawnJuggernaut()
    {

        isGroundSupportAvailable = false;
        
        Random rnd = new System.Random();

        jug = World.CreatePed(juggernaut, enemyPositions[rnd.Next(enemyPositions.Count - 1)].Around(0.5f));
        //jug = World.CreatePed(juggernaut, vehDest);
        jug.MaxHealth = 4000;
        jug.Health = 4000;
        jug.Accuracy = 100;
        jug.Weapons.Give(WeaponHash.Minigun, 3000, true, true);
        jug.CanSufferCriticalHits = false;
        jug.IsFireProof = true;
        //jug.IsExplosionProof = true;
        jug.IsMeleeProof = true;
        jug.CanRagdoll = false;
        jug.FiringPattern = FiringPattern.FullAuto;
        //jug.MovementAnimationSet
        juggernautArrived = false;

        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, jug, EnemyRelationShipGroup);
        jug.Task.RunTo(friendlyPositions[0]);
        enemyPeds.Add(jug);

        isJuggernautInField = true;
        UI.Notify("Spawned Juggernaut.");
    }

    int jugMinigunTimer = 0;

    void JuggernautControll()
    {
        if (isJuggernautInField)
        {
            if (jug.IsDead)
            {
                isJuggernautInField = false;
                isGroundSupportAvailable = true;
            }
        }

    }

    void TankControll()
    {
        //Tank controlls
        if (isTankInField)
        {
            if (tank.IsAlive)
            {

                timeSinceTankSpawned++;

                if (Vector3.Distance(tank.Position, vehDest) < 2)
                {
                    tankArrived = true;
                    int tankWeapon = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_TANK");
                    Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, tankDriver, tankWeapon);
                }
                if (tankArrived)
                {
                    if (friendPeds.Count != 0)
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, tankDriver, friendPeds[new Random().Next(friendPeds.Count - 1)]);

                    tankDriver.Task.ParkVehicle(tankDriver.CurrentVehicle, vehDest, 0);
                    //Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, tankDriver, tankDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 50f, 1, tankDriver.CurrentVehicle, 1, -1.0, -1);
                }
                Function.Call(Hash.SET_PED_KEEP_TASK, tankDriver, true);

                if (Vector3.Distance(enemyPositions[0], tank.Position) > 200 || timeSinceTankSpawned > 5000)
                    tank.Explode();

            }
            else
            {
                tankWreck = tank;
                tankArrived = false;
                isTankInField = false;
                isGroundSupportAvailable = true;
                timeSinceTankSpawned = 0;
            }

        }
    }

    void GuntruckControll()
    {
        //Guntruck controlls
        if (isGuntruckInField)
        {
            if (ins.IsAlive)
            {
                timeSinceGuntruckSpawned++;

                if (enemyGunner.IsAlive)
                {
                    if(friendPeds.Count != 0)
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, enemyGunner, friendPeds[new Random().Next(friendPeds.Count - 1)]);
                    enemyGunner.AlwaysKeepTask = true;
                }
                else
                {
                    guntruckWreck = ins;
                    carArrived = false;
                    isGuntruckInField = false;
                    isGuntruckAvaileble = true;
                    isGroundSupportAvailable = true;
                    //enemyDriver.Task.LeaveVehicle(enemyDriver.CurrentVehicle, LeaveVehicleFlags.LeaveDoorOpen);
                    timeSinceGuntruckSpawned = 0;
                }

                if (Vector3.Distance(ins.Position, vehDest) < 2)
                {
                    carArrived = true;

                }
                if (carArrived)
                    enemyDriver.Task.ParkVehicle(enemyDriver.CurrentVehicle, enemyDriver.CurrentVehicle.Position, 0);

                Function.Call(Hash.SET_PED_KEEP_TASK, enemyDriver, true);

                if (Vector3.Distance(enemyPositions[0], ins.Position) > 200 || timeSinceGuntruckSpawned > 5000)
                    ins.Explode();
            }
            else
            {
                guntruckWreck = ins;
                carArrived = false;
                isGuntruckInField = false;
                isGuntruckAvaileble = true;
                isGroundSupportAvailable = true;
                timeSinceGuntruckSpawned = 0;
            }


        }
    }

    void ChopperControll()
    {
        //Chopper controlls
        if (isChopperInField)
        {
            if (chopper.IsAlive)
            {
                timeSinceFired++;
                timeSinceChopperSpawned++;

                if (timeSinceFired > 300)
                {
                    if(friendPeds.Count != 0)
                    {
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, chopperGunner, friendPeds[new Random().Next(friendPeds.Count - 1)]);
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, heliGunner, friendPeds[new Random().Next(friendPeds.Count - 1)]);
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, heliGunner1, friendPeds[new Random().Next(friendPeds.Count - 1)]);
                    }


                    if (timeSinceFired > 500)
                        timeSinceFired = 0;
                }
                else
                {

                }

                
                chopperGunner.AlwaysKeepTask = true;
                heliGunner.AlwaysKeepTask = true;
                heliGunner1.AlwaysKeepTask = true;

                if (Vector3.Distance(chopper.Position, chopperDest) < 5)
                {
                    chopperArrived = true;
                }
                if (chopperArrived)
                {
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, pilot, chopper, chopperDest.X, chopperDest.Y, chopperDest.Z, 0, 1, pilot.CurrentVehicle, 1, -1.0, -1);

                }
                pilot.AlwaysKeepTask = true;

                if (Vector3.Distance(enemyPositions[0], chopper.Position) > 500 || timeSinceChopperSpawned > 15000)
                    chopper.Explode();
            }
            else
            {
                chopperWreck = chopper;
                chopperArrived = false;
                isChopperInField = false;
                isChopperAvailable = true;
                timeSinceChopperSpawned = 0;
            }
        }
    }



}