// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.VisualStudio.Test.Utilities.Input
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
