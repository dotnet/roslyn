// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;

namespace Roslyn.ComponentDebugger
{
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB)]
    public class CapabilityProvider : ConfiguredProjectCapabilitiesProviderBase
    {
        [ImportingConstructor]
        [System.Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public CapabilityProvider(ConfiguredProject configuredProject)
            : base(nameof(CapabilityProvider), configuredProject)
        {
        }

        protected override async Task<ImmutableHashSet<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            // an alternative design could be to have 'IsRoslynComponent' just define the <Capability... directly in the managed.core targets
            // but that would require a specific roslyn version to work, this allows it to be backwards compatible with older SDKs
            var caps = Empty.CapabilitiesSet;
            if (await IsRoslynComponentAsync(this.ConfiguredProject, cancellationToken).ConfigureAwait(false))
            {
                caps = caps.Add(Constants.RoslynComponentCapability);
            }
            return caps;
        }

        private static Task<bool> IsRoslynComponentAsync(ConfiguredProject configuredProject, CancellationToken token = default) 
            => configuredProject.Services.ProjectLockService.ReadLockAsync(
                    async access =>
                    {
                        var project = await access.GetProjectAsync(configuredProject).ConfigureAwait(false);
                        var isRoslynComponentProperty = project.GetProperty(Constants.RoslynComponentPropertyName);
                        var isComponent = string.Compare(isRoslynComponentProperty?.EvaluatedValue.Trim(), "true", System.StringComparison.OrdinalIgnoreCase) == 0;
                        return isComponent;
                    },
                    token);
    }
}
