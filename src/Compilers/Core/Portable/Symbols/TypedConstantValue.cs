// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a simple value or a read-only array of <see cref="TypedConstant"/>.
    /// </summary>
    internal struct TypedConstantValue : IEquatable<TypedConstantValue>
    {
        // Simple value or ImmutableArray<TypedConstant>.
        // Null array is represented by a null reference.
        private readonly object? _value;

        internal TypedConstantValue(object? value)
        {
            Debug.Assert(value == null || value is string || value.GetType().GetTypeInfo().IsEnum || (value.GetType().GetTypeInfo().IsPrimitive && !(value is System.IntPtr) && !(value is System.UIntPtr)) || value is ITypeSymbol);
            _value = value;
        }

        internal TypedConstantValue(ImmutableArray<TypedConstant> array)
        {
            _value = array.IsDefault ? null : (object)array;
        }

        /// <summary>
        /// True if the constant represents a null literal.
        /// </summary>
        public bool IsNull
        {
            get
            {
                return _value == null;
            }
        }

        public ImmutableArray<TypedConstant> Array
        {
            get
            {
                return _value == null ? default(ImmutableArray<TypedConstant>) : (ImmutableArray<TypedConstant>)_value;
            }
        }

        public object? Object
        {
            get
            {
                Debug.Assert(!(_value is ImmutableArray<TypedConstant>));
                return _value;
            }
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }

        public override bool Equals(object? obj)
        {
            return obj is TypedConstantValue && Equals((TypedConstantValue)obj);
        }

        public bool Equals(TypedConstantValue other)
        {
            return object.Equals(_value, other._value);
        }
    }
}
