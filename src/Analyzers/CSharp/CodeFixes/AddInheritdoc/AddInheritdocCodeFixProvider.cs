// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddInheritdoc), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AddInheritdocCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    /// <summary>
    /// CS1591: Missing XML comment for publicly visible type or member 'Type_or_Member'
    /// </summary>
    private const string CS1591 = nameof(CS1591);

    public override ImmutableArray<string> FixableDiagnosticIds => [CS1591];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var semanticModel = await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node.Kind() is not SyntaxKind.MethodDeclaration and not SyntaxKind.PropertyDeclaration and not SyntaxKind.VariableDeclarator)
                continue;

            if (node.IsKind(SyntaxKind.VariableDeclarator) && node is not { Parent.Parent: EventFieldDeclarationSyntax })
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (symbol is null)
                continue;

            if (symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event)
            {
                if (symbol.IsOverride ||
                    symbol.ImplicitInterfaceImplementations().Any())
                {
                    RegisterCodeFix(context, CSharpCodeFixesResources.Explicitly_inherit_documentation, nameof(CSharpCodeFixesResources.Explicitly_inherit_documentation), diagnostic);
                }
            }
        }
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var options = await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var newLine = options.NewLine;
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan);
            if (node is VariableDeclaratorSyntax { Parent.Parent: EventFieldDeclarationSyntax eventField })
                node = eventField;

            // We can safely assume, that there is no leading doc comment, because that is what CS1591 is telling us.
            // So we create a new /// <inheritdoc/> comment.
            var xmlSpaceAfterTripleSlash = Token(leading: [DocumentationCommentExterior("///")], SyntaxKind.XmlTextLiteralToken, text: " ", valueText: " ", trailing: default);
            var lessThanToken = LessThanToken.WithoutTrivia();
            var inheritdocTagName = XmlName("inheritdoc").WithoutTrivia();
            var slashGreaterThanToken = SlashGreaterThanToken.WithoutTrivia();
            var xmlNewLineToken = Token(leading: default, SyntaxKind.XmlTextLiteralNewLineToken, text: newLine, valueText: newLine, trailing: default);

            var singleLineInheritdocComment = DocumentationCommentTrivia(
                kind: SyntaxKind.SingleLineDocumentationCommentTrivia,
                content:
                [
                    XmlText(xmlSpaceAfterTripleSlash),
                    XmlEmptyElement(lessThanToken, inheritdocTagName, attributes: default, slashGreaterThanToken),
                    XmlText(xmlNewLineToken),
                ],
                endOfComment: EndOfDocumentationCommentToken.WithoutTrivia());

            var indentation = sourceText.GetLeadingWhitespaceOfLineAtPosition(node.Span.Start);
            var newLeadingTrivia = TriviaList(
                Whitespace(indentation),
                Trivia(singleLineInheritdocComment));

            // Insert the new trivia after the existing trivia for the member (but before the whitespace indentation
            // trivia on the line it starts on)
            var finalLeadingTrivia = node.GetLeadingTrivia().ToList();
            var insertionIndex = finalLeadingTrivia.Count;

            if (finalLeadingTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia)])
                insertionIndex--;

            finalLeadingTrivia.InsertRange(insertionIndex, newLeadingTrivia);
            editor.ReplaceNode(node, node.WithLeadingTrivia(finalLeadingTrivia));
        }
    }
}
