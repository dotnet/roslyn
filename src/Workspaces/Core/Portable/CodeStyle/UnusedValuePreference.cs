// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
