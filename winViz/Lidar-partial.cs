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

        private void LIDAR_Click(object sender, RoutedEventArgs e)
        {
            Slam = new Slam();
            try
            {
                if (lidarPort.StartsWith("com"))
                    RpLidar = new RpLidarSerial(lidarPort);
                else if (lidarPort.Equals("mqtt"))
                    RpLidar = new RpLidarMqtt(Mqtt);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception opening LIDAR, (COM port or MQTT not available?", "error");
                Trace.WriteLine(ex.Message, "1");
                return;
            }

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
                // +++borked landmarks1.Landmarks = Slam.FindLandmarksFromDerivatives(LidarCanvas.Scans, derivatives);

                LidarCanvas.InvalidateVisual();
            });
        }
    }
}
