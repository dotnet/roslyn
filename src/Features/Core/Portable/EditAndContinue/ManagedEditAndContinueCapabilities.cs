// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal class ManagedEditAndContinueCapabilities
    {
        internal static readonly ManagedEditAndContinueCapabilities Net5CoreCLR = new(ManagedEditAndContinueCapability.Baseline | ManagedEditAndContinueCapability.AddDefinitionToExistingType | ManagedEditAndContinueCapability.NewTypeDefinition);
        internal static readonly ManagedEditAndContinueCapabilities Net6CoreCLR = new(ManagedEditAndContinueCapability.Baseline | ManagedEditAndContinueCapability.AddDefinitionToExistingType | ManagedEditAndContinueCapability.NewTypeDefinition | ManagedEditAndContinueCapability.RuntimeEdits);
        internal static readonly ManagedEditAndContinueCapabilities Net5MonoVM = new(ManagedEditAndContinueCapability.None);
        internal static readonly ManagedEditAndContinueCapabilities Net6MonoVM = new(ManagedEditAndContinueCapability.Baseline | ManagedEditAndContinueCapability.RuntimeEdits);

        private readonly ManagedEditAndContinueCapability _capabilities;

        private ManagedEditAndContinueCapabilities(ManagedEditAndContinueCapability capabilities)
        {
            _capabilities = capabilities;
        }

        internal ManagedEditAndContinueCapabilities(string? capabilities)
        {
            var caps = ManagedEditAndContinueCapability.None;

            if (!string.IsNullOrEmpty(capabilities))
            {
                foreach (var capability in capabilities.Split(' '))
                {
                    caps |= ParseCapability(capability);
                }
            }

            _capabilities = caps;
        }

        private static ManagedEditAndContinueCapability ParseCapability(string capability)
            => capability switch
            {
                "Baseline" => ManagedEditAndContinueCapability.Baseline,
                "AddDefinitionToExistingType" => ManagedEditAndContinueCapability.AddDefinitionToExistingType,
                "NewTypeDefinition" => ManagedEditAndContinueCapability.NewTypeDefinition,
                "RuntimeEdits" => ManagedEditAndContinueCapability.RuntimeEdits,

                _ => ManagedEditAndContinueCapability.None
            };

        public bool HasCapability(ManagedEditAndContinueCapability capability)
            => (_capabilities & capability) == capability;
    }
}
