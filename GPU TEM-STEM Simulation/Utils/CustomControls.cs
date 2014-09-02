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
using System.Windows.Media.Animation;
using GPU_TEM_STEM_Simulation;

namespace GPUTEMSTEMSimulation
{
    public partial class resourceDictionary
    {
        public resourceDictionary()
        {
            InitializeComponent();
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }
    }
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

namespace ErrTextBox
{
    public partial class ErrTextBox : TextBox
    {
        public static readonly RoutedEvent TapEvent = EventManager.RegisterRoutedEvent(
        "Tap", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ErrTextBox));

        Storyboard story;

        public ErrTextBox()
        {
            this.Style = new Style(GetType(), this.FindResource(typeof(System.Windows.Controls.TextBox)) as Style);
            InitializeComponent();
            setupAnimation();
        }

        public void InitializeComponent()
        { }

        void setupAnimation()
        {
            this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF171717"));

            var tempBrush = this.Background;

            NameScope.SetNameScope(this, new NameScope());

            string brushStr = "CustomBrush";
            this.RegisterName(brushStr, tempBrush);

            ColorAnimation toRed = new ColorAnimation();
            toRed.To = (Color)ColorConverter.ConvertFromString("#FFFE8432");
            toRed.BeginTime = TimeSpan.FromMilliseconds(1);
            toRed.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            toRed.AutoReverse = false;

            ColorAnimation toWhite = new ColorAnimation();
            toWhite.To = (Color)ColorConverter.ConvertFromString("#FF171717");
            toWhite.BeginTime = TimeSpan.FromMilliseconds(1000);
            toWhite.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            toWhite.AutoReverse = false;

            story = new Storyboard();
            story.Children.Add(toRed);
            story.Children.Add(toWhite);
            Storyboard.SetTargetName(toRed, brushStr);
            Storyboard.SetTargetName(toWhite, brushStr);
            Storyboard.SetTargetProperty(toRed, new PropertyPath(SolidColorBrush.ColorProperty));
            Storyboard.SetTargetProperty(toWhite, new PropertyPath(SolidColorBrush.ColorProperty));
        }

        public event RoutedEventHandler Tap
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        public void RaiseTapEvent()
        {
            story.Begin(this);

        }



    }
}