// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Definitions
{
    /// <summary>
    /// XamlDefinition with file path and TextSpan or line and column.
    /// When XamlSourceDefinition was created, the creator may have had a textSpan or line/column.
    /// We should either use Span or Line Column.
    /// </summary>
    internal sealed class XamlSourceDefinition : XamlDefinition
    {
        public XamlSourceDefinition(string filePath, TextSpan span)
        {
            FilePath = filePath;
            Span = span;
        }

        public XamlSourceDefinition(string filePath, int line, int column)
        {
            FilePath = filePath;
            Line = line;
            Column = column;
        }

        public string FilePath { get; }

        public int Line { get; }
        public int Column { get; }

        public TextSpan? Span { get; }
    }
}
