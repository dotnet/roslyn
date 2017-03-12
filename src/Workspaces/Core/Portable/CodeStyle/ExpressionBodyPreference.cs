// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Note: the order of this enum is important.  We originally only supported two values,
    /// and we encoded this as a bool with 'true = WhenPossible' and 'false = never'.  To
    /// preserve compatibility we map the false value to 0 and the true value to 1.  All new
    /// values go after these. 
    /// </summary>
    internal enum ExpressionBodyPreference
    {
        Never = 0,
        WhenPossible = 1,
        WhenOnSingleLine = 2,
    }
}