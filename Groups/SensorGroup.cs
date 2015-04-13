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

    public class SensorGroup
    {
        private IList<SensorStruct> sensors = new List<SensorStruct>();
        private BlockHelper blockHelper;

        public SensorGroup(BlockHelper blockHelper, string name)
        {
            this.blockHelper = blockHelper;
            IList<IMySensorBlock> blocks = this.blockHelper.GetGroup<IMySensorBlock>(name);
            for (int i = 0; i < blocks.Count; i++)
            {
                Sensor sensor = new Sensor(this.blockHelper, blocks[i]);
                this.sensors.Add(new SensorStruct(sensor));
            }
        }

        public bool AreSensorsActive()
        {
            for (int i = 0; i < this.sensors.Count; i++)
            {
                if (this.sensors[i].Sensor.IsSensorActive())
                {
                    return true;
                }
            }
            return false;
        }

        public void EnableSensors()
        {
            for (int i = 0; i < this.sensors.Count; i++)
            {
                this.sensors[i].Sensor.EnableSensor();
            }
        }

        public void DisableSensors()
        {
            for (int i = 0; i < this.sensors.Count; i++)
            {
                this.sensors[i].Sensor.DisableSensor();
            }
        }
    }
    #endregion
}
