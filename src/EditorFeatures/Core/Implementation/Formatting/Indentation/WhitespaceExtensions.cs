// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting.Indentation
{
    internal static class WhitespaceExtensions
    {
        public static bool IsFirstTokenOnLine(this SyntaxToken token, ITextSnapshot snapshot)
        {
            Contract.ThrowIfNull(snapshot);

            var baseLine = snapshot.GetLineFromPosition(token.SpanStart);
            return baseLine.GetFirstNonWhitespacePosition() == token.SpanStart;
        }
    }
}
