// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class OptionBinding<T> : INotifyPropertyChanged
    {
        private readonly OptionStore _optionStore;
        private readonly Option<T> _key;

        public event PropertyChangedEventHandler PropertyChanged;

        public OptionBinding(OptionStore optionStore, Option<T> key)
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
