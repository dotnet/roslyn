// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !METALAMA_COMPILER_INTERFACE

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{
    internal class SyntaxTreeHistory
    {
        private SyntaxTree? _first;
        private SyntaxTree? _next;

        public static SyntaxTree GetFirst(SyntaxTree syntaxTree)
            => AttachedProperties.Get<SyntaxTree, SyntaxTreeHistory>(syntaxTree)?._first ?? syntaxTree;

        public static SyntaxTree GetLast(SyntaxTree syntaxTree)
        {
            SyntaxTree? lastTree = syntaxTree, nextTree;
            while ((nextTree = AttachedProperties.Get<SyntaxTree, SyntaxTreeHistory>(lastTree)?._next) != null)
            {
                lastTree = nextTree;
            }

            return lastTree;
        }

        public static void Update(SyntaxTree oldTree, SyntaxTree newTree)
        {
            if (oldTree == newTree)
            {
                throw new ArgumentOutOfRangeException();
            }

            var previous = AttachedProperties.GetOrAdd<SyntaxTree, SyntaxTreeHistory>(oldTree);
            previous._next = newTree;

            if (previous._first == null)
            {
                previous._first = oldTree;
            }

            AttachedProperties.GetOrAdd<SyntaxTree, SyntaxTreeHistory>(newTree)._first = previous._first;
        }
    }

    internal static class AttachedProperties
    {
        public static TProperty? Get<TTarget, TProperty>(TTarget obj)
            where TTarget : class
            where TProperty : class
        {
            Impl<TTarget, TProperty>.Properties.TryGetValue(obj, out var value);
            return value;
        }

        public static bool Has<TTarget, TProperty>(TTarget obj)
            where TTarget : class
            where TProperty : class
            => Impl<TTarget, TProperty>.Properties.TryGetValue(obj, out _);

        public static TProperty GetOrAdd<TTarget, TProperty>(TTarget obj)
            where TTarget : class
            where TProperty : class, new()
            => Impl<TTarget, TProperty>.Properties.GetOrCreateValue(obj);

        public static void Add<TTarget, TProperty>(TTarget obj, TProperty value)
            where TTarget : class
            where TProperty : class
            => Impl<TTarget, TProperty>.Properties.Add(obj, value);

        private static class Impl<TTarget, TProperty>
            where TTarget : class
            where TProperty : class
        {
            public static readonly ConditionalWeakTable<TTarget, TProperty> Properties = new();
        }
    }
}
#endif
