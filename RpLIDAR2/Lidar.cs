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
    public class LidarBase
    {

        public delegate void NewScanSetHandler(ScanPoint[] Scanset);
    }

    public interface ILidar : IDisposable
    {
        bool Start();
        void Stop();
        event LidarBase.NewScanSetHandler NewScanSet;
    }

    public class ScanPoint
    {
        public double Angle { get; set; }

        public double Distance { get; set; }

        public int Quality { get; set; }

        public DateTime TimeOfDeath { get; set; }
    }

    public class Feature
    {
        public Feature(Point position)
        {
            Position = position;
        }

        private Feature()
        {
            /* for serializer */
        }

        public Point Position { get; set; }
    }

    public class Landmark
    {
        public Point Position;

        public Landmark(Point position)
        {
            Position = position;
        }

        public Landmark()
        {
            /* for serializer */
        }
    }


    
}