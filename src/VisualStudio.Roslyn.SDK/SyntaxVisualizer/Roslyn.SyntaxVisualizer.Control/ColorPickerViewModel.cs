using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    internal class ColorPickerViewModel : INotifyPropertyChanged
    {
        private UpdateBehavior _updateBehavior = UpdateBehavior.None;
        public ColorPickerViewModel()
        {
        }

        private Color _originalColor;
        public Color OriginalColor
        {
            get => _originalColor;
            set => SetProperty(ref _originalColor, value);
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                if (!SetProperty(ref _color, value))
                {
                    return;
                }

                if (_updateBehavior != UpdateBehavior.FromComponent)
                {
                    UpdateColorComponents();
                }
            }
        }

        private double _hue;
        public double Hue
        {
            get => _hue;
            set
            {
                if (!SetProperty(ref _hue, value))
                {
                    return;
                }

                if (_updateBehavior == UpdateBehavior.None)
                {
                    UpdateColorFromHSB();
                }
            }
        }

        private double _saturation;
        public double Saturation
        {
            get => _saturation;
            set
            {
                if (!SetProperty(ref _saturation, value))
                {
                    return;
                }

                if (_updateBehavior == UpdateBehavior.None)
                {
                    UpdateColorFromHSB();
                }
            }
        }

        private double _brightness;
        public double Brightness
        {
            get => _brightness;
            set
            {
                if (!SetProperty(ref _brightness, value))
                {
                    return;
                }

                if (_updateBehavior == UpdateBehavior.None)
                {
                    UpdateColorFromHSB();
                }
            }
        }

        private byte _red;
        public byte Red
        {
            get => _red;
            set
            {
                if (!SetProperty(ref _red, value))
                {
                    return;
                }

                if (_updateBehavior == UpdateBehavior.None)
                {
                    UpdateColorFromRGB();
                }
            }
        }

        private byte _green;
        public byte Green
        {
            get => _green;
            set
            {
                if (!SetProperty(ref _green, value))
                {
                    return;
                }

                if (_updateBehavior == UpdateBehavior.None)
                {
                    UpdateColorFromRGB();
                }
            }
        }

        private byte _blue;
        public byte Blue
        {
            get => _blue;
            set
            {
                if (!SetProperty(ref _blue, value))
                {
                    return;
                }

                if (_updateBehavior == UpdateBehavior.None)
                {
                    UpdateColorFromRGB();
                }
            }
        }

        private void UpdateColorFromRGB()
        {
            _updateBehavior = UpdateBehavior.FromComponent;

            Color = Color.FromRgb(Red, Green, Blue);
            Hue = ColorHelpers.GetHue(Color);
            Saturation = ColorHelpers.GetSaturation(Color);
            Brightness = ColorHelpers.GetSaturation(Color);

            _updateBehavior = UpdateBehavior.None;
        }

        private void UpdateColorFromHSB()
        {
            _updateBehavior = UpdateBehavior.FromComponent;

            Color = ColorHelpers.HSVToColor(Hue, Saturation, Brightness);
            Red = Color.R;
            Green = Color.G;
            Blue = Color.B;

            _updateBehavior = UpdateBehavior.None;
        }

        private void UpdateColorComponents()
        {
            _updateBehavior = UpdateBehavior.FromColor;

            Red = Color.R;
            Green = Color.G;
            Blue = Color.B;
            Hue = ColorHelpers.GetHue(Color);
            Saturation = ColorHelpers.GetSaturation(Color);
            Brightness = ColorHelpers.GetSaturation(Color);

            _updateBehavior = UpdateBehavior.None;
        }

        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        private enum UpdateBehavior
        {
            None,
            
            /// <summary>
            /// Update is triggered from the Color property being updated
            /// </summary>
            FromColor,

            /// <summary>
            /// Update is triggered from a component of Color, such as Hue
            /// </summary>
            FromComponent
        }
    }
}
