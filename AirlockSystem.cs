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
        const string AIRLOCK_PREFIX = "Lab Airlock";

        // This is how long in ticks (normally seconds) to wait for vents and doors to open/close/depressurize
        const int VENT_TIMEOUT = 5;
        const int DOOR_TIMEOUT = 5;
        const int DOOR_DELAY = 1;

        /* Nothing from here on needs editing */

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
        const string _sensor_INTERIOR = AIRLOCK_PREFIX + " Interior Sensors";
        const string _sensor_EXTERIOR = AIRLOCK_PREFIX + " Exterior Sensors";
        const string _sensor_CHAMBER = AIRLOCK_PREFIX + " Chamber Sensors";

        // The names of our door groups
        const string _door_INTERIOR = AIRLOCK_PREFIX + " Interior Doors";
        const string _door_EXTERIOR = AIRLOCK_PREFIX + " Exterior Doors";

        // One-off blocks that are needed
        const string _VENTS = AIRLOCK_PREFIX + " Vents";
        const string _VALVE = AIRLOCK_PREFIX + " Valve";
        const string _TIMER = AIRLOCK_PREFIX + " Timer";
        const string _DEBUG = AIRLOCK_PREFIX + " Debug";

        // Version String
        const string VERSION = "Entropy Airlock v6.4";
        const string GITHUB_URL = "https://github.com/murray-mint/se-airlock";

        Airlock airlock = new Airlock();

        void Main()
        {
            BlockHelper blockHelper = new BlockHelper(GridTerminalSystem);
            airlock.execute(blockHelper);
        }

        public class BlockHelper
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
                List<T> groupBlocks = new List<T>();
                for (int i = 0; i < this.GridTerminalSystem.BlockGroups.Count; i++)
                {
                    if (this.GridTerminalSystem.BlockGroups[i].Name == groupName)
                    {

                        for (int j = 0; j < this.GridTerminalSystem.BlockGroups[i].Blocks.Count; j++)
                        {
                            var block = this.GridTerminalSystem.BlockGroups[i].Blocks[j];
                            if (block is T) {
                                groupBlocks.Add((T)block);

                            }
                        }
                        return groupBlocks;
                    }
                }
                return groupBlocks;
            }

            public IMyTerminalBlock GetBlockWithName(string blockName) {
                return this.GridTerminalSystem.GetBlockWithName(blockName);
            }

            public IList<T> SearchBlocksOfName<T>(string blockName) {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                this.GridTerminalSystem.SearchBlocksOfName(blockName, blocks);
                List<T> result = new List<T>();
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i] is T)
                    {
                        result.Add((T) blocks[i]);
                    }
                }
                return result;
            }
        }

        public class Vent
        {
            private IMyAirVent vent;
            private BlockHelper blockHelper;

            public Vent(BlockHelper blockHelper, IMyAirVent vent)
            {
                this.vent = vent;
                this.blockHelper = blockHelper;
            }

            public int GetVentOxygenLevel()
            {
                return (int) Math.Round((Decimal)(this.vent.GetOxygenLevel() * 100), 0);
            }

            public void EnableVentOxygen()
            {
                this.blockHelper.ApplyAction(this.vent, "Depressurize");
            }

            public void DisableVentOxygen()
            {
                this.blockHelper.ApplyAction(this.vent, "Depressurize");
            }
        }

        public class Door
        {
            private IMyDoor door;
            private BlockHelper blockHelper;

            public Door(BlockHelper blockHelper, IMyDoor door)
            {
                this.door = door;
                this.blockHelper = blockHelper;
            }

            public bool IsDoorClosed()
            {
                return !this.door.Open;
            }

            public void OpenDoor()
            {
                this.EnableDoor();
                blockHelper.ApplyAction(this.door, "Open_On");
            }

            public void CloseDoor()
            {
                this.EnableDoor();
                blockHelper.ApplyAction(this.door, "Open_Off");
            }

            public void DisableDoor()
            {
                this.blockHelper.ApplyAction(this.door, "OnOff_Off");
            }

            public void EnableDoor()
            {
                this.blockHelper.ApplyAction(this.door, "OnOff_On");
            }
        }

        public class DoorGroup
        {
            private IList<DoorStruct> doors = new List<DoorStruct>();
            private BlockHelper blockHelper;

            public DoorGroup(BlockHelper blockHelper, string name)
            {
                this.blockHelper = blockHelper;

                IList<IMyDoor> blocks = blockHelper.GetGroup<IMyDoor>(name);
                for (int i = 0; i < blocks.Count; i++) {
                    Door door = new Door(this.blockHelper, blocks[i]);
                    this.doors.Add(new DoorStruct(door));
                }
            }

            public bool AreDoorsClosed()
            {
                for (int i = 0; i < doors.Count; i++)
                {
                    if (!this.doors[i].Door.IsDoorClosed())
                    {
                        return false;
                    }
                }
                return true;
            }

            public void OpenDoors()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Door.OpenDoor();
                }
            }

            public void CloseDoors()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Door.CloseDoor();
                }
            }

            public void EnableDoors()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Door.EnableDoor();
                }
            }

            public void DisableDoors()
            {
                for (int i = 0; i < this.doors.Count; i++)
                {
                    this.doors[i].Door.DisableDoor();
                }
            }
        }

        public class Sensor
        {
            private IMySensorBlock sensor;
            private BlockHelper blockHelper;

            public Sensor(BlockHelper blockHelper, IMySensorBlock sensor)
            {
                this.sensor = sensor;
                this.blockHelper = blockHelper;
            }

            public bool IsSensorActive() {
                return this.sensor.IsActive;
            }

            public void EnableSensor()
            {
                this.blockHelper.ApplyAction(this.sensor, "OnOff_On");
            }

            public void DisableSensor()
            {
                this.blockHelper.ApplyAction(this.sensor, "OnOff_Off");
            }
        }

        public class SensorGroup
        {
            private IList<SensorStruct> sensors = new List<SensorStruct>();
            private BlockHelper blockHelper;

            public SensorGroup(BlockHelper blockHelper, string name)
            {
                this.blockHelper = blockHelper;
                IList<IMySensorBlock> blocks = this.blockHelper.GetGroup<IMySensorBlock>(name);
                for (int i = 0; i < blocks.Count; i++)
                {
                    Sensor sensor = new Sensor(this.blockHelper, blocks[i]);
                    this.sensors.Add(new SensorStruct(sensor));
                }
            }

            public bool AreSensorsActive()
            {
                for (int i = 0; i < this.sensors.Count; i++)
                {
                    if (this.sensors[i].Sensor.IsSensorActive())
                    {
                        return true;
                    }
                }
                return false;
            }

            public void EnableSensors()
            {
                for (int i = 0; i < this.sensors.Count; i++)
                {
                    this.sensors[i].Sensor.EnableSensor();
                }
            }

            public void DisableSensors()
            {
                for (int i = 0; i < this.sensors.Count; i++)
                {
                    this.sensors[i].Sensor.DisableSensor();
                }
            }
        }

        public class VentGroup
        {
            private IList<VentStruct> vents = new List<VentStruct>();
            private Door valve;
            private BlockHelper blockHelper;

            public VentGroup(BlockHelper blockHelper)
            {
                this.blockHelper = blockHelper;

                IList<IMyAirVent> blocks = this.blockHelper.GetGroup<IMyAirVent>(_VENTS);
                for (int i = 0; i < blocks.Count; i++) {
                    Vent vent = new Vent(this.blockHelper, blocks[i]);
                    this.vents.Add(new VentStruct(vent));
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

                return this.vents[0].Vent.GetVentOxygenLevel();
            }

            public void EnableOxygen() {
                this.CloseValve();
                for (int i = 0; i < this.vents.Count; i++) {
                    this.vents[i].Vent.EnableVentOxygen();
                }
            }

            public void DisableOxygen() {
                for (int i = 0; i < this.vents.Count; i++) {
                    this.vents[i].Vent.DisableVentOxygen();
                }
            }

            public bool HasValve()
            {
                if (this.valve == null)
                {
                    return false;
                }
                return true;
            }

            public void OpenValve() {
                if (this.valve != null) {
                    this.valve.OpenDoor();
                }
            }

            public void CloseValve() {
                if (this.valve != null) {
                    this.valve.CloseDoor();
                }
            }
        }

        public struct VentStruct
        {
            public Vent Vent;

            public VentStruct(Vent vent)
            {
                this.Vent = vent;
            }
        }

        public struct DoorStruct
        {
            public Door Door;

            public DoorStruct(Door door)
            {
                this.Door = door;
            }
        }

        public struct SensorStruct
        {
            public Sensor Sensor;

            public SensorStruct(Sensor sensor)
            {
                this.Sensor = sensor;
            }
        }

        public class DebugOutput
        {
            private List<string> logs = new List<string>();
            private Airlock airlock;
            private BlockHelper blockHelper;

            public DebugOutput(BlockHelper blockHelper, Airlock airlock)
            {
                this.airlock = airlock;
                this.blockHelper = blockHelper;
            }

            public void AddLog(string message)
            {
                string last = "";
                if (this.logs.Count > 0)
                {
                    last = this.logs[this.logs.Count - 1];
                }
                if (last != message)
                {
                    this.logs.Add(message);
                }
                this.TrimLogs();
                this.Render();
            }

            private void TrimLogs()
            {
                int count = 13;
                int index = this.logs.Count - count;
                if (index < 0)
                {
                    index = 0;
                }
                if (count > this.logs.Count)
                {
                    count = this.logs.Count;
                }
                this.logs = this.logs.GetRange(index, count);
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
                message += "      Oxygen: " + this.airlock.GetAirlockOxygenLevel().ToString() + "%";
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
                return "\n-------------------------------------------------------------\n ";
            }
        }

        public class Timer
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

            public void StartTimer()
            {
                this.blockHelper.ApplyAction(this.timer, "OnOff_On");
                this.blockHelper.ApplyAction(this.timer, "Start");
            }

            public void StopTimer()
            {
                this.blockHelper.ApplyAction(this.timer, "OnOff_Off");
            }
        }

        public class Airlock
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
                        else if (this.tick > VENT_TIMEOUT)
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
                        if (this.interiorDoors.AreDoorsClosed() || (tick > DOOR_TIMEOUT))
                        {
                            this.tick = 0;
                            this.state = _state_WAITING_FOR_INTERIOR_DOORLOCK;
                        }
                        break;

                    case _state_WAITING_FOR_INTERIOR_DOORLOCK:
                        if (this.tick > DOOR_DELAY)
                        {
                            this.tick = 0;
                            this.ContinueDepressurisation();
                        }
                        break;

                    case _state_WAITING_FOR_EXTERIOR_DOORS:
                        if (this.exteriorDoors.AreDoorsClosed() || (tick > DOOR_TIMEOUT))
                        {
                            this.tick = 0;
                            this.state = _state_WAITING_FOR_EXTERIOR_DOORLOCK;
                        }
                        break;

                    case _state_WAITING_FOR_EXTERIOR_DOORLOCK:
                        if (this.tick > DOOR_DELAY)
                        {
                            this.tick = 0;
                            this.ContinuePressurisation();
                        }
                        break;

                    case _state_COMPLETE_WAITING_FOR_INTERIOR_DOORS:
                        if (!this.interiorDoors.AreDoorsClosed() || (tick > DOOR_TIMEOUT))
                        {
                            this.tick = 0;
                            this.state = _state_WAITING_FOR_INTERIOR_DOOROPEN;
                        }
                        break;

                    case _state_WAITING_FOR_INTERIOR_DOOROPEN:
                        if (this.tick > DOOR_DELAY)
                        {
                            this.tick = 0;
                            this.CompleteAirlockPressurisation();
                        }
                        break;

                    case _state_COMPLETE_WAITING_FOR_EXTERIOR_DOORS:
                        if (!this.exteriorDoors.AreDoorsClosed() || (tick > DOOR_TIMEOUT))
                        {
                            this.tick = 0;
                            this.state = _state_WAITING_FOR_EXTERIOR_DOOROPEN;
                        }
                        break;

                    case _state_WAITING_FOR_EXTERIOR_DOOROPEN:
                        if (this.tick > DOOR_DELAY)
                        {
                            this.tick = 0;
                            this.CompleteAirlockDepressurisation();
                        }
                        break;

                    case _state_INITIALISATION_PRESSURISED:
                        if (this.tick > DOOR_DELAY)
                        {
                            this.tick = 0;
                            this.CompleteAirlockPressurisation();
                            this.execute(blockHelper);
                        }
                        break;

                    case _state_INITIALISATION_UNPRESSURISED:
                        if (this.tick > DOOR_DELAY)
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
                this.debug = new DebugOutput(this.blockHelper, this);
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
#region footer
    }
}
#endregion
