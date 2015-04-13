using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using AirlockSystem.Utils;
using AirlockSystem.Groups;
using AirlockSystem.Blocks;

namespace AirlockSystem
{
    public class AirlockSystem
    {
        static void Main(string[] args)
        {
        }

        IMyGridTerminalSystem GridTerminalSystem = null;

#region SpaceEngineers
        Airlock airlock = new Airlock();

        void Main()
        {
            BlockHelper blockHelper = new BlockHelper(GridTerminalSystem);
            airlock.execute(blockHelper);
        }

#endregion
#region footer
    }
}
#endregion
