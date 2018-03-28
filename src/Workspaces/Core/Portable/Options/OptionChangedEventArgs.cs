// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class OptionChangedEventArgs : EventArgs
    {
        private readonly OptionKey _optionKey;
        private readonly object _value;

        internal OptionChangedEventArgs(OptionKey optionKey, object value)
        {
            _optionKey = optionKey;
            _value = value;
        }

        public IOption Option => _optionKey.Option;
        public string Language => _optionKey.Language;
        public object Value => _value;
    }
}
