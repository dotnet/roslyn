// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class NativeMethods
    {
        internal const uint
            QS_KEY = 0x0001,
            QS_MOUSEMOVE = 0x0002,
            QS_MOUSEBUTTON = 0x0004,
            QS_MOUSE = QS_MOUSEMOVE | QS_MOUSEBUTTON,
            QS_INPUT = QS_MOUSE | QS_KEY;

        [DllImport("user32.dll")]
        internal static extern uint GetQueueStatus(uint flags);
    }
}
