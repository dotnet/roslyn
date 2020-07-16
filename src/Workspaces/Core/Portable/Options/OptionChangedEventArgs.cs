// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
