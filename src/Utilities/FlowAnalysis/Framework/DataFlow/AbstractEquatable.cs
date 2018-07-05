// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract cache based equatable implementation for objects that are compared frequently and hence need a performance optimization of using a cached hash code.
    /// </summary>
    internal abstract class CacheBasedEquatable<T> : IEquatable<T>
        where T: class
    {
        private readonly Lazy<int> _lazyHashCode;

        protected CacheBasedEquatable()
        {
#pragma warning disable CA2214 // Do not call overridable methods in constructors
                               // https://github.com/dotnet/roslyn-analyzers/issues/1652
            _lazyHashCode = new Lazy<int>(() => HashUtilities.Combine(GetType().GetHashCode(), ComputeHashCode()));
#pragma warning restore CA2214 // Do not call overridable methods in constructors
        }

        protected abstract int ComputeHashCode();

        public sealed override int GetHashCode() => _lazyHashCode.Value;
        public sealed override bool Equals(object obj) => Equals(obj as T);
        public bool Equals(T other) => _lazyHashCode.Value == (other as CacheBasedEquatable<T>)?._lazyHashCode.Value;

        public static bool operator ==(CacheBasedEquatable<T> value1, CacheBasedEquatable<T> value2)
        {
            if ((object)value1 == null)
            {
                return (object)value2 == null;
            }

            return value1.Equals(value2);
        }

        public static bool operator !=(CacheBasedEquatable<T> value1, CacheBasedEquatable<T> value2)
        {
            return !(value1 == value2);
        }
    }
}
