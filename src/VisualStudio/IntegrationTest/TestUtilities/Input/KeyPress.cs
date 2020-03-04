// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public struct KeyPress
    {
        public readonly VirtualKey VirtualKey;
        public readonly ShiftState ShiftState;

        public KeyPress(VirtualKey virtualKey, ShiftState shiftState)
        {
            VirtualKey = virtualKey;
            ShiftState = shiftState;
        }
    }
}
