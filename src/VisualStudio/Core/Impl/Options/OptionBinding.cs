// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class OptionBinding<T> : INotifyPropertyChanged
    {
        private readonly OptionStore _optionStore;
        private readonly Option2<T> _option;

        public event PropertyChangedEventHandler PropertyChanged;

        public OptionBinding(OptionStore optionStore, Option2<T> option)
        {
            _optionStore = optionStore;
            _option = option;

            _optionStore.OptionChanged += (sender, e) =>
            {
                if (e.Option == _option)
                {
                    PropertyChanged?.Raise(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            };
        }

        public T Value
        {
            get
            {
                return _optionStore.GetOption<T>(_option);
            }

            set
            {
                _optionStore.SetOption(_option, value);
            }
        }
    }
}
