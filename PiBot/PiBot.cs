using HelixToolkit.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using spiked3;
using spiked3.winRobotLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace PiBot
{
    public class PiBot : RobotBase
    {
        public override void Initialize(RibbonWindow ApplicationWindow)
        {
            // +++attach my ribbon group and add robot to scene
            throw new NotImplementedException();
        }

        public void Initialize(RibbonWindow ApplicationWindow, string filename)
        {
            Initialize(ApplicationWindow);
            LoadModel(filename);
        }
    }
}

namespace spiked3.winRobotLib
{
    public class RobotBase : IWinRobot
    {
        static Vector3D zAxis = new Vector3D(0, 0, 1);

        public static Geometry3D LoadModel(string filename)
        {
            GeometryModel3D gm3d = new GeometryModel3D();
            MeshBuilder mb = new MeshBuilder(false, false);
            var mi = new HelixToolkit.Wpf.ModelImporter();
            var g = mi.Load(filename);
            foreach (var m in g.Children)
            {
                var mGeo = m as GeometryModel3D;
                var mesh = mGeo.Geometry as MeshGeometry3D;
                if (mesh != null)
                    mb.Append(mesh);
            }

            gm3d.Geometry = mb.ToMesh();
            var xg = new Transform3DGroup();
            xg.Children.Add(new ScaleTransform3D(.01, .01, .01));
            xg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(zAxis, -90)));
            gm3d.Transform = xg;
            return gm3d.Geometry.Clone();  // permanently apply transform
        }

        public static Geometry3D OpenModel(Window owner)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "STL Files|*.stl|All Files|*.*", DefaultExt = "stl" };
            if (d.ShowDialog(owner) ?? false)
            {
                return LoadModel(d.FileName);
            }
            return null;
        }

        public virtual void Initialize(RibbonWindow ApplicationWindow)
        {
            // +++default white sphere robot
            throw new NotImplementedException();
        }
    }

    public interface IWinRobot
    {
        void Initialize(RibbonWindow ApplicationWindow);
    }
}