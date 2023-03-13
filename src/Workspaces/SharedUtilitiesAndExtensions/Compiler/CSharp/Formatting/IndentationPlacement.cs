// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [Flags]
    internal enum IndentationPlacement
    {
        Braces = 1,
        BlockContents = 1 << 1,
        SwitchCaseContents = 1 << 2,
        SwitchCaseContentsWhenBlock = 1 << 3,
        SwitchSection = 1 << 4
    }
}
