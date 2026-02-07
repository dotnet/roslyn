// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Debugging;

[ExportLanguageService(typeof(IDebuggerSplicer), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpDebuggerSplicer() : IDebuggerSplicer
{
    /// <summary>
    /// Splice the expression into the given document so we can get completions from it.
    /// </summary>
    public async ValueTask<DebuggerSpliceResult> SpliceAsync(
        Document document,
        int contextPoint,
        string expression,
        int cursorOffset,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var splicePoint = DebuggerSplicePoint.CalculateSplicePoint(root.SyntaxTree, contextPoint);

        // Build the text to insert: separator + expression + terminator
        var insertedText = splicePoint.SeparatorBefore + expression + DebuggerSplicePoint.StatementTerminator;
        var change = new TextChange(new TextSpan(splicePoint.AdjustedStart, 0), insertedText);
        var splicedText = text.WithChanges(change);

        // The completion position is: insertion point + separator length + cursor offset within expression
        var completionPosition = splicePoint.AdjustedStart + splicePoint.SeparatorBefore.Length + cursorOffset;

        return new DebuggerSpliceResult(
            splicedText,
            completionPosition,
            spliceStart: splicePoint.AdjustedStart,
            insertedLength: insertedText.Length);
    }
}
