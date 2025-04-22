// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly struct DocumentSpan
{
    public Document Document { get; }
    public TextSpan TextSpan { get; }

    public DocumentSpan(Document document, TextSpan textSpan) : this()
    {
        this.Document = document;
        this.TextSpan = textSpan;
    }

    private string GetDebuggerDisplay()
    {
        return $"{Document.Name} [{TextSpan.Start}...{TextSpan.End}]";
    }
}
