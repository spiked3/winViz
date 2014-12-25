using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace gyro1
{
    public class MyCanvas : Canvas
    {
        Point HelloPoint = new Point(20, 20);
        private Typeface DefaultFont = new Typeface("Verdana");

        Pen gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0xff, 0xf0, 0xf0, 0xf0)), 2.0);
        Pen gridThickPen = new Pen(new SolidColorBrush(Color.FromArgb(0xff, 0xf0, 0xf0, 0xf0)), 3.0);

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(MyCanvas), new PropertyMetadata(Brushes.Black));

        protected override void OnRender(System.Windows.Media.DrawingContext dc)
        {
            dc.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
            

            // center of canvas represents 0,0
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;
            var xGrids = centerY / 10;
            var yGrids = centerX / 10;
            for (int i = 0; i < xGrids; i++)
            {
                Pen penToUse = i % 10 == 0 ? gridThickPen : gridPen;
                dc.DrawLine(penToUse, new Point(0, centerY - (i * 10)), new Point(ActualWidth, centerY - (i * 10)));
                dc.DrawLine(penToUse, new Point(0, centerY + (i * 10)), new Point(ActualWidth, centerY + (i * 10)));
            }
            for (int i = 0; i < yGrids; i++)
            {
                Pen penToUse = i % 10 == 0 ? gridThickPen : gridPen;
                dc.DrawLine(penToUse, new Point(centerX - (i * 10), 0), new Point(centerX - (i * 10), ActualHeight));
                dc.DrawLine(penToUse, new Point(centerX + (i * 10), 0), new Point(centerX + (i * 10), ActualHeight));
            }

            // point at 0,0
            dc.DrawEllipse(Brushes.Black, null, new Point(centerX, centerY), 5, 5);

            var t = new FormattedText("Hello World", System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                DefaultFont, 18.0, Foreground);
            dc.DrawText(t, HelloPoint);
        }
    }
}