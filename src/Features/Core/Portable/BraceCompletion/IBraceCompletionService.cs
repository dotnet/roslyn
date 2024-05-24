// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.BraceCompletion;

internal interface IBraceCompletionService
{
    /// <summary>
    /// Checks if this brace completion service should be the service used to provide brace completions at
    /// the specified position with the specified opening brace.
    /// 
    /// Only one implementation of <see cref="IBraceCompletionService"/> should return true
    /// for a given brace, opening position, and document.  Only that service will be asked
    /// for brace completion results.
    /// </summary>
    /// <param name="brace">
    /// The opening brace character to be inserted at the opening position.</param>
    /// <param name="openingPosition">
    /// The opening position to insert the brace.
    /// Note that the brace is not yet inserted at this position in the document.
    /// </param>
    /// <param name="document">The document to insert the brace at the position.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// True if <see cref="BraceCompletionResult"/> is available in the given <paramref name="context"/>.
    /// Completes synchronously unless the service needs Semantic Model to determine the brace completion result.
    /// </summary>
    Task<bool> HasBraceCompletionAsync(BraceCompletionContext context, Document document, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the text change to add the closing brace given the context.
    /// </summary>
    BraceCompletionResult GetBraceCompletion(BraceCompletionContext braceCompletionContext);

    /// <summary>
    /// Returns any text changes that need to be made after adding the closing brace.
    /// </summary>
    /// <remarks>
    /// This cannot be merged with <see cref="GetBraceCompletion(BraceCompletionContext)"/>
    /// as we need to swap the editor tracking mode of the closing point from positive to negative
    /// in BraceCompletionSessionProvider.BraceCompletionSession.Start after completing the brace and before
    /// doing any kind of formatting on it.  So these must be two distinct steps until we fully move to LSP.
    /// </remarks>
    BraceCompletionResult? GetTextChangesAfterCompletion(BraceCompletionContext braceCompletionContext, IndentationOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Get any text changes that should be applied after the enter key is typed inside a brace completion context.
    /// </summary>
    BraceCompletionResult? GetTextChangeAfterReturn(BraceCompletionContext braceCompletionContext, IndentationOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the brace completion context if the caret is located between an already completed
    /// set of braces with only whitespace in between.
    /// </summary>
    BraceCompletionContext? GetCompletedBraceContext(ParsedDocument document, int caretLocation);

    /// <summary>
    /// Returns true if over typing should be allowed given the caret location and completed pair of braces.
    /// For example some providers allow over typing in non-user code and others do not.
    /// </summary>
    bool AllowOverType(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);
}

internal readonly struct BraceCompletionResult(ImmutableArray<TextChange> textChanges, LinePosition caretLocation)
{
    /// <summary>
    /// The set of text changes that should be applied to the input text to retrieve the
    /// brace completion result.
    /// </summary>
    public ImmutableArray<TextChange> TextChanges { get; } = textChanges;

    /// <summary>
    /// The caret location in the new text created by applying all <see cref="TextChanges"/>
    /// to the input text.  Note the column in the line position can be virtual in that it points
    /// to a location in the line which does not actually contain whitespace.
    /// Hosts can determine how best to handle that virtual location.
    /// For example, placing the character in virtual space (when suppported)
    /// or inserting an appropriate number of spaces into the document".
    /// </summary>
    public LinePosition CaretLocation { get; } = caretLocation;
}

internal readonly struct BraceCompletionContext(ParsedDocument document, int openingPoint, int closingPoint, int caretLocation)
{
    public ParsedDocument Document { get; } = document;

    public int OpeningPoint { get; } = openingPoint;

    public int ClosingPoint { get; } = closingPoint;

    public int CaretLocation { get; } = caretLocation;

    public bool HasCompletionForOpeningBrace(char openingBrace)
        => ClosingPoint >= 1 && Document.Text[OpeningPoint] == openingBrace;

    public SyntaxToken GetOpeningToken()
        => Document.Root.FindToken(OpeningPoint, findInsideTrivia: true);
}
