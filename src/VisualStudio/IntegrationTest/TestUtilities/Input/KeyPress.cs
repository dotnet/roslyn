// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
