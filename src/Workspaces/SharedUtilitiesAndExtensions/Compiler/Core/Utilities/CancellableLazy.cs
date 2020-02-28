// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class CancellableLazy
    {
        public static CancellableLazy<T> Create<T>(T value)
        {
            return new CancellableLazy<T>(value);
        }

        public static CancellableLazy<T> Create<T>(Func<CancellationToken, T> valueFactory)
        {
            return new CancellableLazy<T>(valueFactory);
        }
    }
}
