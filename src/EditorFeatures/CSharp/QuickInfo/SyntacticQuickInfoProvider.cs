// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Syntactic, LanguageNames.CSharp)]
    internal class SyntacticQuickInfoProvider : AbstractQuickInfoProvider
    {
        [ImportingConstructor]
        public SyntacticQuickInfoProvider(
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IGlyphService glyphService,
            ClassificationTypeMap typeMap)
            : base(textBufferFactoryService, contentTypeRegistryService, projectionBufferFactoryService,
                   editorOptionsFactoryService, textEditorFactoryService, glyphService, typeMap)
        {
        }

        protected override async Task<IDeferredQuickInfoContent> BuildContentAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            if (token.Kind() != SyntaxKind.CloseBraceToken)
            {
                return null;
            }

            // Don't show for interpolations
            if (token.Parent.IsKind(SyntaxKind.Interpolation) &&
                ((InterpolationSyntax)token.Parent).CloseBraceToken == token)
            {
                return null;
            }

            // Now check if we can find an open brace. 
            var parent = token.Parent;
            var openBrace = parent.ChildNodesAndTokens().FirstOrDefault(n => n.Kind() == SyntaxKind.OpenBraceToken).AsToken();
            if (openBrace.Kind() != SyntaxKind.OpenBraceToken)
            {
                return null;
            }

            // If the open brace is the first token of the node (like in the case of a block node or
            // an accessor list node), then walk up one higher so we can show more useful context
            // (like the method a block belongs to).
            if (parent.GetFirstToken() == openBrace)
            {
                parent = parent.Parent;
            }

            // Now that we know what we want to display, create a small elision buffer with that
            // span, jam it in a view and show that to the user.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
            {
                return null;
            }

            var span = new SnapshotSpan(textSnapshot, Span.FromBounds(parent.SpanStart, openBrace.Span.End));
            return this.CreateElisionBufferDeferredContent(span);
        }
    }
}
