// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.MSBuild;

namespace AnalyzerRunner
{
    public static class AnalyzerRunnerHelper
    {
        public static MSBuildWorkspace CreateWorkspace()
        {
            var properties = new Dictionary<string, string>
            {
                // Use the latest language version to force the full set of available analyzers to run on the project.
                { "LangVersion", "latest" },
            };

            return MSBuildWorkspace.Create(properties, AnalyzerRunnerMefHostServices.DefaultServices);
        }
    }
}
