// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a value type that can be assigned null.
    /// </summary>
    public struct Optional<T>
    {
        /// <summary>
        /// Initializes a new instance to the specified <paramref name="value"/>.
        /// </summary>
        public Optional(T value)
        {
            HasValue = true;
            Value = value;
        }

        /// <summary>
        /// Gets a value indicating whether the current object has a value.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the value of the current object.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Creates a new object initialized to a specified <paramref name="value"/>. 
        /// </summary>
        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }
    }
}
