// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// The capabilities that the runtime has with respect to edit and continue
    /// </summary>
    [Flags]
    internal enum ManagedEditAndContinueCapability
    {
        None = 0,

        Baseline = 1 << 0,
        AddDefinitionToExistingType = 1 << 1,
        NewTypeDefinition = 1 << 2,
        RuntimeEdits = 1 << 3,
    }
}
