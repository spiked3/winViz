using NDesk.Options;
using NKH.MindSqualls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace gyro1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataModel DataModel { get { return (DataContext as DataModel); } }

        public MainWindow()
        {
            InitializeComponent();

            // command line
            var p = new OptionSet
            {
   	            { "delay", v => DataModel.Delay = v != null},
            };

            p.Parse(Environment.GetCommandLineArgs());
            System.Diagnostics.Trace.WriteLine(string.Format("Startup args: nonxt: {0}", DataModel.Delay));
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // enumerate com ports
            DataModel.ComPorts = new List<string>(System.IO.Ports.SerialPort.GetPortNames());
            ComPort.SelectedValue = "COM10"; // default

            if (!DataModel.Delay)
                InitNxt();

            DispatcherTimer t = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1 / 10) };
            t.Tick += Timer_Tick;
            t.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (DataModel.Nxt != null && DataModel.Nxt.IsConnected)
                {
                    DataModel.Name = DataModel.Nxt.Name;
                    DataModel.Battery = DataModel.Nxt.BatteryLevel;
                    UpdatePose(DataModel.Nxt);
                }
            });
        }

        

        private void UpdatePose(NxtBrick nxtBrick)
        {
            if (nxtBrick == null || nxtBrick.MotorC == null || nxtBrick.MotorA == null)
                return;

            if (!nxtBrick.MotorC.TachoCount.HasValue || !nxtBrick.MotorA.TachoCount.HasValue)
                return;

            DataModel d = (DataContext as DataModel);

            double ticksToMM = d.WheelDiameter / d.TicksPerRevolution;

            var leftTachoChange = nxtBrick.MotorC.TachoCount.Value - d.LastLeftTacho;
            var rightTachoChange = nxtBrick.MotorA.TachoCount.Value - d.LastRightTacho;

            var leftDelta = leftTachoChange * ticksToMM;
            var rightDelta = rightTachoChange * ticksToMM;

            double tachoAlpha = (rightDelta - leftDelta) / d.WheelBase;

            double radius = leftDelta / tachoAlpha;
            d.RobotH += tachoAlpha;
            d.RobotH %= (2 * Math.PI);

            d.RobotX += radius * Math.Cos(d.RobotH);
            d.RobotY += radius * Math.Sin(d.RobotH);

            d.LastLeftTacho = nxtBrick.MotorC.TachoCount.Value;
            d.LastRightTacho = nxtBrick.MotorA.TachoCount.Value;            
        }

        void InitNxt()
        {
            byte p = byte.Parse(ComPort.SelectedValue.ToString().Substring(3));
            DataModel.Nxt = new NKH.MindSqualls.NxtBrick(NxtCommLinkType.Bluetooth, p);
            //ViewModel.Nxt = new NKH.MindSqualls.NxtBrick(NxtCommLinkType.USB, 0);
            try
            {
                DataModel.Nxt.Connect();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, Ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!DataModel.Nxt.IsConnected)
            {
                System.Diagnostics.Debugger.Break();
                MessageBox.Show("Nxt did not connect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DataModel.State = RobotState.Connected;
            System.Threading.Thread.Sleep(500);

            DataModel.Nxt.CommLink.StopProgram();
            System.Threading.Thread.Sleep(500);

            DataModel.Left = new NxtMotor();
            DataModel.Right = new NxtMotor(true);
            DataModel.Bumper1 = new NxtTouchSensor();

            DataModel.Nxt.MotorC = DataModel.Left;
            DataModel.Nxt.MotorA = DataModel.Right;
            DataModel.Nxt.Sensor1 = DataModel.Bumper1;

            //ViewModel.Nxt.CommLink.StartProgram("MotorControl22");
            //System.Threading.Thread.Sleep(500);
            //ViewModel.Nxt.InitSensors();

            DataModel.Bumper1.OnPressed += Bumper1_OnPressed;
            DataModel.Bumper1.PollInterval = 1000 / 15;   // x times per second

            DataModel.State = RobotState.Initialized;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            DataModel.State = RobotState.Uninitialized;
            DataModel.Nxt = null;

            InitNxt();
        }

        void Bumper1_OnPressed(NxtSensor sensor)
        {
            System.Diagnostics.Trace.WriteLine("Bumper1_OnPressed");
            Dispatcher.InvokeAsync(() =>
            {
                DataModel.OnPropertyChanged("Touch1Brush");
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
        }
        
        static TimeSpan tsFade = new TimeSpan(0,0,0,0,100);

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            
            var transX = MyCanvas.ActualWidth / 2.0;
            var transY = MyCanvas.ActualHeight / 2.0;
            double deg2rad = Math.PI / 180.0;
            var r = 20;
            for (var angle = 0; angle <= 180; angle += 10)
            {
                var pose = new Point(Math.Cos((angle - 180) * deg2rad) * r, -Math.Sin((angle - 180) * deg2rad) * r);
                System.Diagnostics.Trace.WriteLine(string.Format("Pose {0:F2}", pose));

                var el = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Blue };

                MyCanvas.Children.Add(el);
                new DispatcherTimer(tsFade, DispatcherPriority.Background, (s, ee) => {
                        el.Opacity *= .7;
                        if (el.Opacity < .1)
                            MyCanvas.Children.Remove(el);
                    }, Dispatcher).Start();

                MyCanvas.SetLeft(el, pose.X + transX);
                MyCanvas.SetTop(el, pose.Y + transY);

                MyCanvas.InvalidateVisual();
                Dispatcher.DoEvents();

                System.Threading.Thread.Sleep(50);
            }
        }
    }    
}