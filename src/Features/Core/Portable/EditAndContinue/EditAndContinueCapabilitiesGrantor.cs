// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Grants capabilities. 
/// </summary>
internal sealed class EditAndContinueCapabilitiesGrantor(EditAndContinueCapabilities availableCapabilities)
{
    private readonly EditAndContinueCapabilities _availableCapabilities = availableCapabilities;

    public EditAndContinueCapabilities GrantedCapabilities { get; private set; } = 0;

    public bool Grant(EditAndContinueCapabilities capabilities)
    {
        GrantedCapabilities |= capabilities;
        return (_availableCapabilities & capabilities) == capabilities;
    }

    public bool GrantNewTypeDefinition(INamedTypeSymbol type)
    {
        if (!Grant(EditAndContinueCapabilities.NewTypeDefinition))
        {
            return false;
        }

        if (type.HasExplicitlyImplementedInterfaceMember() && !Grant(EditAndContinueCapabilities.AddExplicitInterfaceImplementation))
        {
            return false;
        }

        return true;
    }
}
