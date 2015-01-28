using NDesk.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace gyro1
{
    public partial class MainWindow : Window
    {
        public string State
        {
            get { return (string)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(string), typeof(MainWindow), new PropertyMetadata("Initial State"));

        public int RobotH
        {
            get { return (int)GetValue(RobotHProperty); }
            set { SetValue(RobotHProperty, value); }
        }
        public static readonly DependencyProperty RobotHProperty =
            DependencyProperty.Register("RobotH", typeof(int), typeof(MainWindow), new PropertyMetadata(0));


        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(MainWindow), new PropertyMetadata("StatusText"));

        public float DrawScale
        {
            get { return (float)GetValue(DrawScaleProperty); }
            set { SetValue(DrawScaleProperty, value); }
        }
        public static readonly DependencyProperty DrawScaleProperty =
            DependencyProperty.Register("DrawScale", typeof(float), typeof(MainWindow), new PropertyMetadata(.1F));



        public double FadeFactor
        {
            get { return (double)GetValue(FadeFactorProperty); }
            set { SetValue(FadeFactorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FadeFactor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FadeFactorProperty =
            DependencyProperty.Register("FadeFactor", typeof(double), typeof(MainWindow), new PropertyMetadata(0.7));

        const string Broker = "127.0.0.1";
        MqttClient Mqtt;

        readonly TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 100);

        Ellipse RobotDot;
        List<Ellipse> FadingDots = new List<Ellipse>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            State = "MQTT Connecting ...";
            Mqtt = new MqttClient(Broker);
            Mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            Mqtt.Connect("pc");
            Mqtt.Subscribe(new[] { "Pilot/#" }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            State = "MQTT Connected";
            Trace.WriteLine("MQTT Connected");

            new DispatcherTimer(tsFade, DispatcherPriority.Background, (s, ee) =>
            {
                for (int i = FadingDots.Count; i > 0; i--)
                {
                    var fadingDot = FadingDots[i - 1];
                    fadingDot.Opacity *= FadeFactor;
                    if (fadingDot.Opacity < .01)
                    {
                        MyCanvas.Children.Remove(fadingDot);
                        FadingDots.Remove(fadingDot);
                    }
                }

            }, Dispatcher).Start();
        }

        void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            //Trace.WriteLine(string.Format("Mqtt_MqttMsgPublishReceived: {0}", e.Topic));
            switch (e.Topic)
            {
                case "Pilot/Pose":
                    RobotPose p = JsonConvert.DeserializeObject<RobotPose>(System.Text.Encoding.UTF8.GetString(e.Message));
                    Dispatcher.InvokeAsync(() =>
                    {
                        NewRobotPose(p.X * DrawScale, p.Y * DrawScale, p.H);
                    });
                    break;

                default:
                    break;
            }
        }

        private void NewRobotPose(double x, double y, double h)
        {
            RobotH = (int)h.inDegrees();    // for compass

            // fading trail
            var fadingDot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Blue, RenderTransform = new TranslateTransform { X = -4, Y = -4 } };

            MyCanvas.Children.Add(fadingDot);
            FadingDots.Add(fadingDot);
            MyCanvas.SetLeft(fadingDot, x);
            MyCanvas.SetTop(fadingDot, y);

            if (RobotDot != null)
                MyCanvas.Children.Remove(RobotDot);

            RobotDot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Cyan, RenderTransform = new TranslateTransform { X = -5, Y = -5 } };
            MyCanvas.Children.Add(RobotDot);
            MyCanvas.SetLeft(RobotDot, x);
            MyCanvas.SetTop(RobotDot, y);
        }

        private void TestG_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("TestG_Click");
            const double deg2rad = Math.PI / 180.0;
            const double r = 150.0;

            new Thread(new ThreadStart(() =>
            {
                for (double angle = 0; angle <= 360; angle += 10)
                {
                    var pose = new Point(Math.Cos(angle * deg2rad) * r, -Math.Sin(angle * deg2rad) * r);
                    MyCanvas.Dispatcher.InvokeAsync(() =>
                    {
                        NewRobotPose(pose.X, pose.Y, (angle - 180).inRadians());
                    });
                    System.Threading.Thread.Sleep(50);
                }
            })).Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();
        }

        private void TestP_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("TestP_Click");
            Mqtt.Publish("PC/Test123", Encoding.UTF8.GetBytes("{Test123}"));
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class RobotPose
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float H { get; set; }
    }
}