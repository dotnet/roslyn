// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Structure;

internal class XamlStructureTag(string? type, TextSpan textSpan)
{
    public string? Type { get; } = type;
    public TextSpan TextSpan { get; } = textSpan;
}
