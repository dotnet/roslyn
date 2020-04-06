// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class CreateTestAccessorFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RoslynDiagnosticIds.CreateTestAccessorRuleId);

        public sealed override FixAllProvider? GetFixAllProvider()
        {
            // This is a refactoring for one-off test accessor creation. Batch fixing is disabled.
            return null;
        }

        protected abstract SyntaxNode GetTypeDeclarationForNode(SyntaxNode reportedNode);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var syntaxTree = diagnostic.Location.SourceTree;
                var syntaxRoot = await syntaxTree.GetRootAsync(context.CancellationToken).ConfigureAwait(false);
                var reportedNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var typeDeclaration = GetTypeDeclarationForNode(reportedNode);
                var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken);
                if (type.GetTypeMembers(CreateTestAccessor.TestAccessorTypeName).Any())
                {
                    continue;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        RoslynDiagnosticsAnalyzersResources.CreateTestAccessorMessage,
                        cancellationToken => CreateTestAccessorAsync(context.Document, diagnostic, cancellationToken),
                        nameof(CreateTestAccessorFixer)),
                    diagnostic);
            }
        }

        private async Task<Document> CreateTestAccessorAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxTree = diagnostic.Location.SourceTree;
            var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var reportedNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var typeDeclaration = GetTypeDeclarationForNode(reportedNode);
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var newTestAccessorExpression = syntaxGenerator.ObjectCreationExpression(
                syntaxGenerator.IdentifierName(CreateTestAccessor.TestAccessorTypeName),
                syntaxGenerator.ThisExpression());
            var getTestAccessorMethod = syntaxGenerator.MethodDeclaration(
                CreateTestAccessor.GetTestAccessorMethodName,
                returnType: syntaxGenerator.IdentifierName(CreateTestAccessor.TestAccessorTypeName),
                accessibility: Accessibility.Internal,
                statements: new[] { syntaxGenerator.ReturnStatement(newTestAccessorExpression) });

            var parameterName = char.ToLowerInvariant(type.Name[0]) + type.Name.Substring(1);
            var fieldName = "_" + parameterName;
            var testAccessorField = syntaxGenerator.FieldDeclaration(
                fieldName,
                syntaxGenerator.TypeExpression(type),
                Accessibility.Private,
                DeclarationModifiers.ReadOnly);
            var testAccessorConstructor = syntaxGenerator.ConstructorDeclaration(
                containingTypeName: CreateTestAccessor.TestAccessorTypeName,
                parameters: new[] { syntaxGenerator.ParameterDeclaration(parameterName, syntaxGenerator.TypeExpression(type)) },
                accessibility: Accessibility.Internal,
                statements: new[] { syntaxGenerator.AssignmentStatement(syntaxGenerator.IdentifierName(fieldName), syntaxGenerator.IdentifierName(parameterName)) });
            var testAccessorType = syntaxGenerator.StructDeclaration(
                CreateTestAccessor.TestAccessorTypeName,
                accessibility: Accessibility.Internal,
                modifiers: DeclarationModifiers.ReadOnly,
                members: new[] { testAccessorField, testAccessorConstructor });

            var newTypeDeclaration = syntaxGenerator.AddMembers(typeDeclaration, getTestAccessorMethod, testAccessorType);
            return document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, newTypeDeclaration));
        }
    }
}
