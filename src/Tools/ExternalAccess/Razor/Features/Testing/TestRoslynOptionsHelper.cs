// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal static class TestRoslynOptionsHelper
    {
        public static void SetAutomaticSourceGeneratorExecution(Workspace workspace)
        {
            var globalOptions = workspace.CurrentSolution.Services.ExportProvider.GetService<IGlobalOptionService>();
            globalOptions.SetGlobalOption(WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution, SourceGeneratorExecutionPreference.Automatic);
        }
    }
}
