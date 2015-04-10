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
        const string AIRLOCK_PREFIX = "Station Airlock";

        const int VENT_TIMEOUT = 5;
        const int DOOR_TIMEOUT = 5;

        const int _state_READY = 1;
        const int _state_LOCKED = 2;
        const int _state_WAITING_FOR_PRESSURE = 3;
        const int _state_WAITING_FOR_VACUUM = 4;
        const int _state_WAITING_FOR_EXTERIOR_DOORS = 5;
        const int _state_WAITING_FOR_INTERIOR_DOORS = 6;

        int currentState = _state_READY;
        int currentTick = 0;

        List<string> logs = new List<string>();

        void Main()
        {
            IncrementTicks();

            switch (GetState())
            {
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

        void StartAirlock()
        {
            SetState(_state_LOCKED);

            // Detect what's happening from what sensor  
            if (IsActive(AIRLOCK_PREFIX + " Chamber Sensor"))
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

            if (IsActive(AIRLOCK_PREFIX + " Interior Sensor"))
            {
                // They walked up to the interior sensor  
                if (AreInteriorDoorsClosed())
                {
                    PressuriseAirlock();
                    return;
                }
            }

            if (IsActive(AIRLOCK_PREFIX + " Exterior Sensor"))
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
            DebugOutput("Enabling vent");
            IMyAirVent vent = GetVent(); 
            vent.GetActionWithName("Depressurize").Apply(vent);
        }

        void DisableOxygen()
        {
            IMyAirVent vent = GetVent();  
            DebugOutput("Depressurizing vent");
            vent.GetActionWithName("Depressurize").Apply(vent);
        }

        // Library Functions

        IMyAirVent GetVent()
        {
            var vent = GridTerminalSystem.GetBlockWithName(AIRLOCK_PREFIX + " Vent") as IMyAirVent;
            if (vent == null)
            {
                throw new Exception("Unable to find air vent");
            }
            return vent;
        }

        IMyDoor GetValve()
        {
            return GridTerminalSystem.GetBlockWithName(AIRLOCK_PREFIX + " Valve") as IMyDoor;
        }

        IMyTimerBlock GetTimer()
        {
            var timer = GridTerminalSystem.GetBlockWithName(AIRLOCK_PREFIX + " Timer") as IMyTimerBlock;
            if (timer == null)
            {
                throw new Exception("Unable to find timer");
            }
            return timer;
        }

        IMyInteriorLight GetLight()
        {
            var light = GridTerminalSystem.GetBlockWithName(AIRLOCK_PREFIX + " Variable Light") as IMyInteriorLight;
            if (light == null)
            {
                throw new Exception("Unable to find state light for airlock");
            }
            return light;
        }

        IMySensorBlock GetSensor(string name)
        {
            var sensor = GridTerminalSystem.GetBlockWithName(name) as IMySensorBlock;
            if (sensor == null)
            {
                throw new Exception("Unable to find sensor");
            }
            return (IMySensorBlock)sensor;
        }

        List<IMyDoor> GetDoors(string name)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            List<IMyDoor> doors = new List<IMyDoor>();
            GridTerminalSystem.SearchBlocksOfName(name, blocks);
            for (int i = 0; i < blocks.Count; i++)
            {
                doors.Add(blocks[i] as IMyDoor);
            }
            return doors;
        }

        List<IMyTextPanel> GetTextPanels(string name)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.SearchBlocksOfName(name, blocks);
            for (int i = 0; i < blocks.Count; i++)
            {
                panels.Add(blocks[i] as IMyTextPanel);
            }
            return panels;
        }

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
            IMyAirVent vent = GetVent();
            return (int)Math.Round((Decimal)(vent.GetOxygenLevel() * 100), 0);
        }

        void OpenDoors(string name)
        {
            List<IMyDoor> doors = GetDoors(name);
            for (int i = 0; i < doors.Count; i++)
            {
                doors[i].GetActionWithName("Open_On").Apply(doors[i]);
            }
        }

        void CloseDoors(string name)
        {
            List<IMyDoor> doors = GetDoors(name);
            for (int i = 0; i < doors.Count; i++)
            {
                doors[i].GetActionWithName("Open_Off").Apply(doors[i]);
            }
        }

        void CloseInteriorDoors()
        {
            CloseDoors(AIRLOCK_PREFIX + " Interior Door");
        }

        void OpenInteriorDoors()
        {
            OpenDoors(AIRLOCK_PREFIX + " Interior Door");
        }

        void OpenExteriorDoors()
        {
            OpenDoors(AIRLOCK_PREFIX + " Exterior Door");
        }

        void CloseExteriorDoors()
        {
            CloseDoors(AIRLOCK_PREFIX + " Exterior Door");
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
            List<IMyDoor> doors = GetDoors(name);
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
            return AreDoorsClosed(AIRLOCK_PREFIX + " Exterior Door");
        }

        bool AreInteriorDoorsClosed()
        {
            return AreDoorsClosed(AIRLOCK_PREFIX + " Interior Door");
        }

        bool IsActive(string name)
        {
            var sensor = GetSensor(name);
            if (sensor.IsActive)
            {
                DebugOutput(name + " Activated");
            }
            return sensor.IsActive;
        }

        void EnableSensor(string name)
        {
            var sensor = GetSensor(name);
            sensor.GetActionWithName("OnOff_On").Apply(sensor);
        }

        void DisableSensor(string name)
        {
            var sensor = GetSensor(name);
            sensor.GetActionWithName("OnOff_Off").Apply(sensor);
        }

        void EnableExteriorSensor()
        {
            EnableSensor(AIRLOCK_PREFIX + " Exterior Sensor");
        }

        void EnableInteriorSensor()
        {
            EnableSensor(AIRLOCK_PREFIX + " Interior Sensor");
        }

        void EnableChamberSensor()
        {
            EnableSensor(AIRLOCK_PREFIX + " Chamber Sensor");
        }

        void DisableSensors()
        {
            DisableSensor(AIRLOCK_PREFIX + " Exterior Sensor");
            DisableSensor(AIRLOCK_PREFIX + " Interior Sensor");
            DisableSensor(AIRLOCK_PREFIX + " Chamber Sensor");
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

        void RenderDebug()
        {
            string status = "";
            switch (currentState)
            {
                case _state_WAITING_FOR_PRESSURE:
                case _state_WAITING_FOR_EXTERIOR_DOORS:
                    status = "Pressurising";
                    break;
                case _state_WAITING_FOR_VACUUM:
                case _state_WAITING_FOR_INTERIOR_DOORS:
                    status = "Depressurising";
                    break;
                case _state_LOCKED:
                    status = "Locked";
                    break;
                default:
                    status = "Idle";
                    break;
            }

            string message = "           AIRLOCK STATUS: " + status + "\n-------------------------------------------------------------\n ";
            message += String.Join("\n ", logs);
            for (int i = 0; i < (13 - logs.Count); i++)
            {
                message += "\n";
            }
            message += "\n------------------------------------------------------------\n";
            message += " State: " + currentState.ToString();
            message += "      Oxygen: " + GetOxygenLevel().ToString() + "%";
            message += "      Tick: " + currentTick.ToString();

            List<IMyTextPanel> panels = GetTextPanels(AIRLOCK_PREFIX + " Debug");
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
