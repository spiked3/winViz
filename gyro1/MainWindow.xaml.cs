using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Win32;

using Newtonsoft.Json;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

using gyro1.Properties;
using spiked3;
using spiked3.winRobot;
using HelixToolkit.Wpf;

// done move console test to test group
// todo WPF bug; expander blocks focus even when not expanded
// FrameLib - eg TF like functions
// restyle ribbon button system menu to be a glassy S3 button, like dex and 3DS does w/infragistics
// robot object 
// robot ribbon menu group should a tab created as a result of adding the robot
// robot simulator - test bed / functions / 

// start defining the infratrucure (ie std_msgs/variables/services(as Libraries))
//     frameLib for transform services. create a frame object, set base, point& frame.tranform(point) 
//     winViz  load robots. accept sensor values (dynamic?). accept pose.  DRFU?

// start thinking navplanner merge

// ?functionality to get presentable demo / set prorities, best roadmap
//  need to get model loaded
//  

namespace gyro1
{
    public partial class MainWindow : Window
    {
        private Vector3D zAxis = new Vector3D(0, 0, 1);

        private bool MotorDirectionForward = true;

        public Brush RobotBrush
        {
            get { return (Brush)GetValue(RobotBrushProperty); }
            set { SetValue(RobotBrushProperty, value); }
        }

        public static readonly DependencyProperty RobotBrushProperty =
            DependencyProperty.Register("RobotBrush", typeof(Brush), typeof(MainWindow), new PropertyMetadata(Brushes.Gray));

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

        public ObservableCollection<object> ViewObjects { get { return _ViewObjects; } }

        private ObservableCollection<object> _ViewObjects = new ObservableCollection<object>();

        private const string Broker = "127.0.0.1";
        private MqttClient Mqtt;

        private readonly TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 100);

        private List<Ellipse> FadingDots = new List<Ellipse>();

        public MainWindow()
        {
            InitializeComponent();

            Width = Settings.Default.Width;
            Height = Settings.Default.Height;
            Top = Settings.Default.Top;
            Left = Settings.Default.Left;

            if (Width == 0 || Height == 0)
            {
                Width = 640;
                Height = 480;
            }

            ViewObjects.Add(robot1);
            ViewObjects.Add(grid1);
            ViewObjects.Add(RobotBrush);
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
                    dynamic pose = JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(e.Message));
                    Dispatcher.Invoke(() =>
                    {
                        NewRobotPose((double)pose.X, (double)pose.Y, (double)0, (double)pose.H);
                    });
                    break;

                case "Pilot/Log":
                    string t = System.Text.Encoding.UTF8.GetString(e.Message);
                    t = t.TrimStart('{').TrimEnd('}');
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

            while (RobotH >= 360)
                RobotH -= 360;
            while (RobotH < 0)
                RobotH += 360;

            // north is up, y+ is up
            var g = new Transform3DGroup();
            g.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, 90 - RobotH)));
            g.Children.Add(new TranslateTransform3D(x, y, z));
            robot1.Transform = g;
        }

        private int StartAt = 0, Step = 2;
        private double startX = -10.0, startY = 0.0;
        private const double r = 10.0;
        private bool firstStep = true;

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            if (firstStep)
            {
                firstStep = false;
                RobotX = startX;
                RobotY = startY;
                RobotH = StartAt;
                NewRobotPose(RobotX, RobotY, 0, (RobotH).inRadians());
            }
            else
            {
                RobotH += Step;
                NewRobotPose(-Math.Cos(RobotH.inRadians()) * r, Math.Sin(RobotH.inRadians()) * r, 0, RobotH.inRadians());
            }
        }

        private void TestG_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("TestG_Click", "1");

            int count = ((StartAt + 360) / Step) + 1;

            new Thread(new ThreadStart(() =>
            {
                firstStep = true;
                for (int i = 0; i < count; i++)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Step_Click(this, null);
                    });
                    System.Threading.Thread.Sleep(50);
                }
            })).Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();

            Settings.Default.Width = (float)((Window)sender).Width;
            Settings.Default.Height = (float)((Window)sender).Height;
            Settings.Default.Top = (float)((Window)sender).Top;
            Settings.Default.Left = (float)((Window)sender).Left;
            Settings.Default.Save();
        }

        private void TestP_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("TestP_Click (empty)", "1");
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
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", Speed).ToBytes());
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", 0).ToBytes());
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MotorDirectionForward = false;
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", -Speed).ToBytes());
        }

        private void SpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Mqtt != null)
                Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", MotorDirectionForward ? Speed : -Speed).ToBytes());
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            NewRobotPose(0, 0, 0, 0);
            firstStep = true;
        }

        private void Model_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "STL Files|*.stl|All Files|*.*", DefaultExt = "stl" };
            if (d.ShowDialog() ?? false)
            {
                MeshBuilder mb = new MeshBuilder(false, false);

                var mi = new HelixToolkit.Wpf.ModelImporter();
                var g = mi.Load(d.FileName);
                foreach (var m in g.Children)
                {
                    var mGeo = m as GeometryModel3D;
                    var mesh = mGeo.Geometry as MeshGeometry3D;
                    if (mesh != null)
                        mb.Append(mesh);
                }

                robot1.Model.Geometry = mb.ToMesh();

                var xg = new Transform3DGroup();
                xg.Children.Add(new ScaleTransform3D(.01, .01, .01));
                xg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, -90)));
                robot1.Model.Transform = xg;
                robot1.Model.Geometry = robot1.Model.Geometry.Clone();  // permanently apply transform
            }
        }
    }
}