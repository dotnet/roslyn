// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// The capabilities that the runtime has with respect to edit and continue
/// </summary>
[Flags]
internal enum EditAndContinueCapabilities
{
    None = 0,

    /// <summary>
    /// Edit and continue is generally available with the set of capabilities that Mono 6, .NET Framework and .NET 5 have in common.
    /// </summary>
    Baseline = 1 << 0,

    /// <summary>
    /// Adding a static or instance method to an existing type.
    /// </summary>
    AddMethodToExistingType = 1 << 1,

    /// <summary>
    /// Adding a static field to an existing type.
    /// </summary>
    AddStaticFieldToExistingType = 1 << 2,

    /// <summary>
    /// Adding an instance field to an existing type.
    /// </summary>
    AddInstanceFieldToExistingType = 1 << 3,

    /// <summary>
    /// Creating a new type definition.
    /// </summary>
    NewTypeDefinition = 1 << 4,

    /// <summary>
    /// Adding, updating and deleting of custom attributes (as distinct from pseudo-custom attributes)
    /// </summary>
    ChangeCustomAttributes = 1 << 5,

    /// <summary>
    /// Whether the runtime supports updating the Param table, and hence related edits (eg parameter renames)
    /// </summary>
    UpdateParameters = 1 << 6,

    /// <summary>
    /// Adding a static or instance method, property or event to an existing type (without backing fields), such that the method and/or the type are generic.
    /// </summary>
    GenericAddMethodToExistingType = 1 << 7,

    /// <summary>
    /// Updating an existing static or instance method, property or event (without backing fields) that is generic and/or contained in a generic type. 
    /// </summary>
    GenericUpdateMethod = 1 << 8,

    /// <summary>
    /// Adding a static or instance field to an existing generic type.
    /// </summary>
    GenericAddFieldToExistingType = 1 << 9,

    /// <summary>
    /// The runtime supports adding to InterfaceImpl table.
    /// </summary>
    AddExplicitInterfaceImplementation = 1 << 10,
}

internal static class EditAndContinueCapabilitiesParser
{
    public static EditAndContinueCapabilities Parse(ImmutableArray<string> capabilities)
    {
        var caps = EditAndContinueCapabilities.None;

        foreach (var capability in capabilities)
        {
            caps |= capability switch
            {
                nameof(EditAndContinueCapabilities.Baseline) => EditAndContinueCapabilities.Baseline,
                nameof(EditAndContinueCapabilities.AddMethodToExistingType) => EditAndContinueCapabilities.AddMethodToExistingType,
                nameof(EditAndContinueCapabilities.AddStaticFieldToExistingType) => EditAndContinueCapabilities.AddStaticFieldToExistingType,
                nameof(EditAndContinueCapabilities.AddInstanceFieldToExistingType) => EditAndContinueCapabilities.AddInstanceFieldToExistingType,
                nameof(EditAndContinueCapabilities.NewTypeDefinition) => EditAndContinueCapabilities.NewTypeDefinition,
                nameof(EditAndContinueCapabilities.ChangeCustomAttributes) => EditAndContinueCapabilities.ChangeCustomAttributes,
                nameof(EditAndContinueCapabilities.UpdateParameters) => EditAndContinueCapabilities.UpdateParameters,
                nameof(EditAndContinueCapabilities.GenericAddMethodToExistingType) => EditAndContinueCapabilities.GenericAddMethodToExistingType,
                nameof(EditAndContinueCapabilities.GenericUpdateMethod) => EditAndContinueCapabilities.GenericUpdateMethod,
                nameof(EditAndContinueCapabilities.GenericAddFieldToExistingType) => EditAndContinueCapabilities.GenericAddFieldToExistingType,
                nameof(EditAndContinueCapabilities.AddExplicitInterfaceImplementation) => EditAndContinueCapabilities.AddExplicitInterfaceImplementation,

                // To make it eaiser for  runtimes to specify more broad capabilities
                "AddDefinitionToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType,

                _ => EditAndContinueCapabilities.None
            };
        }

        return caps;
    }

    public static ImmutableArray<string> ToStringArray(this EditAndContinueCapabilities capabilities)
    {
        using var _ = ArrayBuilder<string>.GetInstance(out var builder);

        if (capabilities.HasFlag(EditAndContinueCapabilities.Baseline))
            builder.Add(nameof(EditAndContinueCapabilities.Baseline));

        if (capabilities.HasFlag(EditAndContinueCapabilities.AddMethodToExistingType))
            builder.Add(nameof(EditAndContinueCapabilities.AddMethodToExistingType));

        if (capabilities.HasFlag(EditAndContinueCapabilities.AddStaticFieldToExistingType))
            builder.Add(nameof(EditAndContinueCapabilities.AddStaticFieldToExistingType));

        if (capabilities.HasFlag(EditAndContinueCapabilities.AddInstanceFieldToExistingType))
            builder.Add(nameof(EditAndContinueCapabilities.AddInstanceFieldToExistingType));

        if (capabilities.HasFlag(EditAndContinueCapabilities.NewTypeDefinition))
            builder.Add(nameof(EditAndContinueCapabilities.NewTypeDefinition));

        if (capabilities.HasFlag(EditAndContinueCapabilities.ChangeCustomAttributes))
            builder.Add(nameof(EditAndContinueCapabilities.ChangeCustomAttributes));

        if (capabilities.HasFlag(EditAndContinueCapabilities.UpdateParameters))
            builder.Add(nameof(EditAndContinueCapabilities.UpdateParameters));

        if (capabilities.HasFlag(EditAndContinueCapabilities.AddExplicitInterfaceImplementation))
            builder.Add(nameof(EditAndContinueCapabilities.AddExplicitInterfaceImplementation));

        return builder.ToImmutableAndClear();
    }
}
