// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class PerLanguageOptionBinding<T> : INotifyPropertyChanged
    {
        private readonly OptionStore _optionStore;
        private readonly PerLanguageOption2<T> _option;
        private readonly string _languageName;

        public event PropertyChangedEventHandler PropertyChanged;

        public PerLanguageOptionBinding(OptionStore optionStore, PerLanguageOption2<T> option, string languageName)
        {
            _optionStore = optionStore;
            _option = option;
            _languageName = languageName;

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
                return _optionStore.GetOption<T>(_option, _languageName);
            }

            set
            {
                _optionStore.SetOption(_option, _languageName, value);
            }
        }
    }
}
