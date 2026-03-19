// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<
    TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
{
    /// <summary>
    /// Editor that takes a type in a scope and creates a scope beside it. For example, if the type is contained within a namespace 
    /// it will evaluate if the namespace scope needs to be closed and reopened to create a new scope. 
    /// </summary>
    private sealed class MoveTypeNamespaceScopeEditor(
        TService service,
        SemanticDocument document,
        TTypeDeclarationSyntax typeDeclaration,
        string fileName,
        CancellationToken cancellationToken)
        : Editor(service, document, typeDeclaration, fileName, cancellationToken)
    {
        public override async Task<Solution?> GetModifiedSolutionAsync()
        {
            return TypeDeclaration.Parent is TNamespaceDeclarationSyntax namespaceDeclaration
                ? await GetNamespaceScopeChangedSolutionAsync(namespaceDeclaration).ConfigureAwait(false)
                : null;
        }

        private async Task<Solution?> GetNamespaceScopeChangedSolutionAsync(
            TNamespaceDeclarationSyntax namespaceDeclaration)
        {
            var syntaxFactsService = SemanticDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var childNodes = syntaxFactsService.GetMembersOfBaseNamespaceDeclaration(namespaceDeclaration);

            if (childNodes.Count <= 1)
                return null;

            var editor = await DocumentEditor.CreateAsync(SemanticDocument.Document, this.CancellationToken).ConfigureAwait(false);
            editor.RemoveNode(this.TypeDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
            var generator = editor.Generator;

            var index = childNodes.IndexOf(this.TypeDeclaration);

            var itemsBefore = childNodes.Take(index).ToImmutableArray();
            var itemsAfter = childNodes.Skip(index + 1).ToImmutableArray();

            var name = syntaxFactsService.GetDisplayName(namespaceDeclaration, DisplayNameOptions.IncludeNamespaces);
            var newNamespaceDeclaration = generator.NamespaceDeclaration(name, WithElasticTrivia(this.TypeDeclaration)).WithAdditionalAnnotations(NamespaceScopeMovedAnnotation);

            if (itemsBefore.Any() && itemsAfter.Any())
            {
                var itemsAfterNamespaceDeclaration = generator.NamespaceDeclaration(name, WithElasticTrivia(itemsAfter));

                foreach (var nodeToRemove in itemsAfter)
                    editor.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepNoTrivia);

                editor.InsertAfter(namespaceDeclaration, [newNamespaceDeclaration, itemsAfterNamespaceDeclaration]);
            }
            else if (itemsBefore.Any())
            {
                editor.InsertAfter(namespaceDeclaration, newNamespaceDeclaration);

                var nodeToCleanup = itemsBefore.Last();
                editor.ReplaceNode(nodeToCleanup, WithElasticTrivia(nodeToCleanup, leading: false));
            }
            else if (itemsAfter.Any())
            {
                editor.InsertBefore(namespaceDeclaration, newNamespaceDeclaration);

                var nodeToCleanup = itemsAfter.First();
                editor.ReplaceNode(nodeToCleanup, WithElasticTrivia(nodeToCleanup, trailing: false));
            }

            var changedDocument = editor.GetChangedDocument();
            return changedDocument.Project.Solution;
        }

        private static SyntaxNode WithElasticTrivia(SyntaxNode syntaxNode, bool leading = true, bool trailing = true)
        {
            if (leading && syntaxNode.HasLeadingTrivia)
                syntaxNode = syntaxNode.WithLeadingTrivia(syntaxNode.GetLeadingTrivia().Select(SyntaxTriviaExtensions.AsElastic));

            if (trailing && syntaxNode.HasTrailingTrivia)
                syntaxNode = syntaxNode.WithTrailingTrivia(syntaxNode.GetTrailingTrivia().Select(SyntaxTriviaExtensions.AsElastic));

            return syntaxNode;
        }

        private static ImmutableArray<SyntaxNode> WithElasticTrivia(ImmutableArray<SyntaxNode> syntaxNodes)
        {
            var result = new FixedSizeArrayBuilder<SyntaxNode>(syntaxNodes.Length);
            for (int i = 0, n = syntaxNodes.Length; i < n; i++)
            {
                var node = syntaxNodes[i];
                if (i == 0)
                    node = WithElasticTrivia(node, leading: true);

                if (i == n - 1)
                    node = WithElasticTrivia(node, trailing: true);

                result.Add(node);
            }

            return result.MoveToImmutable();
        }
    }
}
