using NKH.MindSqualls;

namespace gyro1
{
    public class DiIMU : NxtDigitalSensor
    {
#if false
        public DiIMU()
            : base()
        {
        }

        public double? XAxisAcceleration()
        {
            return XyzAxisAcceleration(XAxisUpper8Bits());
        }

        public double? YAxisAcceleration()
        {
            return XyzAxisAcceleration(YAxisUpper8Bits());
        }

        public double? ZAxisAcceleration()
        {
            return XyzAxisAcceleration(ZAxisUpper8Bits());
        }

        public double scalíng = 9.82 / 200;

        private double? XyzAxisAcceleration(sbyte? xyz)
        {
            if (!xyz.HasValue) return null;

            int tmpXyz = xyz.Value;

            // Reverse the sign.
            tmpXyz *= -1;

            // Shift 2 bits up.
            tmpXyz = tmpXyz << 2;

            return scalíng * tmpXyz;
        }

        public sbyte? XAxisUpper8Bits()
        {
            byte? xAxisUpper8 = ReadByteFromAddress(0x42);
            if (xAxisUpper8.HasValue)
                return (sbyte)xAxisUpper8.Value;
            else
                return null;
        }

        public sbyte? YAxisUpper8Bits()
        {
            byte? yAxisUpper8 = ReadByteFromAddress(0x43);
            if (yAxisUpper8.HasValue)
                return (sbyte)yAxisUpper8.Value;
            else
                return null;
        }

        public sbyte? ZAxisUpper8Bits()
        {
            byte? zAxisUpper8 = ReadByteFromAddress(0x44);
            if (zAxisUpper8.HasValue)
                return (sbyte)zAxisUpper8.Value;
            else
                return null;
        }

        public byte? XAxisLower2Bits()
        {
            return ReadByteFromAddress(0x45);
        }

        public byte? YAxisLower2Bits()
        {
            return ReadByteFromAddress(0x46);
        }

        public byte? ZAxisLower2Bits()
        {
            return ReadByteFromAddress(0x47);
        }
#endif
    }
}