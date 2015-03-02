// RoboNUC ©2014 Mike Partain
// This file is NOT open source
// 
// RnMaster :: RpLidarLib :: RpLidarDriver.cs 
// 
// /* ----------------------------------------------------------------------------------- */

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

#endregion

namespace RpLidarLib
{
    public class RpLidarDriver : IDisposable
    {
        static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly string[] HealtStatusStrings = { "Good", "Poor", "Critical", "Unknown" };

        SerialPort Lidar;
        bool lidarTimedOut;
        byte[] nodeBuf = new byte[5];
        int recvPos;

        ScanPoint[] ScanData = new ScanPoint[360];
        bool StartOfNewScan = true;
        public delegate void NewScanSetHandler(ScanPoint[] Scanset);
        public event NewScanSetHandler NewScanSet;

        public RpLidarDriver(string comPort)
        {
            Open(comPort);
        }

        public void Dispose()
        {
            if (Lidar != null)
            {
                Lidar.Close();
                //Lidar.Dispose();
            }
        }

        void Open(string comPort)
        {
            log.Debug("RpLidarDriver::Open");
            Lidar = new SerialPort(comPort, 115200, Parity.None, 8, StopBits.One);
            try
            {
                Lidar.Open();
            }
            catch (Exception ex)
            {
                    throw ex;   // bubblw up
            }
        }

        public void LidarFlush()
        {
            while (Lidar.BytesToRead > 0)
                Lidar.ReadByte();
        }

        void LidarRequest(LidarCommand cmd, byte[] payload = null)
        {
            byte chksum = 0x00;
            List<byte> buf = new List<byte>();
            buf.Add(0xA5); //start
            buf.Add((byte)cmd);
            if (payload != null && payload.Length > 0)
            {
                buf.Add((byte)payload.Length);
                buf.AddRange(payload);
                buf.ForEach(x => chksum ^= x);
                buf.Add(chksum);
            }
            byte[] b = buf.ToArray<byte>();
            Lidar.Write(b, 0, b.Length);
        }

        public void StartScan()
        {
            if (Lidar != null && Lidar.IsOpen)
            {
                byte[] r;
                LidarScanResponse sr;
                LidarFlush();
                LidarRequest(LidarCommand.Scan);
                if (GetLidarResponseWTimeout(out r, 7, 500))
                {
                    sr = r.ByteArrayToStructure<LidarScanResponse>(0);
                    Lidar.DataReceived += LidarScanDataReceived; // we expect responses until we tell it to stop
                }
            }
        }

        void LidarScanDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //System.Diagnostics.Trace.WriteLine(string.Format("LidarScanDataReceived bytes to read  {0}", Lidar.BytesToRead));
            while (Lidar.IsOpen && Lidar.BytesToRead > 0)
            {
                byte currentByte = (byte)Lidar.ReadByte();

                switch (recvPos)
                {
                    case 0:
                        int tmp = currentByte & 0x03;
                        if (tmp != 0x01 && tmp != 0x02)
                            continue;
                        break;
                    case 1:
                        if ((currentByte & 0x01) != 0x01)
                        {
                            recvPos = 0;
                            continue;
                        }
                        break;
                }

                nodeBuf[recvPos++] = currentByte;

                if (recvPos == 5)
                {
                    if (StartOfNewScan)
                    {
                        Array.Clear(ScanData, 0, 360);
                        StartOfNewScan = false;
                    }
                    // converted values
                    LidarScanData node = nodeBuf.ByteArrayToStructure<LidarScanData>(0);
                    float distance = node.Distance / 4f;

                    // I couldn't tell much difference rounded v not
                    //int angle = (int)((node.Angle >> 1) / 64.0);   
                    int angle = (int)Math.Round(((node.Angle >> 1) / 64.0), MidpointRounding.AwayFromZero);

                    int quality = node.Quality >> 2;
                    bool startBit = (node.Quality & 0x01) == 0x01;

                    //System.Diagnostics.Trace.WriteLine(string.Format("s({0},{1}) c({2}) q({3}) a({4}) d({5})", s, s1, c, q, a, d));
                    if (distance > 0 && angle < 360)
                        ScanData[angle] = new ScanPoint { Angle = angle, Quality = quality, Distance = distance };

                    recvPos = 0;
                    if (startBit)
                        StartOfNewScan = true;
                    if (startBit && NewScanSet != null)
                        NewScanSet(ScanData);       // fire event
                }
            }
        }

        public bool GetHealth(out LidarHealthResponse hr)
        {
            hr = new LidarHealthResponse();
            if (Lidar != null && Lidar.IsOpen)
            {
                LidarFlush();
                LidarRequest(LidarCommand.GetHealth);
                byte[] r;
                if (GetLidarResponseWTimeout(out r, 7 + 3, 500))
                {
                    hr = r.ByteArrayToStructure<LidarHealthResponse>(0);
                    Debug.Assert(hr.Status <= 3);
                    log.Info(string.Format("Lidar Health {0}", HealtStatusStrings[hr.Status]));
                    return true;
                }
            }
            return false;
        }

        public bool GetDeviceInfo(out LidarDevInfoResponse di)
        {
            di = new LidarDevInfoResponse();
            if (Lidar != null && Lidar.IsOpen)
            {
                di = new LidarDevInfoResponse();
                LidarFlush();
                LidarRequest(LidarCommand.GetInfo);
                byte[] r;
                if (GetLidarResponseWTimeout(out r, 7 + 20, 500))
                {
                    di = r.ByteArrayToStructure<LidarDevInfoResponse>(0);
                    log.Info(string.Format("Model({0}) Firmware({1},{2}) Hardware({3}) serial({4})",
                        di.Model, di.FirmwareMajor, di.FirmwareMinor, di.hardware, BitConverter.ToString(di.SerialNum)));
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            if (Lidar != null && Lidar.IsOpen)
            {
                Lidar.DataReceived -= LidarScanDataReceived;
                LidarRequest(LidarCommand.Reset);
                Lidar.Close();
                Lidar.Dispose();
                Thread.Sleep(500);
            }
        }

        public void Stop()
        {
            if (Lidar != null && Lidar.IsOpen)
            {
                LidarRequest(LidarCommand.Stop);
                Thread.Sleep(100);
                LidarFlush();
                Lidar.Close();
                Lidar.Dispose();
            }
        }

        bool GetLidarResponseWTimeout(out byte[] outBuf, int expectedLength, int timeout)
        {
            int idx = 0;
            outBuf = new byte[expectedLength];
            using (Timer timeoutTimer = new Timer(timeout))
            {
                timeoutTimer.Elapsed += LidarResponseTimeoutElapsed;
                lidarTimedOut = false;
                timeoutTimer.Start();
                while (!lidarTimedOut)
                {
                    if (Lidar.BytesToRead > 0)
                    {
                        outBuf[idx++] = (byte)Lidar.ReadByte();
                        if (idx >= expectedLength)
                            return true; // timer should be auto disposed??
                    }
                    Thread.Sleep(2);
                }
                return false;
            }
        }

        void LidarResponseTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Stop();
            lidarTimedOut = true;
        }

    }
}