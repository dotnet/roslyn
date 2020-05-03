// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class OptionBinding<T> : INotifyPropertyChanged
    {
        private readonly OptionStore _optionStore;
        private readonly Option2<T> _key;

        public event PropertyChangedEventHandler PropertyChanged;

        public OptionBinding(OptionStore optionStore, Option2<T> key)
        {
            _optionStore = optionStore;
            _key = key;

            _optionStore.OptionChanged += (sender, e) =>
            {
                if (e.Option == _key)
                {
                    PropertyChanged?.Raise(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            };
        }

        public T Value
        {
            get
            {
                return _optionStore.GetOption(_key);
            }

            set
            {
                _optionStore.SetOption(_key, value);
            }
        }
    }
}
