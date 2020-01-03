// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class OptionChangedEventArgs : EventArgs
    {
        public IOption Option { get; }
        public string? Language { get; }
        public object? Value { get; }

        internal OptionChangedEventArgs(OptionKey optionKey, object? value)
        {
            Option = optionKey.Option;
            Language = optionKey.Language;
            Value = value;
        }
    }
}
