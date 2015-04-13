using AirlockSystem.Blocks;
using AirlockSystem.Groups;
using AirlockSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AirlockSystem;

namespace AirlockSystem
{
    #region SpaceEngineers
    public class Airlock
    {

        // State Table
        const int _state_NONE = 0;
        const int _state_READY = 1;
        const int _state_LOCKED = 2;

        const int _state_WAITING_FOR_EXTERIOR_DOORS = 10;
        const int _state_WAITING_FOR_EXTERIOR_DOORLOCK = 11;
        const int _state_WAITING_FOR_PRESSURE = 12;
        const int _state_COMPLETE_WAITING_FOR_INTERIOR_DOORS = 13;
        const int _state_WAITING_FOR_INTERIOR_DOOROPEN = 14;
        const int _state_INITIALISATION_PRESSURISED = 15;

        const int _state_WAITING_FOR_INTERIOR_DOORS = 20;
        const int _state_WAITING_FOR_INTERIOR_DOORLOCK = 21;
        const int _state_WAITING_FOR_VACUUM = 22;
        const int _state_COMPLETE_WAITING_FOR_EXTERIOR_DOORS = 23;
        const int _state_WAITING_FOR_EXTERIOR_DOOROPEN = 24;
        const int _state_INITIALISATION_UNPRESSURISED = 25;

        // The names of our sensor groups
        const string _sensor_INTERIOR = UserConfig.AIRLOCK_PREFIX + " Interior Sensors";
        const string _sensor_EXTERIOR = UserConfig.AIRLOCK_PREFIX + " Exterior Sensors";
        const string _sensor_CHAMBER = UserConfig.AIRLOCK_PREFIX + " Chamber Sensors";

        // The names of our door groups
        const string _door_INTERIOR = UserConfig.AIRLOCK_PREFIX + " Interior Doors";
        const string _door_EXTERIOR = UserConfig.AIRLOCK_PREFIX + " Exterior Doors";

        // One-off blocks that are needed
        const string _VENTS = UserConfig.AIRLOCK_PREFIX + " Vents";
        const string _VALVE = UserConfig.AIRLOCK_PREFIX + " Valve";
        const string _TIMER = UserConfig.AIRLOCK_PREFIX + " Timer";
        const string _DEBUG = UserConfig.AIRLOCK_PREFIX + " Debug";

        // Version String
        const string VERSION = "Entropy Airlock v6.4";
        const string GITHUB_URL = "https://github.com/murray-mint/se-airlock";

        private BlockHelper blockHelper;
        private int state = _state_NONE;
        private int tick = 0;
        private DebugOutput debug;

        public int State
        {
            get
            {
                return this.state;
            }
        }

        public int Tick
        {
            get
            {
                return this.tick;
            }
        }

        private SensorGroup chamberSensors
        {
            get
            {
                return new SensorGroup(this.blockHelper, _sensor_CHAMBER);
            }
        }

        private SensorGroup interiorSensors
        {
            get
            {
                return new SensorGroup(this.blockHelper, _sensor_INTERIOR);
            }
        }

        private SensorGroup exteriorSensors
        {
            get
            {
                return new SensorGroup(this.blockHelper, _sensor_EXTERIOR);
            }
        }

        private DoorGroup interiorDoors
        {
            get
            {
                return new DoorGroup(this.blockHelper, _door_INTERIOR);
            }
        }

        private DoorGroup exteriorDoors
        {
            get
            {
                return new DoorGroup(this.blockHelper, _door_EXTERIOR);
            }
        }

        private VentGroup vents
        {
            get
            {
                return new VentGroup(this.blockHelper, _VENTS, _VALVE);
            }
        }

        private Timer timer
        {
            get
            {
                return new Timer(this.blockHelper, _TIMER);
            }
        }

        public void execute(BlockHelper blockHelper)
        {
            this.blockHelper = blockHelper;
            this.tick++;

            switch (this.State)
            {
                case _state_NONE:
                    this.tick = 0;
                    this.Initialise();
                    break;

                case _state_READY:
                    this.tick = 0;
                    this.StartAirlock();
                    break;

                case _state_LOCKED:
                    this.tick = 0;
                    break;

                case _state_WAITING_FOR_PRESSURE:
                    if ((this.vents.GetOxygenLevel() >= 100) || (this.tick > UserConfig.VENT_TIMEOUT))
                    {
                        this.tick = 0;
                        this.OpenInterior();
                    }
                    break;

                case _state_WAITING_FOR_VACUUM:
                    if (this.vents.GetOxygenLevel() <= 0)
                    {
                        this.tick = 0;
                        this.OpenExterior();
                    }
                    else if (this.tick > UserConfig.VENT_TIMEOUT)
                    {
                        if (this.vents.HasValve())
                        {
                            this.vents.OpenValve();
                        }
                        else
                        {
                            this.tick = 0;
                            this.OpenExterior();
                        }
                    }
                    break;

                case _state_WAITING_FOR_INTERIOR_DOORS:
                    if (this.interiorDoors.AreDoorsClosed() || (tick > UserConfig.DOOR_TIMEOUT))
                    {
                        this.tick = 0;
                        this.state = _state_WAITING_FOR_INTERIOR_DOORLOCK;
                    }
                    break;

                case _state_WAITING_FOR_INTERIOR_DOORLOCK:
                    if (this.tick > UserConfig.DOOR_DELAY)
                    {
                        this.tick = 0;
                        this.ContinueDepressurisation();
                    }
                    break;

                case _state_WAITING_FOR_EXTERIOR_DOORS:
                    if (this.exteriorDoors.AreDoorsClosed() || (tick > UserConfig.DOOR_TIMEOUT))
                    {
                        this.tick = 0;
                        this.state = _state_WAITING_FOR_EXTERIOR_DOORLOCK;
                    }
                    break;

                case _state_WAITING_FOR_EXTERIOR_DOORLOCK:
                    if (this.tick > UserConfig.DOOR_DELAY)
                    {
                        this.tick = 0;
                        this.ContinuePressurisation();
                    }
                    break;

                case _state_COMPLETE_WAITING_FOR_INTERIOR_DOORS:
                    if (!this.interiorDoors.AreDoorsClosed() || (tick > UserConfig.DOOR_TIMEOUT))
                    {
                        this.tick = 0;
                        this.state = _state_WAITING_FOR_INTERIOR_DOOROPEN;
                    }
                    break;

                case _state_WAITING_FOR_INTERIOR_DOOROPEN:
                    if (this.tick > UserConfig.DOOR_DELAY)
                    {
                        this.tick = 0;
                        this.CompleteAirlockPressurisation();
                    }
                    break;

                case _state_COMPLETE_WAITING_FOR_EXTERIOR_DOORS:
                    if (!this.exteriorDoors.AreDoorsClosed() || (tick > UserConfig.DOOR_TIMEOUT))
                    {
                        this.tick = 0;
                        this.state = _state_WAITING_FOR_EXTERIOR_DOOROPEN;
                    }
                    break;

                case _state_WAITING_FOR_EXTERIOR_DOOROPEN:
                    if (this.tick > UserConfig.DOOR_DELAY)
                    {
                        this.tick = 0;
                        this.CompleteAirlockDepressurisation();
                    }
                    break;

                case _state_INITIALISATION_PRESSURISED:
                    if (this.tick > UserConfig.DOOR_DELAY)
                    {
                        this.tick = 0;
                        this.CompleteAirlockPressurisation();
                        this.execute(blockHelper);
                    }
                    break;

                case _state_INITIALISATION_UNPRESSURISED:
                    if (this.tick > UserConfig.DOOR_DELAY)
                    {
                        this.tick = 0;
                        this.CompleteAirlockDepressurisation();
                        this.execute(blockHelper);
                    }
                    break;
            }

            this.debug.Render();
        }

        private void Initialise()
        {
            this.debug = new DebugOutput(this.blockHelper, this, _DEBUG);
            this.debug.AddLog("Initialising");
            this.debug.AddLog(VERSION);
            this.debug.AddLog(GITHUB_URL);
            this.DisableAllSensors();
            if (this.exteriorDoors.AreDoorsClosed())
            {
                this.debug.AddLog("Initial state is pressurised");
                this.interiorDoors.OpenDoors();
                this.state = _state_INITIALISATION_PRESSURISED;
            }
            else
            {
                this.debug.AddLog("Initial state is unpressurised");
                this.interiorDoors.CloseDoors();
                this.state = _state_INITIALISATION_UNPRESSURISED;
            }
            this.debug.AddLog("Initialisation Complete");
            this.timer.StartTimer();
        }

        private void StartAirlock()
        {
            this.state = _state_LOCKED;
            // Detect what's happening from what sensor  
            if (this.chamberSensors.AreSensorsActive())
            {
                if (this.interiorDoors.AreDoorsClosed())
                {
                    // Interior doors are closed, they probably walked from outside  
                    this.PressuriseAirlock();
                }
                else
                {
                    // Interior doors are open, they probably walked from inside  
                    this.DepressuriseAirlock();
                }
                return;
            }

            if (this.interiorSensors.AreSensorsActive())
            {
                // They walked up to the interior sensor  
                if (this.interiorDoors.AreDoorsClosed())
                {
                    this.PressuriseAirlock();
                    return;
                }
            }

            if (this.exteriorSensors.AreSensorsActive())
            {
                if (this.exteriorDoors.AreDoorsClosed())
                {
                    this.DepressuriseAirlock();
                    return;
                }
            }

            // We did nothing, let's lock it again  
            this.state = _state_READY;
        }

        private void OpenInterior()
        {
            this.state = _state_COMPLETE_WAITING_FOR_INTERIOR_DOORS;
            this.debug.AddLog("Opening Interior Doors");
            this.interiorDoors.OpenDoors();
        }

        private void OpenExterior()
        {
            this.state = _state_COMPLETE_WAITING_FOR_EXTERIOR_DOORS;
            this.debug.AddLog("Opening Exterior Doors");
            this.exteriorDoors.OpenDoors();
        }

        private void CompleteAirlockPressurisation()
        {
            this.state = _state_READY;
            this.timer.StopTimer();
            this.exteriorDoors.DisableDoors();
            this.interiorDoors.DisableDoors();
            this.chamberSensors.EnableSensors();

            this.interiorSensors.DisableSensors();
            this.exteriorSensors.EnableSensors();
            this.debug.AddLog("*** Pressurisation Complete ***");
        }

        private void CompleteAirlockDepressurisation()
        {
            this.state = _state_READY;
            this.timer.StopTimer();
            this.exteriorDoors.DisableDoors();
            this.interiorDoors.DisableDoors();
            this.chamberSensors.EnableSensors();

            this.interiorSensors.EnableSensors();
            this.exteriorSensors.DisableSensors();
            this.debug.AddLog("***Depressurisation Complete***");
        }

        private void PressuriseAirlock()
        {
            this.debug.AddLog("*** Starting Pressurisation Sequence ****");
            this.DisableAllSensors();
            this.timer.StartTimer();
            this.exteriorDoors.CloseDoors();
            this.state = _state_WAITING_FOR_EXTERIOR_DOORS;
            this.debug.AddLog("Waiting for exterior doors to close");
        }

        private void ContinuePressurisation()
        {
            this.debug.AddLog("Exterior doors closed");
            this.exteriorDoors.DisableDoors();
            this.vents.EnableOxygen();
            this.state = _state_WAITING_FOR_PRESSURE;
            this.debug.AddLog("Waiting for air pressure");
        }

        private void DepressuriseAirlock()
        {
            this.debug.AddLog("*** Starting Depressurisation Sequence ****");
            this.DisableAllSensors();
            this.timer.StartTimer();
            this.interiorDoors.CloseDoors();
            this.state = _state_WAITING_FOR_INTERIOR_DOORS;
            this.debug.AddLog("Waiting for interior doors to close");
        }

        private void ContinueDepressurisation()
        {
            this.debug.AddLog("Interior doors closed");
            this.interiorDoors.DisableDoors();
            this.vents.DisableOxygen();
            this.state = _state_WAITING_FOR_VACUUM;
            this.debug.AddLog("Waiting for vacuum");
        }

        private void DisableAllSensors()
        {
            this.chamberSensors.DisableSensors();
            this.exteriorSensors.DisableSensors();
            this.interiorSensors.DisableSensors();
        }

        public string GetStatus()
        {
            switch (this.state)
            {
                case _state_WAITING_FOR_PRESSURE:
                case _state_WAITING_FOR_EXTERIOR_DOORS:
                case _state_WAITING_FOR_EXTERIOR_DOORLOCK:
                case _state_COMPLETE_WAITING_FOR_INTERIOR_DOORS:
                case _state_WAITING_FOR_INTERIOR_DOOROPEN:
                    return "Pressurising";

                case _state_WAITING_FOR_VACUUM:
                case _state_WAITING_FOR_INTERIOR_DOORS:
                case _state_WAITING_FOR_INTERIOR_DOORLOCK:
                case _state_COMPLETE_WAITING_FOR_EXTERIOR_DOORS:
                case _state_WAITING_FOR_EXTERIOR_DOOROPEN:
                    return "Depressurising";

                case _state_LOCKED:
                    return "Locked";

                case _state_NONE:
                case _state_INITIALISATION_PRESSURISED:
                case _state_INITIALISATION_UNPRESSURISED:
                    return "Initialising";

                case _state_READY:
                    return "Ready";

                default:
                    return "Offline";
            }
        }

        public int GetAirlockOxygenLevel()
        {
            return this.vents.GetOxygenLevel();
        }
    }
    #endregion
}
