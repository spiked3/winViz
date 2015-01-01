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
        private DataModel DataModel;

        private readonly TimeSpan tsFade = new TimeSpan(0, 0, 0, 0, 500);
        private const double fadeFactor = .4;

        private Ellipse RobotDot;

        public MainWindow()
        {
            InitializeComponent();

            DataModel = (DataContext as DataModel);

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
            if (DataModel == null || DataModel.Left == null || DataModel.Right == null)
                return;

            if (!DataModel.Left.TachoCount.HasValue || !DataModel.Right.TachoCount.HasValue)
                return;

            double ticksToMM = DataModel.WheelDiameter / DataModel.TicksPerRevolution;

            var leftTachoChange = DataModel.CurrentLeftTacho - DataModel.LastLeftTacho;
            var rightTachoChange = DataModel.CurrentRightTacho - DataModel.LastRightTacho;

            if (Math.Abs(leftTachoChange) + Math.Abs(rightTachoChange) < 1)
                return; // insignificant movement, avoid updating

            if (leftTachoChange - rightTachoChange == 0)
            {
                // straight
                double leftDelta = leftTachoChange * ticksToMM;
                DataModel.RobotX += leftDelta * Math.Sin(DataModel.RobotH);
                DataModel.RobotY += leftDelta * Math.Cos(DataModel.RobotH);
                //Trace.WriteLine(string.Format("Pose straight(unexpected) L ticks {0}  L mm {1:F2}  newXY ({2:F2}, {3:F2})",
                //    leftTachoChange, leftDelta, DataModel.RobotX, DataModel.RobotY));
            }
            else
            {
                // turned
                double leftDelta = leftTachoChange * ticksToMM;
                double rightDelta = rightTachoChange * ticksToMM;

                double delta = (leftDelta + rightDelta) / 2.0;

                double tachoAlpha = (rightDelta - leftDelta) / DataModel.WheelBase;

                DataModel.RobotH += tachoAlpha;
                DataModel.RobotH %= (2 * Math.PI);

                DataModel.RobotX += delta * Math.Sin(DataModel.RobotH);
                DataModel.RobotY += delta * Math.Cos(DataModel.RobotH);

                //Trace.WriteLine(string.Format("Pose LR ticks ({0}, {1})  LR mm ({2:F2}, {3:F2})  newXYH ({4:F2}, {5:F2}, {6:F0})",
                //    leftTachoChange, rightTachoChange, leftDelta, rightDelta, DataModel.RobotX, DataModel.RobotY, DataModel.HeadingInDegrees));
            }

            DataModel.LastLeftTacho = DataModel.CurrentLeftTacho;
            DataModel.LastRightTacho = DataModel.CurrentRightTacho;

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
                {
                    MyCanvas.Children.Remove(fadingDot);
                    ((DispatcherTimer)s).Stop();
                }
            }, fadingDot.Dispatcher).Start();

            MyCanvas.SetLeft(fadingDot, X);
            MyCanvas.SetTop(fadingDot, Y);

            if (RobotDot != null)
                MyCanvas.Children.Remove(RobotDot);

            RobotDot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Cyan, RenderTransform = new TranslateTransform { X = -5, Y = -5 } };
            MyCanvas.Children.Add(RobotDot);
            MyCanvas.SetLeft(RobotDot, X);
            MyCanvas.SetTop(RobotDot, Y);

            MyCanvas.InvalidateVisual();
            Dispatcher.DoEvents();
        }

        private void Bumper1_OnChanged(NxtSensor sensor)
        {
            System.Diagnostics.Trace.WriteLine("Bumper1_OnPressed");
            Dispatcher.InvokeAsync(() =>
            {
                DataModel.OnPropertyChanged("Touch1Brush");
            });
        }

        private void ResetTacho(object sender, RoutedEventArgs e)
        {
            DataModel.MotorPair.ResetMotorPosition(true);
            DataModel.LastLeftTacho = DataModel.LastRightTacho =
                DataModel.CurrentLeftTacho = DataModel.CurrentRightTacho = 0;

        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            DataModel.MotorPair.Idle();
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
            ResetTacho(null, null);
            var ticksToMove = 500.0 / (Math.PI * DataModel.WheelDiameter) * DataModel.TicksPerRevolution;
            Trace.WriteLine(string.Format("Fwd_Click {0:F0} ticks", ticksToMove));
            DataModel.MotorPair.Run(30, (ushort)ticksToMove, 0);
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
            ResetTacho(null, null);
            var ticksToMove = 500.0 / (Math.PI * DataModel.WheelDiameter) * DataModel.TicksPerRevolution;
            Trace.WriteLine(string.Format("Fwd_Click {0:F0} ticks", ticksToMove));
            DataModel.MotorPair.Run(-30, (ushort)ticksToMove, 0);
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