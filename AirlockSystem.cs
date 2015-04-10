#region header
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace AirlockSystem
{
    class AirlockSystem
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

        Airlock airlock;

        void Main()
        {
            BlockHelper blockHelper = new BlockHelper(GridTerminalSystem);
            if (airlock == null)
            {
                airlock = new Airlock(blockHelper);
            }
            airlock.execute(blockHelper);
        }

        protected class BlockHelper
        {
            IMyGridTerminalSystem GridTerminalSystem;

            public BlockHelper(IMyGridTerminalSystem GridTerminalSystem) {
                this.GridTerminalSystem = GridTerminalSystem;
            }

            public void ApplyAction(IMyTerminalBlock block, string actionName)
            {
                ITerminalAction action = block.GetActionWithName(actionName);
                if (action == null)
                {
                    throw new ArgumentException("Action could not be found: " + actionName);
                }
                action.Apply(block);
            }

            public void ApplyAction<T>(IList<T> blocks, string actionName)
            {
                for (int i = 0; i < blocks.Count; i++)
                {
                    this.ApplyAction(blocks[i] as IMyTerminalBlock, actionName);
                }
            }

            public IList<T> GetGroup<T>(string groupName)
            {
                for (int i = 0; i < this.GridTerminalSystem.BlockGroups.Count; i++)
                {
                    if (this.GridTerminalSystem.BlockGroups[i].Name == groupName)
                    {
                        return this.GridTerminalSystem.BlockGroups[i].Blocks.OfType<T>().ToList();
                    }
                }
                return new List<T>();
            }

            public IMyTerminalBlock GetBlockWithName(string blockName) {
                return this.GridTerminalSystem.GetBlockWithName(blockName);
            }

            public IList<T> SearchBlocksOfName<T>(string blockName) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                this.GridTerminalSystem.SearchBlocksOfName(blockName, blocks);
                return blocks.OfType<T>().ToList();
            }
        }

        protected class Vent
        {
            private IMyAirVent vent;
            private BlockHelper blockHelper;

            public Vent(BlockHelper blockHelper, IMyAirVent vent)
            {
                this.vent = vent;
                this.blockHelper = blockHelper;
            }

            public int GetOxygenLevel()
            {
                return (int) Math.Round((Decimal)(this.vent.GetOxygenLevel() * 100), 0);
            }

            public void EnableOxygen()
            {
                this.blockHelper.ApplyAction(this.vent, "Pressurize");
            }

            public void DisableOxygen()
            {
                this.blockHelper.ApplyAction(this.vent, "Pressurize");
            }
        }

        protected class Door
        {
            private IMyDoor door;
            private BlockHelper blockHelper;

            public Door(BlockHelper blockHelper, IMyDoor door)
            {
                this.door = door;
                this.blockHelper = blockHelper;
            }

            public bool IsClosed()
            {
                return !this.door.Open;
            }

            public void Open()
            {
                blockHelper.ApplyAction(this.door, "Open_On");
            }

            public void Close()
            {
                blockHelper.ApplyAction(this.door, "Open_Off");
            }

            public void Disable()
            {
                this.blockHelper.ApplyAction(this.door, "OnOff_Off");
            }

            public void Enable()
            {
                this.blockHelper.ApplyAction(this.door, "OnOff_On");
            }
        }

        protected class DoorGroup
        {
            private IList<Door> doors = new List<Door>();
            private BlockHelper blockHelper;

            public DoorGroup(BlockHelper blockHelper, string name)
            {
                this.blockHelper = blockHelper;

                IList<IMyDoor> blocks = blockHelper.GetGroup<IMyDoor>(name);
                for (int i = 0; i < blocks.Count; i++) {
                    this.doors.Add(new Door(this.blockHelper, blocks[i]));
                }
            }

            public bool IsClosed()
            {
                for (int i = 0; i < doors.Count; i++)
                {
                    if (!this.doors[i].IsClosed())
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Open()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Open();
                }
            }

            public void Close()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Close();
                }
            }

            public void Enable()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Enable();
                }
            }

            public void Disable()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Disable();
                }
            }
        }

        protected class Sensor
        {
            private IMySensorBlock sensor;
            private BlockHelper blockHelper;

            public Sensor(BlockHelper blockHelper, IMySensorBlock sensor)
            {
                this.sensor = sensor;
                this.blockHelper = blockHelper;
            }

            public bool IsActive() {
                return this.sensor.IsActive;
            }

            public void Enable()
            {
                this.blockHelper.ApplyAction(this.sensor, "OnOff_On");
            }

            public void Disable()
            {
                this.blockHelper.ApplyAction(this.sensor, "OnOff_Off");
            }
        }

        protected class SensorGroup
        {
            private IList<Sensor> sensors = new List<Sensor>();
            private BlockHelper blockHelper;

            public SensorGroup(BlockHelper blockHelper, string name)
            {
                this.blockHelper = blockHelper;
                IList<IMySensorBlock> blocks = this.blockHelper.GetGroup<IMySensorBlock>(name);
                for (int i = 0; i < blocks.Count; i++)
                {
                    this.sensors.Add(new Sensor(this.blockHelper, blocks[i]));
                }
            }

            public bool IsActive()
            {
                for (int i = 0; i < this.sensors.Count; i++)
                {
                    if (this.sensors[i].IsActive())
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Enable()
            {
                for (int i = 0; i < this.sensors.Count; i++)
                {
                    this.sensors[i].Enable();
                }
            }

            public void Disable()
            {
                for (int i = 0; i < this.sensors.Count; i++)
                {
                    this.sensors[i].Disable();
                }
            }
        }

        protected class VentGroup {
            private IList<Vent> vents = new List<Vent>();
            private Door valve;
            private BlockHelper blockHelper;

            public VentGroup(BlockHelper blockHelper)
            {
                this.blockHelper = blockHelper;

                IList<IMyAirVent> blocks = this.blockHelper.GetGroup<IMyAirVent>(_VENTS);
                for (int i = 0; i < blocks.Count; i++) {
                    this.vents.Add(new Vent(this.blockHelper, blocks[i]));
                }
                
                IMyDoor door = this.blockHelper.GetBlockWithName(_VALVE) as IMyDoor;
                if (door != null) {
                    this.valve = new Door(this.blockHelper, door);
                }
            }

            public int GetOxygenLevel() {
                if (this.vents.Count == 0) {
                    return 0;
                }

                return this.vents[0].GetOxygenLevel();
            }

            public void EnableOxygen() {
                for (int i = 0; i < this.vents.Count; i++) {
                    this.vents[i].EnableOxygen();
                }
            }

            public void DisableOxygen() {
                for (int i = 0; i < this.vents.Count; i++) {
                    this.vents[i].DisableOxygen();
                }
            }

            public void OpenValve() {
                if (this.valve != null) {
                    this.valve.Open();
                }
            }

            public void CloseValve() {
                if (this.valve != null) {
                    this.valve.Close();
                }
            }
        }

        protected class DebugOutput
        {
            private IList<string> logs = new List<string>();
            private Airlock airlock;
            private BlockHelper blockHelper;

            public DebugOutput(BlockHelper blockHelper, Airlock airlock)
            {
                this.airlock = airlock;
                this.blockHelper = blockHelper;
            }

            public void AddLog(string message) {
                this.logs.Add(message);
                this.TrimLogs();
                this.Render();
            }

            private void TrimLogs()
            {
                int toRemove = 13 - this.logs.Count;
                if (toRemove > 0)
                {
                    for (int i = 0; i < toRemove; i++)
                    {
                        this.logs.RemoveAt(0);
                    }
                }
            }

            public void Render()
            {
                string message = "           AIRLOCK STATUS: " + this.airlock.GetStatus() + this.Line();
                message += String.Join("\n ", logs);
                for (int i = 0; i < (13 - logs.Count); i++)
                {
                    message += "\n";
                }
                message += this.Line();
                message += " State: " + this.airlock.State.ToString();
                message += "      Oxygen: " + this.airlock.GetOxygenLevel().ToString() + "%";
                message += "      Tick: " + this.airlock.Tick.ToString();

                IList<IMyTextPanel> panels = this.blockHelper.SearchBlocksOfName<IMyTextPanel>(_DEBUG);
                for (int i = 0; i < panels.Count; i++)
                {
                    panels[i].WritePublicText(message, false);
                    panels[i].ShowPublicTextOnScreen();
                }
            }

            private string Line()
            {
                return "\n-------------------------------------------------------------\n";
            }
        }

        protected class Timer
        {
            private IMyTimerBlock timer;
            private BlockHelper blockHelper;

            public Timer(BlockHelper blockHelper)
            {
                this.blockHelper = blockHelper;
                this.timer = this.blockHelper.GetBlockWithName(_TIMER) as IMyTimerBlock;
                if (this.timer == null)
                {
                    throw new Exception("Unable to find a timer block called " + _TIMER);
                }
            }

            public void Start()
            {
                this.blockHelper.ApplyAction(this.timer, "OnOff_On");
                this.blockHelper.ApplyAction(this.timer, "Start");
            }

            public void Stop()
            {
                this.blockHelper.ApplyAction(this.timer, "OnOff_Off");
            }
        }

        protected class Airlock
        {
            private BlockHelper blockHelper;
            private int state = _state_NONE;
            private int tick = 0;
            private DebugOutput debug;

            public int State {
                get {
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

            private SensorGroup chamberSensors {
                get {
                    return new SensorGroup(this.blockHelper, _sensor_CHAMBER);
                }
            }

            private SensorGroup interiorSensors {
                get {
                    return new SensorGroup(this.blockHelper, _sensor_INTERIOR);
                }
            }

            private SensorGroup exteriorSensors {
                get {
                    return new SensorGroup(this.blockHelper, _sensor_EXTERIOR);
                }
            }

            private DoorGroup interiorDoors {
                get {
                    return new DoorGroup(this.blockHelper, _door_INTERIOR);
                }
            }

            private DoorGroup exteriorDoors {
                get {
                    return new DoorGroup(this.blockHelper, _door_EXTERIOR);
                }
            }

            private VentGroup vents
            {
                get
                {
                    return new VentGroup(this.blockHelper);
                }
            }

            private Timer timer
            {
                get
                {
                    return new Timer(this.blockHelper);
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
                        if ((this.vents.GetOxygenLevel() >= 100) || (this.tick > VENT_TIMEOUT))
                        {
                            this.OpenInterior();
                        }
                        break;

                    case _state_WAITING_FOR_VACUUM:
                        if (this.vents.GetOxygenLevel() <= 0)
                        {
                            OpenExterior();
                            return;
                        }
                        else if (this.tick > VENT_TIMEOUT)
                        {
                            this.vents.OpenValve();
                            return;
                        }
                        break;

                    case _state_WAITING_FOR_INTERIOR_DOORS:
                        if (this.interiorDoors.IsClosed() || (tick > DOOR_TIMEOUT))
                        {
                            this.ContinueDepressurisation();
                        }
                        break;

                    case _state_WAITING_FOR_EXTERIOR_DOORS:
                        if (this.exteriorDoors.IsClosed() || (tick > DOOR_TIMEOUT))
                        {
                            this.ContinuePressurisation();
                        }
                        break;
                }
            }

            public Airlock(BlockHelper blockHelper)
            {
                this.blockHelper = blockHelper;
                this.Initialise();
            }

            private void Initialise()
            {
                this.state = _state_READY;
                this.debug = new DebugOutput(this.blockHelper, this);
            }

            private void StartAirlock()
            {
                this.state = _state_LOCKED;

                // Detect what's happening from what sensor  
                if (this.chamberSensors.IsActive())
                {
                    if (this.interiorDoors.IsClosed())
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

                if (this.interiorSensors.IsActive())
                {
                    // They walked up to the interior sensor  
                    if (this.interiorDoors.IsClosed())
                    {
                        this.PressuriseAirlock();
                        return;
                    }
                }

                if (this.exteriorSensors.IsActive())
                {
                    if (this.exteriorDoors.IsClosed())
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
                this.state = _state_READY;
                this.debug.AddLog("Opening Interior Doors");
                this.interiorDoors.Open();
                this.timer.Stop();
                this.exteriorSensors.Enable();
                this.chamberSensors.Enable();
                this.debug.AddLog("*** Pressurisation Complete ***");
            }

            private void OpenExterior()
            {
                this.debug.AddLog("Opening Exterior Doors");
                this.state = _state_READY;
                this.exteriorDoors.Open();
                this.timer.Stop();
                this.interiorSensors.Enable();
                this.chamberSensors.Enable();
                this.debug.AddLog("***Depressurisation Complete***");
            }

            private void PressuriseAirlock()
            {
                this.debug.AddLog("*** Starting Pressurisation Sequence ****");
                this.DisableSensors();
                this.timer.Start();
                this.exteriorDoors.Close();
                this.state = _state_WAITING_FOR_EXTERIOR_DOORS;
                this.debug.AddLog("Waiting for exterior doors to close");
            }

            private void ContinuePressurisation()
            {
                this.debug.AddLog("Exterior doors closed");
                this.vents.EnableOxygen();
                this.state = _state_WAITING_FOR_PRESSURE;
                this.debug.AddLog("Waiting for air pressure");
            }

            private void DepressuriseAirlock()
            {
                this.debug.AddLog("*** Starting Depressurisation Sequence ****");
                this.DisableSensors();
                this.timer.Start();
                this.interiorDoors.Close();
                this.state = _state_WAITING_FOR_INTERIOR_DOORS;
                this.debug.AddLog("Waiting for interior doors to close");
            }

            private void ContinueDepressurisation()
            {
                this.debug.AddLog("Interior doors closed");
                this.vents.DisableOxygen();
                this.state = _state_WAITING_FOR_VACUUM;
                this.debug.AddLog("Waiting for vacuum");
            }

            private void DisableSensors()
            {
                this.chamberSensors.Disable();
                this.exteriorSensors.Disable();
                this.interiorSensors.Disable();
            }

            public string GetStatus()
            {
                switch (this.state)
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

            public int GetOxygenLevel()
            {
                return this.vents.GetOxygenLevel();
            }
        }
#endregion
#region footer
    }
}
#endregion
