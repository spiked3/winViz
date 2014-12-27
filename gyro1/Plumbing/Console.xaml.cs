using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace gyro1
{
    public partial class Console : UserControl
    {
        public Console()
        {
            InitializeComponent();
            new TraceDecorator(listBox1);
        }

        class TraceDecorator : TraceListener
        {
            ListBox ListBox;
            public TraceDecorator(ListBox listBox)
            {
                ListBox = listBox;
                System.Diagnostics.Trace.Listeners.Add(this);
            }

            public override void WriteLine(string text)
            {
                if (ListBox == null)
                    return;

                ListBox.Dispatcher.InvokeAsync(() =>
                {
                    TextBlock t = new TextBlock();
                    t.Text = text;
                    t.Foreground = ListBox.Foreground;
                    int i = ListBox.Items.Add(t);
                    var sv = ListBox.TryFindParent<ScrollViewer>();
                    if (sv != null)
                        sv.ScrollToBottom();  //  +++  not doing it
                });
            }

            public override void Write(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}
