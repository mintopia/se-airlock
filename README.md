# Space Engineers Airlock

This is a in-game programming block script for controlling and managing an Airlock. The main features are:

 * Fully automatic operation
 * Safety Valve to prevent explosive decompression
 
## Setup

The airlock works based on the names of the groups and blocks involved. You will need to decide on a prefix for your airlock. This needs to be entered at the top of the script.

The groups and blocks you need are:

### Programmable Block

**Name** ```<AIRLOCK_PREFIX> Control```

The contents of the script should be entered into this block. It controls the airlock and all other blocks and groups. The name is not essential, but it helps to give it the same name as the airlock.

### Timer Block

**Name** ```<AIRLOCK_PREFIX> Timer```

**Initial State** Turned Off

This should be set to activate every second. The first action should be 'Run' on the programmable block. The second action should be the Timer itself with the 'Start' action. It should be turned off.

### Safety Valve

**Name** ```<AIRLOCK_PREFIX> Valve```

This is an airtight door, any type. It must be separated from the main airlock chamber by a vented window. The other side of this door must be in the zero pressure area. If the airlock is unable to de-pressurise within the specified time limit (default 5s), this door is opened before the exterior doors. This prevents explosive decompression, your character will not experience any of the normal effects.

The safety valve is optional. If your airlock doesn't need this, you don't need to add it.

### Interior Door

**Name** ```<AIRLOCK_PREFIX> Interior Door```

This is the door leading from the airlock chamber to the pressurised area.

### Exterior Door

**Name** ```<AIRLOCK_PREFIX> Exterior Doors```

This is the door leading from the airlock chamber to the unpressurised area.

### Air Vent

**Name** ```<AIRLOCK_PREFIX> Ven```

This is the vent inside the airlock chamber.

### Chamber Sensor

**Name** ```<AIRLOCK_PREFIX> Chamber Sensor```

This is the sensor within the chamber. You should adjust the range on it so that it only covers the airlock chamber. The action on it should be to 'Run' the programmable block.

### Interior Sensor

**Name** ```<AIRLOCK_PREFIX> Interior Sensor```

This sensor should be placed at the approach to the interior airlock door inside the station/ship. The purpose is to activate the airlock if it is not pressurised. The range on the sensor should not cover any of the airlock chamber. The action should be to 'Run' the programmable block.

### Exterior Sensor

**Name** ```<AIRLOCK_PREFIX> Exterior Sensors```

This sensor should be placed at the approach to the exterior airlock door outside of the station. The range on the sensor should not cover any of the airlock chamber. The action should be to 'Run' the programmable block.

### Debug TextPanel

**Name** ```<AIRLOCK_PREFIX> Debug```

Any TextPanels with this name will be updated with debug output of the airlock including what is currently being executed and the current state. You do not need these.
