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

        private readonly TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 500);
        private const double fadeFactor = .4;

        private Ellipse RobotDot;

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

            var t = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1000 / 10) };
            t.Tick += (s, ee) => Dispatcher.Invoke(() => UpdatePose());
            t.Start();
        }

        private void UpdatePose()
        {
            DataModel d = (DataContext as DataModel);

            if (d == null || DataModel.Left == null || DataModel.Right == null)
                return;

            if (!DataModel.Left.TachoCount.HasValue || !DataModel.Right.TachoCount.HasValue)
                return;

            double ticksToMM = d.WheelDiameter / d.TicksPerRevolution;

            int leftTachoChange = (int)(DataModel.CurrentLeftTacho - d.LastLeftTacho);
            int rightTachoChange = (int)(DataModel.CurrentRightTacho - d.LastRightTacho);

            if (Math.Abs(leftTachoChange) + Math.Abs(rightTachoChange) < 2)
                return; // insignificant movement, avoid updating

            if (leftTachoChange - rightTachoChange == 0)
            {
                // straight
                double leftDelta = leftTachoChange * ticksToMM;
                d.RobotX += leftDelta * Math.Sin(d.RobotH);
                d.RobotY += leftDelta * -Math.Cos(d.RobotH);
                Trace.WriteLine(string.Format("Pose straight(unexpected) L ticks {0}  L mm {1:F2}  newXY ({2:F2}, {3:F2})",
                    leftTachoChange, leftDelta, d.RobotX, d.RobotY));
            }
            else
            {
                // turned
                double leftDelta = leftTachoChange * ticksToMM;
                double rightDelta = rightTachoChange * ticksToMM;

                double tachoAlpha = (rightDelta - leftDelta) / d.WheelBase;

                double radius = leftDelta / tachoAlpha;

                d.RobotH += tachoAlpha;
                d.RobotH %= (2 * Math.PI);

                d.RobotX += radius * Math.Sin(d.RobotH);
                d.RobotY += radius * -Math.Cos(d.RobotH);

                Trace.WriteLine(string.Format("Pose LR ticks ({0}, {1})  LR mm ({2:F2}, {3:F2})  newXYH ({4:F2}, {5:F2}, {6:F0})",
                    leftTachoChange, rightTachoChange, leftDelta, rightDelta, d.RobotX, d.RobotY, d.HeadingInDegrees));
            }

            d.LastLeftTacho = DataModel.CurrentLeftTacho;
            d.LastRightTacho = DataModel.CurrentRightTacho;

            NewRobotPose(DataModel.RobotX, DataModel.RobotY);
        }

        private void NewRobotPose(double X, double Y)
        {
            // fading trail
            var fadingDot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Blue, RenderTransform = new TranslateTransform { X = -4, Y = -4 } };
            MyCanvas.Children.Add(fadingDot);
            new DispatcherTimer(tsFade, DispatcherPriority.Background, (s, ee) =>
            {
                fadingDot.Opacity *= fadeFactor;
                if (fadingDot.Opacity < .01)
                    MyCanvas.Children.Remove(fadingDot);
            }, Dispatcher).Start();

            MyCanvas.SetLeft(fadingDot, X / 100);
            MyCanvas.SetTop(fadingDot, Y / 100);

            if (RobotDot != null)
                MyCanvas.Children.Remove(RobotDot);

            RobotDot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Cyan, RenderTransform = new TranslateTransform { X = -5, Y = -5 } };
            MyCanvas.Children.Add(RobotDot);
            MyCanvas.SetLeft(RobotDot, X / 100);
            MyCanvas.SetTop(RobotDot, Y / 100);

            MyCanvas.InvalidateVisual();
            Dispatcher.DoEvents();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            DataModel.State = RobotState.Uninitialized;
            DataModel.Nxt = null;

            InitNxt();
        }

        private void Bumper1_OnChanged(NxtSensor sensor)
        {
            System.Diagnostics.Trace.WriteLine("Bumper1_OnPressed");
            Dispatcher.InvokeAsync(() =>
            {
                DataModel.OnPropertyChanged("Touch1Brush");
            });
        }

        private void ResetTacho()
        {
            DataModel.MotorPair.ResetMotorPosition(false);
            DataModel.LastLeftTacho = DataModel.LastRightTacho =
                DataModel.CurrentLeftTacho = DataModel.CurrentRightTacho = 0;
            DataModel.RobotX = DataModel.RobotY = DataModel.RobotH = 0.0;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            ResetTacho();

            var ticksPerMeter = 1000.0 / (Math.PI * DataModel.WheelDiameter) * DataModel.TicksPerRevolution;

            Trace.WriteLine(string.Format("moving straight {0:F0} ticks ({1:F2} revolutions)", ticksPerMeter, ticksPerMeter / 360.0));
            DataModel.MotorPair.Run(30, (ushort)ticksPerMeter, 0);

            //var halfTurnMM = DataModel.WheelBase * Math.PI / 2.0;
            //var halfTurnTicks = halfTurnMM / DataModel.WheelDiameter * DataModel.TicksPerRevolution;
            //Trace.WriteLine(string.Format("turn 180 = {0:F2} mm  = {1} ticks", halfTurnMM, halfTurnTicks));

            //DataModel.MotorPair.Run(40, (ushort)halfTurnTicks, 100);

            //DataModel.MotorPair.Run(30, 360 * 1, 0);
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            DataModel.MotorPair.Idle();
            //DataModel.Right.Run(0, 0);
            //DataModel.Left.Run(0, 0);
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            double deg2rad = Math.PI / 180.0;
            var r = 15000;
            for (var angle = 0; angle <= 360; angle += 10)
            {
                var pose = new Point(Math.Sin(angle * deg2rad) * r, -Math.Cos(angle * deg2rad) * r);
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

        private void JoystickControl_JoystickMoved(rChordata.DiamondPoint p)
        {
            if (DataModel.Left != null && DataModel.Right != null)
            {
                DataModel.Left.Run((sbyte)p.Left, 0);
                DataModel.Right.Run((sbyte)p.Right, 0);
            }
        }
    }
}