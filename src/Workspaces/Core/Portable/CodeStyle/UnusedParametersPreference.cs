// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Preferences for flagging unused parameters.
    /// </summary>
    internal enum UnusedParametersPreference
    {
        // Ununsed parameters of non-public methods are flagged.
        NonPublicMethods = 0,

        // Unused parameters of methods with any accessibility (private/public/protected/internal) are flagged.
        AllMethods = 1,
    }
}
