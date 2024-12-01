// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options;

public abstract partial class OptionSet
{
    private sealed class EmptyOptionSet : OptionSet
    {
        internal override object? GetInternalOptionValue(OptionKey optionKey)
            => optionKey.Option.DefaultValue;

        internal override OptionSet WithChangedOptionInternal(OptionKey optionKey, object? internalValue)
            => throw new NotSupportedException();
    }
}
