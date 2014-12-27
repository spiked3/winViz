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
        private static MainWindow _thisInstance;

        public static string Status { set { (_thisInstance.DataContext as ViewModel).StatusText = value; } }

        private ViewModel ViewModel { get { return (DataContext as ViewModel); } }

        List<UIElement> ObjectsToFade = new List<UIElement>();

        public MainWindow()
        {
            _thisInstance = this;
            InitializeComponent();

            // command line
            var p = new OptionSet
            {
   	            { "delay", v => ViewModel.Delay = v != null},
            };

            p.Parse(Environment.GetCommandLineArgs());
            System.Diagnostics.Trace.WriteLine(string.Format("Startup args: nonxt: {0}", ViewModel.Delay));
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // enumerate com ports
            ViewModel.ComPorts = new List<string>(System.IO.Ports.SerialPort.GetPortNames());
            ComPort.SelectedValue = "COM10"; // default

            if (!ViewModel.Delay)
                InitNxt();

            DispatcherTimer t = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1 / 10) };
            t.Tick += Timer_Tick;
            t.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (ViewModel.Nxt != null && ViewModel.Nxt.IsConnected)
                {
                    ViewModel.Name = ViewModel.Nxt.Name;
                    ViewModel.Battery = ViewModel.Nxt.BatteryLevel;
                }
            });
        }

        void InitNxt()
        {
            byte p = byte.Parse(ComPort.SelectedValue.ToString().Substring(3));
            ViewModel.Nxt = new NKH.MindSqualls.NxtBrick(NxtCommLinkType.Bluetooth, p);
            //ViewModel.Nxt = new NKH.MindSqualls.NxtBrick(NxtCommLinkType.USB, 0);
            try
            {
                ViewModel.Nxt.Connect();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, Ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ViewModel.Nxt.IsConnected)
            {
                System.Diagnostics.Debugger.Break();
                MessageBox.Show("Nxt did not connect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ViewModel.State = RobotState.Connected;
            System.Threading.Thread.Sleep(500);

            ViewModel.Nxt.CommLink.StopProgram();
            System.Threading.Thread.Sleep(500);

            ViewModel.Left = new NxtMotor();
            ViewModel.Right = new NxtMotor(true);
            ViewModel.Bumper1 = new NxtTouchSensor();

            ViewModel.Nxt.MotorC = ViewModel.Left;
            ViewModel.Nxt.MotorA = ViewModel.Right;
            ViewModel.Nxt.Sensor1 = ViewModel.Bumper1;

            //ViewModel.Nxt.CommLink.StartProgram("MotorControl22");
            //System.Threading.Thread.Sleep(500);
            //ViewModel.Nxt.InitSensors();

            ViewModel.Bumper1.OnPressed += Bumper1_OnPressed;
            ViewModel.Bumper1.PollInterval = 1000 / 15;   // x times per second

            ViewModel.State = RobotState.Initialized;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.State = RobotState.Uninitialized;
            ViewModel.Nxt = null;

            InitNxt();
        }

        void Bumper1_OnPressed(NxtSensor sensor)
        {
            System.Diagnostics.Trace.WriteLine("Bumper1_OnPressed");
            Dispatcher.InvokeAsync(() =>
            {
                ViewModel.OnPropertyChanged("Touch1Brush");
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
        }

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

                var el = new FadingEllipse { Width = 20, Height = 20, Fill = Brushes.Blue };

                MyCanvas.Children.Add(el);
                ObjectsToFade.Add(el);
                MyCanvas.SetLeft(el, pose.X + transX);
                MyCanvas.SetTop(el, pose.Y + transY);

                MyCanvas.InvalidateVisual();
                Dispatcher.DoEvents();

                System.Threading.Thread.Sleep(200);
            }
        }
    }

    public class FadingEllipse : Ellipse
    {
        static TimeSpan i1 = new TimeSpan(0, 0, 0, 0, 200);

        public FadingEllipse() : base()
        {
            var t = new DispatcherTimer { Interval = i1 };
            t.Tick += t_Tick;
        }

        void t_Tick(object sender, EventArgs e)
        {

        }
    }

    public static class extensions
    {
        public static void DoEvents(this Dispatcher d)
        {
            d.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }


}