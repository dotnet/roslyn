// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract partial class AbstractPullMemberUpRefactoringProvider
    {
        private class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
        {
            private IEnumerable<ISymbol> Members { get; }

            private ISymbol SelectedNodeSymbol { get; }

            private Document ContextDocument { get; }

            private Dictionary<ISymbol, Lazy<ImmutableList<ISymbol>>> LazyDependentsMap { get; }

            public override string Title => FeaturesResources.DotDotDot;

            private readonly IPullMemberUpOptionsService _service;

            internal PullMemberUpWithDialogCodeAction(
                SemanticModel semanticModel,
                CodeRefactoringContext context,
                ISymbol selectedNodeSymbol,
                AbstractPullMemberUpRefactoringProvider provider)
            {
                Members = selectedNodeSymbol.ContainingType.GetMembers().Where(
                    member => {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            return methodSymbol.MethodKind == MethodKind.Ordinary;
                        }
                        else if (member is IFieldSymbol fieldSymbol)
                        {
                            return !member.IsImplicitlyDeclared;
                        }
                        else if (member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });

                var membersSet = new HashSet<ISymbol>(Members);

                // This map contains the content used by select dependents button
                LazyDependentsMap = Members.ToDictionary(
                    memberSymbol => memberSymbol,
                    memberSymbol => new Lazy<ImmutableList<ISymbol>>(
                       () =>
                       {
                           if (memberSymbol.Kind == SymbolKind.Field)
                           {
                               return ImmutableList<ISymbol>.Empty;
                           }
                           else
                           {
                               return SymbolDependentsBuilder.Build(memberSymbol, membersSet, context.Document, context.CancellationToken);
                           }

                       }, false));

                SelectedNodeSymbol = selectedNodeSymbol;
                ContextDocument = context.Document;
                _service = provider._pullMemberUpOptionsService;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var pullMemberUpService = _service ?? ContextDocument.Project.Solution.Workspace.Services.GetService<IPullMemberUpOptionsService>();
                return pullMemberUpService.GetPullTargetAndMembers(SelectedNodeSymbol, Members, LazyDependentsMap);
            }
            
            protected async override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                if (options is PullMemberDialogResult result && !result.IsCanceled)
                {
                    var generator = new CodeActionAndSolutionGenerator();
                    var changedSolution = await generator.GetSolutionAsync(result.PullMembersAnalysisResult, ContextDocument, cancellationToken).ConfigureAwait(false);
                    var operation = new ApplyChangesOperation(changedSolution);
                    return new CodeActionOperation[] { operation };
                }
                else
                {
                    return new CodeActionOperation[0];
                }
            }
        }
    }
}
