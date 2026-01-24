// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests;

internal static class Options
{
    internal static readonly CSharpParseOptions Script = new(kind: SourceCodeKind.Script);
    internal static readonly CSharpParseOptions Regular = new(kind: SourceCodeKind.Regular);
}
