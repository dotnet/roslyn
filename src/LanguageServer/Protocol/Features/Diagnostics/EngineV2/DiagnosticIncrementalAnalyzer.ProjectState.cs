// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// State for diagnostics that belong to a project at given time.
        /// </summary>
        private sealed class ProjectState
        {
            private const string SyntaxStateName = nameof(SyntaxStateName);
            private const string SemanticStateName = nameof(SemanticStateName);
            private const string NonLocalStateName = nameof(NonLocalStateName);

            // project id of this state
            private readonly StateSet _owner;
            private readonly ProjectId _projectId;

            public ProjectState(StateSet owner, ProjectId projectId)
            {
                _owner = owner;
                _projectId = projectId;
            }
        }
    }
}
