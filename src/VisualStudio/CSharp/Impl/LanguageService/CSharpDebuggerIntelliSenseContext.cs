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

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    internal class CSharpDebuggerIntelliSenseContext : AbstractDebuggerIntelliSenseContext
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

        protected override int GetAdjustedContextPoint(int contextPoint, Document document)
        {
            // Determine the position in the buffer at which to end the tracking span representing
            // the part of the imaginary buffer before the text in the view. 
            var tree = document.GetSyntaxTreeSynchronously(CancellationToken.None);
            var token = tree.FindTokenOnLeftOfPosition(contextPoint, CancellationToken.None);

            // Special case to handle class designer because it asks for debugger IntelliSense using
            // spans between members.
            if (contextPoint > token.Span.End &&
                token.IsKindOrHasMatchingText(SyntaxKind.CloseBraceToken) &&
                token.Parent.IsKind(SyntaxKind.Block) &&
                token.Parent.Parent is MemberDeclarationSyntax)
            {
                return contextPoint;
            }

            if (token.IsKindOrHasMatchingText(SyntaxKind.CloseBraceToken) &&
                token.Parent.IsKind(SyntaxKind.Block))
            {
                return token.SpanStart;
            }

            return token.FullSpan.End;
        }

        protected override ITrackingSpan GetPreviousStatementBufferAndSpan(int contextPoint, Document document)
        {
            var previousTrackingSpan = ContextBuffer.CurrentSnapshot.CreateTrackingSpan(Span.FromBounds(0, contextPoint), SpanTrackingMode.EdgeNegative);

            // terminate the previous expression/statement
            var buffer = ProjectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: [previousTrackingSpan, this.StatementTerminator],
                options: ProjectionBufferOptions.None,
                contentType: this.ContentType);

            return buffer.CurrentSnapshot.CreateTrackingSpan(0, buffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeNegative);
        }

        public override bool CompletionStartsOnQuestionMark
        {
            get { return false; }
        }

        protected override string StatementTerminator
        {
            get { return ";"; }
        }
    }
}
