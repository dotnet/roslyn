// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;

    internal static class ExceptionUtilities
    {
        internal static Exception Unreachable
            => new InvalidOperationException("This program location is thought to be unreachable.");
    }
}
