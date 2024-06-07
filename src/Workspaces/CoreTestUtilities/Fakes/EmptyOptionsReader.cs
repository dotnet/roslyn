// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UnitTests;

internal class EmptyOptionsReader : IOptionsReader
{
    public static readonly EmptyOptionsReader Instance = new();

    public bool TryGetOption<T>(OptionKey2 optionKey, out T value)
    {
        value = default!;
        return false;
    }
}
