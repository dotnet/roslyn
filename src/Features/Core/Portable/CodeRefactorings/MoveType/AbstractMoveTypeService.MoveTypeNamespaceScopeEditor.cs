// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{

    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        /// <summary>
        /// Editor that takes a type in a scope and creates a scope beside it. For example, if the type is contained within a namespace 
        /// it will evaluate if the namespace scope needs to be closed and reopened to create a new scope. 
        /// </summary>
        private class MoveTypeNamespaceScopeEditor : Editor
        {
            public MoveTypeNamespaceScopeEditor(TService service, State state, string fileName, CancellationToken cancellationToken)
                : base(service, state, fileName, cancellationToken)
            {
            }

            public override async Task<Solution> GetModifiedSolutionAsync()
            {
                var node = State.TypeNode;
                var documentToEdit = State.SemanticDocument.Document;

                if (node.Parent is TNamespaceDeclarationSyntax namespaceDeclaration)
                {
                    return await GetNamespaceScopeChangedSolutionAsync(namespaceDeclaration, node, documentToEdit, CancellationToken).ConfigureAwait(false);
                }

                return null;
            }

            private static async Task<Solution> GetNamespaceScopeChangedSolutionAsync(
                TNamespaceDeclarationSyntax namespaceDeclaration,
                TTypeDeclarationSyntax typeToMove,
                Document documentToEdit,
                CancellationToken cancellationToken)
            {
                var syntaxFactsService = documentToEdit.GetLanguageService<ISyntaxFactsService>();
                var childNodes = syntaxFactsService.GetMembersOfNamespaceDeclaration(namespaceDeclaration);

                if (childNodes.Count <= 1)
                {
                    return null;
                }

                var editor = await DocumentEditor.CreateAsync(documentToEdit, cancellationToken).ConfigureAwait(false);
                editor.RemoveNode(typeToMove, SyntaxRemoveOptions.KeepNoTrivia);

                var syntaxGenerator = editor.Generator;
                var index = childNodes.IndexOf(typeToMove);

                var itemsBefore = index > 0 ? childNodes.Take(index) : Enumerable.Empty<SyntaxNode>();
                var itemsAfter = index < childNodes.Count - 1 ? childNodes.Skip(index + 1) : Enumerable.Empty<SyntaxNode>();

                var name = syntaxFactsService.GetDisplayName(namespaceDeclaration, DisplayNameOptions.IncludeNamespaces);
                var newNamespaceDeclaration = syntaxGenerator.NamespaceDeclaration(name, WithElasticTrivia(typeToMove)).WithAdditionalAnnotations(NamespaceScopeMovedAnnotation);

                if (itemsBefore.Any() && itemsAfter.Any())
                {
                    var itemsAfterNamespaceDeclaration = syntaxGenerator.NamespaceDeclaration(name, WithElasticTrivia(itemsAfter));

                    foreach (var nodeToRemove in itemsAfter)
                    {
                        editor.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                    }

                    editor.InsertAfter(namespaceDeclaration, new[] { newNamespaceDeclaration, itemsAfterNamespaceDeclaration });
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
                {
                    syntaxNode = syntaxNode.WithLeadingTrivia(syntaxNode.GetLeadingTrivia().Select(SyntaxTriviaExtensions.AsElastic));
                }

                if (trailing && syntaxNode.HasTrailingTrivia)
                {
                    syntaxNode = syntaxNode.WithTrailingTrivia(syntaxNode.GetTrailingTrivia().Select(SyntaxTriviaExtensions.AsElastic));
                }

                return syntaxNode;
            }

            private static IEnumerable<SyntaxNode> WithElasticTrivia(IEnumerable<SyntaxNode> syntaxNodes)
            {
                if (syntaxNodes.Any())
                {
                    var firstNode = syntaxNodes.First();
                    var lastNode = syntaxNodes.Last();

                    if (firstNode == lastNode)
                    {
                        yield return WithElasticTrivia(firstNode);
                    }
                    else
                    {
                        yield return WithElasticTrivia(firstNode, trailing: false);

                        foreach (var node in syntaxNodes.Skip(1))
                        {
                            if (node == lastNode)
                            {
                                yield return WithElasticTrivia(node, leading: false);
                            }
                            else
                            {
                                yield return node;
                            }
                        }
                    }
                }
            }
        }
    }
}
