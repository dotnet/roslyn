// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Emit
{

    using System;
#pragma warning disable RS0016 // Add public types and members to the declared API

    [System.Runtime.InteropServices.ComVisible(true)]
    public enum OperandType
    {

        InlineBrTarget = 0,
        InlineField = 1,
        InlineI = 2,
        InlineI8 = 3,
        InlineMethod = 4,
        InlineNone = 5,
#if !FEATURE_CORECLR
        /// <internalonly/>
        [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        InlinePhi = 6,
#endif
        InlineR = 7,
        InlineSig = 9,
        InlineString = 10,
        InlineSwitch = 11,
        InlineTok = 12,
        InlineType = 13,
        InlineVar = 14,
        ShortInlineBrTarget = 15,
        ShortInlineI = 16,
        ShortInlineR = 17,
        ShortInlineVar = 18,
    }
#pragma warning restore RS0016 // Add public types and members to the declared API
}
