// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.TypeRename;

internal class XamlTypeRenameResult(ImmutableArray<TextSpan> ranges, string? wordPattern)
{
    public ImmutableArray<TextSpan> Ranges { get; } = ranges;
    public string? WordPattern { get; } = wordPattern;
}
