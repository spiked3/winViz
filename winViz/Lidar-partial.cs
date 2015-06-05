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

        private void LIDAR_Serial_Click(object sender, RoutedEventArgs e)
        {
            Slam = new Slam();
            try
            {
                RpLidar = new RpLidarSerial(ConfigManager.Get<string>("lidar"));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception opening LIDAR COM port, LIDAR not available.", "error");
                Trace.WriteLine(ex.Message, "1");
                return;
            }

            RpLidar.NewScanSet += LidarNewScanSet;
            RpLidar.Start();
        }

        private void LIDAR_MQTT_Click(object sender, RoutedEventArgs e)
        {
            Slam = new Slam();
            RpLidar = new RpLidarMqtt(Mqtt);
            RpLidar.NewScanSet += LidarNewScanSet;
            RpLidar.Start();
        }

        void LidarNewScanSet(ScanPoint[] scanset)
        {
            Dispatcher.InvokeAsync(() =>
            {
                // provide an immutable sorted list for LIDARCanvas and others to use
                LidarCanvas.Scans = new List<ScanPoint>(scanset.Length);

                foreach (ScanPoint p in scanset)
                    LidarCanvas.Scans.Add(new ScanPoint
                    {
                        Angle = (float)(p.Angle * Math.PI / 180.0),
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
