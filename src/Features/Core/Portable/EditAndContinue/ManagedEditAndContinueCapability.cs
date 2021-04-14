// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// The capabilities that the runtime has with respect to edit and continue
    /// </summary>
    internal enum ManagedEditAndContinueCapability
    {
        Baseline,
        AddDefinitionToExistingType,
        NewTypeDefinition,
        RuntimeEdits,

        // Must be last
        Count
    }
}
