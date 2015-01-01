using NKH.MindSqualls;
using NKH.MindSqualls.MotorControl;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace gyro1
{
    public partial class MainWindow
    {
        private void InitNxt()
        {
            if (DataModel.Nxt != null && DataModel.Nxt.CommLink.IsConnected)
                DataModel.Nxt.CommLink.Disconnect();

            //byte p = byte.Parse(ComPort.SelectedValue.ToString().Substring(3));
            //DataModel.Nxt = new NKH.MindSqualls.NxtBrick(NxtCommLinkType.Bluetooth, p);
            DataModel.Nxt = new NKH.MindSqualls.NxtBrick(NxtCommLinkType.USB, 0);
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

            DataModel.Nxt.CommLink.StopProgram();
            System.Threading.Thread.Sleep(500);

            while (!DataModel.Nxt.Program.StartsWith("MotorControl"))
            {
                DataModel.Nxt.CommLink.StartProgram("MotorControl22.rxe");
                System.Threading.Thread.Sleep(500);
            }

            DataModel.Left = new McNxtMotor();
            DataModel.Right = new McNxtMotor();

            DataModel.Nxt.CommLink.ResetMotorPosition(NxtMotorPort.PortC, false);
            DataModel.Nxt.CommLink.ResetMotorPosition(NxtMotorPort.PortA, false);

            DataModel.Bumper1 = new NxtTouchSensor();

            DataModel.Imu = new DiIMU();

            DataModel.Nxt.MotorC = DataModel.Left;
            DataModel.Nxt.MotorA = DataModel.Right;
            DataModel.Nxt.Sensor1 = DataModel.Bumper1;
            DataModel.Nxt.Sensor4 = DataModel.Imu;

            DataModel.MotorPair = new McNxtMotorSync(DataModel.Right, DataModel.Left);

            DataModel.Left.PollInterval =
                DataModel.Right.PollInterval =
                DataModel.Bumper1.PollInterval =
                DataModel.Imu.PollInterval =
                1000 / 20;

            DataModel.Bumper1.OnPressed += Bumper1_OnChanged;
            DataModel.Bumper1.OnReleased += Bumper1_OnChanged;

            DataModel.Left.OnPolled += (s) =>
            {
                Dispatcher.Invoke(() => DataModel.CurrentLeftTacho = DataModel.Left.TachoCount.Value);
            };

            DataModel.Right.OnPolled += (s) =>
            {
                Dispatcher.Invoke(() => DataModel.CurrentRightTacho = DataModel.Right.TachoCount.Value);
            };

            DataModel.Imu.OnPolled += Imu_OnPolled;

            DataModel.Nxt.InitSensors();    // reqd by motor control

            DataModel.State = RobotState.Initialized;
        }

        private void Imu_OnPolled(NxtPollable polledItem)
        {
            Trace.WriteLine(string.Format("gyroZ {0:F3}", DataModel.Imu.GyroZ));
        }
    }
}