using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    /// <summary>
    /// Interaction logic for HuePicker.xaml
    /// </summary>
    public partial class HuePicker : UserControl
    {
        public static readonly DependencyProperty HueProperty
            = DependencyProperty.Register(
                nameof(Hue),
                typeof(double),
                typeof(HuePicker), 
                new PropertyMetadata(0.0, OnHueChanged));


        private readonly HuePickerAdorner _adorner;
        private bool _isMouseUpdate = false;

        public HuePicker()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            _adorner = new HuePickerAdorner(this);
        }

        public double Hue
        {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        private static void OnHueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var huePicker = (HuePicker)o;
            Debug.WriteLine($"OnHueChanged: {e.NewValue}");
            if (huePicker._isMouseUpdate)
            {
                return;
            }

            var hue = (double)e.NewValue;
            var percent = hue / 360;

            huePicker.UpdateAdornment(ColorHelpers.HSVToColor(hue, 1, 1), percent);
        }

        private void UpdateAdornment(Color color, double percent)
        {
            _adorner.VerticalPercent = percent;
            _adorner.Color = color;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Mouse.Capture(this);

            Update(e.GetPosition(this).Clamp(this));
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            Mouse.Capture(null);
            Update(e.GetPosition(this).Clamp(this));
        }

        private void Update(Point mousePos)
        {
            _isMouseUpdate = true;

            var verticalPercent = mousePos.Y / ActualHeight;
            var color = Gradients.GradientStops.GetColorAtOffset(verticalPercent);
            UpdateAdornment(color, verticalPercent);

            var hue = ColorHelpers.GetHue(color);
            Debug.WriteLine($"Updating hue to {hue}");
            Hue = hue;

            _isMouseUpdate = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _adorner.ElementSize = new Rect(new Size(ActualWidth, ActualHeight));
            AdornerLayer.GetAdornerLayer(this).Add(_adorner);
        }

        internal class HuePickerAdorner : Adorner
        {
            private static readonly DependencyProperty VerticalPercentProperty
                = DependencyProperty.Register(nameof(VerticalPercent), typeof(double), typeof(HuePickerAdorner), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
            private static readonly DependencyProperty ColorProperty
                = DependencyProperty.Register(nameof(Color), typeof(Color), typeof(HuePickerAdorner), new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender));
            private static readonly Pen Pen = new Pen(Brushes.Black, 1);
            private Brush _brush = Brushes.Red;

            public HuePickerAdorner(UIElement adornedElement)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
            }

            public double VerticalPercent
            {
                get => (double)GetValue(VerticalPercentProperty);
                set => SetValue(VerticalPercentProperty, value);
            }

            public Color Color
            {
                get => (Color)GetValue(ColorProperty);
                set
                {
                    SetValue(ColorProperty, value);
                    _brush = new SolidColorBrush(value);
                }
            }

            public Rect ElementSize { get; set; }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);
                var width = 5;
                var y = ElementSize.Height * VerticalPercent;
                var x = 5;

                var triangleGeometry = new StreamGeometry();
                using (var context = triangleGeometry.Open())
                {
                    context.BeginFigure(new Point(x, y + width / 2), true, true);
                    context.LineTo(new Point(x + width, y), true, false);
                    context.LineTo(new Point(x, y - width / 2), true, false);
                }

                var rightTri = triangleGeometry.Clone();
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(-1, 1));
                transformGroup.Children.Add(new TranslateTransform(ElementSize.Width, 0));
                rightTri.Transform = transformGroup;


                drawingContext.DrawGeometry(_brush, Pen, triangleGeometry);
                drawingContext.DrawGeometry(_brush, Pen, rightTri);
            }
        }
    }
}
