// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractCreateTestAccessor<TTypeDeclarationSyntax> : CodeRefactoringProvider
        where TTypeDeclarationSyntax : SyntaxNode
    {
        protected AbstractCreateTestAccessor()
        {
        }

        private protected abstract IRefactoringHelpers RefactoringHelpers { get; }

        protected abstract SyntaxNode GetTypeDeclarationForNode(SyntaxNode reportedNode);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var type = await context.TryGetRelevantNodeAsync<TTypeDeclarationSyntax>(RefactoringHelpers).ConfigureAwait(false);
            if (type is null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var typeSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(type, context.CancellationToken);
            if (!IsClassOrStruct(typeSymbol))
                return;

            if (typeSymbol.GetTypeMembers(TestAccessorHelper.TestAccessorTypeName).Any())
                return;

            var location = typeSymbol.Locations.FirstOrDefault(location => location.IsInSource && Equals(location.SourceTree, semanticModel.SyntaxTree));
            if (location is null)
                return;

            context.RegisterRefactoring(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.CreateTestAccessorMessage,
                    cancellationToken => CreateTestAccessorAsync(context.Document, location.SourceSpan, cancellationToken),
                    nameof(AbstractCreateTestAccessor<>)));
        }

        private static bool IsClassOrStruct(ITypeSymbol typeSymbol)
            => typeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct;

        private async Task<Document> CreateTestAccessorAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var reportedNode = syntaxRoot.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var typeDeclaration = GetTypeDeclarationForNode(reportedNode);
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var newTestAccessorExpression = syntaxGenerator.ObjectCreationExpression(
                syntaxGenerator.IdentifierName(TestAccessorHelper.TestAccessorTypeName),
                syntaxGenerator.ThisExpression());
            var getTestAccessorMethod = syntaxGenerator.MethodDeclaration(
                TestAccessorHelper.GetTestAccessorMethodName,
                returnType: syntaxGenerator.IdentifierName(TestAccessorHelper.TestAccessorTypeName),
                accessibility: Accessibility.Internal,
                statements: new[] { syntaxGenerator.ReturnStatement(newTestAccessorExpression) });

            var parameterName = "instance";
            var fieldName = "_" + parameterName;
            var testAccessorField = syntaxGenerator.FieldDeclaration(
                fieldName,
                syntaxGenerator.TypeExpression(type),
                Accessibility.Private,
                DeclarationModifiers.ReadOnly);
            var testAccessorConstructor = syntaxGenerator.ConstructorDeclaration(
                containingTypeName: TestAccessorHelper.TestAccessorTypeName,
                parameters: new[] { syntaxGenerator.ParameterDeclaration(parameterName, syntaxGenerator.TypeExpression(type)) },
                accessibility: Accessibility.Internal,
                statements: new[] { syntaxGenerator.AssignmentStatement(syntaxGenerator.IdentifierName(fieldName), syntaxGenerator.IdentifierName(parameterName)) });
            var testAccessorType = syntaxGenerator.StructDeclaration(
                TestAccessorHelper.TestAccessorTypeName,
                accessibility: Accessibility.Internal,
                modifiers: DeclarationModifiers.ReadOnly,
                members: new[] { testAccessorField, testAccessorConstructor });

            var newTypeDeclaration = syntaxGenerator.AddMembers(typeDeclaration, getTestAccessorMethod, testAccessorType);
            return document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, newTypeDeclaration));
        }
    }
}
