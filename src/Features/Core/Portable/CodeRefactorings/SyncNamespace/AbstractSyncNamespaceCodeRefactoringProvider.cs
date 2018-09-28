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
            if (state.QualifiedIdentifierFromDeclaration != null)
            {
                builder.Add(new MoveFileCodeAction(service, state));
            }

            // No change namespace action if we can't construct a valid namespace from rootnamespace and folder names.
            if (state.TargetNamespace != null)
            {
                builder.Add(new RenameNamespaceCodeAction(service, state));
            }

            return builder.ToImmutableAndFree();
        }                      

        abstract protected TNamespaceDeclarationSyntax ChangeNamespace(TNamespaceDeclarationSyntax node, ImmutableArray<string> namespaceParts);

        /// <summary>
        /// Try to get a replacement node for given node which is a reference to a type declared inside the namespce to be renamed.
        /// If this reference is the right side of a qualified name, the replacement node would be the entire qualified name. 
        /// </summary>
        /// <param name="reference">A reference to a type declared inside the namespce to be renamed, which is calculated based on results from `SymbolFinder.FindReferencesAsync`.</param>
        /// <param name="namespaceParts">If specified, the namespace of original reference will be replaced with given namespace in the replacement node.</param>
        /// <param name="old">The original reference node to be replaced.</param>
        /// <param name="new">The replacement node.</param>
        abstract protected bool TryGetReplacementSyntax(SyntaxNode reference, ImmutableArray<string> namespaceParts, out SyntaxNode old, out SyntaxNode @new);

        abstract protected ImmutableArray<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);

        abstract protected string EscapeIdentifier(string identifier);

        abstract protected SyntaxNode CreateUsingDirective(ImmutableArray<string> namespaceParts);

        /// <summary>
        /// Determine if this refactoring should be triggered based on current cursor position. 
        /// It should only be triggered if the cursor is:
        /// (1) in the name of only namespace declaration
        /// (2) in the name of first declaration in global namespace if there's no namespace declaration in this document.
        /// </summary>
        /// <param name="root">Root node of the syntax tree</param>
        /// <param name="position">Current cursor position</param>
        /// <param name="namespaceDeclaration">the namesapce declaration node if there's only one in the document, otherwise null</param>
        abstract protected bool ShouldPositionTriggerRefactoring(SyntaxNode root, int position, out TNamespaceDeclarationSyntax namespaceDeclaration);

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
