// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum TaggedTextStyle
    {
        None = 0,

        Strong = 0x1,

        Emphasis = 0x2,

        Underline = 0x4,

        Code = 0x8,
    }
}
