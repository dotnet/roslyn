// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1000 // Do not declare static members on generic types

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provides bounded per-compilation static cache for analyzers.
    /// </summary>
    internal static class BoundedCompilationCacheWithFactory<TValue>
    {
        public static TValue GetOrCreateValue(Compilation compilation, Func<Compilation, TValue> valueFactory)
            => BoundedCacheWithFactory<Compilation, TValue>.GetOrCreateValue(compilation, valueFactory);
    }
}
