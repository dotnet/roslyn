// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract partial class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPullMemberUpOptionsService _service;
        private const int None = 0;

        protected abstract bool IsSelectionValid(TextSpan span, SyntaxNode selectedMemberNode);

        /// <summary>
        /// Test purpose only
        /// </summary>
        protected AbstractPullMemberUpRefactoringProvider(IPullMemberUpOptionsService service)
        {
            _service = service;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectedMemberNode = await GetMatchedNodeAsync(document, context.Span, cancellationToken).ConfigureAwait(false);

            if (selectedMemberNode == null)
            {
                return;
            }

            var selectedMember = semanticModel.GetDeclaredSymbol(selectedMemberNode);
            if (selectedMember == null || selectedMember.ContainingType == null)
            {
                return;
            }

            if (!MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                return;
            }

            if (!IsSelectionValid(context.Span, selectedMemberNode))
            {
                return;
            }

            var allDestinations = FindAllValidDestinations(
                selectedMember,
                document.Project.Solution,
                cancellationToken);
            if (allDestinations.Length == 0)
            {
                return;
            }

            var allActions = allDestinations.Select(destination => MembersPuller.TryComputeCodeAction(document, selectedMember, destination))
                .WhereNotNull().Concat(new PullMemberUpWithDialogCodeAction(document, selectedMember, _service))
                .ToImmutableArray();

            var nestedCodeAction = new CodeActionWithNestedActions(
                string.Format(FeaturesResources.Pull_0_up, selectedMember.ToNameDisplayString()),
                allActions, isInlinable: true);
            context.RegisterRefactoring(nestedCodeAction);
        }

        private ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
            ISymbol selectedMember,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var containingType = selectedMember.ContainingType;
            var allDestinations = selectedMember.IsKind(SymbolKind.Field)
                ? containingType.GetBaseTypes().ToImmutableArray()
                : containingType.AllInterfaces.Concat(containingType.GetBaseTypes()).ToImmutableArray();

            return allDestinations.WhereAsArray(destination => MemberAndDestinationValidator.IsDestinationValid(solution, destination, cancellationToken));
        }

        private async Task<SyntaxNode> GetMatchedNodeAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (span.IsEmpty)
            {
                // root.FindNode() won't return the SyntaxNode contains the identifier in some special cases like the following
                // void Bar[||]();
                // int i[||]= 0;
                // int j[||]=> 100;
                // int k[||]{set; }
                // but refactoring should be provided in for those cases
                var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                var token = await root.SyntaxTree.GetTouchingWordAsync(span.Start, syntaxFactsService, cancellationToken).ConfigureAwait(false);
                if (token.RawKind != None)
                {
                    return root.FindNode(token.Span);
                }
            }

            return root.FindNode(span);
        }
    }
}
