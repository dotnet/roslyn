// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PossiblyDeclareAsNullable
{
    /// <summary>
    /// If you apply a null test on a symbol that isn't nullable, then we'll help you make that symbol nullable.
    /// For example: `nonNull == null`, `nonNull?.Property`
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PossiblyDeclareAsNullable), Shared]
    internal class CSharpPossiblyDeclareAsNullableCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        public CSharpPossiblyDeclareAsNullableCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.PossiblyDeclareAsNullableDiagnosticId);

        // No support for FixAll at the moment. For example, in public API code this fix is likely incorrect.
        public override FixAllProvider GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var fixableType = await TryGetFixableTypeAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
            if (fixableType is object)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    c => FixAsync(context.Document, diagnostic, c)),
                    context.Diagnostics);
            }
        }

        private async Task<TypeSyntax> TryGetFixableTypeAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolToFix = CSharpPossiblyDeclareAsNullableDiagnosticAnalyzer.IsFixable(node, model);

            var declarationLocation = symbolToFix.Locations[0];
            var typeNode = declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var typeToFix = TryGetTypeToFix(typeNode);

            if (typeToFix == null || typeToFix is NullableTypeSyntax)
            {
                return null;
            }

            return typeToFix;
        }

        private async Task<Document> FixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);

            var model = await document.GetSemanticModelAsync().ConfigureAwait(false);

            var symbolToFix = CSharpPossiblyDeclareAsNullableDiagnosticAnalyzer.IsFixable(node, model);
            if (symbolToFix == null)
            {
                return document;
            }

            var declarationLocation = symbolToFix.Locations[0];
            var typeNode = declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            var typeToFix = TryGetTypeToFix(typeNode);
            if (typeToFix == null || typeToFix is NullableTypeSyntax)
            {
                return document;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var fixedType = SyntaxFactory.NullableType(typeToFix.WithoutTrivia()).WithTriviaFrom(typeToFix);
            var newRoot = root.ReplaceNode(typeToFix, fixedType);
            return document.WithSyntaxRoot(newRoot);
        }

        private static TypeSyntax TryGetTypeToFix(SyntaxNode node)
        {
            switch (node)
            {
                case ParameterSyntax parameter:
                    return parameter.Type;

                case VariableDeclaratorSyntax declarator:
                    if (declarator.IsParentKind(SyntaxKind.VariableDeclaration))
                    {
                        var declaration = (VariableDeclarationSyntax)declarator.Parent;
                        return declaration.Variables.Count == 1 ? declaration.Type : null;
                    }

                    return null;

                case PropertyDeclarationSyntax property:
                    return property.Type;

                case MethodDeclarationSyntax method:
                    if (method.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        // partial methods should only return void (ie. already an error scenario)
                        return null;
                    }

                    return method.ReturnType;
            }

            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Declare_as_nullable,
                     createChangedDocument,
                     CSharpFeaturesResources.Declare_as_nullable)
            {
            }
        }
    }
}
