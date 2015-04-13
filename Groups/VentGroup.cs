using AirlockSystem.Blocks;
using AirlockSystem.Utils;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirlockSystem.Groups
{
    #region SpaceEngineers

    public class VentGroup
    {
        private IList<VentStruct> vents = new List<VentStruct>();
        private Door valve;
        private BlockHelper blockHelper;

        public VentGroup(BlockHelper blockHelper, string ventGroupName, string valveName)
        {
            this.blockHelper = blockHelper;

            IList<IMyAirVent> blocks = this.blockHelper.GetGroup<IMyAirVent>(ventGroupName);
            for (int i = 0; i < blocks.Count; i++)
            {
                Vent vent = new Vent(this.blockHelper, blocks[i]);
                this.vents.Add(new VentStruct(vent));
            }

            IMyDoor door = this.blockHelper.GetBlockWithName(valveName) as IMyDoor;
            if (door != null)
            {
                this.valve = new Door(this.blockHelper, door);
            }
        }

        public int GetOxygenLevel()
        {
            if (this.vents.Count == 0)
            {
                return 0;
            }

            return this.vents[0].Vent.GetVentOxygenLevel();
        }

        public void EnableOxygen()
        {
            this.CloseValve();
            for (int i = 0; i < this.vents.Count; i++)
            {
                this.vents[i].Vent.EnableVentOxygen();
            }
        }

        public void DisableOxygen()
        {
            for (int i = 0; i < this.vents.Count; i++)
            {
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

        public void OpenValve()
        {
            if (this.valve != null)
            {
                this.valve.OpenDoor();
            }
        }

        public void CloseValve()
        {
            if (this.valve != null)
            {
                this.valve.CloseDoor();
            }
        }
    }
    #endregion
}
