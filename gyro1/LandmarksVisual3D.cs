using HelixToolkit.Wpf;
using RpLidarLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace spiked3.winViz
{
    public class LandmarksVisual3D : ModelVisual3D
    {
        private static readonly Geometry3D DefaultGeometry = GetDefaultGeometry();

        public static readonly DependencyProperty LandmarksProperty =
            DependencyProperty.Register("Landmarks", typeof(List<Landmark>), typeof(LandmarksVisual3D), new PropertyMetadata(LandmarksChanged));

        public List<Landmark> Landmarks
        {
            get { return (List<Landmark>)GetValue(LandmarksProperty); }
            set { SetValue(LandmarksProperty, value); }
        }

        public LandmarksVisual3D()
        {
            UpdateLandmarks();
        }

        private static void LandmarksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LandmarksVisual3D)d).UpdateLandmarks();
        }

        private static Geometry3D GetDefaultGeometry()
        {
            var mb = new MeshBuilder(false, false);
            mb.AddPipe(new Point3D(0, 0, 0), new Point3D(0, 0, 2), 0, 2, 12);
            return mb.ToMesh();
        }

        public void UpdateLandmarks()
        {
            Material material = MaterialHelper.CreateMaterial(Brushes.Green);

            if (Landmarks == null || Landmarks.Count < 1)
                Content = new GeometryModel3D(DefaultGeometry, material);
            else
            {
                var group = new Model3DGroup();
                for (int i = 0; i < Landmarks.Count; i++)
                {
                    var tg = new Transform3DGroup();
                    tg.Children.Add(new TranslateTransform3D(Landmarks[i].Position.X/100, Landmarks[i].Position.Y/100, 0));
                    group.Children.Add(new GeometryModel3D(DefaultGeometry, material) { Transform = tg });
                }
                Content = group;
            }
        }
    }
}