// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract class AbstractMoveTypeService : IMoveTypeService
    {
        /// <summary>
        /// Annotation to mark the namespace encapsulating the type that has been moved
        /// </summary>
        public static SyntaxAnnotation NamespaceScopeMovedAnnotation = new(nameof(MoveTypeOperationKind.MoveTypeNamespaceScope));

        public abstract Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<CodeAction>> GetRefactoringAsync(Document document, TextSpan textSpan, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken);
    }

    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax> :
        AbstractMoveTypeService
        where TService : AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
        where TTypeDeclarationSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
    {
        public override async Task<ImmutableArray<CodeAction>> GetRefactoringAsync(
            Document document, TextSpan textSpan, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var state = await CreateStateAsync(document, textSpan, fallbackOptions, cancellationToken).ConfigureAwait(false);

            if (state == null)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var actions = CreateActions(state, cancellationToken);
            return actions;
        }

        public override async Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var state = await CreateStateAsync(document, textSpan, fallbackOptions, cancellationToken).ConfigureAwait(false);

            if (state == null)
            {
                return document.Project.Solution;
            }

            var suggestedFileNames = GetSuggestedFileNames(
                state.TypeNode,
                IsNestedType(state.TypeNode),
                state.TypeName,
                state.SemanticDocument.Document.Name,
                state.SemanticDocument.SemanticModel,
                cancellationToken);

            var editor = Editor.GetEditor(operationKind, (TService)this, state, suggestedFileNames.FirstOrDefault(), cancellationToken);
            var modifiedSolution = await editor.GetModifiedSolutionAsync().ConfigureAwait(false);
            return modifiedSolution ?? document.Project.Solution;
        }

        protected abstract Task<TTypeDeclarationSyntax> GetRelevantNodeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        private async Task<State> CreateStateAsync(Document document, TextSpan textSpan, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var nodeToAnalyze = await GetRelevantNodeAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (nodeToAnalyze == null)
            {
                return null;
            }

            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            return State.Generate(semanticDocument, nodeToAnalyze, fallbackOptions, cancellationToken);
        }

        private ImmutableArray<CodeAction> CreateActions(State state, CancellationToken cancellationToken)
        {
            var typeMatchesDocumentName = TypeMatchesDocumentName(
                state.TypeNode,
                state.TypeName,
                state.DocumentNameWithoutExtension,
                state.SemanticDocument.SemanticModel,
                cancellationToken);

            if (typeMatchesDocumentName)
            {
                // if type name matches document name, per style conventions, we have nothing to do.
                return ImmutableArray<CodeAction>.Empty;
            }

            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
            var manyTypes = MultipleTopLevelTypeDeclarationInSourceDocument(state.SemanticDocument.Root);
            var isNestedType = IsNestedType(state.TypeNode);

            var syntaxFacts = state.SemanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var isClassNextToGlobalStatements = manyTypes
                ? false
                : ClassNextToGlobalStatements(state.SemanticDocument.Root, syntaxFacts);

            var suggestedFileNames = GetSuggestedFileNames(
                state.TypeNode,
                isNestedType,
                state.TypeName,
                state.SemanticDocument.Document.Name,
                state.SemanticDocument.SemanticModel,
                cancellationToken);

            // (1) Add Move type to new file code action:
            // case 1: There are multiple type declarations in current document. offer, move to new file.
            // case 2: This is a nested type, offer to move to new file.
            // case 3: If there is a single type decl in current file, *do not* offer move to new file,
            //         rename actions are sufficient in this case.
            // case 4: If there are top level statements(Global statements) offer to move even
            //         in cases where there are only one class in the file.
            if (manyTypes || isNestedType || isClassNextToGlobalStatements)
            {
                foreach (var fileName in suggestedFileNames)
                {
                    actions.Add(GetCodeAction(state, fileName, operationKind: MoveTypeOperationKind.MoveType));
                }
            }

            // (2) Add rename file and rename type code actions:
            // Case: No type declaration in file matches the file name.
            if (!AnyTopLevelTypeMatchesDocumentName(state, cancellationToken))
            {
                foreach (var fileName in suggestedFileNames)
                {
                    actions.Add(GetCodeAction(state, fileName, operationKind: MoveTypeOperationKind.RenameFile));
                }

                // only if the document name can be legal identifier in the language,
                // offer to rename type with document name
                if (state.IsDocumentNameAValidIdentifier)
                {
                    actions.Add(GetCodeAction(
                        state, fileName: state.DocumentNameWithoutExtension,
                        operationKind: MoveTypeOperationKind.RenameType));
                }
            }

            Debug.Assert(actions.Count != 0, "No code actions found for MoveType Refactoring");

            return actions.ToImmutable();
        }

        private static bool ClassNextToGlobalStatements(SyntaxNode root, ISyntaxFactsService syntaxFacts)
            => syntaxFacts.ContainsGlobalStatement(root);

        private CodeAction GetCodeAction(State state, string fileName, MoveTypeOperationKind operationKind)
            => new MoveTypeCodeAction((TService)this, state, operationKind, fileName);

        private static bool IsNestedType(TTypeDeclarationSyntax typeNode)
            => typeNode.Parent is TTypeDeclarationSyntax;

        /// <summary>
        /// checks if there is a single top level type declaration in a document
        /// </summary>
        /// <remarks>
        /// optimized for perf, uses Skip(1).Any() instead of Count() > 1
        /// </remarks>
        private static bool MultipleTopLevelTypeDeclarationInSourceDocument(SyntaxNode root)
            => TopLevelTypeDeclarations(root).Skip(1).Any();

        private static IEnumerable<TTypeDeclarationSyntax> TopLevelTypeDeclarations(SyntaxNode root)
            => root.DescendantNodes(n => n is TCompilationUnitSyntax or TNamespaceDeclarationSyntax)
                .OfType<TTypeDeclarationSyntax>();

        private static bool AnyTopLevelTypeMatchesDocumentName(State state, CancellationToken cancellationToken)
        {
            var root = state.SemanticDocument.Root;
            var semanticModel = state.SemanticDocument.SemanticModel;

            return TopLevelTypeDeclarations(root).Any(
                typeDeclaration =>
                {
                    var typeName = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken).Name;
                    return TypeMatchesDocumentName(
                        typeDeclaration, typeName, state.DocumentNameWithoutExtension,
                        semanticModel, cancellationToken);
                });
        }

        /// <summary>
        /// checks if type name matches its parent document name, per style rules.
        /// </summary>
        /// <remarks>
        /// Note: For a nested type, a matching document name could be just the type name or a
        /// dotted qualified name of its type hierarchy.
        /// </remarks>
        protected static bool TypeMatchesDocumentName(
            TTypeDeclarationSyntax typeNode,
            string typeName,
            string documentNameWithoutExtension,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // If it is not a nested type, we compare the unqualified type name with the document name.
            // If it is a nested type, the type name `Outer.Inner` matches file names `Inner.cs` and `Outer.Inner.cs`
            var namesMatch = typeName.Equals(documentNameWithoutExtension, StringComparison.CurrentCulture);
            if (!namesMatch)
            {
                var typeNameParts = GetTypeNamePartsForNestedTypeNode(typeNode, semanticModel, cancellationToken);
                var fileNameParts = documentNameWithoutExtension.Split('.');

                // qualified type name `Outer.Inner` matches file names `Inner.cs` and `Outer.Inner.cs`
                return typeNameParts.SequenceEqual(fileNameParts, StringComparer.CurrentCulture);
            }

            return namesMatch;
        }

        private static ImmutableArray<string> GetSuggestedFileNames(
            TTypeDeclarationSyntax typeNode,
            bool isNestedType,
            string typeName,
            string documentNameWithExtension,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var fileExtension = Path.GetExtension(documentNameWithExtension);

            var standaloneName = typeName + fileExtension;

            // If it is a nested type, we should match type hierarchy's name parts with the file name.
            if (isNestedType)
            {
                var typeNameParts = GetTypeNamePartsForNestedTypeNode(typeNode, semanticModel, cancellationToken);
                var dottedName = typeNameParts.Join(".") + fileExtension;

                return ImmutableArray.Create(standaloneName, dottedName);
            }
            else
            {
                return ImmutableArray.Create(standaloneName);
            }
        }

        private static IEnumerable<string> GetTypeNamePartsForNestedTypeNode(
            TTypeDeclarationSyntax typeNode, SemanticModel semanticModel, CancellationToken cancellationToken)
                => typeNode.AncestorsAndSelf()
                        .OfType<TTypeDeclarationSyntax>()
                        .Select(n => semanticModel.GetDeclaredSymbol(n, cancellationToken).Name)
                        .Reverse();
    }
}
