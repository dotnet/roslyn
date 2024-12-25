// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options;

internal sealed class OptionChangedEventArgs(ImmutableArray<(OptionKey2 key, object? newValue)> changedOptions) : EventArgs
{
    public ImmutableArray<(OptionKey2 key, object? newValue)> ChangedOptions => changedOptions;

    public bool HasOption(Func<IOption2, bool> predicate)
        => changedOptions.Any(static (option, predicate) => predicate(option.key.Option), predicate);
}
