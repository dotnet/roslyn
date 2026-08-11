// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal enum LspSolutionContextPreference
{
    NoPreference,
    Project,
    ProjectAndDependencies,
    Workspace,
}

internal enum LspSolutionContextCompleteness
{
    NotEvaluated,
    None,
    Miscellaneous,
    Project,
    ProjectAndDependencies,
    Workspace,
}

internal interface ISolutionRequiredHandler
{
    bool RequiresLSPSolution { get; }

    LspSolutionContextPreference SolutionContextPreference
        => RequiresLSPSolution ? LspSolutionContextPreference.ProjectAndDependencies : LspSolutionContextPreference.NoPreference;
}
