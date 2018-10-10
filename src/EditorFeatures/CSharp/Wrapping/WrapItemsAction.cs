// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping
{
    internal partial class CSharpParameterWrappingCodeRefactoringProvider
    {
        private class WrapItemsAction : DocumentChangeAction
        {
            private readonly string _parentTitle;

            public string SortTitle { get; }

            public WrapItemsAction(string title, string parentTitle, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
                _parentTitle = parentTitle;
                SortTitle = parentTitle + "_" + title;
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                // For preview, we don't want to compute the normal operations.  Specifically, we don't
                // want to compute the stateful operation that tracks which code action was triggered.
                return base.ComputeOperationsAsync(cancellationToken);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var operations = await base.ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);
                var operationsList = operations.ToList();

                operationsList.Add(new RecordCodeActionOperation(this.SortTitle, _parentTitle));
                return operationsList;
            }

            private class RecordCodeActionOperation : CodeActionOperation
            {
                private readonly string _sortTitle;
                private readonly string _parentTitle;

                public RecordCodeActionOperation(string sortTitle, string parentTitle)
                {
                    _sortTitle = sortTitle;
                    _parentTitle = parentTitle;
                }

                internal override bool ApplyDuringTests => false;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    // Record both the sortTitle of the nested action and the tile of the parent
                    // action.  This way we any invocation of a code action helps prioritize both
                    // the parent lists and the nested lists.
                    s_mruTitles = s_mruTitles.Remove(_sortTitle).Remove(_parentTitle)
                                           .Insert(0, _sortTitle).Insert(0, _parentTitle);
                }
            }
        }
    }
}
