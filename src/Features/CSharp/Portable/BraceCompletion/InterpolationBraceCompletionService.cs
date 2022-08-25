// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    /// <summary>
    /// Brace completion service used for completing curly braces inside interpolated strings.
    /// In other curly brace completion scenarios the <see cref="CurlyBraceCompletionService"/> should be used.
    /// </summary>
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class InterpolationBraceCompletionService : AbstractCSharpBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InterpolationBraceCompletionService()
        {
        }

        protected override char OpeningBrace => CurlyBrace.OpenCharacter;
        protected override char ClosingBrace => CurlyBrace.CloseCharacter;

        public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeWithValidClosingToken(context);

        /// <summary>
        /// Only return this service as valid when we're typing curly braces inside an interpolated string.
        /// Otherwise curly braces should be completed using the <see cref="CurlyBraceCompletionService"/>
        /// </summary>
        public override bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
            => OpeningBrace == brace && IsPositionInInterpolationContext(document, openingPosition);

        protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
            => IsValidOpeningBraceToken(token) && token.SpanStart == position;

        protected override bool IsValidOpeningBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation);

        protected override bool IsValidClosingBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.CloseBraceToken);

        /// <summary>
        /// Returns true when the input position could be starting an interpolation expression if a curly brace was typed.
        /// </summary>
        public static bool IsPositionInInterpolationContext(ParsedDocument document, int position)
        {
            // First, check to see if the character to the left of the position is an open curly.
            // If it is, we shouldn't complete because the user may be trying to escape a curly.
            // E.g. they are trying to type $"{{"
            if (CouldEscapePreviousOpenBrace('{', position, document.Text))
            {
                return false;
            }

            var token = document.Root.FindTokenOnLeftOfPosition(position);
            if (!token.Span.IntersectsWith(position))
            {
                return false;
            }

            // We can be starting an interpolation expression if we're inside an interpolated string.
            return token.Parent.IsKind(SyntaxKind.InterpolatedStringExpression) || token.Parent.IsParentKind(SyntaxKind.InterpolatedStringExpression);
        }
    }
}
