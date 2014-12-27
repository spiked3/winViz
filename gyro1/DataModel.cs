using NKH.MindSqualls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
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

        public bool Delay { get { return _Delay; } set { _Delay = value; OnPropertyChanged(); } } bool _Delay = false; 

        [Browsable(false)]
        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(); } } private string _Title = "Gyro1";

        [Browsable(false)]
        public string StatusText { get { return _StatusText; } set { _StatusText = value; OnPropertyChanged(); } } private string _StatusText = "Ready";

        public List<string> ComPorts { get { return _ComPorts; } set { _ComPorts = value; OnPropertyChanged(); } } private List<string> _ComPorts;

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

        //-------------------------------------------------------------

        [ExpandableObject]
        public NxtBrick Nxt { get { return _Nxt; } set { _Nxt = value; OnPropertyChanged(); } } private NxtBrick _Nxt;

        public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged(); } } private string _Name = "???";

        public NxtMotor Left { get { return _Left; } set { _Left = value; OnPropertyChanged(); } } private NxtMotor _Left;

        public NxtMotor Right { get { return _Right; } set { _Right = value; OnPropertyChanged(); } } private NxtMotor _Right;

        public NxtTouchSensor Bumper1 { get { return _Bumper1; } set { _Bumper1 = value; OnPropertyChanged(); } } private NxtTouchSensor _Bumper1;

        public float Battery { get { return _Battery; } set { _Battery = value; OnPropertyChanged(); } } private float _Battery = -1f;

        public long LeftEncoder { get { return _LeftEncoder; } set { _LeftEncoder = value; OnPropertyChanged(); } } private long _LeftEncoder = 0;

        public long RightEncoder { get { return _RightEncoder; } set { _RightEncoder = value; OnPropertyChanged(); } } private long _RightEncoder = 0;

        public double X { get { return _X; } set { _X = value; OnPropertyChanged(); } } private double _X = 0.0;

        public double Y { get { return _Y; } set { _Y = value; OnPropertyChanged(); } } private double _Y = 0.0;

        public double Heading { get { return _Heading; } set { _Heading = value; OnPropertyChanged(); } } private double _Heading = 0.0;

        public RobotState State { get { return _State; } set { _State = value; OnPropertyChanged(); } } private RobotState _State = RobotState.Uninitialized;
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