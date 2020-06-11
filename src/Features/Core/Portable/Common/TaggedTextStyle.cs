// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
