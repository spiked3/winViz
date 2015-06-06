using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using uPLibrary.Networking.M2Mqtt;

namespace spiked3.winViz
{
    public partial class RobotPanel : UserControl
    {
        public Robot Robot { get { return (DataContext as Robot); } }

        public RobotPanel()
        {
            InitializeComponent();
        }

        private void ToggleButton_Esc(object sender, RoutedEventArgs e)
        {
            Robot.SendPilot(new { Cmd = "Esc", Value = tglEsc.IsChecked ?? false ? 1 : 0 });
        }

        private void Init_Click(object sender, RoutedEventArgs e)
        {
            Robot.SendPilot(new { Cmd = "Config", PID = new float[] { 0.15F, 0.03F, 0.04F } });
            Robot.SendPilot(new { Cmd = "Config", Geom = new float[] { (float)((1000 / (Math.PI * 175) * 60)), 500F } });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tglEsc.IsChecked = false;
            Robot.SendPilot(new { Cmd = "Pwr", M1 = 0.0, M2 = 0.0 });
        }
    }
}


        //private void Stop_Click(object sender, RoutedEventArgs e)
        //{
        //    Robot.SendPilot(new { Cmd = "Pwr", M1 = 0.0, M2 = 0.0 });
        //}

        //private void Forward_Click(object sender, RoutedEventArgs e)
        //{
        //    Robot.SendPilot(new { Cmd = "Pwr", M1 = 40.0, M2 = 40.0 });
        //}

        //private void Back_Click(object sender, RoutedEventArgs e)
        //{
        //    Robot.SendPilot(new { Cmd = "Pwr", M1 = -40.0, M2 = -40.0 });
        //}

        //private void Left_Click(object sender, RoutedEventArgs e)
        //{
        //    Robot.SendPilot(new { Cmd = "Rot", Rel = -45 });
        //}

        //private void Right_Click(object sender, RoutedEventArgs e)
        //{
        //    Robot.SendPilot(new { Cmd = "Rot", Rel = 45 });
        //}

        //private void UTurn_Click(object sender, RoutedEventArgs e)
        //{
        //    Robot.SendPilot(new { Cmd = "Rot", Rel = 180 });
        //}
