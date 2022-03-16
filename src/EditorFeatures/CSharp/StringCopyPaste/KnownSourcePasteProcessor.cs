// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    internal class KnownSourcePasteProcessor : AbstractPasteProcessor
    {
        public KnownSourcePasteProcessor(
            string newLine,
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentBeforePaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            ImmutableArray<(VirtualCharSequence virtualChars, InterpolationSyntax? interpolation)> copiedPieces)
            : base(newLine, snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste)
        {
        }

        public override ImmutableArray<TextChange> GetEdits(CancellationToken cancellationToken)
        {
            if (StringExpressionBeforePaste is LiteralExpressionSyntax)
        }
    }
}
