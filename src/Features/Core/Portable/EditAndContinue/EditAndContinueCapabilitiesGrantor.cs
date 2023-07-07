// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Grants capabilities. 
    /// </summary>
    internal sealed class EditAndContinueCapabilitiesGrantor
    {
        private readonly EditAndContinueCapabilities _availableCapabilities;

        public EditAndContinueCapabilities GrantedCapabilities { get; private set; }

        public EditAndContinueCapabilitiesGrantor(EditAndContinueCapabilities availableCapabilities)
        {
            _availableCapabilities = availableCapabilities;
            GrantedCapabilities = 0;
        }

        public bool Grant(EditAndContinueCapabilities capabilities)
        {
            GrantedCapabilities |= capabilities;
            return (_availableCapabilities & capabilities) == capabilities;
        }
    }
}
