using NKH.MindSqualls;
using System.Threading;

namespace gyro1
{
    public class DiIMU : NxtDigitalSensor
    {
        const byte DeviceAddr = 0xD2;

        const int DIMU_GYRO_CTRL_REG1 = 0x20;
        const int DIMU_GYRO_CTRL_REG2 = 0x21;
        const int DIMU_GYRO_CTRL_REG3 = 0x22;
        const int DIMU_GYRO_CTRL_REG4 = 0x23;
        const int DIMU_GYRO_CTRL_REG5 = 0x24;

        const bool lpfEnable = false;

        float[] divisors = { 114.28571F, 57.142857F, 14.285714F };
        byte[] rangeCommands = { 0x00, 0x10, 0x30 };
        float[] ranges = { 250, 500, 2000 };

        int range = 0;

        private object pollDataLock = new object();

        double _GyroZ = 0.0;

        public DiIMU() : base()
        {
            CommandToAddress(DIMU_GYRO_CTRL_REG2, 0x00);   // no high pass filter
            CommandToAddress(DIMU_GYRO_CTRL_REG3, 0x08);   // no interrupts, date ready
            CommandToAddress(DIMU_GYRO_CTRL_REG4, (byte)((0x01 << range) + 0x80));   // selected range +++ ???
            CommandToAddress(DIMU_GYRO_CTRL_REG5, lpfEnable ? 0x02 : 0x00);   // filtering - low pass
            CommandToAddress(DIMU_GYRO_CTRL_REG1, 0x04);   // Enable Z, Disable power down.
        }

        public double GyroZ
        {
            get
            {
                return _GyroZ;
            }
        }

        public override void Poll()
        {
            if (Brick.IsConnected)
            {
                lock (pollDataLock)
                {
                    byte? lb = ReadByteFromAddress(0x2C + 0x80);
                    byte? hb = ReadByteFromAddress(0x2D + 0x80);
                    if (lb.HasValue && hb.HasValue)
                        _GyroZ = (hb.Value << 8 + lb.Value) / divisors[range];
                }
            }
        }

        internal byte[] SendN(byte[] request, byte rxDataLength)
        {
            Brick.CommLink.LsWrite(sensorPort, request, rxDataLength);

            if (rxDataLength == 0) 
                return null;

            byte? bytesReady = 0;
            do
            {
                try
                {
                    Thread.Sleep(10);
                    bytesReady = Brick.CommLink.LsGetStatus(sensorPort);
                }
                catch (NxtCommunicationProtocolException ex)
                {
                    if (ex.errorMessage == NxtErrorMessage.PendingCommunicationTransactionInProgress)
                    {
                        bytesReady = 0;
                        Thread.Sleep(10);
                        continue;
                    }

                    if (ex.errorMessage != NxtErrorMessage.CommunicationBusError) 
                        throw;

                    DoAnyLsWrite();

                    return null;
                }
            }
            while (bytesReady < rxDataLength);

            return Brick.CommLink.LsRead(sensorPort);
        }

        byte[] Send1(byte[] request)
        {
            Brick.CommLink.LsWrite(sensorPort, request, 1);

            while (true)
            {
                LsReadDelay();
                try
                {
                    return Brick.CommLink.LsRead(sensorPort);
                }
                catch (NxtCommunicationProtocolException ex)
                {
                    if (ex.errorMessage == NxtErrorMessage.PendingCommunicationTransactionInProgress) 
                        continue;

                    if (ex.errorMessage == NxtErrorMessage.CommunicationBusError)
                    {
                        Brick.CommLink.LsWrite(sensorPort, request, 1); // Doing a LsWrite() clears the CommunicationBusError from the bus.
                        continue;
                    }
                    throw;
                }
            }
        }        

        void LsReadDelay()
        {
            if (this.Brick.CommLink is NxtBluetoothConnection)
            {
                Thread.Sleep(lsReadDelayTimeMs);
            }
        }

        void DoAnyLsWrite()
        {
            ReadByteFromAddress(0x42);
        }

        byte? ReadByteFromAddress(byte address)
        {
            byte[] request = new byte[] { DeviceAddr, address };

            byte[] reply = (this.Brick.CommLink is NxtBluetoothConnection)
                ? Send1(request)
                : SendN(request, 1);

            if (reply != null && reply.Length >= 1)
                return reply[0];
            else
                return null;
        }

        void CommandToAddress(byte address, byte command)
        {
            byte[] request = new byte[] { DeviceAddr, address, command };
            SendN(request, 0);
        }
    }
}

 