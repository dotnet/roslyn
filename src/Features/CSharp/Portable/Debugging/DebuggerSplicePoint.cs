// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Debugging;

/// <summary>
/// Splicing is used to get IntelliSense completions on debugger expressions (from Watch, QuickWatch and Immediate window).
/// </summary>
internal readonly struct DebuggerSplicePoint(int adjustedStart, string separatorBefore)
{
    /// <summary>
    /// The position in the source text where the debugger expression should be inserted.
    /// </summary>
    public int AdjustedStart { get; } = adjustedStart;

    /// <summary>
    /// The separator string to insert before the debugger expression.
    /// Typically ";" but may be " " in cases like semicolon-terminated statements.
    /// </summary>
    public string SeparatorBefore { get; } = separatorBefore;

    public const string StatementTerminator = ";";

    public static DebuggerSplicePoint CalculateSplicePoint(SyntaxTree tree, int contextPoint)
    {
        var token = CodeAnalysis.Shared.Extensions.SyntaxTreeExtensions.FindTokenOnLeftOfPosition(tree, contextPoint, CancellationToken.None);

        // Typically, the separator between the text before adjustedStart and debuggerMappedSpan is
        // a semicolon (StatementTerminator), unless a specific condition outlined later in the
        // method is encountered.
        var separatorBeforeDebuggerMappedSpan = StatementTerminator;
        var adjustedStart = token.FullSpan.End;

        // Special case to handle class designer because it asks for debugger IntelliSense using
        // spans between members.
        if (contextPoint > token.Span.End &&
            token.IsKindOrHasMatchingText(SyntaxKind.CloseBraceToken) &&
            token.Parent.IsKind(SyntaxKind.Block) &&
            token.Parent.Parent is MemberDeclarationSyntax)
        {
            adjustedStart = contextPoint;
        }
        // Insert inside the block, not after it
        else if (token.IsKindOrHasMatchingText(SyntaxKind.CloseBraceToken) &&
            token.Parent.IsKind(SyntaxKind.Block))
        {
            adjustedStart = token.SpanStart;
        }
        else if (token.IsKindOrHasMatchingText(SyntaxKind.SemicolonToken) &&
            token.Parent is StatementSyntax)
        {
            // If the context is at a semicolon terminated statement, then we use the start of
            // that statement as the adjusted context position. This is to ensure the placement
            // of debuggerMappedSpan is in the same block as token originally was. For example,
            // 
            // for (int i = 0; i < 10; i++)
            //   [Console.WriteLine(i);]
            //
            // where [] denotes CurrentStatementSpan, should use the start of CurrentStatementSpan
            // as the adjusted context, and should not place a semicolon before debuggerMappedSpan.
            // Not doing either of those would place debuggerMappedSpan outside the for loop.
            // We use a space as the separator in this case (instead of an empty string) to help
            // the vs editor out and not have a projection seam at the location they will bring
            // up completion.
            separatorBeforeDebuggerMappedSpan = " ";
            adjustedStart = token.Parent.SpanStart;
        }

        return new DebuggerSplicePoint(adjustedStart, separatorBeforeDebuggerMappedSpan);
    }
}

