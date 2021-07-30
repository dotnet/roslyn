// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Definitions
{
    /// <summary>
    /// XamlDefinition with file path and TextSpan or line and column.
    /// </summary>
    internal sealed class XamlSourceDefinition : XamlDefinition
    {
        private readonly TextSpan? _span;
        private readonly int _line, _character;

        public XamlSourceDefinition(string filePath, TextSpan span)
        {
            FilePath = filePath;
            _span = span;
        }

        public XamlSourceDefinition(string filePath, int line, int character)
        {
            FilePath = filePath;
            _line = line;
            _character = character;
        }

        public string FilePath { get; }

        /// <summary>
        /// When XamlSourceDefinition was created, the creator may have had a textSpan or line/column.
        /// We should either use _span or _line _character. This property will tell you which one to use.
        /// </summary>
        private bool CanUseSpan => _span != null;

        public TextSpan? GetTextSpan(SourceText text)
        {
            if (CanUseSpan)
            {
                return _span;
            }

            // Convert the line column to TextSpan
            if (_line < text.Lines.Count)
            {
                var character = Math.Min(_character, text.Lines[_line].Span.Length);
                var start = text.Lines.GetPosition(new LinePosition(_line, character));
                return new TextSpan(start, 0);
            }

            return null;
        }
    }
}
