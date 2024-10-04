// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddInheritdoc;

using static CSharpSyntaxTokens;

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
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        SemanticModel? semanticModel = null;
        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node.Kind() is not SyntaxKind.MethodDeclaration and not SyntaxKind.PropertyDeclaration and not SyntaxKind.VariableDeclarator)
            {
                continue;
            }

            if (node.IsKind(SyntaxKind.VariableDeclarator) && node.Parent?.Parent?.IsKind(SyntaxKind.EventFieldDeclaration) == false)
            {
                continue;
            }

            semanticModel ??= await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (symbol is null)
            {
                continue;
            }

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
        string? newLine = null;
        SourceText? sourceText = null;
        foreach (var diagnostic in diagnostics)
        {
            var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan);
            if (node.IsKind(SyntaxKind.VariableDeclarator) && !(node = node.Parent?.Parent).IsKind(SyntaxKind.EventFieldDeclaration))
            {
                continue;
            }

            if (newLine == null)
            {
                var options = await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
                newLine = options.NewLine;
            }

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

            sourceText ??= await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var indentation = sourceText.GetLeadingWhitespaceOfLineAtPosition(node.FullSpan.Start);
            var newLeadingTrivia = TriviaList(
                Whitespace(indentation),
                Trivia(singleLineInheritdocComment));

            editor.ReplaceNode(node, node.WithPrependedLeadingTrivia(newLeadingTrivia));
        }
    }
}
