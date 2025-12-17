// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal readonly struct FSharpInlineRenameLocation
{
    public Document Document { get; }
    public TextSpan TextSpan { get; }

    public FSharpInlineRenameLocation(Document document, TextSpan textSpan)
    {
        this.Document = document;
        this.TextSpan = textSpan;
    }
}
