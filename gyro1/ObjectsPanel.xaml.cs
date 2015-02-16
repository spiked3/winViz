using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public partial class ObjectsPanel : UserControl, INotifyCollectionChanged
    {

        public ObservableCollection<object> ViewObjects { get { return _ViewObjects; } }
        ObservableCollection<object> _ViewObjects = new ObservableCollection<object>();

        public ObjectsPanel()
        {
            InitializeComponent();
        }

        public void Add(object o)
        {
            ViewObjects.Add(o);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o));
        }

        public void Remove(object o)
        {
            ViewObjects.Remove(o);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, o));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
