using NKH.MindSqualls;
using NKH.MindSqualls.MotorControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace gyro1
{
    public class DataModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion INotifyPropertyChanged

        [Category("Program")]
        public bool Delay { get { return _Delay; } set { _Delay = value; OnPropertyChanged(); } } private bool _Delay = false;

        [Browsable(false)]
        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(); } } private string _Title = "Gyro1";

        [Browsable(false)]
        public string StatusText { get { return _StatusText; } set { _StatusText = value; OnPropertyChanged(); } } private string _StatusText = "Ready";

        [Browsable(false)]
        public List<string> ComPorts { get { return _ComPorts; } set { _ComPorts = value; OnPropertyChanged(); } } private List<string> _ComPorts;

        [Browsable(false)]
        public Brush Touch1Brush
        {
            get
            {
                Brush b = Brushes.White;
                if (Bumper1 != null && Bumper1.IsPressed.Value)
                    b = Brushes.Red;
                return b;
            }
        }

        //-----  Robot --------------------------------------------------------

        // robot geometry in MM
        [Category("Robot")]
        public double WheelBase { get { return _WheelBase; } set { _WheelBase = value; OnPropertyChanged(); } } private double _WheelBase = 120;

        [Category("Robot")]
        public double WheelDiameter { get { return _WheelDiameter; } set { _WheelDiameter = value; OnPropertyChanged(); } } private double _WheelDiameter = 80;

        [Category("Robot")]
        public int TicksPerRevolution { get { return _TicksPerRevolution; } set { _TicksPerRevolution = value; OnPropertyChanged(); } } private int _TicksPerRevolution = 360;

        [Category("Robot")]
        public long CurrentLeftTacho { get { return _CurrentLeftTacho; } set { _CurrentLeftTacho = value; OnPropertyChanged(); } } private long _CurrentLeftTacho = 0L;

        [Category("Robot")]
        public long CurrentRightTacho { get { return _CurrentRightTacho; } set { _CurrentRightTacho = value; OnPropertyChanged(); } } private long _CurrentRightTacho = 0L;

        [Category("Robot")]
        public long LastLeftTacho { get { return _LastLeftTacho; } set { _LastLeftTacho = value; OnPropertyChanged(); } } private long _LastLeftTacho = 0L;

        [Category("Robot")]
        public long LastRightTacho { get { return _LastRightTacho; } set { _LastRightTacho = value; OnPropertyChanged(); } } private long _LastRightTacho = 0L;

        [Category("Robot")]
        public double RobotX { get { return _RobotX; } set { _RobotX = value; OnPropertyChanged(); } } private double _RobotX = 0;

        [Category("Robot")]
        public double RobotY { get { return _RobotY; } set { _RobotY = value; OnPropertyChanged(); } } private double _RobotY = 0;

        [Category("Robot")]
        public double RobotH { get { return _RobotH; } set { _RobotH = value; OnPropertyChanged(); OnPropertyChanged("HeadingInDegrees"); } } private double _RobotH = 0;

        [Category("Robot")]
        public int HeadingInDegrees { get { return (int)(RobotH * 180.0 / Math.PI); } }

        [Category("Robot")]
        public RobotState State { get { return _State; } set { _State = value; OnPropertyChanged(); } } private RobotState _State = RobotState.Uninitialized;

        //--  NXT  ----------------------------------------------------

        [Category("NXT"), ExpandableObject]
        public NxtBrick Nxt { get { return _Nxt; } set { _Nxt = value; OnPropertyChanged(); } } private NxtBrick _Nxt;

        [Category("NXT")]
        public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged(); } } private string _Name = "???";


        [Category("NXT")]
        public McNxtMotor Left { get { return _Left; } set { _Left = value; OnPropertyChanged(); } } private McNxtMotor _Left;

        [Category("NXT")]
        public McNxtMotor Right { get { return _Right; } set { _Right = value; OnPropertyChanged(); } } private McNxtMotor _Right;

        [Category("NXT")]
        public McNxtMotorSync MotorPair { get { return _MotorPair; } set { _MotorPair = value; OnPropertyChanged(); } } McNxtMotorSync _MotorPair;

        [Category("NXT")]
        public NxtTouchSensor Bumper1 { get { return _Bumper1; } set { _Bumper1 = value; OnPropertyChanged(); } } private NxtTouchSensor _Bumper1;

        [Category("NXT")]
        public DiIMU Imu { get { return _Imu; } set { _Imu = value; OnPropertyChanged(); } } DiIMU _Imu;

        [Category("NXT")]
        public float Battery { get { return _Battery; } set { _Battery = value; OnPropertyChanged(); } } private float _Battery = -1f;
    }

    public enum RobotState { Uninitialized, Connected, Initialized, Idle, Stop, Moving };

    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}