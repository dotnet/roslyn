// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    [Flags]
    public enum ShiftState : byte
    {
        Shift = 1,
        Ctrl = 1 << 1,
        Alt = 1 << 2,
        Hankaku = 1 << 3,
        Reserved1 = 1 << 4,
        Reserved2 = 1 << 5
    }
}
