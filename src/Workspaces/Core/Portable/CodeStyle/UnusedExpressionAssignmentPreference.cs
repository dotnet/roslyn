// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Assignment preference for expressions with unused value.
    /// </summary>
    internal enum UnusedExpressionAssignmentPreference
    {
        // The rule is not run.
        None = 0,

        // Unused expressions values must be explicitly assigned to a discard '_' variable.
        DiscardVariable = 1,

        // Unused expressions values must be explicitly assigned to a local variable
        // that is never read/used.
        UnusedLocalVariable = 2,
    }
}
