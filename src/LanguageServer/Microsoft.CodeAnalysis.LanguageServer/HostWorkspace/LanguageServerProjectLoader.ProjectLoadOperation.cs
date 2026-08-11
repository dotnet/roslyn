// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    internal sealed class ProjectLoadOperation(string? projectGuid)
    {
        public LanguageServerProjectLoadHandle Handle { get; } = new();
        public string? ProjectGuid { get; private set; } = projectGuid;
        public bool EvaluationStarted { get; private set; }

        public string? StartEvaluation()
        {
            EvaluationStarted = true;
            return ProjectGuid;
        }

        public bool TrySetProjectGuid(string projectGuid)
        {
            if (EvaluationStarted || ProjectGuid is not null)
                return false;

            ProjectGuid = projectGuid;
            return true;
        }
    }
}
