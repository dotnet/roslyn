// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Debugging
{
    /// <summary>
    /// CSharp's LSP Debugger Intellisense context
    /// Used for intellisense completions in Watch and Immediate Window.
    /// Similar to Roslyn's CSharp debugger intellisense context:
    /// \src\VisualStudio\CSharp\Impl\LanguageService\CSharpDebuggerIntelliSenseContext.cs
    /// </summary>
    internal class CSharpLspDebuggerIntelliSenseContext : AbstractDebuggerIntelliSenseContext
    {
        public CSharpLspDebuggerIntelliSenseContext(
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
                componentModel.GetService<IContentTypeRegistryService>().GetContentType(StringConstants.CSharpLspContentTypeName))
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
                IsKindOrHasMatchingText(token, SyntaxKind.CloseBraceToken) &&
                token.Parent.IsKind(SyntaxKind.Block) &&
                token.Parent.Parent is MemberDeclarationSyntax)
            {
                return contextPoint;
            }

            if (IsKindOrHasMatchingText(token, SyntaxKind.CloseBraceToken) &&
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
            var buffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: new object[] { previousTrackingSpan, StatementTerminator },
                options: ProjectionBufferOptions.None,
                contentType: ContentType);

            return buffer.CurrentSnapshot.CreateTrackingSpan(0, buffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeNegative);
        }

        public override bool CompletionStartsOnQuestionMark => false;

        protected override string StatementTerminator => ";";

        private static bool IsKindOrHasMatchingText(SyntaxToken token, SyntaxKind kind) =>
            token.Kind() == kind || token.ToString() == SyntaxFacts.GetText(kind);
    }
}
