// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Debugging;

/// <summary>
/// Language-specific service for splicing debugger expressions into source documents.
/// </summary>
internal interface IDebuggerSplicer : ILanguageService
{
    /// <summary>
    /// Splices a debugger expression into a document at the specified context point.
    /// </summary>
    /// <param name="document">The source document where debugger is stopped.</param>
    /// <param name="contextPoint">The absolute position in the document where the debugger is stopped.</param>
    /// <param name="expression">The debugger expression being typed (from QuickWatch/Immediate window).</param>
    /// <param name="cursorOffset">The caret position within the expression, in UTF-16 code units.</param>
    /// <returns>A DebuggerSpliceResult containing the modified source text and the completion position.</returns>
    ValueTask<DebuggerSpliceResult> SpliceAsync(
        Document document,
        int contextPoint,
        string expression,
        int cursorOffset,
        CancellationToken cancellationToken);
}
