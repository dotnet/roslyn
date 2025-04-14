// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.FileBasedPrograms;

internal readonly record struct SourceFile(string Path, SourceText Text)
{
    public string GetLocationString(TextSpan span)
    {
        var positionSpan = new FileLinePositionSpan(Path, Text.Lines.GetLinePositionSpan(span));
        return $"{positionSpan.Path}:{positionSpan.StartLinePosition.Line + 1}";
    }
}
