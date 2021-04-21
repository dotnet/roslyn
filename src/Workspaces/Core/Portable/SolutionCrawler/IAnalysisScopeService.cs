// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal interface IAnalysisScopeService : IWorkspaceService
    {
        event EventHandler AnalysisScopeChanged;

        ValueTask<BackgroundAnalysisScope> GetAnalysisScopeAsync(Project project, CancellationToken cancellationToken);

        ValueTask<BackgroundAnalysisScope> GetAnalysisScopeAsync(OptionSet options, string language, CancellationToken cancellationToken);
    }
}
