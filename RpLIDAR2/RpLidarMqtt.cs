using RpLidarLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Timers;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace RpLidarLib
{
    public class RpLidarMqtt : ILidar
    {
        MqttClient Mqtt;
        public event LidarBase.NewScanSetHandler NewScanSet;

        public RpLidarMqtt(MqttClient m)
        {
            Mqtt = m;
            m.MqttMsgPublishReceived += MqttMsgReceived;
        }

        private void MqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // +++ convert e.message to a scanset
            if (NewScanSet != null)
                NewScanSet(null);
                //NewScanSet(e.Message);
        }

        public void Dispose()
        {
            Mqtt?.Disconnect();
        }

        public bool Start()
        {
            Mqtt.Subscribe(new string[] { "RpLidar/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            return true;
        }

        public void Stop()
        {
            Mqtt.Unsubscribe(new string[] { "RpLidar/#" });
        }
    }
}
