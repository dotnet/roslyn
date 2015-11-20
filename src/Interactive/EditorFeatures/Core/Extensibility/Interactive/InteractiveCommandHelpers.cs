// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    internal static class InteractiveCommandHelpers
    {
        internal static async Task<Compilation> FindSubmissionCompilationAsync(Project submissionProject)
        {
            var solution = submissionProject.Solution;
            while (true)
            {
                var compilation = await submissionProject.GetCompilationAsync();
                if (compilation?.ScriptCompilationInfo != null)
                {
                    return compilation;
                }

                // Even if we seed-from-project with ProjectReferences, they should be from another solution.
                var previousSubmissionReference = submissionProject.ProjectReferences.SingleOrDefault();
                if (previousSubmissionReference == null)
                {
                    return null;
                }

                submissionProject = solution.GetProject(previousSubmissionReference.ProjectId);
            }
        }
    }
}

