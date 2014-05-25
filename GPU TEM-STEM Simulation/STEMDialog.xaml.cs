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
using System.Windows.Shapes;
using System.Globalization;

namespace GPUTEMSTEMSimulation
{
    /// <summary>
    /// Interaction logic for STEMDialog.xaml
    /// </summary>

    public partial class STEMDialog : Window
    {
        public event EventHandler<DetectorArgs> AddDetectorEvent;
        public event EventHandler<DetectorArgs> RemDetectorEvent;

        public STEMDialog(List<DetectorItem> MainDet)
        {
            InitializeComponent();
            //DetectorListGrid.Columns[DetectorListGrid.Columns.Count - 1].Width = 100;
            foreach (DetectorItem i in MainDet)
            {
                DetectorListView.Items.Add(i);
            }
            ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Hidden);

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            bool valid = true;

            string Sname = NameTxtbx.Text;
            string Sin = InnerTxtbx.Text;
            string Sout = OuterTxtbx.Text;

            float Fin = 0;
            float Fout = 0;

            if (Sname.Length == 0)
            {
                NameTxtbx.RaiseTapEvent();
                valid = false;
            }

            Array ListItems = DetectorListView.Items.Cast<Object>().ToArray();
            foreach (DetectorItem i in ListItems)
            {
                if (i.Name.Equals(Sname))
                {
                    valid = false;
                    NameTxtbx.RaiseTapEvent();
                    break;
                }
            }

            Fout = Convert.ToSingle(Sout);
            Fin = Convert.ToSingle(Sin);

            if (Fin >= Fout)
            {
                InnerTxtbx.RaiseTapEvent();
                OuterTxtbx.RaiseTapEvent();
                valid = false;
            }

            if (!valid)
            {
                return;
            }

            TabItem tempTab = new TabItem();
            tempTab.Header = Sname;

            DetectorItem temp = new DetectorItem { Name = Sname, Inner = Fin, Outer = Fout, Tab = tempTab};

            // modify the mainWindow List
            AddDetectorEvent(this, new DetectorArgs(temp));

            DetectorListView.Items.Add(temp);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Array selected = DetectorListView.SelectedItems.Cast<Object>().ToArray();
            if (selected.Length > 0)
            {
                foreach (var item in selected) DetectorListView.Items.Remove(item);
                // modify list in mainwindow
                RemDetectorEvent(this, new DetectorArgs(selected));
            }
        }

        private void DetectorListView_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DetectorListView.Items.Count > 7) // bodged and hard coded
            {
                ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Visible);
            }
        }

        private void DetectorListView_MouseLeave(object sender, MouseEventArgs e)
        {
            ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Hidden);
            
        }

    }

    public class DetectorArgs : EventArgs
    {
        private DetectorItem msg;
        public DetectorArgs(DetectorItem s)
        {
            msg = s;
        }
        public DetectorItem Detector
        {
            get { return msg; }
        }

        private Array msgArr;
        public DetectorArgs(Array sArr)
        {
            msgArr = sArr;
        }
        public Array DetectorArr
        {
            get { return msgArr; }
        }
    }
}

public class DetectorItem
{
    public string Name { get; set; }

    public float Inner { get; set; }

    public float Outer { get; set; }

    public float[] Image { get; set; }

    public TabItem Tab { get; set; }
}

namespace FixedWidthColumnSample
{
    public class FixedWidthColumn : GridViewColumn
    {
        static FixedWidthColumn()
        {
            WidthProperty.OverrideMetadata(typeof(FixedWidthColumn),
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
        }

        public double FixedWidth
        {
            get { return (double)GetValue(FixedWidthProperty); }
            set { SetValue(FixedWidthProperty, value); }
        }

        public static readonly DependencyProperty FixedWidthProperty =
            DependencyProperty.Register(
                "FixedWidth",
                typeof(double),
                typeof(FixedWidthColumn),
                new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnFixedWidthChanged)));

        private static void OnFixedWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            FixedWidthColumn fwc = o as FixedWidthColumn;
            if (fwc != null)
                fwc.CoerceValue(WidthProperty);
        }

        private static object OnCoerceWidth(DependencyObject o, object baseValue)
        {
            FixedWidthColumn fwc = o as FixedWidthColumn;
            if (fwc != null)
                return fwc.FixedWidth;
            return baseValue;
        }
    }
}

namespace ErrTextBoxSample
{
    public class ErrTextBox : TextBox
    {
        public static readonly RoutedEvent TapEvent = EventManager.RegisterRoutedEvent(
        "Tap", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ErrTextBox));

        public event RoutedEventHandler Tap
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        public void RaiseTapEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ErrTextBox.TapEvent);
            RaiseEvent(newEventArgs);
        }
    }
}