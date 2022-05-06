// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;
    using static SyntaxFactory;

    /// <summary>
    /// Implementation of <see cref="AbstractPasteProcessor"/> used when we know the original string literal expression
    /// we were copying text out of.  Because we know the original literal expression, we can determine what the
    /// characters being pasted meant in the original context and we can attempt to preserve that as closely as
    /// possible.
    /// </summary>
    internal class KnownSourcePasteProcessor : AbstractPasteProcessor
    {
        /// <summary>
        /// The selection in the document prior to the paste happening.
        /// </summary>
        private readonly SnapshotSpan _selectionBeforePaste;

        /// <summary>
        /// Information stored to the clipboard about the original cut/copy.
        /// </summary>
        private readonly StringCopyPasteData _copyPasteData;

        public KnownSourcePasteProcessor(
            string newLine,
            IndentationOptions indentationOptions,
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentBeforePaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            SnapshotSpan selectionBeforePaste,
            StringCopyPasteData copyPasteData)
            : base(newLine, indentationOptions, snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste)
        {
            _selectionBeforePaste = selectionBeforePaste;
            _copyPasteData = copyPasteData;
        }

        public override ImmutableArray<TextChange> GetEdits(CancellationToken cancellationToken)
        {
            // For pastes into non-raw strings, we can just determine how the change should be escaped in-line at that
            // same location the paste originally happened at.  For raw-strings things get more complex as we have to
            // deal with things like indentation and potentially adding newlines to make things legal.

            // Smart Pasting into raw string not supported yet.  
            if (IsAnyRawStringExpression(StringExpressionBeforePaste))
                return default;

            return GetEditsForNonRawString();
        }

        private ImmutableArray<TextChange> GetEditsForNonRawString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);

            if (StringExpressionBeforePaste is LiteralExpressionSyntax literal)
            {
                var isVerbatim = literal.Token.IsVerbatimStringLiteral();
                foreach (var content in _copyPasteData.Contents)
                {
                    if (content.IsText)
                    {
                        builder.Append(EscapeForNonRawStringLiteral(
                            isVerbatim, isInterpolated: false, trySkipExistingEscapes: false, content.TextValue));
                    }
                    else if (content.IsInterpolation)
                    {
                        // we're copying an interpolation from an interpolated string to a string literal. For example,
                        // we're pasting `{x + y}` into the middle of `"goobar"`.  One thing we could potentially do in
                        // the future is split the literal into `"goo" + $"{x + y}" + "bar"`, or just making the
                        // containing literal into an interpolation itself.  However, for now, we do the simple thing
                        // and just treat the interpolation as raw text that should just be escaped as appropriate into
                        // the destination.
                        builder.Append('{');
                        builder.Append(EscapeForNonRawStringLiteral(
                            isVerbatim, isInterpolated: false, trySkipExistingEscapes: false, content.InterpolationExpression));

                        if (content.InterpolationAlignmentClause != null)
                            builder.Append(content.InterpolationAlignmentClause);

                        if (content.InterpolationFormatClause != null)
                        {
                            builder.Append(':');
                            builder.Append(EscapeForNonRawStringLiteral(
                                isVerbatim, isInterpolated: false, trySkipExistingEscapes: false, content.InterpolationFormatClause));
                        }

                        builder.Append('}');
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(content.Kind);
                    }
                }
            }
            else if (StringExpressionBeforePaste is InterpolatedStringExpressionSyntax interpolatedString)
            {
                var isVerbatim = interpolatedString.StringStartToken.Kind() is SyntaxKind.InterpolatedVerbatimStringStartToken;
                foreach (var content in _copyPasteData.Contents)
                {
                    if (content.IsText)
                    {
                        builder.Append(EscapeForNonRawStringLiteral(
                            isVerbatim, isInterpolated: true, trySkipExistingEscapes: false, content.TextValue));
                    }
                    else if (content.IsInterpolation)
                    {
                        // we're moving an interpolation from one interpolation to another.  This can just be copied
                        // wholesale *except* for the format literal portion (e.g. `{...:XXXX}` which may have to be updated
                        // for the destination type.
                        builder.Append('{');
                        builder.Append(content.InterpolationExpression);

                        if (content.InterpolationAlignmentClause != null)
                            builder.Append(content.InterpolationAlignmentClause);

                        if (content.InterpolationFormatClause != null)
                        {
                            builder.Append(':');
                            builder.Append(EscapeForNonRawStringLiteral(
                                isVerbatim, isInterpolated: true, trySkipExistingEscapes: false, content.InterpolationFormatClause));
                        }

                        builder.Append('}');
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(content.Kind);
                    }
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }

            return ImmutableArray.Create(new TextChange(_selectionBeforePaste.Span.ToTextSpan(), builder.ToString()));
        }
    }
}
