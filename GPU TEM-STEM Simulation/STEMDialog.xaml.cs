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
        public STEMDialog()
        {
            InitializeComponent();
            //DetectorListGrid.Columns[DetectorListGrid.Columns.Count - 1].Width = 100;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string Sname = NameTxtbx.Text;
            string Sin = InnerTxtbx.Text;
            string Sout = OuterTxtbx.Text;

            float Fin, Fout;

            if (Sname.Length == 0)
            {
                NameTxtbx.RaiseTapEvent();
                return;
            }

            try
            {
                Fin = Convert.ToSingle(Sin);
                Fout = Convert.ToSingle(Sout);
            }
            catch (FormatException er)
            {
                return;
            }
            catch (OverflowException er)
            {
                return;
            }

            if (Fin >= Fout)
            {
                InnerTxtbx.RaiseTapEvent();
                OuterTxtbx.RaiseTapEvent();
                return;
            }

            DetectorItem tits = new DetectorItem { Name = Sname, Inner = Fin, Outer = Fout };

            DetectorList.Items.Add(tits);
        }

    }
}

public class DetectorItem
{
    public string Name { get; set; }

    public float Inner { get; set; }

    public float Outer { get; set; }
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