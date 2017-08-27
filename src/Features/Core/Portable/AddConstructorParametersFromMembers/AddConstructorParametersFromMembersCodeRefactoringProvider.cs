// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.GenerateOverrides)]
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await this.AddConstructorParametersFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        public async Task<ImmutableArray<CodeAction>> AddConstructorParametersFromMembersAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_AddConstructorParametersFromMembers, cancellationToken))
            {
                var info = await this.GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    var state = State.Generate(this, document, textSpan, info.SelectedMembers, cancellationToken);
                    if (state != null && state.MatchingConstructor == null)
                    {
                        return CreateCodeActions(document, state).AsImmutableOrNull();
                    }
                }

                return default;
            }
        }

        private IEnumerable<CodeAction> CreateCodeActions(Document document, State state)
        {
            var lastParameter = state.DelegatedConstructor.Parameters.Last();
            if (!lastParameter.IsOptional)
            {
                yield return new AddConstructorParametersCodeAction(this, document, state, state.Parameters);
            }

            var parameters = state.Parameters.Select(p => CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: p.RefKind,
                isParams: p.IsParams,
                type: p.Type,
                name: p.Name,
                isOptional: true,
                hasDefaultValue: true)).ToList();

            yield return new AddConstructorParametersCodeAction(this, document, state, parameters);
        }
    }
}
