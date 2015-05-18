using RnSlamLib;
using RpLidarLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Ribbon;

namespace spiked3.winViz
{
    public partial class MainWindow : RibbonWindow
    {
        void InitLIDAR()
        {
            Slam = new Slam();
            try
            {
                RpLidar = new RpLidarDriver(ConfigManager.Get<string>("lidar"));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception opening LIDAR COM port, LIDAR not available.", "error");
                Trace.WriteLine(ex.Message, "1");
                return;
            }

            RpLidar.NewScanSet += LidarNewScanSet;

            // retry until valid device info
            int tries = 0;
            while (++tries < 5)
            {
                LidarDevInfoResponse di;
                if (RpLidar.GetDeviceInfo(out di))
                {
                    if (di.Model == 0 && di.hardware == 0)
                    {
                        Trace.WriteLine(string.Format("Lidar Model({0}, {1}), Firmware({2}, {3})", di.Model, di.hardware,
                            di.FirmwareMajor, di.FirmwareMinor));
                        RpLidar.StartScan();
                        return;
                    }
                }
                else
                {
                    Trace.WriteLine("Unable to get device info from RP LIDAR, device reset", "warn");
                    RpLidar.Reset();
                    Thread.Sleep(500);
                }
            }

            Trace.WriteLine("Start Lidar failed 5 (re)tries", "error");
        }

        private void LIDAR_Click(object sender, RoutedEventArgs e)
        {
            InitLIDAR();
        }

        void LidarNewScanSet(ScanPoint[] scanset)
        {
            Dispatcher.InvokeAsync(() =>
            {
                // provide an immutable sorted list for LIDARCanvas and others to use
                LidarCanvas.Scans = new List<ScanPoint>(scanset.Length);

                foreach (ScanPoint p in scanset)
                    if (p != null)
                        LidarCanvas.Scans.Add(new ScanPoint
                        {
                            Angle = p.Angle * Math.PI / 180.0,
                            Distance = p.Distance,
                            Quality = p.Quality
                        });

                List<double> derivatives = Slam.ComputeScanDerivatives(LidarCanvas.Scans);

                LidarCanvas.Landmarks = Slam.FindLandmarksFromDerivatives(LidarCanvas.Scans, derivatives);
                landmarks1.Landmarks = Slam.FindLandmarksFromDerivatives(LidarCanvas.Scans, derivatives);

                LidarCanvas.InvalidateVisual();

            });
        }
    }
}
