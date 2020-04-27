// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PullMemberUp)), Shared]
    internal partial class PullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IMoveMembersOptionService _service;

        /// <summary>
        /// Test purpose only
        /// </summary>
        [ImportingConstructor]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "<Pending>")]
        public PullMemberUpRefactoringProvider(IMoveMembersOptionService service)
            => _service = service;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var (document, textSpan, cancellationToken) = context;

            var moveMembersService = document.GetRequiredLanguageService<AbstractMoveMembersService>();
            var analysis = await moveMembersService.AnalyzeAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (analysis.SelectedMember == null)
            {
                return;
            }

            var allActions = analysis.DestinationAnalysisResults.Select(destination => MembersPuller.TryComputeCodeAction(document, analysis.SelectedMember, analysis.SelectedNode, destination.Destination))
                .WhereNotNull().Concat(new PullMemberUpWithDialogCodeAction(document, analysis, _service))
                .ToImmutableArray();

            var nestedCodeAction = new CodeActionWithNestedActions(
                string.Format(FeaturesResources.Pull_0_up, analysis.SelectedMember.ToNameDisplayString()),
                allActions, isInlinable: true);
            context.RegisterRefactoring(nestedCodeAction, analysis.SelectedNode.Span);
        }
    }
}
