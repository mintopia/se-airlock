#region header
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace Airlock
{
    class Airlock
    {
        static void Main(string[] args)
        {
        }
        
        IMyGridTerminalSystem GridTerminalSystem = null;
#endregion
#region CodeEditor
        // This is the prefix for all the blocks and groups for the airlock
        const string AIRLOCK_PREFIX = "Station Airlock";

        // This is how long in ticks (normally seconds) to wait for vents and doors to open/close/depressurize
        const int VENT_TIMEOUT = 5;
        const int DOOR_TIMEOUT = 5;

        /* Nothing from here on needs editing */

        // State Table
        const int _state_NONE = 0;
        const int _state_READY = 1;
        const int _state_LOCKED = 2;
        const int _state_WAITING_FOR_PRESSURE = 3;
        const int _state_WAITING_FOR_VACUUM = 4;
        const int _state_WAITING_FOR_EXTERIOR_DOORS = 5;
        const int _state_WAITING_FOR_INTERIOR_DOORS = 6;

        // The names of our sensor groups
        const string _sensor_INTERIOR = AIRLOCK_PREFIX + " Internal Sensors";
        const string _sensor_EXTERIOR = AIRLOCK_PREFIX + " External Sensors";
        const string _sensor_CHAMBER = AIRLOCK_PREFIX + " Chamber Sensors";

        // The names of our door groups
        const string _door_INTERIOR = AIRLOCK_PREFIX + " Internal Doors";
        const string _door_EXTERIOR = AIRLOCK_PREFIX + " External Doors";

        // One-off blocks that are needed
        const string _VENTS = AIRLOCK_PREFIX + " Vents";
        const string _VALVE = AIRLOCK_PREFIX + " Valve";
        const string _TIMER = AIRLOCK_PREFIX + " Timer";
        const string _DEBUG = AIRLOCK_PREFIX + " Debug";

        // The current state and tick counter
        int currentState = _state_NONE;
        int currentTick = 0;

        // The debug logs
        List<string> logs = new List<string>();

        void Main()
        {
            IncrementTicks();

            switch (GetState())
            {
                case _state_NONE:
                    Initialise();
                    break;

                case _state_LOCKED:
                    break;

                case _state_WAITING_FOR_PRESSURE:
                    // Waiting for pressure     
                    if (IsPressurised() || (GetTicks() > VENT_TIMEOUT))
                    {
                        OpenInterior();
                    }
                    break;

                case _state_WAITING_FOR_VACUUM:
                    // Waiting for vacuum 
                    if (!IsPressurised()) {
                        OpenExterior();
                        return;
                    } else if (GetTicks() > VENT_TIMEOUT) {
                        OpenValve();
                        return;
                    }
                    break;

                case _state_WAITING_FOR_EXTERIOR_DOORS:
                    // Waiting for exterior doors to close     
                    if (AreExteriorDoorsClosed() || (GetTicks() > DOOR_TIMEOUT))
                    {
                        ContinuePressurisation();
                    }
                    break;

                case _state_WAITING_FOR_INTERIOR_DOORS:
                    // Waiting for interior doors to close     
                    if (AreInteriorDoorsClosed() || (GetTicks() > DOOR_TIMEOUT))
                    {
                        ContinueDepressurisation();
                    }
                    break;

                case _state_READY:
                default:
                    SetTicks(0);
                    StartAirlock();
                    break;
            }
            RenderDebug();
        }

        // Actions

        void Initialise()
        {
            SetState(_state_READY);
        }    

        void StartAirlock()
        {
            SetState(_state_LOCKED);

            // Detect what's happening from what sensor  
            if (IsActive(_sensor_CHAMBER))
            {
                if (AreInteriorDoorsClosed())
                {
                    // Interior doors are closed, they probably walked from outside  
                    PressuriseAirlock();
                }
                else
                {
                    // Interior doors are open, they probably walked from inside  
                    DepressuriseAirlock();
                }
                return;
            }

            if (IsActive(_sensor_INTERIOR))
            {
                // They walked up to the interior sensor  
                if (AreInteriorDoorsClosed())
                {
                    PressuriseAirlock();
                    return;
                }
            }

            if (IsActive(_sensor_EXTERIOR))
            {
                if (AreExteriorDoorsClosed())
                {
                    DepressuriseAirlock();
                    return;
                }
            }

            // We did nothing, let's lock it again  
            SetState(_state_READY);
        }

        void OpenInterior()
        {
            currentState = _state_READY;
            DebugOutput("Opening Interior Doors");
            OpenInteriorDoors();
            StopTimer();
            EnableExteriorSensor();
            EnableChamberSensor();
            DebugOutput("*** Pressurisation Complete ***");
        }

        void OpenExterior()
        {
            DebugOutput("Opening Exterior Doors");
            SetState(_state_READY);
            OpenExteriorDoors();
            StopTimer();
            EnableInteriorSensor();
            EnableChamberSensor();
            DebugOutput("***Depressurisation Complete***");
        }

        void PressuriseAirlock()
        {
            DebugOutput("*** Starting Pressurisation Sequence ****");
            DisableSensors();
            StartTimer();
            CloseExteriorDoors();
            SetState(_state_WAITING_FOR_EXTERIOR_DOORS);
            DebugOutput("Waiting for exterior doors to close");
        }

        void ContinuePressurisation()
        {
            DebugOutput("Exterior doors closed");
            EnableOxygen();
            SetState(_state_WAITING_FOR_PRESSURE);
            DebugOutput("Waiting for air pressure");
        }

        void DepressuriseAirlock()
        {
            DebugOutput("*** Starting Depressurisation Sequence ****");
            DisableSensors();
            StartTimer();
            CloseInteriorDoors();
            SetState(_state_WAITING_FOR_INTERIOR_DOORS);
            DebugOutput("Waiting for interior doors to close");
        }

        void ContinueDepressurisation()
        {
            DebugOutput("Interior doors closed");
            DisableOxygen();
            SetState(_state_WAITING_FOR_VACUUM);
            DebugOutput("Waiting for vacuum");
        }

        void EnableOxygen()
        {
            if (IsValveOpen())
            {
                DebugOutput("Closing safety valve");
                CloseValve();
            }
            DebugOutput("Pressurising vent");
            ApplyAction(GetVents(), "Depressurize");
        }

        void DisableOxygen()
        {  
            DebugOutput("Depressurising vents");
            ApplyAction(GetVents(), "Depressurize");
        }

        // Library Functions

        void ApplyAction<T>(IList<T> objects, string actionName)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                var block = objects[i] as IMyTerminalBlock;
                ITerminalAction action = block.GetActionWithName(actionName);
                if (action == null)
                {
                    throw new Exception("Unable to find " + actionName + " action");
                }
                action.Apply(block);
            }
        }

        IList<T> GetGroup<T>(string groupName)
        {
            for (int i = 0; i < GridTerminalSystem.BlockGroups.Count; i++)
            {
                if (GridTerminalSystem.BlockGroups[i].Name == groupName)
                {
                    return GridTerminalSystem.BlockGroups[i].Blocks.OfType<T>().ToList();
                }
            }
            return new List<T>();
        }

        IList<IMyAirVent> GetVents()
        {
            return GetGroup<IMyAirVent>(_VENTS);
        }

        IMyDoor GetValve()
        {
            return GridTerminalSystem.GetBlockWithName(_VALVE) as IMyDoor;
        }

        IMyTimerBlock GetTimer()
        {
            var timer = GridTerminalSystem.GetBlockWithName(_TIMER) as IMyTimerBlock;
            if (timer == null)
            {
                throw new Exception("Unable to find timer");
            }
            return timer;
        }

        IList<IMySensorBlock> GetSensors(string name)
        {
            return GetGroup<IMySensorBlock>(name);
        }

        IList<IMyDoor> GetDoors(string name)
        {
            return GetGroup<IMyDoor>(name);
        }

        // State and Ticks

        int GetState()
        {
            return currentState;
        }

        void SetState(int state)
        {
            currentState = state;
        }

        int GetTicks()
        {
            return currentTick;
        }

        void SetTicks(int value)
        {
            currentTick = value;
        }

        void IncrementTicks()
        {
            currentTick++;
        }

        // Utils

        void StartTimer()
        {
            var timer = GetTimer();
            timer.GetActionWithName("OnOff_On").Apply(timer);
            timer.GetActionWithName("Start").Apply(timer);
        }

        void StopTimer()
        {
            var timer = GetTimer();
            timer.GetActionWithName("OnOff_Off").Apply(timer);
        }

        bool IsPressurised()
        {
            if (GetOxygenLevel() == 100)
            {
                return true;
            }
            return false;
        }

        int GetOxygenLevel()
        {
            IList<IMyAirVent> vents = GetVents();
            if (vents.Count == 0)
            {
                return 0;
            }

            return (int)Math.Round((Decimal)(vents[0].GetOxygenLevel() * 100), 0);
        }

        void OpenDoors(string name)
        {
            ApplyAction(GetDoors(name), "Open_On");
        }

        void CloseDoors(string name)
        {
            ApplyAction(GetDoors(name), "Open_Off");
        }

        void CloseInteriorDoors()
        {
            CloseDoors(_door_INTERIOR);
        }

        void OpenInteriorDoors()
        {
            OpenDoors(_door_INTERIOR);
        }

        void OpenExteriorDoors()
        {
            OpenDoors(_door_EXTERIOR);
        }

        void CloseExteriorDoors()
        {
            CloseDoors(_door_EXTERIOR);
        }

        void OpenValve()
        {
            IMyDoor valve = GetValve();
            if (valve == null)
            {
                return;
            }
            valve.GetActionWithName("Open_On").Apply(valve);
        }

        void CloseValve()
        {
            IMyDoor valve = GetValve();
            if (valve == null)
            {
                return;
            }
            valve.GetActionWithName("Open_Off").Apply(valve);
        }

        bool IsValveOpen()
        {
            IMyDoor valve = GetValve();
            if (valve == null)
            {
                // It doesn't exist, so it can't be open
                return false;
            }
            return valve.Open;
        }

        bool AreDoorsClosed(string name)
        {
            IList<IMyDoor> doors = GetDoors(name);
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i].Open)
                {
                    return false;
                }
            }
            return true;
        }

        bool AreExteriorDoorsClosed()
        {
            return AreDoorsClosed(_door_EXTERIOR);
        }

        bool AreInteriorDoorsClosed()
        {
            return AreDoorsClosed(_door_INTERIOR);
        }

        bool IsActive(string name)
        {
            IList<IMySensorBlock> sensors = GetSensors(name);
            for (int i = 0; i < sensors.Count; i++)
            {
                if (sensors[i].IsActive)
                {
                    DebugOutput(name + " Activated");
                    return true;
                }
            }
            return false;
        }

        void EnableSensors(string name)
        {
            ApplyAction(GetSensors(name), "OnOff_On");
        }

        void DisableSensor(string name)
        {
            ApplyAction(GetSensors(name), "OnOff_Off");
        }

        void EnableExteriorSensor()
        {
            EnableSensors(_sensor_EXTERIOR);
        }

        void EnableInteriorSensor()
        {
            EnableSensors(_sensor_INTERIOR);
        }

        void EnableChamberSensor()
        {
            EnableSensors(_sensor_CHAMBER);
        }

        void DisableSensors()
        {
            DisableSensor(_sensor_EXTERIOR);
            DisableSensor(_sensor_INTERIOR);
            DisableSensor(_sensor_CHAMBER);
        }

        // Debug functionality

        List<IMyTextPanel> GetDebugPanels()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.SearchBlocksOfName(_DEBUG, blocks);
            for (int i = 0; i < blocks.Count; i++)
            {
                panels.Add(blocks[i] as IMyTextPanel);
            }
            return panels;
        }

        void DebugOutput(string message)
        {
            string last = "";
            if (logs.Count > 0)
            {
                last = logs[logs.Count - 1];
            }
            if (last != message)
            {
                logs.Add(message);
            }
            int count = 13;
            int index = logs.Count - count;
            if (index < 0)
            {
                index = 0;
            }
            if (count > logs.Count)
            {
                count = logs.Count;
            }
            logs = logs.GetRange(index, count);
            RenderDebug();
        }

        string GetAirlockStatus()
        {
            switch (currentState)
            {
                case _state_WAITING_FOR_PRESSURE:
                case _state_WAITING_FOR_EXTERIOR_DOORS:
                    return "Pressurising";

                case _state_WAITING_FOR_VACUUM:
                case _state_WAITING_FOR_INTERIOR_DOORS:
                    return "Depressurising";

                case _state_LOCKED:
                    return "Locked";

                case _state_NONE:
                    return "Initialising";

                case _state_READY:
                    return "Ready";

                default:
                    return "Offline";
            }
        }

        string GetDebugHR()
        {
            return "\n-------------------------------------------------------------\n ";
        }

        void RenderDebug()
        {
            string message = "           AIRLOCK STATUS: " + GetAirlockStatus() + GetDebugHR();
            message += String.Join("\n ", logs);
            for (int i = 0; i < (13 - logs.Count); i++)
            {
                message += "\n";
            }
            message += GetDebugHR();
            message += " State: " + currentState.ToString();
            message += "      Oxygen: " + GetOxygenLevel().ToString() + "%";
            message += "      Tick: " + currentTick.ToString();

            List<IMyTextPanel> panels = GetDebugPanels();
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].WritePublicText(message, false);
                panels[i].ShowPublicTextOnScreen();
            }
        }
#endregion
#region footer
    }
}
#endregion
