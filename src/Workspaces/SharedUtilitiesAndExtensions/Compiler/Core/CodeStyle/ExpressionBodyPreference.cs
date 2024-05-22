// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeStyle;

/// <remarks>
/// Note: the order of this enum is important.  We originally only supported two values,
/// and we encoded this as a bool with 'true = WhenPossible' and 'false = never'.  To
/// preserve compatibility we map the false value to 0 and the true value to 1.  All new
/// values go after these. 
/// </remarks>
internal enum ExpressionBodyPreference
{
    // Value can not be changed. 'false' was the "never" value back when we used CodeStyleOption<bool>
    // and that will map to '0' when derialized.
    Never = 0,
    // Value can not be changed. 'true' was the 'whenever possible' value back when we used
    // CodeStyleOption<bool> and that will map to '1' when deserialized.
    WhenPossible = 1,
    WhenOnSingleLine = 2,
}
