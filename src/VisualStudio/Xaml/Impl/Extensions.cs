// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Xaml;

internal static class Extensions
{
    extension(ITextView textView)
    {
        public string GetFilePath()
        => textView.TextBuffer.GetFilePath();
    }

    extension(ITextBuffer textBuffer)
    {
        public string GetFilePath()
        {
            if (textBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDoc))
            {
                return textDoc.FilePath;
            }

            return string.Empty;
        }
    }

    extension(TextDocument document)
    {
        public Project GetCodeProject()
        {
            if (document.Project.SupportsCompilation)
            {
                return document.Project;
            }

            // There has to be a match
            return document.Project.Solution.Projects.Single(p => p.SupportsCompilation && p.FilePath == document.Project.FilePath);
        }
    }
}
