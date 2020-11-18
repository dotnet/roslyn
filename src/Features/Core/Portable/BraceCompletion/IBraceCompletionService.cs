// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.BraceCompletion
{
    internal interface IBraceCompletionService : ILanguageService
    {
        /// <summary>
        /// Checks if this brace completion service should be the one used to provide brace completions at
        /// the specified position with the specified opening brace.
        /// Note that this does not mean that the service will provide brace completion, but that
        /// only this service will be asked to provide them.
        /// </summary>
        Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the text change to add the closing brace given the context.
        /// </summary>
        Task<BraceCompletionResult?> GetBraceCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);

        /// <summary>
        /// Returns any text changes that need to be made after adding the closing brace.
        /// </summary>
        /// <remarks>
        /// This cannot be merged with <see cref="GetBraceCompletionAsync(BraceCompletionContext, CancellationToken)"/>
        /// as we need to make modifications to tracking spans in between adding the closing brace and doing
        /// any kind of formatting on it.  So these must be two distinct steps until we fully move to LSP.
        /// </remarks>
        Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);

        /// <summary>
        /// Get any text changes that should be applied after the enter key is typed inside a brace completion context.
        /// </summary>
        Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the brace completion context if the caret is located between an already completed
        /// set of braces with only whitespace in between.
        /// </summary>
        Task<BraceCompletionContext?> IsInsideCompletedBracesAsync(int caretLocation, Document document, CancellationToken cancellationToken);

        /// <summary>
        /// Whether or not overtype should be allowed given a brace completion context.
        /// </summary>
        Task<bool> AllowOverTypeAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);
    }

    internal struct BraceCompletionResult
    {
        /// <summary>
        /// The set of text changes that should be applied to the input text to retrieve the
        /// brace completion result.
        /// </summary>
        public ImmutableArray<TextChange> TextChanges { get; }

        /// <summary>
        /// The caret location in the new text created by applying all <see cref="TextChanges"/>
        /// to the input text.
        /// </summary>
        public LinePosition CaretLocation { get; }

        public BraceCompletionResult(ImmutableArray<TextChange> textChanges, LinePosition caretLocation)
        {
            CaretLocation = caretLocation;
            TextChanges = textChanges;
        }
    }

    internal class BraceCompletionContext
    {
        public Document Document { get; }

        public int OpeningPoint { get; }

        public int ClosingPoint { get; }

        public int? CaretLocation { get; }

        public BraceCompletionContext(Document document, int openingPoint, int closingPoint, int? caretLocation)
        {
            Document = document;
            OpeningPoint = openingPoint;
            ClosingPoint = closingPoint;
            CaretLocation = caretLocation;
        }
    }
}
