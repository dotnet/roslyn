// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace AnalyzerRunner
{
    [ExportWorkspaceService(typeof(IWorkspaceConfigurationService), ServiceLayer.Host), Shared]
    internal sealed class AnalyzerRunnerWorkspaceConfigurationService : IWorkspaceConfigurationService
    {
        public WorkspaceConfigurationOptions Options { get; set; } = WorkspaceConfigurationOptions.Default;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerRunnerWorkspaceConfigurationService()
        {
        }
    }
}
