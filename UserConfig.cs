using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirlockSystem
{
    #region SpaceEngineers
    public class UserConfig
    {
        // This is the prefix for all the blocks and groups for the airlock
        public const string AIRLOCK_PREFIX = "Lab Airlock";

        // This is how long in ticks (normally seconds) to wait for vents and doors to open/close/depressurize
        public const int VENT_TIMEOUT = 5;
        public const int DOOR_TIMEOUT = 5;
        public const int DOOR_DELAY = 1;
    }
    #endregion
}
