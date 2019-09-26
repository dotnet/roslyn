// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Roslyn.Compilers.Extension
{
    [Export(typeof(IProjectGlobalPropertiesProvider))]
    [AppliesTo("(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ")" + " & " + ProjectCapabilities.LanguageService)]
    public class SetGlobalGlobalPropertiesForCPS : StaticGlobalPropertiesProviderBase
    {
        [ImportingConstructor]
        public SetGlobalGlobalPropertiesForCPS(IProjectCommonServices commonServices)
            : base(commonServices)
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
