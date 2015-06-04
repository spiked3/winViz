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

using RpLidarLib;
using RnSlamLib;

using HelixToolkit.Wpf;
using System.Windows.Controls;
using System.Text;
using NDesk.Options;

// restyle ribbon button system menu to be a glassy S3 button, like dex and 3DS does w/infragistics
//  robot object as object - need model to know how to comm
//  robot ribbon menu group should a tab created as a result of adding the robot
//  start thinking navplanner merge

namespace spiked3.winViz
{
    public partial class MainWindow : RibbonWindow
    {

        #region Depency_Properties
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

        #endregion

        //////////////////////////

        private const double r = 10.0;
        ObservableCollection<object> _ViewObjects = new ObservableCollection<object>();
        private bool firstStep = true;
        string LastRobot;
        LidarCanvas LidarCanvas;

        string brokerString;
        MqttClient Mqtt;
        Dictionary<string, Visual3D> RobotDictionary = new Dictionary<string, Visual3D>();
        ILidar RpLidar;
        Slam Slam;
        private int StartAt = 0, Step = 4;
        private double startX = -10.0, startY = 0.0;
        private Vector3D xAxis = new Vector3D(1, 0, 0);
        private Vector3D yAxis = new Vector3D(0, 1, 0);
        private Vector3D zAxis = new Vector3D(0, 0, 1);

        public MainWindow()
        {
            InitializeComponent();
            //Mqtt = new MqttClient(ConfigManager.Get<string>(brokerString));

            brokerString = "127.0.0.1";

            var p = new OptionSet
            {
                   { "broker=", (v) => { brokerString = ConfigManager.Get<string>(v); } },
            };

            p.Parse(Environment.GetCommandLineArgs());

            Width = Settings.Default.Width;
            Height = Settings.Default.Height;
            Top = Settings.Default.Top;
            Left = Settings.Default.Left;

            if (Width == 0 || Height == 0)
            {
                Width = 640;
                Height = 480;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            spiked3.Console.MessageLevel = 3;
            Trace.WriteLine("winViz / Gyro Fusion 0.3 © 2015 spiked3.com", "+");
            //Mqtt = new MqttClient(ConfigManager.Get<string>("brokerPi"));
            //Mqtt = new MqttClient(ConfigManager.Get<string>("brokerSelf"));
            State = $"MQTT Connecting {brokerString}";
            Trace.WriteLine(State, "1");
            Mqtt = new MqttClient(brokerString);
            Mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            Mqtt.Connect("PC");
            Mqtt.Subscribe(new[] { "robot1/#" }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });  //+++ per robot
            State = "MQTT Connected";
            Trace.WriteLine("MQTT Connected", "1");

            ViewObjects.Add(Mqtt);

            if (Settings.Default.LastRobot != null && System.IO.File.Exists(Settings.Default.LastRobot))
                theRobot = LoadRobot(Settings.Default.LastRobot);

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

        Robot LoadRobot(string filename)
        {
            Robot r = new Robot { Mqtt = Mqtt };

            // +++ handle multi robots

            // for now - delete existing panel if there
            for (int miniIdx = MiniUis.Count - 1; miniIdx >= 0; miniIdx--)
                if (MiniUis[miniIdx] is RobotPanel)
                    MiniUis.RemoveAt(miniIdx);

            r.Panel = new RobotPanel { MaxWidth = 340, ToolTip = filename, DataContext = r };
            r.Panel.joy1.JoystickMovedListeners += JoystickHandler;
            MiniUiAdd(r.Panel, "Robot1", Brushes.Blue);
            
            foreach (var robt in RobotDictionary.Values)
                view1.Children.Remove(robt);
            RobotDictionary.Clear();                // we are only supporting one at the moment

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
            xg.Children.Add(new ScaleTransform3D(.001, .001, .001));
            xg.Children.Add(new TranslateTransform3D(0, 0, .04));
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

            return r;
        }

        double lastJoyM1, lastJoyM2;
        Robot theRobot;

        private void JoystickHandler(rChordata.DiamondPoint p)
        {
            if (p.Left != lastJoyM1 || p.Right != lastJoyM2)
            {
                theRobot.SendPilot(new { Cmd = "Pwr", M1 = p.Left, M2 = p.Right });
                lastJoyM1 = p.Left;
                lastJoyM2 = p.Right;
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void MiniUiAdd(UIElement u, string title, Brush bg)
        {
            var ex = new Expander
            {
                ExpandDirection = ExpandDirection.Down,
                Header = title,
                Background = bg,
                Foreground = Brushes.White,
                Padding = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            ex.Content = u;
            MiniUis.Add(ex);
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
                        if (j != null)
                        {
                            string type = (string)j["T"];
                            if (type.Equals("Pose"))
                                Dispatcher.InvokeAsync(() =>
                                {
                                    NewRobotPose("robot1", (double)j["X"], (double)j["Y"], 0.0, (double)j["H"]);
                                }, DispatcherPriority.Render);
                        }
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

                // heading 0 = north,  is up (in 2D) ie. y+ is North
                var g = new Transform3DGroup();
                g.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, 90 - RobotH)));
                g.Children.Add(new TranslateTransform3D(RobotX, RobotY, RobotZ));

                RobotDictionary[robot].Transform = g;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            NewRobotPose("robot1", 0, 0, 0, 0);
            Mqtt.Publish("robot1/Cmd", UTF8Encoding.ASCII.GetBytes(@"{""Cmd"":""Reset""}"));
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
            Mqtt.Publish("Cmd/robot1", UTF8Encoding.ASCII.GetBytes(@"{""T"":""Cmd"", ""Cmd"":""Test1""}"));
        }

        private void SaveLayout_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        void Test2_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Test2_Click", "1");
            Mqtt.Publish("Cmd/robot1", UTF8Encoding.ASCII.GetBytes(@"{""T"":""Cmd"", ""Cmd"":""Reset""}"));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Mqtt != null && Mqtt.IsConnected)
                Mqtt.Disconnect();
            SaveSettings();
        }

        private void SaveSettings()
        {
            Settings.Default.Width = (float)Width;
            Settings.Default.Height = (float)Height;
            Settings.Default.Top = (float)Top;
            Settings.Default.Left = (float)Left;
            Settings.Default.LastRobot = LastRobot;
            Settings.Default.Save();
        }
    }
}