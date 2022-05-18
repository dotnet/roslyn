// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum DeclarationScope : byte
    {
        None = 0x0,
        RefScoped = 0x1,
        ValueScoped = 0x2,
    }
}
