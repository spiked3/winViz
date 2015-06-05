using RpLidarLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
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
            ScanPoint[] scanData = FromByteArray<ScanPoint>(e.Message);
            if (NewScanSet != null)
                NewScanSet(scanData);                
        }

        // +++ may consider faster unsafe method if needed
        static byte[] ToByteArray<T>(T[] source) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                byte[] destination = new byte[source.Length * Marshal.SizeOf(typeof(T))];
                Marshal.Copy(pointer, destination, 0, destination.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        static T[] FromByteArray<T>(byte[] source) where T : struct
        {
            T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
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
