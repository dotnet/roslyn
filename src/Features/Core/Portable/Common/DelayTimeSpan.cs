// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class DelayTimeSpan
    {
        public static readonly TimeSpan NearImmediate = TimeSpan.FromMilliseconds(50);
        public static readonly TimeSpan Short = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan Medium = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan Idle = TimeSpan.FromMilliseconds(1500);
        public static readonly TimeSpan NonFocus = TimeSpan.FromMilliseconds(3000);
    }
}
