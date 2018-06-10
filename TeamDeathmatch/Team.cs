using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

namespace TeamDeathmatch
{
    public class Team : Script
    {
        //References
        private Team enemyTeam;
        public Team EnemyTeam
        {
            get { return enemyTeam; }
            set { enemyTeam = value; }
        }

        string teamName = "default";
        public void ChangeName(string newName)
        {
            teamName = newName;
        }

        //Members
        private PedHash ped;
        public PedHash Ped0
        {
            get { return ped; }
            set { ped = value; }
        }

        private PedHash ped1;
        public PedHash Ped1
        {
            get { return ped1; }
            set { ped1 = value; }
        }

        private int relationshipGroup = 0;
        public int RelationshipGroup
        {
            get { return relationshipGroup; }
            set { relationshipGroup = value; }
        }

        public List<Ped> members = new List<Ped>();
        public List<Ped> enemies = new List<Ped>();
        public List<Vector3> spawnPositions = new List<Vector3>();

        //Weapons
        public WeaponHash[] weaponSet;

        //Score
        public int kills = 0;

        //Vehicle support values 
        bool vehicleSupport = true;
        int timeSinceVehicleCalled = 0;
        int vehicleSupportInterval = 2000;
        int timeSinceFired = 0;
        enum supportVehicles { Insurgent, Valkyrie, Tank };
        supportVehicles vehicle;
        public Vector3 vehSpawn;
        public Vector3 vehDest;

        //Guntruck values
        public bool enableGuntruck = false;
        bool isGuntruckAvaileble = true;
        public bool isGuntruckInField = false;
        bool carArrived = false;
        Ped guntruckDriver;
        Ped guntruckGunner;
        Vehicle ins;
        Vehicle guntruckWreck;
        int timeSinceGuntruckSpawned = 0;
        int guntruckSpawnTimer = 0;
        int guntruckInterval = 2000;

        //Tank values
        public bool enableTank = false;
        public bool isTankInField = false;
        bool tankArrived = false;
        Ped tankDriver;
        Vehicle tank;
        Vehicle tankWreck;
        int timeSinceTankSpawned = 0;
        int tankSpawnTimer = 0;
        int tankInterval = 3000;

        //Chopper values
        public bool enableChopper = false;
        public bool isChopperInField = false;
        bool chopperArrived = false;
        public Vector3 chopperPos;
        public Vector3 chopperDest;
        Ped pilot;
        Ped chopperGunner;
        Ped heliGunner;
        Ped heliGunner1;
        Vehicle chopper;
        Vehicle chopperWreck;
        int timeSinceChopperSpawned = 0;
        int chopperSpawnTimer = 0;
        int chopperInterval = 2500;

        //Juggernaut values
        public bool enableJuggernaut = false;
        public bool isJuggernautInField = false;
        PedHash juggernaut = PedHash.Juggernaut01M;
        Ped jug;
        int timeSinceJuggernautSpawned = 0;
        int juggernautSpawnTimer = 0;
        int juggernautInterval = 3500;

        public void AddMember(Ped ped)
        {
            if (ped != null && !members.Contains(ped))
                members.Add(ped);
        }

        public void AddEnemy(Ped ped)
        {
            if (ped != null && !enemies.Contains(ped))
                enemies.Add(ped);
        }

        public void RemoveEnemy(Ped ped)
        {
            if (ped != null && enemies.Contains(ped))
                enemies.Remove(ped);
        }

        public void SpawnMember(int amount)
        {
            Random rnd = new System.Random();

            for (int i = 0; i < amount; i++)
            {
                PedHash selectedPed = ped;
                if (rnd.Next(0, 50) > 25)
                    selectedPed = ped1;

                
                Ped member = World.CreatePed(selectedPed, spawnPositions[rnd.Next(spawnPositions.Count)].Around(0.5f));

                if(member != null)
                {
                    GiveWeapon(member);
                    Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, member, relationshipGroup);
                    member.Task.RunTo(enemyTeam.spawnPositions[rnd.Next(enemyTeam.spawnPositions.Count)]);
                    members.Add(member);
                    enemyTeam.AddEnemy(member);
                }
            }
        }

        

        void GiveWeapon(Ped target)
        {
            Random rnd = new System.Random();
            target.Weapons.Give(weaponSet[rnd.Next(weaponSet.Length - 1)]/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
        }


        public void CheckMembers()
        {
            if (members.Count == 0 || Game.Player.Character == null)
                return;

            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] != Game.Player.Character)
                {
                    DeleteFarAway(members[i]);

                    AvoidFleeing(members[i]);

                    CheckDeadbodies(members[i]);

                }
                else
                {
                    if (!members[i].IsAlive)
                    {
                        members.Remove(members[i]);
                        enemyTeam.kills++;
                        enemyTeam.RemoveEnemy(members[i]);
                    }
                }

            }
        }

        void DeleteFarAway(Ped member)
        {
            //Remove member who went far away
            if (Vector3.Distance(member.Position, spawnPositions[0]) > 300)
            {
                member.MarkAsNoLongerNeeded();
                members.Remove(member);
                //member.Health = 0;
            }

        }

        void AvoidFleeing(Ped member)
        {
            //Block fleeing
            if (member.IsFleeing && enemies.Count != 0)
            {
                member.Task.ShootAt(enemies[new Random().Next(enemies.Count)], 10000);
            }
        }

        void CheckDeadbodies(Ped member)
        {
            //Remove member who died
            if (member.IsDead)
            {
                member.MarkAsNoLongerNeeded();
                members.Remove(member);
                enemyTeam.kills++;
                enemyTeam.RemoveEnemy(member);
            }
        }

        public void Support()
        {
            

            if (enableGuntruck && !isGuntruckInField)
            {
                guntruckSpawnTimer++;
                if (guntruckSpawnTimer > guntruckInterval)
                {
                    guntruckSpawnTimer = 0;
                    SpawnGuntruck();
                }
            }

            if (enableTank && !isTankInField)
            {
                tankSpawnTimer++;
                if (tankSpawnTimer > tankInterval)
                {
                    tankSpawnTimer = 0;
                    SpawnTank();
                }
            }

            if (enableChopper && !isChopperInField)
            {
                chopperSpawnTimer++;
                if (chopperSpawnTimer > chopperInterval)
                {
                    chopperSpawnTimer = 0;
                    SpawnChopper();
                }
            }

            if (enableJuggernaut && !isJuggernautInField)
            {
                juggernautSpawnTimer++;
                if (juggernautSpawnTimer > juggernautInterval)
                {
                    juggernautSpawnTimer = 0;
                    SpawnJuggernaut();
                }
            }

            if (vehicleSupport)
                timeSinceVehicleCalled++;
        }

        public void SpawnTank()
        {
            //spawn Tank
            isTankInField = true;

            tank = World.CreateVehicle(new Model(VehicleHash.Rhino), vehSpawn);
            tank.Health = 100;
            tank.LockStatus = VehicleLockStatus.Locked;
            tankDriver = tank.CreatePedOnSeat(VehicleSeat.Driver, ped);
            //Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH, tankDriver, tankDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 40f, 156, 2.5f);
            members.Add(tankDriver);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, tankDriver, relationshipGroup);
            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, tankDriver, tankDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 20f, 1, tankDriver.CurrentVehicle, DrivingStyle.AvoidTrafficExtremely.GetHashCode(), -1.0, -1);

            UI.Notify(teamName + " Tank is approaching.");

            //Delete previous tank
            if (tankWreck != null)
                tankWreck.Delete();
        }

        public void SpawnGuntruck()
        {
            //spawn insurgent
            isGuntruckInField = true;

            ins = World.CreateVehicle(new Model(VehicleHash.Insurgent), vehSpawn);
            ins.LockStatus = VehicleLockStatus.Locked;
            ins.CustomPrimaryColor = Color.Black;
            ins.CustomSecondaryColor = Color.Black;
            //ins = World.CreateVehicle(new Model(VehicleHash.Technical), vehSpawn);

            guntruckDriver = ins.CreatePedOnSeat(VehicleSeat.Driver, ped);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, guntruckDriver, relationshipGroup);
            Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH, guntruckDriver, guntruckDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 30f, 156, 2.5f);
            guntruckDriver.Weapons.Give(WeaponHash.MicroSMG/*whatever weapon you wanna give*/, 2000, true/*equip now*/, true);
            members.Add(guntruckDriver);

            guntruckGunner = ins.CreatePedOnSeat((VehicleSeat)7, ped);
            guntruckGunner.Weapons.Give(WeaponHash.MicroSMG/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, guntruckGunner, relationshipGroup);
            int turret = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_TURRET_INSURGENT");
            Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, guntruckGunner, turret);
            members.Add(guntruckGunner);

            UI.Notify(teamName + " Guntruck is approaching.");

            //Delete previous truck
            if (guntruckWreck != null)
                guntruckWreck.Delete();
        }

        public void SpawnChopper()
        {
            Random rnd = new System.Random();

            //spawn chopper
            isChopperInField = true;

            //Determine chopper randomly
            int intResult = rnd.Next(3);
            if (intResult == 0)
                chopper = World.CreateVehicle(new Model(VehicleHash.Valkyrie), new Vector3(chopperPos.X, chopperPos.Y, chopperPos.Z + 70));
            else if (intResult == 1)
                chopper = World.CreateVehicle(new Model(VehicleHash.Buzzard), new Vector3(chopperPos.X, chopperPos.Y, chopperPos.Z + 70));
            else
                chopper = World.CreateVehicle(new Model(VehicleHash.Savage), new Vector3(chopperPos.X, chopperPos.Y, chopperPos.Z + 70));

            chopper.EngineRunning = true;

            //pilot setup
            pilot = chopper.CreatePedOnSeat(VehicleSeat.Driver, ped);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, pilot, relationshipGroup);
            members.Add(pilot);
            pilot.Accuracy = 100;
            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, pilot, chopper, chopperDest.X, chopperDest.Y, chopperDest.Z, 50f, 1, pilot.CurrentVehicle, 1, -1.0, -1);
            pilot.Weapons.Give(WeaponHash.CombatMG/*whatever weapon you wanna give*/, 300, true/*equip now*/, true);
            int rocketHash = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_SPACE_ROCKET");
            //int rocketHash = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_PLAYER_SAVAGE");
            Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, pilot, rocketHash);

            //gunner setup
            chopperGunner = chopper.CreatePedOnSeat(VehicleSeat.Passenger, ped);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, chopperGunner, relationshipGroup);
            members.Add(chopperGunner);
            int wephash = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_NOSE_TURRET_VALKYRIE");
            Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, chopperGunner, wephash);

            //sidegunner1 setup
            heliGunner = chopper.CreatePedOnSeat(VehicleSeat.LeftRear, ped);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, heliGunner, relationshipGroup);
            members.Add(heliGunner);
            heliGunner.Accuracy = 100;
            heliGunner.Weapons.Give(WeaponHash.CombatMG/*whatever weapon you wanna give*/, 2000, true/*equip now*/, true);
            int valTurret = Function.Call<int>(Hash.GET_HASH_KEY, "VEHICLE_WEAPON_TURRET_VALKYRIE");
            Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, heliGunner, valTurret);

            //sidegunner2 setup
            heliGunner1 = chopper.CreatePedOnSeat(VehicleSeat.RightRear, ped);
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, heliGunner1, relationshipGroup);
            members.Add(heliGunner1);
            heliGunner1.Accuracy = 100;
            heliGunner1.Weapons.Give(WeaponHash.CombatMG/*whatever weapon you wanna give*/, 2000, true/*equip now*/, true);
            Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, heliGunner1, valTurret);

            UI.Notify(teamName + " Chopper is approaching.");


            //delete previous chopper
            if (chopperWreck != null)
                chopperWreck.Delete();

        }

        public void SpawnJuggernaut()
        {


            Random rnd = new System.Random();

            jug = World.CreatePed(juggernaut, spawnPositions[rnd.Next(spawnPositions.Count - 1)].Around(0.5f));
            //jug = World.CreatePed(juggernaut, vehDest);
            jug.MaxHealth = 4000;
            jug.Health = 2000;
            jug.Accuracy = 100;
            jug.Weapons.Give(WeaponHash.Minigun, 3000, true, true);
            jug.CanSufferCriticalHits = false;
            jug.IsFireProof = true;
            //jug.IsExplosionProof = true;
            jug.IsMeleeProof = true;
            jug.CanRagdoll = false;
            jug.FiringPattern = FiringPattern.FullAuto;
            //jug.MovementAnimationSet

            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, jug, relationshipGroup);
            jug.Task.RunTo(enemyTeam.spawnPositions[0]);
            members.Add(jug);

            isJuggernautInField = true;
            UI.Notify(teamName + " Juggernaut is approaching.");
        }

        public void JuggernautControl()
        {
            if (isJuggernautInField)
            {
                if (jug.IsAlive)
                {
                    timeSinceJuggernautSpawned++;
                }
                else
                {
                    timeSinceJuggernautSpawned = 0;
                    isJuggernautInField = false;
                }
            }

        }

        public void TankControl()
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
                        if (enemies.Count != 0)
                            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, tankDriver, enemies[new Random().Next(enemies.Count)]);

                        tankDriver.Task.ParkVehicle(tankDriver.CurrentVehicle, vehDest, 0);
                        //Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD, tankDriver, tankDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z, 50f, 1, tankDriver.CurrentVehicle, 1, -1.0, -1);
                    }
                    Function.Call(Hash.SET_PED_KEEP_TASK, tankDriver, true);

                    if (Vector3.Distance(spawnPositions[0], tank.Position) > 200 || timeSinceTankSpawned > 5000)
                        tank.Explode();

                }
                else
                {
                    tankWreck = tank;
                    tankArrived = false;
                    isTankInField = false;
                    timeSinceTankSpawned = 0;
                    tankDriver.MarkAsNoLongerNeeded();
                }

            }
        }

        public void GuntruckControl()
        {
            //Guntruck controlls
            if (isGuntruckInField)
            {
                if (ins.IsAlive)
                {
                    timeSinceGuntruckSpawned++;

                    if(guntruckGunner == null || guntruckDriver == null)
                    {
                        guntruckWreck = ins;
                        timeSinceGuntruckSpawned = 0;
                    }

                    if (guntruckGunner.IsAlive)
                    {
                        if (enemies.Count != 0)
                            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, guntruckGunner, enemies[new Random().Next(enemies.Count)]);
                        Function.Call(Hash.SET_PED_KEEP_TASK, guntruckGunner, true);
                    }
                    else
                    {
                        guntruckWreck = ins;
                        carArrived = false;
                        isGuntruckInField = false;
                        //guntruckDriver.Task.LeaveVehicle(guntruckDriver.CurrentVehicle, LeaveVehicleFlags.LeaveDoorOpen);
                        timeSinceGuntruckSpawned = 0;
                        guntruckDriver.MarkAsNoLongerNeeded();
                        guntruckGunner.MarkAsNoLongerNeeded();
                    }

                    if (Vector3.Distance(ins.Position, vehDest) < 2)
                    {
                        carArrived = true;
                        UI.Notify(teamName + " Guntruck arrived.");
                        //Function.Call(Hash.TASK_VEHICLE_PARK, guntruckDriver.CurrentVehicle, vehDest.X, vehDest.Y, vehDest.Z,0,0,30,true);
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, guntruckDriver, enemies[new Random().Next(enemies.Count)]);
                    }
                    if (carArrived)
                    {
                        
                    }


                    Function.Call(Hash.SET_PED_KEEP_TASK, guntruckDriver, true);
                    //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guntruckDriver, 1, true);
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guntruckDriver, 3, false);

                    //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guntruckGunner, 1, true);
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guntruckGunner, 3, false);

                    if (Vector3.Distance(spawnPositions[0], ins.Position) > 200 || timeSinceGuntruckSpawned > 5000)
                        ins.Explode();
                }
                else
                {
                    guntruckWreck = ins;
                    carArrived = false;
                    isGuntruckInField = false;
                    isGuntruckAvaileble = true;
                    timeSinceGuntruckSpawned = 0;
                }


            }
        }

        public void ChopperControl()
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
                        if (enemies.Count != 0)
                        {
                            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, pilot, enemies[new Random().Next(enemies.Count)]);
                            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, chopperGunner, enemies[new Random().Next(enemies.Count)]);
                            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, heliGunner, enemies[new Random().Next(enemies.Count)]);
                            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, heliGunner1, enemies[new Random().Next(enemies.Count)]);
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

                    if (Vector3.Distance(spawnPositions[0], chopper.Position) > 500 || timeSinceChopperSpawned > 15000)
                        chopper.Explode();
                }
                else
                {
                    chopperWreck = chopper;
                    chopperArrived = false;
                    isChopperInField = false;
                    timeSinceChopperSpawned = 0;
                    pilot.MarkAsNoLongerNeeded();
                    chopperGunner.MarkAsNoLongerNeeded();
                    heliGunner.MarkAsNoLongerNeeded();
                    heliGunner1.MarkAsNoLongerNeeded();
                }
            }
        }

        public void StartMatch()
        {
            chopperDest = new Vector3(chopperDest.X, chopperDest.Y, chopperDest.Z + 25);
        }

        public void Matched()
        {
            guntruckSpawnTimer = 0;
            tankSpawnTimer = 0;
            chopperSpawnTimer = 0;
            juggernautSpawnTimer = 0;
        }

        public void StopMatch()
        {
            foreach (Ped ped in members)
                ped.MarkAsNoLongerNeeded();

            members.Clear();
            enemies.Clear();
            kills = 0;

            timeSinceVehicleCalled = 0;
            timeSinceTankSpawned = 0;
            timeSinceGuntruckSpawned = 0;
            timeSinceChopperSpawned = 0;

            isGuntruckInField = false;
            carArrived = false;
            if (ins != null)
                ins.Delete();
            if (guntruckDriver != null)
                guntruckDriver.MarkAsNoLongerNeeded();
            if (guntruckGunner != null)
                guntruckGunner.MarkAsNoLongerNeeded();

            isChopperInField = false;
            chopperArrived = false;
            if (chopper != null)
                chopper.Delete();
            if (pilot != null)
                pilot.MarkAsNoLongerNeeded();
            if (chopperGunner!= null)
                chopperGunner.MarkAsNoLongerNeeded();
            if (heliGunner != null)
                heliGunner.MarkAsNoLongerNeeded();
            if (heliGunner1 != null)
                heliGunner1.MarkAsNoLongerNeeded();

            isTankInField = false;
            tankArrived = false;
            if (tank != null)
                tank.Delete();
            if (tankDriver != null)
                tankDriver.MarkAsNoLongerNeeded();

            isJuggernautInField = false;
            if (jug != null)
                jug.Delete();
        }

    }


}
