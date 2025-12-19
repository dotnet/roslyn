// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal readonly struct FSharpInlineRenameReplacement
{
    public FSharpInlineRenameReplacementKind Kind { get; }
    public TextSpan OriginalSpan { get; }
    public TextSpan NewSpan { get; }

    public FSharpInlineRenameReplacement(FSharpInlineRenameReplacementKind kind, TextSpan originalSpan, TextSpan newSpan)
    {
        this.Kind = kind;
        this.OriginalSpan = originalSpan;
        this.NewSpan = newSpan;
    }
}
