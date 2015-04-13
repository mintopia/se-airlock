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

    public struct DoorStruct
    {
        public Door Door;

        public DoorStruct(Door door)
        {
            this.Door = door;
        }
    }
    #endregion
}
