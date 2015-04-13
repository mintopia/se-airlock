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

    public class DoorGroup
    {
        private IList<DoorStruct> doors = new List<DoorStruct>();
        private BlockHelper blockHelper;

        public DoorGroup(BlockHelper blockHelper, string name)
        {
            this.blockHelper = blockHelper;

            IList<IMyDoor> blocks = blockHelper.GetGroup<IMyDoor>(name);
            for (int i = 0; i < blocks.Count; i++)
            {
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
    #endregion
}
