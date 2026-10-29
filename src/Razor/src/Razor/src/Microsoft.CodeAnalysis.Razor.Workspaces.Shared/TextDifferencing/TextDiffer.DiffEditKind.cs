// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal abstract partial class TextDiffer
{
    protected enum DiffEditKind : byte
    {
        Insert,
        Delete,
    }
}
