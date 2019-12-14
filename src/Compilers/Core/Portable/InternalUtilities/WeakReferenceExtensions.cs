// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    // Helpers that are missing from Dev11 implementation:
    internal static class WeakReferenceExtensions
    {
        [return: MaybeNull]
        public static T GetTarget<T>(this WeakReference<T> reference) where T : class?
        {
            reference.TryGetTarget(out var target);
            return target;
        }

        public static bool IsNull<T>(this WeakReference<T> reference) where T : class?
        {
            return !reference.TryGetTarget(out var target);
        }
    }
}
