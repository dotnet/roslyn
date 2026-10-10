// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Debugging;
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
        var tree = document.GetSyntaxTreeSynchronously(CancellationToken.None);
        var splicePoint = DebuggerSplicePoint.CalculateSplicePoint(tree, contextPoint);

        var beforeAdjustedStart = ContextBuffer.CurrentSnapshot.CreateTrackingSpan(Span.FromBounds(0, splicePoint.AdjustedStart), SpanTrackingMode.EdgeNegative);
        var afterAdjustedStart = ContextBuffer.CurrentSnapshot.CreateTrackingSpanFromIndexToEnd(splicePoint.AdjustedStart, SpanTrackingMode.EdgePositive);

        return ProjectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: [beforeAdjustedStart, splicePoint.SeparatorBefore, debuggerMappedSpan, DebuggerSplicePoint.StatementTerminator, afterAdjustedStart],
                options: ProjectionBufferOptions.None,
                contentType: ContentType);
    }

    public override bool CompletionStartsOnQuestionMark
    {
        get { return false; }
    }
}
