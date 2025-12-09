// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;

    internal static class ExceptionUtilities
    {
        internal static Exception Unreachable
            => new InvalidOperationException("This program location is thought to be unreachable.");
    }
}
