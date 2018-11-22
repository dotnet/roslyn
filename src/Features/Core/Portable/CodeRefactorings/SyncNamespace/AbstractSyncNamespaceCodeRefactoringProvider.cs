// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax>
        : CodeRefactoringProvider
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;
            
            var state = await State.CreateAsync(this, document, textSpan, cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                return;
            }

            // No move file action if rootnamespace isn't a prefix of current declared namespace
            if (state.RelativeDeclaredNamespace != null)
            {
                context.RegisterRefactorings(MoveFileCodeAction.Create(state));
            }

            // No change namespace action if we can't construct a valid namespace from rootnamespace and folder names.
            if (state.TargetNamespace != null)
            {
                var service = document.GetLanguageService<IChangeNamespaceService>();
                var solutionChangeAction = new SolutionChangeAction(ChangeNamespaceActionTitle(state), token => service.ChangeNamespaceAsync(state.Solution, state.DocumentIds, state.DeclaredNamespace, state.TargetNamespace, token));
                context.RegisterRefactoring(solutionChangeAction);
            }            
        }

        /// <summary>
        /// Determine if this refactoring should be triggered based on current cursor position and if there's any partial 
        /// type declarations. It should only be triggered if the cursor is:
        ///     (1) in the name of only namespace declaration
        ///     (2) in the name of first declaration in global namespace if there's no namespace declaration in this document.
        /// </summary>
        /// <returns>
        /// If the refactoring should be triggered, then returns the only namespace declaration node in the document (of type 
        /// <typeparamref name="TNamespaceDeclarationSyntax"/>) or the compilation unit node (of type <typeparamref name="TCompilationUnitSyntax"/>)
        /// if no namespace declaration in the document. Otherwise, return null.
        /// </returns>
        protected abstract Task<SyntaxNode> TryGetApplicableInvocationNode(Document document, int position, CancellationToken cancellationToken);

        protected abstract string EscapeIdentifier(string identifier);

        protected abstract SyntaxList<TMemberDeclarationSyntax> GetMemberDeclarationsInContainer(SyntaxNode compilationUnitOrNamespaceDecl);

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

        private static string ChangeNamespaceActionTitle(State state)
            => state.TargetNamespace.Length == 0
                ? FeaturesResources.Change_to_global_namespace
                : string.Format(FeaturesResources.Change_namespace_to_0, state.TargetNamespace);

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
            else if (relativeTo.Length >= @namespace.Length)
            {
                return null;
            }

            var containingText = relativeTo + ".";
            var namespacePrefix = @namespace.Substring(0, containingText.Length);

            return syntaxFacts.StringComparer.Equals(containingText, namespacePrefix)
                ? @namespace.Substring(relativeTo.Length + 1)
                : null;
        }
    }
}
