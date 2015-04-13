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
    public class Timer
    {
        private IMyTimerBlock timer;
        private BlockHelper blockHelper;

        public Timer(BlockHelper blockHelper, string timerName)
        {
            this.blockHelper = blockHelper;
            this.timer = this.blockHelper.GetBlockWithName(timerName) as IMyTimerBlock;
            if (this.timer == null)
            {
                throw new Exception("Unable to find a timer block called " + timerName);
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
    #endregion
}
