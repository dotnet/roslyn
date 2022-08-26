// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
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
                    "Baseline" => EditAndContinueCapabilities.Baseline,
                    "AddMethodToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType,
                    "AddStaticFieldToExistingType" => EditAndContinueCapabilities.AddStaticFieldToExistingType,
                    "AddInstanceFieldToExistingType" => EditAndContinueCapabilities.AddInstanceFieldToExistingType,
                    "NewTypeDefinition" => EditAndContinueCapabilities.NewTypeDefinition,
                    "ChangeCustomAttributes" => EditAndContinueCapabilities.ChangeCustomAttributes,
                    "UpdateParameters" => EditAndContinueCapabilities.UpdateParameters,

                    // To make it eaiser for  runtimes to specify more broad capabilities
                    "AddDefinitionToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType,

                    _ => EditAndContinueCapabilities.None
                };
            }

            return caps;
        }
    }
}
