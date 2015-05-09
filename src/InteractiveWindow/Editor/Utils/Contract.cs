// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class Contract
    {
        [DebuggerDisplay("Unreachable")]
        public static Exception Unreachable
        {
            get
            {
                Debug.Fail("This code path should not be reachable");
                return new InvalidOperationException();
            }
        }
    }
}
