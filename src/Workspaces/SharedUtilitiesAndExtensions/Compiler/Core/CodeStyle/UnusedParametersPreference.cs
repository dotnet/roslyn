// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
