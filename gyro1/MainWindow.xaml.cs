using NDesk.Options;
using NKH.MindSqualls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace gyro1
{
    public partial class MainWindow : Window
    {
        private DataModel DataModel { get { return (DataContext as DataModel); } }

        static TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 100);

        Ellipse RobotDot;

        public MainWindow()
        {
            InitializeComponent();

            // command line
            var p = new OptionSet
            {
   	            { "delay", v => DataModel.Delay = v != null},
            };

            p.Parse(Environment.GetCommandLineArgs());
            System.Diagnostics.Trace.WriteLine(string.Format("Startup args: delay: {0}", DataModel.Delay));
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
        }

        private void UpdatePose()
        {
            DataModel d = (DataContext as DataModel);

            if (d == null || DataModel.Left == null || DataModel.Right == null)
                return;

            if (!DataModel.Left.TachoCount.HasValue || !DataModel.Right.TachoCount.HasValue)
                return;

            double ticksToMM = d.WheelDiameter / d.TicksPerRevolution;

            var leftTachoChange = DataModel.Left.TachoCount.Value - d.LastLeftTacho;
            var rightTachoChange = DataModel.Right.TachoCount.Value - d.LastRightTacho;

            if (Math.Abs(leftTachoChange) + Math.Abs(rightTachoChange) < .01)
                return; // insignificant movement, avoid updating

            if (leftTachoChange - rightTachoChange == 0)
            {
                // straight
                var leftDelta = leftTachoChange * ticksToMM;
                d.RobotX += leftDelta * Math.Sin(d.RobotH);
                d.RobotY += leftDelta * Math.Cos(d.RobotH);
                Trace.WriteLine(string.Format("Pose straight(unexpected) L ticks {0}  L mm {1:F2}  newXY ({2:F2}, {3:F2})",
                    leftTachoChange, leftDelta, d.RobotX, d.RobotY));
            }
            else
            {
                // turned
                var leftDelta = leftTachoChange * ticksToMM;
                var rightDelta = rightTachoChange * ticksToMM;

                double tachoHeadingChange = (rightDelta - leftDelta) / d.WheelBase;

                double radius = leftDelta / tachoHeadingChange;
                d.RobotH += tachoHeadingChange;
                d.RobotH %= (2 * Math.PI);

                d.RobotX += radius * Math.Sin(d.RobotH);
                d.RobotY += radius * Math.Cos(d.RobotH);

                Trace.WriteLine(string.Format("Pose LR ticks ({0}, {1})  LR mm ({2:F2}, {3:F2})  newXY ({4:F2}, {5:F2})",
                    leftTachoChange, rightTachoChange, leftDelta, rightDelta, d.RobotX, d.RobotY));
            }

            d.LastLeftTacho = DataModel.Left.TachoCount.Value;
            d.LastRightTacho = DataModel.Right.TachoCount.Value;

            NewRobotPose(DataModel.RobotX, DataModel.RobotY);
        }

        void NewRobotPose(double X, double Y)
        {
            // fading trail
            var fadingDot = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Blue, RenderTransform = new TranslateTransform { X = -2.5, Y = -2.5 } };
            MyCanvas.Children.Add(fadingDot);
            new DispatcherTimer(tsFade, DispatcherPriority.Background, (s, ee) =>
            {
                fadingDot.Opacity *= .7;
                if (fadingDot.Opacity < .1)
                    MyCanvas.Children.Remove(fadingDot);
            }, Dispatcher).Start();

            MyCanvas.SetLeft(fadingDot, X / 10);
            MyCanvas.SetTop(fadingDot, Y / 10);

            if (RobotDot != null)
                MyCanvas.Children.Remove(RobotDot);

            RobotDot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Cyan, RenderTransform = new TranslateTransform { X = -5, Y = -5 } };
            MyCanvas.Children.Add(RobotDot);
            MyCanvas.SetLeft(RobotDot, X / 10);
            MyCanvas.SetTop(RobotDot, Y / 10);

            MyCanvas.InvalidateVisual();
            Dispatcher.DoEvents();
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

            DataModel.Nxt.CommLink.StartProgram("MotorControl22.rxe");
            System.Threading.Thread.Sleep(500);

            DataModel.Left = new NxtMotor();
            DataModel.Right = new NxtMotor();

            DataModel.Nxt.CommLink.ResetMotorPosition(NxtMotorPort.PortC, false);
            DataModel.Nxt.CommLink.ResetMotorPosition(NxtMotorPort.PortA, false);

            DataModel.Bumper1 = new NxtTouchSensor();

            DataModel.Nxt.MotorC = DataModel.Left;
            DataModel.Nxt.MotorA = DataModel.Right;
            DataModel.Nxt.Sensor1 = DataModel.Bumper1;

            DataModel.MotorPair = new NxtMotorSync(DataModel.Left, DataModel.Right);

            DataModel.Left.PollInterval = 
                DataModel.Right.PollInterval = 
                DataModel.Bumper1.PollInterval = 1000 / 20;

            DataModel.Bumper1.OnPressed += Bumper1_OnChanged;
            DataModel.Bumper1.OnReleased += Bumper1_OnChanged;

            // +++ i do not think we are getting consistent left and right info by just using left here
            // but it works really well with both
            DataModel.Left.OnPolled += (s) => { Dispatcher.InvokeAsync(() => { UpdatePose(); }); };
            DataModel.Right.OnPolled += (s) => { Dispatcher.InvokeAsync(() => { UpdatePose(); }); };

            DataModel.Nxt.InitSensors();

            DataModel.State = RobotState.Initialized;
        }

        void Motor_Polled(NxtPollable polledItem)
        {
            UpdatePose();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            DataModel.State = RobotState.Uninitialized;
            DataModel.Nxt = null;

            InitNxt();
        }

        void Bumper1_OnChanged(NxtSensor sensor)
        {
            System.Diagnostics.Trace.WriteLine("Bumper1_OnPressed");
            Dispatcher.InvokeAsync(() =>
            {
                DataModel.OnPropertyChanged("Touch1Brush");
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            DataModel.MotorPair.ResetMotorPosition(false);
            DataModel.RobotX = DataModel.RobotY = DataModel.RobotH = 0.0;

            DataModel.MotorPair.Run(30, 360, 0);

            // +++ turn 180, go back

            DataModel.MotorPair.Run(-30, 1, 0);
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            DataModel.MotorPair.Idle();
            //DataModel.Right.Run(0, 0);
            //DataModel.Left.Run(0, 0);
        }
        
        private void Test_Click(object sender, RoutedEventArgs e)
        {
            var transX = MyCanvas.ActualWidth / 2.0;
            var transY = MyCanvas.ActualHeight / 2.0;
            double deg2rad = Math.PI / 180.0;
            var r = 200;
            for (var angle = 0; angle <= 180; angle += 10)
            {
                var pose = new Point(Math.Sin(angle * deg2rad) * r, -Math.Cos(angle * deg2rad) * r);
                //System.Diagnostics.Trace.WriteLine(string.Format("Pose {0:F2}", pose));
                NewRobotPose(pose.X, pose.Y);
                System.Threading.Thread.Sleep(50);
            }
        }

        private void Fwd_Click(object sender, RoutedEventArgs e)
        {
            DataModel.MotorPair.Run(30, 0, 0);            
        }

        private void Turn90_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Turn_Minus90(object sender, RoutedEventArgs e)
        {

        }

        private void Turn180_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {

        }
    }    
}