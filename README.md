# Space Engineers Airlock

This is a in-game programming block script for controlling and managing an Airlock. The main features are:

 * Multiple Interior and Exterior Doors
 * Fully automatic operation
 * Multiple Vents
 * Safety Valve to prevent explosive decompression
 * Activation of status lights, sound blocks, etc.
 
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

### Interior Doors Group

**Name** ```<AIRLOCK_PREFIX> Interior Doors```

All the interior doors leading to the pressurised area should be a member of this group. It's reccomended that you also rename the doors.

### Exterior Doors Group

**Name** ```<AIRLOCK_PREFIX> Exterior Doors```

All the exterior doors leading to the unpressurised area should be a member of this group. You should also give the doors meaningful names.

### Air Vents Group

**Name** ```<AIRLOCK_PREFIX> Vents```

These are the vents inside the airlock chamber. Name the individual vents.

### Chamber Sensors Group

**Name** ```<AIRLOCK_PREFIX> Chamber Sensors```

This group should contain all sensor blocks inside the chamber. You should adjust the range on them so they don't overlap and they only cover the airlock chamber. The action on them should be to 'Run' the programmable block.

### Interior Sensors Group

**Name** ```<AIRLOCK_PREFIX> Interior Sensors```

The sensors in this group should be placed at the approaches to the interior airlock doors inside the station/ship. The purpose is to activate the airlock if it is not pressurised. The range on each sensor should not overlap and should not cover any of the airlock chamber. The action should be to 'Run' the programmable block.

### Exterior Sensors Group

**Name** ```<AIRLOCK_PREFIX> Exterior Sensors```

The sensors in this group should be placed at the approaches to the exterior airlock doors outside of the station. The range on each sensor should not overlap and should not cover any of the airlock chamber. The action should be to 'Run' the programmable block.

### Pressurised Blocks Group

**Name** ```<AIRLOCK_PREFIX> Pressurised Blocks```

These are blocks that you want to be turned on when the airlock is idle and is pressurised. These could be status lights inside the station. They will be turned on or off at the start and end of each airlock sequence.

### Unpressurised Blocks Group

**Name** ```<AIRLOCK_PREFIX> Unpressurised Blocks```

These are blocks that you want to be turned on when the airlock is idle and is pressurised. These could be status lights outside the station.

### Active Blocks Group

**Name** ```<AIRLOCK_PREFIX> Active Blocks```

These are blocks that you want to be turned on when the airlock is active and running a sequence. These could be lights inside the airlock.

### Debug TextPanel

**Name** ```<AIRLOCK_PREFIX> Debug```

Any TextPanels with this name will be updated with debug output of the airlock including what is currently being executed and the current state. You do not need these.
