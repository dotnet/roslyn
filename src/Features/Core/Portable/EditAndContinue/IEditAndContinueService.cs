// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueService
    {
        EditSession EditSession { get; }
        DebuggingSession DebuggingSession { get; }

        void StartDebuggingSession(Solution currentSolution);

        void StartEditSession(
            Solution currentSolution,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            bool stoppedAtException);

        void EndEditSession(ImmutableArray<(ActiveInstructionId, LinePositionSpan)> updatedActiveStatementSpansOpt);

        void EndDebuggingSession();

        bool IsProjectReadOnly(ProjectId id, out SessionReadOnlyReason sessionReason, out ProjectReadOnlyReason projectReason);
    }
}
