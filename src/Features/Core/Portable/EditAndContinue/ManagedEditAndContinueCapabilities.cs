// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http.Headers;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal class ManagedEditAndContinueCapabilities
    {
        internal static readonly ManagedEditAndContinueCapabilities Net5CoreCLR = new(ManagedEditAndContinueCapability.Baseline, ManagedEditAndContinueCapability.AddDefinitionToExistingType, ManagedEditAndContinueCapability.NewTypeDefinition);
        internal static readonly ManagedEditAndContinueCapabilities Net6CoreCLR = new(ManagedEditAndContinueCapability.Baseline, ManagedEditAndContinueCapability.AddDefinitionToExistingType, ManagedEditAndContinueCapability.NewTypeDefinition, ManagedEditAndContinueCapability.RuntimeEdits);
        internal static readonly ManagedEditAndContinueCapabilities Net5MonoVM = new();
        internal static readonly ManagedEditAndContinueCapabilities Net6MonoVM = new(ManagedEditAndContinueCapability.Baseline, ManagedEditAndContinueCapability.RuntimeEdits);

        private readonly bool[] _capabilities;

        private ManagedEditAndContinueCapabilities(params ManagedEditAndContinueCapability[] capabilities)
            : this("")
        {
            foreach (var capability in capabilities)
            {
                _capabilities[(int)capability] = true;
            }
        }

        internal ManagedEditAndContinueCapabilities(string capabilities)
        {
            _capabilities = new bool[(int)ManagedEditAndContinueCapability.Count];

            if (string.IsNullOrEmpty(capabilities))
            {
                return;
            }

            foreach (var capability in capabilities.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse(capability, out ManagedEditAndContinueCapability result) && (int)result < (int)ManagedEditAndContinueCapability.Count)
                {
                    _capabilities[(int)result] = true;
                }
            }
        }

        public bool HasCapability(ManagedEditAndContinueCapability capability)
        {
            return (int)capability < (int)ManagedEditAndContinueCapability.Count && _capabilities[(int)capability];
        }
    }
}
