// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A <see cref="CodeActionOperation"/> for applying solution changes to a workspace.
    /// </summary>
    public sealed class ApplyChangesOperation : CodeActionOperation
    {
        private readonly Solution _changedSolution;

        public ApplyChangesOperation(Solution changedSolution)
        {
            if (changedSolution == null)
            {
                throw new ArgumentNullException("changedSolution");
            }

            _changedSolution = changedSolution;
        }

        public Solution ChangedSolution
        {
            get { return _changedSolution; }
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            workspace.TryApplyChanges(_changedSolution);
        }
    }
}
