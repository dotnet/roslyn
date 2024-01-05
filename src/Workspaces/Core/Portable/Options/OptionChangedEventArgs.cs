// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class OptionChangedEventArgs : EventArgs
    {
        public OptionKey2 OptionKey { get; }
        public object? Value { get; }

        internal OptionChangedEventArgs(OptionKey2 optionKey, object? value)
        {
            OptionKey = optionKey;
            Value = value;
        }

        public IOption2 Option => OptionKey.Option;
        public string? Language => OptionKey.Language;
    }
}
