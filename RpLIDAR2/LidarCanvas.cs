// RoboNUC ©2014 Mike Partain
// This file is NOT open source
// 
// RnMaster :: RpLidarLib :: LidarCanvas.cs 
// 
// /* ----------------------------------------------------------------------------------- */

#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

#endregion

namespace RpLidarLib
{
    public class LidarCanvas : Canvas
    {
        const double deg2rad = Math.PI / 180.0;

        const int QualityRadius = 2;
        const int PointQaulityThresholdRed = 20;
        const int PointQaulityThresholdYellow = 35;

        public static readonly DependencyProperty LandmarkBrushProperty =
            DependencyProperty.Register("LandmarkBrush", typeof(Brush), typeof(LidarCanvas),
                new PropertyMetadata(Brushes.Green));

        public static readonly DependencyProperty LandmarkSizeProperty =
            DependencyProperty.Register("LandmarkSize", typeof(int), typeof(LidarCanvas), new PropertyMetadata(5));

        public static readonly DependencyProperty LandmarksProperty =
            DependencyProperty.Register("Landmarks", typeof(List<Landmark>), typeof(LidarCanvas));

        public static readonly DependencyProperty DrawObjectsProperty =
            DependencyProperty.Register("Scans", typeof(List<ScanPoint>), typeof(LidarCanvas));

        public static readonly DependencyProperty AxisPenProperty =
            DependencyProperty.Register("AxisPen", typeof(Pen), typeof(LidarCanvas));

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register("TextColor", typeof(Brush), typeof(LidarCanvas),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ScanPenProperty =
            DependencyProperty.Register("ScanPen", typeof(Pen), typeof(LidarCanvas));

        public static readonly DependencyProperty ScanBrushProperty =
            DependencyProperty.Register("ScanBrush", typeof(Brush), typeof(LidarCanvas));

        readonly Brush[] PointBrush = { Brushes.Red, Brushes.Yellow, Brushes.LightGreen };
        readonly Typeface textTypeface = new Typeface("Verdana");

        double Zoom = .025;
        Rect canvasRect;
        Point centerPoint;

        public Brush LandmarkBrush
        {
            get { return (Brush)GetValue(LandmarkBrushProperty); }
            set { SetValue(LandmarkBrushProperty, value); }
        }

        public int LandmarkSize
        {
            get { return (int)GetValue(LandmarkSizeProperty); }
            set { SetValue(LandmarkSizeProperty, value); }
        }

        public List<Landmark> Landmarks
        {
            get { return (List<Landmark>)GetValue(LandmarksProperty); }
            set { SetValue(LandmarksProperty, value); }
        }

        public List<ScanPoint> Scans
        {
            get { return (List<ScanPoint>)GetValue(DrawObjectsProperty); }
            set { SetValue(DrawObjectsProperty, value); }
        }

        public Pen AxisPen
        {
            get { return (Pen)GetValue(AxisPenProperty); }
            set { SetValue(AxisPenProperty, value); }
        }

        public Brush TextColor
        {
            get { return (Brush)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        public Pen ScanPen
        {
            get { return (Pen)GetValue(ScanPenProperty); }
            set { SetValue(ScanPenProperty, value); }
        }

        public Brush ScanBrush
        {
            get { return (Brush)GetValue(ScanBrushProperty); }
            set { SetValue(ScanBrushProperty, value); }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            centerPoint = new Point(sizeInfo.NewSize.Width / 2, sizeInfo.NewSize.Height / 2);
            canvasRect = new Rect(0, 0, sizeInfo.NewSize.Width, sizeInfo.NewSize.Height);
            InvalidateVisual();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta < 0)
                Zoom -= (Zoom * .10);
            else
                Zoom += (Zoom * .10);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Background, null, canvasRect); // erase

            // Range Circles at 1 meter intervals
            FormattedText t = new FormattedText(string.Format("Zoom:\n{0:F3}", Zoom), CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight, textTypeface, 12, TextColor);
            dc.DrawText(t, new Point(10, 5));

            for (int dist = 1000; dist <= 6000; dist += 1000)
                dc.DrawEllipse(null, AxisPen, centerPoint, dist * Zoom, dist * Zoom);

            // Angle Lines
            double r = ActualHeight * .5;
            for (int theta = 0; theta <= 150; theta += 30)
            {
                Point sp = new Point(Math.Sin(theta * deg2rad) * r + centerPoint.X,
                    -Math.Cos(theta * deg2rad) * r + centerPoint.Y);
                Point ep = new Point(Math.Sin(theta * deg2rad) * -r + centerPoint.X,
                    -Math.Cos(theta * deg2rad) * -r + centerPoint.Y);
                dc.DrawLine(AxisPen, sp, ep);

                // keep text centered and in bounds
                t = new FormattedText(theta.ToString(), CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    textTypeface, 12, TextColor);

                sp.X -= t.Width / 2;
                sp.Y -= t.Height / 2;

                if (sp.Y < 0)
                    sp.Y = 0;

                dc.DrawText(t, sp);

                t = new FormattedText((theta + 180).ToString(), CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    textTypeface, 12, TextColor);

                ep.X -= t.Width / 2;
                ep.Y -= t.Height / 2;

                if ((ep.Y + t.Height) > ActualHeight)
                    ep.Y = ActualHeight - t.Height;

                dc.DrawText(t, ep);
            }

            if (Scans != null)
            {
                List<LineSegment> segments = new List<LineSegment>();
                foreach (var m in Scans)
                {
                    Point p = new Point(centerPoint.X + Zoom * m.Distance * Math.Sin(m.Angle),
                        centerPoint.Y + Zoom * m.Distance * -Math.Cos(m.Angle));
                    if (m.Distance > 0)
                        segments.Add(new LineSegment(p, true));
                }
                if (segments.Count > 0)
                {
                    PathFigure pf = new PathFigure(segments[0].Point, segments, true);
                    dc.DrawGeometry(ScanBrush, ScanPen, new PathGeometry(new List<PathFigure> { pf }));
                }

                // quality dots
                foreach (var m in Scans)
                {
                    Point p = new Point(centerPoint.X + Zoom * m.Distance * Math.Sin(m.Angle),
                        centerPoint.Y + Zoom * m.Distance * -Math.Cos(m.Angle));
                    Brush brush2Use = m.Quality > PointQaulityThresholdYellow
                        ? PointBrush[2]
                        : m.Quality > PointQaulityThresholdRed
                            ? PointBrush[1]
                            : PointBrush[0];
                    dc.DrawEllipse(brush2Use, null, p, QualityRadius, QualityRadius);
                }
            }

            if (Landmarks != null)
                foreach (Landmark l in Landmarks)
                {
                    Point p = new Point(centerPoint.X + Zoom * l.Position.X, centerPoint.Y + Zoom * l.Position.Y);
                    dc.DrawEllipse(LandmarkBrush, null, p, LandmarkSize, LandmarkSize);
                }
        }
    }
}