using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirlockSystem.Utils
{
    #region SpaceEngineers

    public class BlockHelper
    {
        IMyGridTerminalSystem GridTerminalSystem;

        public BlockHelper(IMyGridTerminalSystem GridTerminalSystem)
        {
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
                        if (block is T)
                        {
                            groupBlocks.Add((T)block);

                        }
                    }
                    return groupBlocks;
                }
            }
            return groupBlocks;
        }

        public IMyTerminalBlock GetBlockWithName(string blockName)
        {
            return this.GridTerminalSystem.GetBlockWithName(blockName);
        }

        public IList<T> SearchBlocksOfName<T>(string blockName)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            this.GridTerminalSystem.SearchBlocksOfName(blockName, blocks);
            List<T> result = new List<T>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is T)
                {
                    result.Add((T)blocks[i]);
                }
            }
            return result;
        }
    }
    #endregion
}
