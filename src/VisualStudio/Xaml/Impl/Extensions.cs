// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Xaml;

internal static class Extensions
{
    public static string GetFilePath(this ITextView textView)
        => textView.TextBuffer.GetFilePath();

    public static string GetFilePath(this ITextBuffer textBuffer)
    {
        if (textBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDoc))
        {
            return textDoc.FilePath;
        }

        return string.Empty;
    }

    public static Project GetCodeProject(this TextDocument document)
    {
        if (document.Project.SupportsCompilation)
        {
            return document.Project;
        }

        // There has to be a match
        return document.Project.Solution.Projects.Single(p => p.SupportsCompilation && p.FilePath == document.Project.FilePath);
    }
}
