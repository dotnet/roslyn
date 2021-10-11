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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddInheritdoc), Shared]
    internal sealed class AddInheritdocCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        /// <summary>
        /// CS1591: Missing XML comment for publicly visible type or member 'Type_or_Member'
        /// </summary>
        private const string CS1591 = nameof(CS1591);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AddInheritdocCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS1591);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {

            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return;
            }

            SemanticModel? semanticModel = null;
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id != CS1591)
                {
                    continue;
                }

                var node = root.FindNode(diagnostic.Location.SourceSpan);
                if (node.Kind() is not SyntaxKind.MethodDeclaration and not SyntaxKind.PropertyDeclaration)
                {
                    continue;
                }

                semanticModel ??= await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (semanticModel is null)
                {
                    continue;
                }

                var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                if (symbol is null)
                {
                    continue;
                }

                if (symbol.Kind is SymbolKind.Method or SymbolKind.Property)
                {
                    if (symbol.IsOverride ||
                        symbol.ImplicitInterfaceImplementations().Any())
                    {
                        context.RegisterCodeFix(new MyCodeAction(FeaturesResources.Explicitly_inherit_documentation,
                            c => FixAsync(context.Document, diagnostic, c)), context.Diagnostics);
                    }
                }
            }
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            string? newLine = null;
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan);
                newLine ??= (await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false)).GetOption(FormattingOptions2.NewLine);
                // We can safely assume, that there is no leading doc comment, because that is what CS1591 is telling us.
                // So we create a new /// <inheritdoc/> comment.
                var xmlSpaceAfterTrippleSlash = Token(leading: new SyntaxTriviaList(DocumentationCommentExterior("///")), SyntaxKind.XmlTextLiteralToken, text: " ", valueText: " ", trailing: default);
                var lessThanToken = Token(SyntaxKind.LessThanToken).WithoutTrivia();
                var inheritdocTagName = XmlName("inheritdoc").WithoutTrivia();
                var slashGreaterThanToken = Token(SyntaxKind.SlashGreaterThanToken).WithoutTrivia();
                var xmlNewLineToken = Token(leading: default, SyntaxKind.XmlTextLiteralNewLineToken, text: newLine, valueText: newLine, trailing: default);

                var singleLineInheritdocComment = DocumentationCommentTrivia(
                    kind: SyntaxKind.SingleLineDocumentationCommentTrivia,
                    content: new SyntaxList<XmlNodeSyntax>(new XmlNodeSyntax[]
                    {
                        XmlText(xmlSpaceAfterTrippleSlash),
                        XmlEmptyElement(lessThanToken, inheritdocTagName, attributes: default, slashGreaterThanToken),
                        XmlText(xmlNewLineToken),
                    }),
                    endOfComment: Token(SyntaxKind.EndOfDocumentationCommentToken).WithoutTrivia());

                var indendation = node.GetLocation().GetLineSpan().StartLinePosition.Character;
                var newLeadingTrivia = new SyntaxTrivia[]
                {
                    Whitespace(new string(' ', indendation)),
                    Trivia(singleLineInheritdocComment),
                };

                editor.ReplaceNode(node, node.WithPrependedLeadingTrivia(newLeadingTrivia));
            }
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
