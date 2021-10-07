// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritDoc
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddInheritDoc), Shared]
    internal sealed class AddInheritDocCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        /// <summary>
        /// CS1591: Missing XML comment for publicly visible type or member 'Type_or_Member'
        /// </summary>
        private const string CS1591 = nameof(CS1591);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AddInheritDocCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS1591);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == CS1591);
            if (diagnostic != null)
            {
                context.RegisterCodeFix(new MyCodeAction("TODO", c => FixAsync(context.Document, diagnostic, c)), context.Diagnostics);
            }
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan);
                // We can safely assume, that there is no leading doc comment, because that is what CS1591 is telling us.
                // So we create a new ///<inheritdoc/> comment.
                var lessThanToken = Token(SyntaxKind.LessThanToken).WithLeadingTrivia(DocumentationCommentExterior("///"));
                var singleLineInheritDocComment = DocumentationCommentTrivia(
                    kind: SyntaxKind.SingleLineDocumentationCommentTrivia,
                    content: new SyntaxList<Syntax.XmlNodeSyntax>(XmlEmptyElement(lessThanToken, name: XmlName("inheritdoc"), attributes: default, slashGreaterThanToken: Token(SyntaxKind.SlashGreaterThanToken))),
                    endOfComment: Token(SyntaxKind.EndOfDocumentationCommentToken));
                var wrappedInheritedDoc = Trivia(singleLineInheritDocComment);
                var existingLeadingTrivia = node.GetLeadingTrivia();
                var newLeadingTrivia = existingLeadingTrivia.Add(wrappedInheritedDoc);
                editor.ReplaceNode(node, node.WithLeadingTrivia(newLeadingTrivia));
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}
