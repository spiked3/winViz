// RoboNUC ©2014 Mike Partain
// This file is NOT open source
// 
// RnMaster :: RnSlamLib :: SLAM.cs 
// 
// /* ----------------------------------------------------------------------------------- */

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using RpLidarLib;

#endregion

namespace RnSlamLib
{
    // implmentations from youtube lecture
    public class Slam
    {
        const double Deg2Rad = Math.PI / 180.0;
        const double Rad2Deg = 180.0 / Math.PI;

        public double LandmarkDerivativeThreshold { get; set; }
        public double LandmarkOffset { get; set; }
        public double MinLandmarkWidth { get; set; }
        public double MinValidScanDistance { get; set; }

        public Slam()
        {
            LandmarkDerivativeThreshold = 150.0;
            LandmarkOffset = 0.0; // todo determine real LandmarkOffset (if any) from observation
            MinLandmarkWidth = 5.0; // minimum rays to consider a landmark
            MinValidScanDistance = 5.0; // values less than this are considered noise
        }

        public List<double> ComputeScanDerivatives(List<ScanPoint> scans)
        {
            List<double> ders = new List<double>();
            ders.Add(0.0); // dummy start

            for (int i = 1; i < scans.Count - 1; i++)
            {
                double l = scans[i - 1].Distance;
                double r = scans[i + 1].Distance;
                if (l > MinValidScanDistance && r > MinValidScanDistance)
                    ders.Add((r - l) / 2.0);
                else
                    ders.Add(0.0);
            }
            ders.Add(0.0); // dummy end
            return ders;
        }

        public List<Landmark> FindLandmarksFromDerivatives(List<ScanPoint> scans, List<double> scanDerivatives)
        {
            List<Landmark> landmarks = new List<Landmark>();
            bool onLandmark = false;
            float sumRay = 0f, sumDepth = 0f;
            int rays = 0;

            for (int i = 0; i < scanDerivatives.Count; i++)
            {
                if (scanDerivatives[i] < -LandmarkDerivativeThreshold)
                {
                    onLandmark = true;
                    sumRay = sumDepth = 0f;
                    rays = 0;
                }

                if (onLandmark && scans[i].Distance > MinValidScanDistance)
                {
                    sumRay += i;
                    sumDepth += (float)scans[i].Distance;
                    rays++;
                }

                if (onLandmark && scanDerivatives[i] > LandmarkDerivativeThreshold)
                    if (rays > MinLandmarkWidth)
                    {
                        ScanPoint p = scans[(int)(sumRay / rays)];
                        double d = (sumDepth / rays) + LandmarkOffset;
                        landmarks.Add(new Landmark { Position = new Point(d * Math.Sin(p.Angle), d * -Math.Cos(p.Angle)) });
                        onLandmark = false;
                    }
            }

            return landmarks;
        }
    }
}