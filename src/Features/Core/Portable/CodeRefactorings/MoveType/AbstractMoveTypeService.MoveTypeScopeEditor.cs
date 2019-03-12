using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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
        private class MoveTypeScopeEditor : Editor
        {
            public MoveTypeScopeEditor(TService service, State state, string fileName, CancellationToken cancellationToken)
                : base(service, state, fileName, cancellationToken)
            {
            }

            internal override async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
            {
                var node = State.TypeNode;
                var documentToEdit = State.SemanticDocument.Document;

                var parent = node.Parent;

                if (parent == null)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                CodeActionOperation operationToPerform = null;
                switch (parent)
                {
                    case TNamespaceDeclarationSyntax namespaceDeclaration:
                        operationToPerform = await GetNamespaceScopeOperationAsync(namespaceDeclaration, node, documentToEdit, CancellationToken.None).ConfigureAwait(false);
                        break;
                    case TTypeDeclarationSyntax _:
                        Debug.Assert(false, "Moving a nested type is not supported");
                        break;
                }

                if (operationToPerform == null)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                return ImmutableArray.Create(operationToPerform);
            }

            private async Task<CodeActionOperation> GetNamespaceScopeOperationAsync(TNamespaceDeclarationSyntax namespaceDeclaration, TTypeDeclarationSyntax typeToMove, Document documentToEdit, CancellationToken cancellationToken)
            {
                var syntaxFactsService = documentToEdit.GetLanguageService<ISyntaxFactsService>();
                var childNodes = syntaxFactsService.GetMembersOfNamespaceDeclaration(namespaceDeclaration);
                if (childNodes.Count <= 1)
                {
                    return null;
                }

                var editor = await DocumentEditor.CreateAsync(documentToEdit, cancellationToken).ConfigureAwait(false);
                var syntaxGenerator = editor.Generator;

                var index = childNodes.IndexOf(typeToMove);

                var itemsBefore = index > 0 ? childNodes.Take(index) : Enumerable.Empty<SyntaxNode>();
                var itemsAfter = index < childNodes.Count - 1 ? childNodes.Skip(index + 1) : Enumerable.Empty<SyntaxNode>();

                var name = syntaxFactsService.GetDisplayName(namespaceDeclaration, DisplayNameOptions.IncludeNamespaces);

                var newNamespaceDeclaration = syntaxGenerator.NamespaceDeclaration(name, WithElasticTrivia(typeToMove));
                editor.RemoveNode(typeToMove, SyntaxRemoveOptions.KeepNoTrivia);

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
                else
                {
                    throw new Exception("WTF Happened");
                }


                return new ApplyChangesOperation(editor.GetChangedDocument().Project.Solution);
            }

            private SyntaxNode WithElasticTrivia(SyntaxNode syntaxNode, bool leading = true, bool trailing = true)
            {
                if (leading && syntaxNode.HasLeadingTrivia)
                {
                    syntaxNode = syntaxNode.WithLeadingTrivia(syntaxNode.GetLeadingTrivia().Select(AsElasticTrivia));
                }

                if (trailing && syntaxNode.HasTrailingTrivia)
                {
                    syntaxNode = syntaxNode.WithTrailingTrivia(syntaxNode.GetTrailingTrivia().Select(AsElasticTrivia));
                }

                return syntaxNode.WithAdditionalAnnotations(Formatter.Annotation);
            }

            private IEnumerable<SyntaxNode> WithElasticTrivia(IEnumerable<SyntaxNode> syntaxNodes)
            {
                if (syntaxNodes.Any())
                {
                    var firstNode = syntaxNodes.FirstOrDefault();
                    var lastNode = syntaxNodes.LastOrDefault();

                    if (firstNode == lastNode)
                    {
                        yield return WithElasticTrivia(firstNode);
                    }
                    else
                    {
                        yield return WithElasticTrivia(firstNode, trailing: false);

                        foreach (var node in syntaxNodes.Skip(1))
                        {
                            if (node != lastNode)
                            {
                                yield return node;
                            }
                            else
                            {
                                yield return WithElasticTrivia(node, leading: false);
                            }
                        }
                    }
                }
            }

            private SyntaxTrivia AsElasticTrivia(SyntaxTrivia trivia)
            {
                return trivia.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);
            }
        }
    }
}
