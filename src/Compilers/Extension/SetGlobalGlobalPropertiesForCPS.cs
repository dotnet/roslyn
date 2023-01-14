// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Roslyn.Compilers.Extension
{
    [ExportBuildGlobalPropertiesProvider]
    [AppliesTo("(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ")" + " & " + ProjectCapabilities.LanguageService)]
    public class SetGlobalGlobalPropertiesForCPS : StaticGlobalPropertiesProviderBase
    {
        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public SetGlobalGlobalPropertiesForCPS(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            // Currently the SolutionExists context will always occur before CPS calls this class
            // If this behavior ever changes we will need to modify this class.
            return CompilerPackage.RoslynHive != null
                ? Task.FromResult<IImmutableDictionary<string, string>>(Empty.PropertiesMap.Add("RoslynHive", CompilerPackage.RoslynHive))
                : Task.FromResult<IImmutableDictionary<string, string>>(Empty.PropertiesMap);
        }
    }
}
