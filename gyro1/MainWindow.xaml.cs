﻿using Newtonsoft.Json;
using spiked3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace gyro1
{
    public partial class MainWindow : Window
    {
        private Vector3D zAxis = new Vector3D(0, 0, 1);

        private bool MotorDirectionForward = true;

        public int Speed
        {
            get { return (int)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(int), typeof(MainWindow), new PropertyMetadata(50));

        public string State
        {
            get { return (string)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(string), typeof(MainWindow), new PropertyMetadata("Initial State"));

        public double RobotX
        {
            get { return (double)GetValue(RobotXProperty); }
            set { SetValue(RobotXProperty, value); }
        }

        public static readonly DependencyProperty RobotXProperty =
            DependencyProperty.Register("RobotX", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public double RobotY
        {
            get { return (double)GetValue(RobotYProperty); }
            set { SetValue(RobotYProperty, value); }
        }

        public static readonly DependencyProperty RobotYProperty =
            DependencyProperty.Register("RobotY", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public double RobotZ
        {
            get { return (double)GetValue(RobotZProperty); }
            set { SetValue(RobotZProperty, value); }
        }

        public static readonly DependencyProperty RobotZProperty =
            DependencyProperty.Register("RobotZ", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

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

        public static readonly DependencyProperty FadeFactorProperty =
            DependencyProperty.Register("FadeFactor", typeof(double), typeof(MainWindow), new PropertyMetadata(0.7));

        public ObservableCollection<object> ViewObjects { get { return _ViewObjects; } }

        private ObservableCollection<object> _ViewObjects = new ObservableCollection<object>();

        private const string Broker = "127.0.0.1";
        private MqttClient Mqtt;

        private readonly TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 100);

        private List<Ellipse> FadingDots = new List<Ellipse>();

        public MainWindow()
        {
            InitializeComponent();
            ViewObjects.Add(robot1);
            ViewObjects.Add(grid1);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            spiked3.Console.MessageLevel = 1;
            Trace.WriteLine("S3 Gyro1 Encoder/Gyro Fusion 0.9 © 2015 spiked3.com", "+");
            State = "MQTT Connecting ...";
            Mqtt = new MqttClient(Broker);
            Mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            Mqtt.Connect("pc");
            Mqtt.Subscribe(new[] { "Pilot/#" }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            State = "MQTT Connected";
            Trace.WriteLine("MQTT Connected", "1");

            ViewObjects.Add(Mqtt);

            //new DispatcherTimer(tsFade, DispatcherPriority.Normal, (s, ee) =>
            //{
            //    for (int i = FadingDots.Count; i > 0; i--)
            //    {
            //        var fadingDot = FadingDots[i - 1];
            //        fadingDot.Opacity *= FadeFactor;
            //        if (fadingDot.Opacity < .01)
            //        {
            //            //MyCanvas.Children.Remove(fadingDot);
            //            FadingDots.Remove(fadingDot);
            //        }
            //    }
            //}, Dispatcher).Start();

            NewRobotPose(0, 0, 0, 0);
        }

        private class RobotPose
        {
            public float X { get; set; }

            public float Y { get; set; }

            public float H { get; set; }
        }

        private void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Trace.WriteLine(string.Format("Mqtt_MqttMsgPublishReceived: {0}/{1}", e.Topic, System.Text.Encoding.UTF8.GetString(e.Message)), "3");
            switch (e.Topic)
            {
                case "Pilot/Pose":
                    // +++ change to dynamic
                    RobotPose p = JsonConvert.DeserializeObject<RobotPose>(System.Text.Encoding.UTF8.GetString(e.Message));
                    Dispatcher.InvokeAsync(() =>
                    {
                        NewRobotPose(p.X * DrawScale, p.Y * DrawScale, 0, p.H);
                    });
                    break;

                case "Pilot/Log":
                    string t = System.Text.Encoding.UTF8.GetString(e.Message);
                    Trace.WriteLine(t);
                    break;

                default:
                    break;
            }
        }

        private void NewRobotPose(double x, double y, double z, double h_radians)
        {
            RobotX = x;
            RobotY = y;
            RobotZ = z;
            RobotH = (int)h_radians.inDegrees();

            // fading trail
            //var fadingDot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Blue, RenderTransform = new TranslateTransform { X = -4, Y = -4 } };
            //MyCanvas.Children.Add(fadingDot);
            //FadingDots.Add(fadingDot);
            //MyCanvas.SetLeft(fadingDot, x);
            //MyCanvas.SetTop(fadingDot, y);

            // x/north is up
            var g = new Transform3DGroup();
            g.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, RobotH)));
            g.Children.Add(new TranslateTransform3D(y, x, z));
            robot1.Transform = g;
        }

        private void TestG_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("TestG_Click", "1");
            const double r = 5.0;

            new Thread(new ThreadStart(() =>
            {
                for (double angle = 0; angle <= 360; angle += 10)
                {
                    var pose = new Point(-Math.Sin((angle).inRadians()) * r, Math.Cos((angle).inRadians()) * r);
                    Dispatcher.Invoke(() =>
                    {
                        NewRobotPose(pose.X, pose.Y, 0, (angle + 90).inRadians());
                    });
                    System.Threading.Thread.Sleep(1000);
                }
                Dispatcher.Invoke(() => { NewRobotPose(0, 0, 0, 0); });
            })).Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();
        }

        private void TestP_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("TestP_Click/ ", "1");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConsoleTest_Click(object sender, RoutedEventArgs e)
        {
            console1.Test();
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            MotorDirectionForward = true;
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("{0}", Speed).ToBytes());
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("{0}", 0).ToBytes());
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MotorDirectionForward = false;
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("{0}", -Speed).ToBytes());
        }

        private void SpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Mqtt != null)
                Mqtt.Publish("PC/M1", string.Format("{0}", MotorDirectionForward ? Speed : -Speed).ToBytes());
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            NewRobotPose(0, 0, 0, 0);
        }
    }
}