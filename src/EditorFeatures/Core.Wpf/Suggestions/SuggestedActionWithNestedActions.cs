// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Lightbulb item that has child items that should be displayed as 'menu items'
    /// (as opposed to 'flavor items').
    /// </summary>
    internal sealed class SuggestedActionWithNestedActions : SuggestedAction
    {
        public readonly ImmutableArray<SuggestedActionSet> NestedActionSets;

        public SuggestedActionWithNestedActions(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            Solution originalSolution,
            ITextBuffer subjectBuffer,
            object provider,
            CodeAction codeAction,
            ImmutableArray<SuggestedActionSet> nestedActionSets)
            : base(threadingContext, sourceProvider, workspace, originalSolution, subjectBuffer, provider, codeAction)
        {
            Debug.Assert(!nestedActionSets.IsDefaultOrEmpty);
            NestedActionSets = nestedActionSets;
        }

        public SuggestedActionWithNestedActions(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            Solution originalSolution,
            ITextBuffer subjectBuffer,
            object provider,
            CodeAction codeAction,
            SuggestedActionSet nestedActionSet)
            : this(threadingContext, sourceProvider, workspace, originalSolution, subjectBuffer, provider, codeAction, [nestedActionSet])
        {
        }

        public override bool HasActionSets => true;

        public sealed override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SuggestedActionSet>>(NestedActionSets);

        protected override Task InnerInvokeAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            // A code action with nested actions is itself never invokable.  So just do nothing if this ever gets asked.
            // Report a message in debug and log a watson exception so that if this is hit we can try to narrow down how
            // this happened.
            Debug.Fail($"{nameof(InnerInvokeAsync)} should not be called on a {nameof(SuggestedActionWithNestedActions)}");
            FatalError.ReportAndCatch(new InvalidOperationException($"{nameof(InnerInvokeAsync)} should not be called on a {nameof(SuggestedActionWithNestedActions)}"), ErrorSeverity.Critical);

            return Task.CompletedTask;
        }
    }
}
