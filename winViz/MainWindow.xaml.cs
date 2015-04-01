using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Controls.Ribbon;

using Newtonsoft.Json;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

using spiked3;
using spiked3.winViz.Properties;
using spiked3.winRobotLib;

using RpLidarLib;
using RnSlamLib;

using HelixToolkit.Wpf;
using System.Windows.Controls;
using System.Text;

// done move console test to test group
// todo WPF bug; expander blocks focus even when not expanded
// Frame - eg TF like functions
// restyle ribbon button system menu to be a glassy S3 button, like dex and 3DS does w/infragistics
// robot object as object
//  robot ribbon menu group should a tab created as a result of adding the robot
// robot simulator - test bed / functions / 

// start defining the infratrucure (ie std_msgs/variables/services(as Libraries))
//     frameLib for transform services. create a frame object, set base, point& frame.tranform(point) 
//     winViz  load robots. accept sensor values (dynamic?). accept pose.  DRFU?

// start thinking navplanner merge

// ?functionality to get presentable demo / set prorities, best roadmap
//  need to get model loaded
//  

namespace spiked3.winViz
{
    public partial class MainWindow : RibbonWindow
    {
        public static readonly DependencyProperty MiniUisProperty =
            DependencyProperty.Register("MiniUis", typeof(ObservableCollection<UIElement>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<UIElement>()));

        public static readonly DependencyProperty RobotBrushProperty =
            DependencyProperty.Register("RobotBrush", typeof(Brush), typeof(MainWindow), new PropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty RobotHProperty =
            DependencyProperty.Register("RobotH", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public static readonly DependencyProperty RobotXProperty =
            DependencyProperty.Register("RobotX", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public static readonly DependencyProperty RobotYProperty =
            DependencyProperty.Register("RobotY", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public static readonly DependencyProperty RobotZProperty =
            DependencyProperty.Register("RobotZ", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(int), typeof(MainWindow), new PropertyMetadata(50));

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(string), typeof(MainWindow), new PropertyMetadata("Initial State"));

        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(MainWindow), new PropertyMetadata("StatusText"));

        public ObservableCollection<UIElement> MiniUis
        {
            get { return (ObservableCollection<UIElement>)GetValue(MiniUisProperty); }
            set { SetValue(MiniUisProperty, value); }
        }

        public Brush RobotBrush
        {
            get { return (Brush)GetValue(RobotBrushProperty); }
            set { SetValue(RobotBrushProperty, value); }
        }

        public int RobotH
        {
            get { return (int)GetValue(RobotHProperty); }
            set { SetValue(RobotHProperty, value); }
        }

        public double RobotX
        {
            get { return (double)GetValue(RobotXProperty); }
            set { SetValue(RobotXProperty, value); }
        }

        public double RobotY
        {
            get { return (double)GetValue(RobotYProperty); }
            set { SetValue(RobotYProperty, value); }
        }

        public double RobotZ
        {
            get { return (double)GetValue(RobotZProperty); }
            set { SetValue(RobotZProperty, value); }
        }

        public int Speed
        {
            get { return (int)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        public string State
        {
            get { return (string)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        public ObservableCollection<object> ViewObjects { get { return _ViewObjects; } }

        //////////////////////////

        private const string Broker = "127.0.0.1";
        private const double r = 10.0;
        ObservableCollection<object> _ViewObjects = new ObservableCollection<object>();
        private bool firstStep = true;
        string LastRobot;
        LidarCanvas LidarCanvas;
        Dictionary<string, string> MachineToLidarPort = new Dictionary<string, string>();

        private MqttClient Mqtt;
        Dictionary<string, Visual3D> RobotDictionary = new Dictionary<string, Visual3D>();
        RpLidarDriver RpLidar;
        Slam Slam;
        private int StartAt = 0, Step = 4;
        private double startX = -10.0, startY = 0.0;
        private Vector3D xAxis = new Vector3D(1, 0, 0);
        private Vector3D yAxis = new Vector3D(0, 1, 0);
        private Vector3D zAxis = new Vector3D(0, 0, 1);

        public MainWindow()
        {
            InitializeComponent();

            MachineToLidarPort.Add("ws1", "com6");
            MachineToLidarPort.Add("msi2", "com3");

            Width = Settings.Default.Width;
            Height = Settings.Default.Height;
            Top = Settings.Default.Top;
            Left = Settings.Default.Left;

            if (Width == 0 || Height == 0)
            {
                Width = 640;
                Height = 480;
            }

            if (Settings.Default.LastRobot != null && System.IO.File.Exists(Settings.Default.LastRobot))
                LoadRobot(Settings.Default.LastRobot);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            spiked3.Console.MessageLevel = 3;
            Trace.WriteLine("winViz / Gyro Fusion 0.2 © 2015 spiked3.com", "+");
            State = "MQTT Connecting ...";
            Mqtt = new MqttClient(Broker);
            Mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            Mqtt.Connect("PC");
            Mqtt.Subscribe(new[] { "robot1/#" }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });  //+++ per robot
            State = "MQTT Connected";
            Trace.WriteLine("MQTT Connected", "1");

            ViewObjects.Add(Mqtt);

            var u = new LidarPanel { Width = 320, Height = 300, Margin = new Thickness(0, 4, 0, 4) };
            LidarCanvas = u.LidarCanvas;
            MiniUiAdd(u, "LIDAR", Brushes.Red);
        }

        private void ConsoleTest_Click(object sender, RoutedEventArgs e)
        {
            console1.Test();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void LoadRobot(string filename)
        {
            // +++ handle multi robots

            // for now - delete existing panel if there
            for (int i = MiniUis.Count - 1; i >= 0; i--)
                if (MiniUis[i] is RobotPanel)
                    MiniUis.RemoveAt(i);

            MiniUiAdd(new RobotPanel { Width = 320, ToolTip = filename }, "Robot1", Brushes.Blue);

            foreach (var r in RobotDictionary.Values)
                view1.Children.Remove(r);
            RobotDictionary.Clear();        // we are only supporting one at the moment

            MeshGeometryVisual3D robot = new MeshGeometryVisual3D();

            MeshBuilder mb = new MeshBuilder(false, false);

            var mi = new HelixToolkit.Wpf.ModelImporter();
            var g = mi.Load(filename);
            foreach (var m in g.Children)
            {
                var mGeo = m as GeometryModel3D;
                var mesh = mGeo.Geometry as MeshGeometry3D;
                if (mesh != null)
                    mb.Append(mesh);
            }

            robot.MeshGeometry = mb.ToMesh();
            robot.Material = Materials.Gray;

            var xg = new Transform3DGroup();
            // +++these would be values from import dialog
            xg.Children.Add(new ScaleTransform3D(.01, .01, .01));
            xg.Children.Add(new TranslateTransform3D(0, 0, .5));
            xg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, -90)));
            //xg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(xAxis, 180)));
            robot.Model.Transform = xg;
            robot.Model.Geometry = robot.Model.Geometry.Clone();  // permanently apply transform

            RobotDictionary.Add("robot1", robot);
            view1.Children.Add(robot);
            ViewObjects.Add(robot);

            NewRobotPose("robot1", 0, 0, 0, 0);
            firstStep = true;
            LastRobot = filename;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void MiniUiAdd(UIElement u, string title, Brush bg)
        {
            var ex = new Expander { ExpandDirection = ExpandDirection.Down, Header = title, Background = bg, Foreground = Brushes.White, Padding = new Thickness(4) };
            var gr = new Grid { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            gr.Children.Add(u);
            ex.Content = gr;
            MiniUis.Add(ex);
            //MiniUis.Add(new Separator { Width = 260, Margin = new Thickness(12) });
        }

        private void Model_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "STL Files|*.stl|All Files|*.*", DefaultExt = "stl" };
            if (d.ShowDialog() ?? false)
            {
                LoadRobot(d.FileName);
            }
        }

        private void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Trace.WriteLine(string.Format("Mqtt_MqttMsgPublishReceived: {0}/{1}", e.Topic, System.Text.Encoding.UTF8.GetString(e.Message)), "3");

            switch (e.Topic)
            {
                case "robot1":
                    {
                        dynamic j = JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(e.Message));
                        string type = (string)j["T"];
                        if (type.Equals("Pose"))
                            Dispatcher.InvokeAsync(() =>
                            {
                                NewRobotPose("robot1", (double)j["X"] / 100.0, (double)j["Y"] / 100.0, 0.0, (double)j["H"]);
                            }, DispatcherPriority.Render);
                        else if (type.Equals("Log"))
                            Trace.WriteLine(System.Text.Encoding.UTF8.GetString(e.Message));
                    }
                    break;

                default:
                    //System.Diagnostics.Debugger.Break();
                    break;
            }
        }

        // +++ pose display should be via a billboard near robot
        private void NewRobotPose(string robot, double x, double y, double z, double h_degrees)
        {
            if (RobotDictionary.ContainsKey(robot))
            {
                RobotX = x;
                RobotY = y;
                RobotZ = z;

                RobotH = (int)h_degrees;

                while (RobotH >= 360)
                    RobotH -= 360;
                while (RobotH < 0)
                    RobotH += 360;

                // north is up (in 2D), y+ is North
                var g = new Transform3DGroup();
                g.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, 90 - RobotH)));
                g.Children.Add(new TranslateTransform3D(RobotX, RobotY, RobotZ));

                RobotDictionary[robot].Transform = g;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            NewRobotPose("robot1", 0, 0, 0, 0);
            Mqtt.Publish("Cmd/robot1", UTF8Encoding.ASCII.GetBytes("{\"T\":\"Cmd\", \"Cmd\":\"Reset\"}"));
            firstStep = true;
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            if (firstStep)
            {
                firstStep = false;
                RobotX = startX;
                RobotY = startY;
                RobotH = StartAt;
                NewRobotPose("robot1", RobotX, RobotY, 0, RobotH);
            }
            else
            {
                RobotH += Step;
                NewRobotPose("robot1", -Math.Cos(RobotH.inRadians()) * r, Math.Sin(RobotH.inRadians()) * r, 0, RobotH);
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
                    Dispatcher.InvokeAsync(() =>
                    {
                        Step_Click(this, null);
                    });
                    System.Threading.Thread.Sleep(1000 / 30);
                }
            })).Start();
        }

        void Test1_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Test1_Click", "1");
            Mqtt.Publish("Cmd/robot1", UTF8Encoding.ASCII.GetBytes("{\"T\":\"Cmd\", \"Cmd\":\"Test1\"}"));
        }

        void Test2_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Test2_Click", "1");
            Mqtt.Publish("Cmd/robot1", UTF8Encoding.ASCII.GetBytes("{\"T\":\"Cmd\", \"Cmd\":\"Reset\"}"));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();

            Settings.Default.Width = (float)((Window)sender).Width;
            Settings.Default.Height = (float)((Window)sender).Height;
            Settings.Default.Top = (float)((Window)sender).Top;
            Settings.Default.Left = (float)((Window)sender).Left;
            Settings.Default.LastRobot = LastRobot;
            Settings.Default.Save();
        }

        #region LIDAR
        void InitLIDAR()
        {
            Slam = new Slam();
            try
            {
                RpLidar = new RpLidarDriver(MachineToLidarPort[System.Environment.MachineName]);
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

        #endregion

        private class RobotPose
        {
            public float H { get; set; }

            public float X { get; set; }

            public float Y { get; set; }
        }

    }
}