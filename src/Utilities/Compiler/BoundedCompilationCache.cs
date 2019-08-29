// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1000 // Do not declare static members on generic types

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provides bounded per-compilation static cache for analyzers.
    /// </summary>
    internal static class BoundedCompilationCache<TValue>
        where TValue : new()
    {
        public static TValue GetOrCreateValue(Compilation compilation)
            => BoundedCache<Compilation, TValue>.GetOrCreateValue(compilation);
    }
}
