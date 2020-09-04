using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    /// <summary>
    /// Interaction logic for SaturationBrightnessPicker.xaml
    /// </summary>
    public partial class SaturationBrightnessPicker : UserControl
    {
        public static readonly DependencyProperty HueProperty
            = DependencyProperty.Register(
                nameof(Hue),
                typeof(double),
                typeof(SaturationBrightnessPicker),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty SaturationProperty
            = DependencyProperty.Register(
                nameof(Saturation), 
                typeof(double), 
                typeof(SaturationBrightnessPicker), 
                new PropertyMetadata(0.0, OnSaturationChanged));

        public static readonly DependencyProperty BrightnessProperty
            = DependencyProperty.Register(
                nameof(Brightness), 
                typeof(double), 
                typeof(SaturationBrightnessPicker), 
                new PropertyMetadata(0.0, OnBrightnessChanged));

        private readonly SaturationBrightnessPickerAdorner _adorner;

        private bool _inMouseUpdate = false;

        public SaturationBrightnessPicker()
        {
            InitializeComponent();
            _adorner = new SaturationBrightnessPickerAdorner(this);
            Loaded += SaturationBrightnessPickerOnLoaded;
        }

        public double Hue
        {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        public double Saturation
        {
            get => (double)GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        public double Brightness
        {
            get => (double)GetValue(BrightnessProperty);
            set => SetValue(BrightnessProperty, value);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Mouse.Capture(this);
            var pos = e.GetPosition(this).Clamp(this);
            Update(pos);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            Mouse.Capture(null);
            var pos = e.GetPosition(this).Clamp(this);
            Update(pos);
        }

        private static void OnSaturationChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var picker = (SaturationBrightnessPicker)o;

            if (picker._inMouseUpdate)
            {
                return;
            }

            var sat = (double)e.NewValue;
            var pos = picker._adorner.Position;
            picker._adorner.Position = new Point(sat * picker.ActualWidth, pos.Y);
        }

        private static void OnBrightnessChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var picker = (SaturationBrightnessPicker)o;

            if (picker._inMouseUpdate)
            {
                return;
            }

            var bright = (double)e.NewValue;
            var pos = picker._adorner.Position;
            picker._adorner.Position = new Point(pos.X, (1 - bright) * picker.ActualHeight);
        }

        private void SaturationBrightnessPickerOnLoaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer.GetAdornerLayer(this).Add(_adorner);
            _adorner.Position = new Point(Saturation * ActualWidth, (1 - Brightness) * ActualHeight);
        }

        private void Update(Point p)
        {
            _inMouseUpdate = true;

            _adorner.Position = p;
            Saturation = p.X / ActualWidth;
            Brightness = 1 - (p.Y / ActualHeight); // directions reversed

            _inMouseUpdate = false;
        }

        internal class SaturationBrightnessPickerAdorner : Adorner
        {
            private static readonly DependencyProperty PositionProperty
                = DependencyProperty.Register(nameof(Position), typeof(Point), typeof(SaturationBrightnessPickerAdorner), new FrameworkPropertyMetadata(new Point(), FrameworkPropertyMetadataOptions.AffectsRender));
            private static readonly Brush FillBrush = Brushes.Transparent;
            private static readonly Pen InnerRingPen = new Pen(Brushes.White, 2);
            private static readonly Pen OuterRingPen = new Pen(Brushes.Black, 2);

            internal SaturationBrightnessPickerAdorner(UIElement adornedElement)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
            }

            internal Point Position
            {
                get => (Point)GetValue(PositionProperty);
                set => SetValue(PositionProperty, value);
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                drawingContext.DrawEllipse(FillBrush, InnerRingPen, Position, 4, 4);
                drawingContext.DrawEllipse(FillBrush, OuterRingPen, Position, 6, 6);
            }
        }
    }
}
