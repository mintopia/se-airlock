using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirlockSystem;
using Sandbox.ModAPI.Ingame;

namespace AirlockSystem.Utils
{
    #region SpaceEngineers

    public class DebugOutput
    {
        private List<string> logs = new List<string>();
        private Airlock airlock;
        private BlockHelper blockHelper;

        string debugName;

        public DebugOutput(BlockHelper blockHelper, Airlock airlock, string debugName)
        {
            this.airlock = airlock;
            this.blockHelper = blockHelper;
            this.debugName = debugName;
        }

        public void AddLog(string message)
        {
            string last = "";
            if (this.logs.Count > 0)
            {
                last = this.logs[this.logs.Count - 1];
            }
            if (last != message)
            {
                this.logs.Add(message);
            }
            this.TrimLogs();
            this.Render();
        }

        private void TrimLogs()
        {
            int count = 13;
            int index = this.logs.Count - count;
            if (index < 0)
            {
                index = 0;
            }
            if (count > this.logs.Count)
            {
                count = this.logs.Count;
            }
            this.logs = this.logs.GetRange(index, count);
        }

        public void Render()
        {
            string message = "           AIRLOCK STATUS: " + this.airlock.GetStatus() + this.Line();
            message += String.Join("\n ", logs);
            for (int i = 0; i < (13 - logs.Count); i++)
            {
                message += "\n";
            }
            message += this.Line();
            message += " State: " + this.airlock.State.ToString();
            message += "      Oxygen: " + this.airlock.GetAirlockOxygenLevel().ToString() + "%";
            message += "      Tick: " + this.airlock.Tick.ToString();

            IList<IMyTextPanel> panels = this.blockHelper.SearchBlocksOfName<IMyTextPanel>(this.debugName);
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].WritePublicText(message, false);
                panels[i].ShowPublicTextOnScreen();
            }
        }

        private string Line()
        {
            return "\n-------------------------------------------------------------\n ";
        }
    }
    #endregion
}
