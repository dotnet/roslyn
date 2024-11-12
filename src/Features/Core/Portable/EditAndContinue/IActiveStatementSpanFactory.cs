// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal interface IActiveStatementSpanFactory
{
    /// <summary>
    /// Returns base mapped active statement spans contained in each specified document projected to a given solution snapshot
    /// (i.e. the solution snapshot the base active statements are current for could be different from the given <paramref name="solution"/>).
    /// </summary>
    /// <returns>
    /// <see langword="default"/> if called outside of an edit session.
    /// The length of the returned array matches the length of <paramref name="documentIds"/> otherwise.
    /// </returns>
    /// <remarks>
    /// The document may be any text document.
    /// <paramref name="documentIds"/> may not correspond to any document in the given <paramref name="solution"/> (an empty array of spans is returned for such document).
    /// Returns <see cref="DocumentId"/> of the unmapped document containing the active statement (i.e. the document that has the #line directive mapping the statement to one of the specified <paramref name="documentIds"/>),
    /// or null the unmapped document has not been determined (the active statement has not changed from the baseline).
    /// </remarks>
    ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Returns adjusted active statements in the specified mapped <paramref name="document"/> snapshot.
    /// </summary>
    /// <returns>
    /// <see langword="default"/> if called outside of an edit session, or active statements for the document can't be determined for some reason
    /// (e.g. the document has syntax errors or is out-of-sync).
    /// </returns>
    ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);
}
