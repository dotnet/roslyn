// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax> :
        CodeRefactoringProvider
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;            

            var state = await State.CreateAsync((TService)this, document, textSpan, cancellationToken);
            if (state == null)
            {
                return;
            }

            context.RegisterRefactorings(CreateCodeActions((TService)this, state));
        }

        private static ImmutableArray<CodeAction> CreateCodeActions(TService service, State state)
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
        /// Try to get a replacement node for given node which is a reference to a top-level type declared inside the namespce to be renamed.
        /// If this reference is the right side of a qualified name, the replacement node returned would be the entire qualified name.
        /// If <paramref name="namespaceParts"/> is specified, the replacement node will be qualified with given namespace instead.
        /// </summary>
        /// <param name="reference">A reference to a type declared inside the namespce to be renamed, which is calculated based on results from `SymbolFinder.FindReferencesAsync`.</param>
        /// <param name="namespaceParts">If specified, the namespace of original reference will be replaced with given namespace in the replacement node.</param>
        /// <param name="old">The original reference node to be replaced.</param>
        /// <param name="new">The replacement node.</param>
        abstract protected bool TryGetReplacementSyntax(SyntaxNode reference, ImmutableArray<string> namespaceParts, out SyntaxNode old, out SyntaxNode @new);

        abstract protected ImmutableArray<ISymbol> GetDeclaredSymbolsInContainer(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);

        abstract protected string EscapeIdentifier(string identifier);

        abstract protected SyntaxNode CreateUsingDirective(ImmutableArray<string> namespaceParts);

        abstract protected SyntaxNode ChangeNamespaceDeclaration(SyntaxNode root, ImmutableArray<string> declaredNamespaceParts, ImmutableArray<string> targetNamespaceParts);

        /// <summary>
        /// Determine if this refactoring should be triggered based on current cursor position and if there's any partial type declarations. 
        /// It should only be triggered if the cursor is:
        /// (1) in the name of only namespace declaration
        /// (2) in the name of first declaration in global namespace if there's no namespace declaration in this document.
        /// </summary>
        abstract protected Task<(bool, TNamespaceDeclarationSyntax)> ShouldPositionTriggerRefactoringAsync(Document document, int position, CancellationToken cancellationToken);

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
    }
}
