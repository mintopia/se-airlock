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
    public class Sensor
    {
        private IMySensorBlock sensor;
        private BlockHelper blockHelper;

        public Sensor(BlockHelper blockHelper, IMySensorBlock sensor)
        {
            this.sensor = sensor;
            this.blockHelper = blockHelper;
        }

        public bool IsSensorActive()
        {
            return this.sensor.IsActive;
        }

        public void EnableSensor()
        {
            this.blockHelper.ApplyAction(this.sensor, "OnOff_On");
        }

        public void DisableSensor()
        {
            this.blockHelper.ApplyAction(this.sensor, "OnOff_Off");
        }
    }

    public struct SensorStruct
    {
        public Sensor Sensor;

        public SensorStruct(Sensor sensor)
        {
            this.Sensor = sensor;
        }
    }
    #endregion
}
