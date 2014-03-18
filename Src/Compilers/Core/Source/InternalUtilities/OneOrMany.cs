// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Represents a single item or many items. 
    /// </summary>
    /// <remarks>
    /// Used when a collection usually contains a single item but sometimes might contain multiple.
    /// </remarks>
    internal struct OneOrMany<T>
    {
        private readonly T one;
        private readonly ImmutableArray<T> many;

        public OneOrMany(T one)
        {
            this.one = one;
            this.many = default(ImmutableArray<T>);
        }

        public OneOrMany(ImmutableArray<T> many)
        {
            if (many.IsDefault)
            {
                throw new ArgumentNullException("many");
            }

            this.one = default(T);
            this.many = many;
        }

        public T this[int index]
        {
            get
            {
                if (this.many.IsDefault)
                {
                    if (index != 0)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return one;
                }
                else
                {
                    return many[index];
                }
            }
        }

        public int Count
        {
            get
            {
                return this.many.IsDefault ? 1 : this.many.Length;
            }
        }
    }

    internal static class OneOrMany
    {
        public static OneOrMany<T> Create<T>(T one)
        {
            return new OneOrMany<T>(one);
        }

        public static OneOrMany<T> Create<T>(ImmutableArray<T> many)
        {
            return new OneOrMany<T>(many);
        }
    }
}
