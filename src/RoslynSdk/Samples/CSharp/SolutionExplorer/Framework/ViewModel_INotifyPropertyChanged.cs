using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSBuildWorkspaceTester.Framework
{
    internal abstract partial class ViewModel : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler _propertyChangedHandler;

        protected void PropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs eventArgs = PropertyChangedEventArgsCache.GetEventArgs(propertyName);
            _propertyChangedHandler?.Invoke(this, eventArgs);
        }

        protected void AllPropertiesChanged()
        {
            PropertyChanged(string.Empty);
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChangedHandler += value; }
            remove { _propertyChangedHandler -= value; }
        }

        protected void SetValue<T>(ref T value, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                value = newValue;
                PropertyChanged(propertyName);
            }
        }
    }
}
