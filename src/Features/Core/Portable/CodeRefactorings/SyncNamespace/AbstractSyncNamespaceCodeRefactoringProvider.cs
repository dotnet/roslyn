// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax> :
        CodeRefactoringProvider
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;            

            var state = await State.CreateAsync(this, document, textSpan, cancellationToken);
            if (state == null)
            {
                return;
            }

            context.RegisterRefactorings(CreateCodeActions(this, state));
        }

        private static ImmutableArray<CodeAction> CreateCodeActions(AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax> service, State state)
        {
            var builder = ArrayBuilder<CodeAction>.GetInstance();

            // No move file action if rootnamespace isn't a prefix of current declared namespace
            if (state.RelativeDeclaredNamespace != null)
            {
                builder.AddRange(MoveFileCodeAction.Create(state));
            }

            // No change namespace action if we can't construct a valid namespace from rootnamespace and folder names.
            if (state.TargetNamespace != null)
            {
                builder.Add(new ChangeNamespaceCodeAction(service, state));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Try to get a new node to replace given node, which is a reference to a top-level type declared inside the namespce to be changed.
        /// If this reference is the right side of a qualified name, the new node returned would be the entire qualified name. Depends on 
        /// whether <paramref name="newNamespaceParts"/> is provided, the name in the new node might be qualified with this new namespace instead.
        /// </summary>
        /// <param name="reference">A reference to a type declared inside the namespce to be changed, which is calculated based on results from `SymbolFinder.FindReferencesAsync`.</param>
        /// <param name="newNamespaceParts">If specified, the namespace of original reference will be replaced with given namespace in the replacement node.</param>
        /// <param name="old">The node to be replaced. This might be an ancestor of original </param>
        /// <param name="new">The replacement node.</param>
        abstract protected bool TryGetReplacementReferenceSyntax(SyntaxNode reference, ImmutableArray<string> newNamespaceParts, out SyntaxNode old, out SyntaxNode @new);

        abstract protected string EscapeIdentifier(string identifier);

        abstract protected SyntaxNode CreateUsingDirective(ImmutableArray<string> namespaceParts);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="declaredNamespaceParts"></param>
        /// <param name="targetNamespaceParts"></param>
        /// <returns></returns>
        abstract protected SyntaxNode ChangeNamespaceDeclaration(SyntaxNode root, ImmutableArray<string> declaredNamespaceParts, ImmutableArray<string> targetNamespaceParts);

        abstract protected IReadOnlyList<SyntaxNode> GetMemberDeclarationsInContainer(SyntaxNode node);

        /// <summary>
        /// Determine if this refactoring should be triggered based on current cursor position and if there's any partial type declarations. 
        /// It should only be triggered if the cursor is:
        /// (1) in the name of only namespace declaration
        /// (2) in the name of first declaration in global namespace if there's no namespace declaration in this document.
        /// </summary>
        abstract protected Task<(bool, TNamespaceDeclarationSyntax)> ShouldPositionTriggerRefactoringAsync(Document document, int position, CancellationToken cancellationToken);

        // Add comments
        private static bool TryGetRelativeNamespace(string relativeTo, string @namespace, out string relativeNamespace)
        {
            Debug.Assert(relativeTo != null && @namespace != null);

            if (string.Equals(@namespace, relativeTo, StringComparison.Ordinal))
            {
                relativeNamespace = string.Empty;
            }
            else if (@namespace.StartsWith(relativeTo + ".", StringComparison.Ordinal))
            {
                relativeNamespace = @namespace.Substring(relativeTo.Length + 1);
            }
            else if (relativeTo.Length == 0)
            {
                relativeNamespace = @namespace;
            }
            else
            {
                relativeNamespace = null;
            }
            return relativeNamespace == null;
        }

        protected static bool IsGlobalNamespace(ImmutableArray<string> parts)
        {
            return !parts.IsDefaultOrEmpty && parts.Length == 1 && parts[1].Length == 0;
        }
    }
}
