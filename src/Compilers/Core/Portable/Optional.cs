// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Combines a value, <see cref="Value"/>, and a flag, <see cref="HasValue"/>, 
    /// indicating whether or not that value is meaningful.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public readonly struct Optional<T>
    {
        private readonly bool _hasValue;
        private readonly T _value;

        /// <summary>
        /// Constructs an <see cref="Optional{T}"/> with a meaningful value.
        /// </summary>
        /// <param name="value"></param>
        public Optional(T value)
        {
            _hasValue = true;
            _value = value;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="Value"/> will return a meaningful value.
        /// </summary>
        /// <returns></returns>
        public bool HasValue
        {
            get { return _hasValue; }
        }

        /// <summary>
        /// Gets the value of the current object.  Not meaningful unless <see cref="HasValue"/> returns <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>Unlike <see cref="Nullable{T}.Value"/>, this property does not throw an exception when
        /// <see cref="HasValue"/> is <see langword="false"/>.</para>
        /// </remarks>
        /// <returns>
        /// <para>The value if <see cref="HasValue"/> is <see langword="true"/>; otherwise, the default value for type
        /// <typeparamref name="T"/>.</para>
        /// </returns>
        public T Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Creates a new object initialized to a meaningful value. 
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            // Note: For nullable types, it's possible to have _hasValue true and _value null.
            return _hasValue
                ? _value?.ToString() ?? "null"
                : "unspecified";
        }
    }
}
