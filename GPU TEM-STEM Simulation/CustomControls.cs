﻿using System;
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
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            SolidColorBrush tempBrush = new SolidColorBrush();

            this.Background = tempBrush;
            NameScope.SetNameScope(this, new NameScope());

            string brushStr = "CustomBrush";
            this.RegisterName(brushStr, tempBrush);

            ColorAnimation toRed = new ColorAnimation();
            toRed.To = Colors.Red;
            toRed.BeginTime = TimeSpan.Zero;
            toRed.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            toRed.AutoReverse = false;

            ColorAnimation toWhite = new ColorAnimation();
            toWhite.To = Colors.White;
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
            //RoutedEventArgs newEventArgs = new RoutedEventArgs(ErrTextBox.TapEvent);
            //RaiseEvent(newEventArgs);

            story.Begin(this);

        }



    }
}