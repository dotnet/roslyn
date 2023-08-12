// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit;

[DebuggerDisplay("{Name, nq}")]
internal readonly struct AnonymousTypeValue(string name, int uniqueIndex, Cci.ITypeDefinition type)
{
    public readonly string Name = name;
    public readonly int UniqueIndex = uniqueIndex;
    public readonly Cci.ITypeDefinition Type = type;
}
