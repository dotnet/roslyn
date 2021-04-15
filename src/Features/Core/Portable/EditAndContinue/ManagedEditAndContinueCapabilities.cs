// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal class ManagedEditAndContinueCapabilities
    {
        private readonly ManagedEditAndContinueCapability _capabilities;

        // For testing purposes
        internal ManagedEditAndContinueCapabilities(ManagedEditAndContinueCapability capabilities)
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
