using AirlockSystem.Utils;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirlockSystem.Blocks
{
    #region SpaceEngineers
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
            return (int)Math.Round((Decimal)(this.vent.GetOxygenLevel() * 100), 0);
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

    public struct VentStruct
    {
        public Vent Vent;

        public VentStruct(Vent vent)
        {
            this.Vent = vent;
        }
    }
    
    #endregion
}
