// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal static class TextChangeExtensions
{
    public static ITextChange ToVisualStudioTextChange(this RazorTextChange razorTextChange)
        => new VisualStudioTextChange(razorTextChange.Span.Start, razorTextChange.Span.Length, razorTextChange.NewText!);
}
