// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;

internal sealed class CSharpDebuggerIntelliSenseContext : AbstractDebuggerIntelliSenseContext
{
    private const string StatementTerminator = ";";

    public CSharpDebuggerIntelliSenseContext(
        IWpfTextView view,
        IVsTextView vsTextView,
        IVsTextLines debuggerBuffer,
        ITextBuffer contextBuffer,
        TextSpan[] currentStatementSpan,
        IComponentModel componentModel,
        IServiceProvider serviceProvider)
        : base(view,
            vsTextView,
            debuggerBuffer,
            contextBuffer,
            currentStatementSpan,
            componentModel,
            serviceProvider,
            componentModel.GetService<IContentTypeRegistryService>().GetContentType(ContentTypeNames.CSharpContentType))
    {
    }

    // Test constructor
    internal CSharpDebuggerIntelliSenseContext(
        IWpfTextView view,
        ITextBuffer contextBuffer,
        TextSpan[] currentStatementSpan,
        IComponentModel componentModel,
        bool immediateWindow)
        : base(view,
            contextBuffer,
            currentStatementSpan,
            componentModel,
            componentModel.GetService<IContentTypeRegistryService>().GetContentType(ContentTypeNames.CSharpContentType),
            immediateWindow)
    {
    }

    protected override IProjectionBuffer GetAdjustedBuffer(int contextPoint, Document document, ITrackingSpan debuggerMappedSpan)
    {
        // Determine the position in the buffer at which to end the tracking span representing
        // the part of the imaginary buffer before the text in the view. 
        var tree = document.GetSyntaxTreeSynchronously(CancellationToken.None);
        var token = tree.FindTokenOnLeftOfPosition(contextPoint, CancellationToken.None);

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

        var beforeAdjustedStart = ContextBuffer.CurrentSnapshot.CreateTrackingSpan(Span.FromBounds(0, adjustedStart), SpanTrackingMode.EdgeNegative);
        var afterAdjustedStart = ContextBuffer.CurrentSnapshot.CreateTrackingSpanFromIndexToEnd(adjustedStart, SpanTrackingMode.EdgePositive);

        return ProjectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: [beforeAdjustedStart, separatorBeforeDebuggerMappedSpan, debuggerMappedSpan, StatementTerminator, afterAdjustedStart],
                options: ProjectionBufferOptions.None,
                contentType: ContentType);
    }

    public override bool CompletionStartsOnQuestionMark
    {
        get { return false; }
    }
}
