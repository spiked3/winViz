// RoboNUC ©2014 Mike Partain
// This file is NOT open source
//
// RnMaster :: RpLidarLib :: Class1.cs
//
// /* ----------------------------------------------------------------------------------- */

#region Usings

using System;
using System.Runtime.InteropServices;
using System.Windows;

#endregion Usings

namespace RpLidarLib
{
    // todo common headers

    public enum LidarCommand : byte
    {
        Stop = 0x25,
        Reset = 0x40,
        Scan = 0x20,
        ForceScan = 0x21,
        GetInfo = 0x50,
        GetHealth = 0x52
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LidarScanData
    {
        public byte Quality;
        public UInt16 Angle;
        public UInt16 Distance;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LidarScanResponse
    {
        public byte Header1;
        public byte Header2;
        public Int32 Size;
        public byte DataType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LidarDevInfoResponse
    {
        public byte Header1;
        public byte Header2;
        public Int32 Size; // first 30 bits, last 2 bits are send mode
        public byte DataType;
        public byte Model; // first 6 bits, last 2 bits are scan indicators
        public byte FirmwareMinor;
        public byte FirmwareMajor;
        public byte hardware;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] SerialNum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LidarHealthResponse
    {
        public byte Header1;
        public byte Header2;
        public Int32 Size; // first 30 bits, last 2 bits are send mode
        public byte DataType;
        public byte Status;
        public Int16 ErrorCode;
    }

    public class ScanPoint
    {
        public double Angle { get; set; }

        public double Distance { get; set; }

        public int Quality { get; set; }

        public DateTime TimeOfDeath { get; set; }
    }

    public class Feature
    {
        public Feature(Point position)
        {
            Position = position;
        }

        private Feature()
        {
            /* for serializer */
        }

        public Point Position { get; set; }
    }

    public class Landmark
    {
        public Point Position;

        public Landmark(Point position)
        {
            Position = position;
        }

        public Landmark()
        {
            /* for serializer */
        }
    }

    public static class Extensions
    {
        public static T ByteArrayToStructure<T>(this byte[] bytes, int offset) where T : struct
        {
            GCHandle h = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T s = (T)Marshal.PtrToStructure(h.AddrOfPinnedObject() + offset, typeof(T));
            h.Free();
            return s;
        }
    }
}