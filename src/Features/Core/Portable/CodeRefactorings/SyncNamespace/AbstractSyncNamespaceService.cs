﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceService<TNamespaceDeclarationSyntax, TCompilationUnitSyntax> :
       ISyncNamespaceService
       where TNamespaceDeclarationSyntax : SyntaxNode
       where TCompilationUnitSyntax : SyntaxNode
    {
        public async Task<ImmutableArray<CodeAction>> GetRefactoringsAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var state = await State.CreateAsync(this, document, textSpan, cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                return default;
            }

            return CreateCodeActions(this, state);
        }

        public abstract bool TryGetReplacementReferenceSyntax(
            SyntaxNode reference, ImmutableArray<string> newNamespaceParts, ISyntaxFactsService syntaxFacts, out SyntaxNode old, out SyntaxNode @new);

        protected abstract string EscapeIdentifier(string identifier);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="declaredNamespaceParts"></param>
        /// <param name="targetNamespaceParts"></param>
        /// <returns></returns>
        protected abstract SyntaxNode ChangeNamespaceDeclaration(
            SyntaxNode root, ImmutableArray<string> declaredNamespaceParts, ImmutableArray<string> targetNamespaceParts);

        protected abstract IReadOnlyList<SyntaxNode> GetMemberDeclarationsInContainer(SyntaxNode compilationUnitOrNamespaceDecl);

        /// <summary>
        /// Determine if this refactoring should be triggered based on current cursor position and if there's any partial 
        /// type declarations. It should only be triggered if the cursor is:
        ///     (1) in the name of only namespace declaration
        ///     (2) in the name of first declaration in global namespace if there's no namespace declaration in this document.
        /// </summary>
        /// <returns>
        /// A tuple with two items:
        ///     1. a boolean value indicates if the refactoring should be triggered.
        ///     2. the only namespace declaration node in the document, null if none or more than one.
        /// </returns>
        protected abstract Task<(bool shouldTrigger, TNamespaceDeclarationSyntax singleNamespaceDecl)> ShouldPositionTriggerRefactoringAsync(
            Document document, int position, CancellationToken cancellationToken);

        protected SyntaxAnnotation WarningAnnotation
            => CodeActions.WarningAnnotation.Create(
                FeaturesResources.Warning_colon_changing_namespace_may_produce_invalid_code_and_change_code_meaning);

        protected async Task<bool> ContainsPartialTypeWithMultipleDeclarationsAsync(
            Document document, SyntaxNode compilationUnitOrNamespaceDecl, CancellationToken cancellationToken)
        {
            var memberDecls = GetMemberDeclarationsInContainer(compilationUnitOrNamespaceDecl);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            foreach (var memberDecl in memberDecls)
            {
                var memberSymbol = semanticModel.GetDeclaredSymbol(memberDecl, cancellationToken);

                // Simplify the check by assuming no multiple partial declarations in one document
                if (memberSymbol is ITypeSymbol typeSymbol
                    && typeSymbol.DeclaringSyntaxReferences.Length > 1
                    && semanticFacts.IsPartial(typeSymbol, cancellationToken))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Try get the relative namespace for <paramref name="namespace"/> based on <paramref name="relativeTo"/>,
        /// if <paramref name="relativeTo"/> is the containing namespace of <paramref name="namespace"/>.
        /// For example:
        /// - If <paramref name="relativeTo"/> is "A.B" and <paramref name="namespace"/> is "A.B.C.D", then
        /// the relative namespace is "C.D".
        /// - If <paramref name="relativeTo"/> is "A.B" and <paramref name="namespace"/> is also "A.B", then
        /// the relative namespace is "".
        /// - If <paramref name="relativeTo"/> is "" then the relative namespace us <paramref name="namespace"/>.
        /// </summary>
        private static string GetRelativeNamespace(string relativeTo, string @namespace, ISyntaxFactsService syntaxFacts)
        {
            Debug.Assert(relativeTo != null && @namespace != null);

            if (syntaxFacts.StringComparer.Equals(@namespace, relativeTo))
            {
                return string.Empty;
            }
            else if (relativeTo.Length == 0)
            {
                return @namespace;
            }

            var containingText = relativeTo + ".";
            if (syntaxFacts.IsCaseSensitive
                ? @namespace.StartsWith(containingText, StringComparison.Ordinal)       // For C#
                : CaseInsensitiveComparison.StartsWith(@namespace, containingText))     // for VB
            {
                return @namespace.Substring(relativeTo.Length + 1);
            }

            return null;
        }

        private static ImmutableArray<CodeAction> CreateCodeActions(
            AbstractSyncNamespaceService<TNamespaceDeclarationSyntax, TCompilationUnitSyntax> service, State state)
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
    }
}
