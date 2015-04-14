// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a value type that can be assigned null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Optional<T>
    {
        private readonly bool _hasValue;
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance to the specified value.
        /// </summary>
        /// <param name="value"></param>
        public Optional(T value)
        {
            _hasValue = true;
            _value = value;
        }

        /// <summary>
        /// Gets a value indicating whether the current object has a value.
        /// </summary>
        /// <returns></returns>
        public bool HasValue
        {
            get { return _hasValue; }
        }

        /// <summary>
        /// Gets the value of the current object.
        /// </summary>
        /// <returns></returns>
        public T Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Creates a new object initialized to a specified value. 
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }
    }
}
