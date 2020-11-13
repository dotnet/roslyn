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
        Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken, bool supportsVirtualSpace = true);

        /// <summary>
        /// Returns the brace completion context if the caret is located between an already completed
        /// set of braces with only whitespace in between.
        /// </summary>
        BraceCompletionContext? IsInsideCompletedBraces(int caretLocation, SyntaxNode root, Document document);

        /// <summary>
        /// Whether or not overtype should be allowed given a brace completion context.
        /// </summary>
        Task<bool> AllowOverTypeAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);
    }

    internal struct BraceCompletionResult
    {
        /// <summary>
        /// A list of text changes that should be applied to the original input text in sequential order.
        /// E.g. Apply the first set of text changes to original text to get a new text, then apply the 
        /// the second set of text changes to the previous result text, and so on.
        /// 
        /// This is required for the editor's implementation of brace completion as they rely on tracking spans
        /// to know if the brace completion session is active.  So to avoid trampling all over the tracking spans,
        /// we need to apply the edits incrementally.  Can be removed once brace completion uses LSP locally.
        /// </summary>
        public ImmutableArray<ImmutableArray<TextChange>> TextChangesPerVersion { get; }

        /// <summary>
        /// The caret location in the new text created by applying all <see cref="TextChangesPerVersion"/>
        /// to the input text.
        /// </summary>
        public int CaretLocation { get; }

        public BraceCompletionResult(ImmutableArray<ImmutableArray<TextChange>> textChangesPerVersion, int caretLocation)
        {
            CaretLocation = caretLocation;
            TextChangesPerVersion = textChangesPerVersion;
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
