// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        public readonly SuggestedActionSet NestedActionSet;

        public SuggestedActionWithNestedActions(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider, Workspace workspace,
            ITextBuffer subjectBuffer, object provider,
            CodeAction codeAction, SuggestedActionSet nestedActionSet)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer, provider, codeAction)
        {
            NestedActionSet = nestedActionSet;
        }

        public override bool HasActionSets => true;

        public sealed override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SuggestedActionSet>>(ImmutableArray.Create(NestedActionSet));
    }
}
