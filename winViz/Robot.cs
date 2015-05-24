using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;

namespace spiked3.winViz
{
    public class Robot
    {
        public MqttClient Mqtt { get; internal set; }
        public RobotPanel Panel { get; internal set; }

        public void SendPilot(dynamic p)
        {
            Mqtt.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(p)));
        }
    }
}