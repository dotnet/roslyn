// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
