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

namespace spiked3.winViz
{
    /// <summary>
    /// Interaction logic for RobotPanel.xaml
    /// </summary>
    public partial class RobotPanel : UserControl
    {
        public RobotPanel()
        {
            InitializeComponent();
        }

        /*
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            MotorDirectionForward = true;
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", Speed).ToBytes());
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", 0).ToBytes());
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MotorDirectionForward = false;
            if (Mqtt != null) Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", -Speed).ToBytes());
        }

        

        private void SpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Mqtt != null)
                Mqtt.Publish("PC/M1", string.Format("\"p\":{0}", MotorDirectionForward ? Speed : -Speed).ToBytes());
        }         
        */
    }
}
