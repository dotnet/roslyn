// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueWorkspaceService : IWorkspaceService
    {
        EditSession EditSession { get; }
        DebuggingSession DebuggingSession { get; }

        void StartDebuggingSession(Solution currentSolution);

        void StartEditSession(
            Solution currentSolution,
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatementSpan>> activeStatements,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            bool stoppedAtException);

        void EndEditSession();
        void EndDebuggingSession();

        bool IsProjectReadOnly(string projectName, out SessionReadOnlyReason sessionReason, out ProjectReadOnlyReason projectReason);
    }
}
