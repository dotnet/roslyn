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
        private readonly IProjectSnapshotService snapshotService;

        [ImportingConstructor]
        [System.Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public CapabilityProvider(ConfiguredProject configuredProject, IProjectSnapshotService snapshotService)
            : base(nameof(CapabilityProvider), configuredProject)
        {
            this.snapshotService = snapshotService;
        }

        protected override async Task<ImmutableHashSet<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            // an alternative design could be to have 'IsRoslynComponent' just define the <Capability... directly in the managed.core targets
            // but that would require a specific roslyn version to work, this allows it to be backwards compatible with older SDKs
            var caps = Empty.CapabilitiesSet;

            var snapshot = await snapshotService.GetLatestVersionAsync(ConfiguredProject, cancellationToken: cancellationToken).ConfigureAwait(false);
            var isRoslynComponentProperty = snapshot.Value.ProjectInstance.GetPropertyValue(Constants.RoslynComponentPropertyName);
            var isComponent = string.Compare(isRoslynComponentProperty.Trim(), "true", System.StringComparison.OrdinalIgnoreCase) == 0;
            if (isComponent)
            {
                caps = caps.Add(Constants.RoslynComponentCapability);
            }
            return caps;
        }
    }
}
