// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IActiveStatementSpanProvider
    {
        /// <summary>
        /// Returns base active statement spans contained in each specified document.
        /// </summary>
        /// <returns>
        /// <see langword="default"/> if called outside of an edit session.
        /// The length of the returned array matches the length of <paramref name="documentIds"/> otherwise.
        /// </returns>
        ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);

        /// <summary>
        /// Returns adjusted active statements in the specified <paramref name="document"/> snapshot.
        /// </summary>
        /// <returns>
        /// <see langword="default"/> if called outside of an edit session, or active statements for the document can't be determined for some reason
        /// (e.g. the document has syntax errors or is out-of-sync).
        /// </returns>
        ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);
    }
}
