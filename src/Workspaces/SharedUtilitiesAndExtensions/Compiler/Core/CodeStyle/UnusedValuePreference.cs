// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Assignment preference for unused values from expression statements and assignments.
    /// </summary>
    internal enum UnusedValuePreference
    {
        // Unused values must be explicitly assigned to a local variable
        // that is never read/used.
        UnusedLocalVariable = 1,

        // Unused values must be explicitly assigned to a discard '_' variable.
        DiscardVariable = 2,
    }
}
