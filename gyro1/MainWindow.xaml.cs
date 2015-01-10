using NDesk.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public List<string> ComPorts
        {
            get { return (List<string>)GetValue(ComPortsProperty); }
            set { SetValue(ComPortsProperty, value); }
        }
        public static readonly DependencyProperty ComPortsProperty =
            DependencyProperty.Register("ComPorts", typeof(List<string>), typeof(MainWindow), new PropertyMetadata(null));


        public float DrawScale
        {
            get { return (float)GetValue(DrawScaleProperty); }
            set { SetValue(DrawScaleProperty, value); }
        }
        public static readonly DependencyProperty DrawScaleProperty =
            DependencyProperty.Register("DrawScale", typeof(float), typeof(MainWindow), new PropertyMetadata(1F));

        const string Broker = "127.0.0.1";
        public MqttClient Mqtt;

        bool NoAuto = false;

        private readonly TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 500);
        private const double fadeFactor = .4;

        private Ellipse RobotDot;

        public MainWindow()
        {
            InitializeComponent();

            // command line
            var p = new OptionSet
            {
   	            { "noauto", v => NoAuto = v != null},
            };

            p.Parse(Environment.GetCommandLineArgs());
            System.Diagnostics.Trace.WriteLine(string.Format("Startup args; NoAuto {0}", NoAuto));
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // enumerate com ports
            ComPorts = new List<string>(System.IO.Ports.SerialPort.GetPortNames());
            ComPort.SelectedValue = "COM14"; // default

            if (!NoAuto)
                Connect();
        }

        void Connect()
        {
            Mqtt = new MqttClient(Broker);
            Mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            Mqtt.Connect("pc");
            Mqtt.Subscribe(new[] { "nxt/#" }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            switch (e.Topic)
            {
                case "nxt/Pose":
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
            RobotH = (int)h.inDegrees();
            // fading trail
            var fadingDot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Blue, RenderTransform = new TranslateTransform { X = -4, Y = -4 } };
            MyCanvas.Children.Add(fadingDot);
            new DispatcherTimer(tsFade, DispatcherPriority.Background, (s, ee) =>
            {
                fadingDot.Opacity *= fadeFactor;
                if (fadingDot.Opacity < .01)
                {
                    MyCanvas.Children.Remove(fadingDot);
                    ((DispatcherTimer)s).Stop();
                }
            }, fadingDot.Dispatcher).Start();

            MyCanvas.SetLeft(fadingDot, x);
            MyCanvas.SetTop(fadingDot, y);

            if (RobotDot != null)
                MyCanvas.Children.Remove(RobotDot);

            RobotDot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Cyan, RenderTransform = new TranslateTransform { X = -5, Y = -5 } };
            MyCanvas.Children.Add(RobotDot);
            MyCanvas.SetLeft(RobotDot, x);
            MyCanvas.SetTop(RobotDot, y);

            MyCanvas.InvalidateVisual();
            Dispatcher.DoEvents();
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            double deg2rad = Math.PI / 180.0;
            var r = 150;
            for (double angle = 0; angle <= 360; angle += 10)
            {
                var pose = new Point(Math.Sin(angle * deg2rad) * r, -Math.Cos(angle * deg2rad) * r);
                NewRobotPose(pose.X, pose.Y, (angle +90).inRadians());
                System.Threading.Thread.Sleep(50);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();
        }
    }

    public class RobotPose
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float H { get; set; }
    }
}