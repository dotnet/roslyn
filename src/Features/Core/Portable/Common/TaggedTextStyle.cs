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

        Strong = 1 << 0,

        Emphasis = 1 << 1,

        Underline = 1 << 2,

        Code = 1 << 3,

        PreserveWhitespace = 1 << 4,
    }
}
