// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a constant value used as an argument to a custom attribute.
    /// </summary>
    public struct TypedConstant : IEquatable<TypedConstant>
    {
        private readonly TypedConstantKind _kind;
        private readonly ITypeSymbol _type;
        private readonly object? _value;

        internal TypedConstant(ITypeSymbol type, TypedConstantKind kind, object? value)
        {
            Debug.Assert(kind == TypedConstantKind.Array || !(value is ImmutableArray<TypedConstant>));
            _kind = kind;
            _type = type;
            _value = value;
        }

        internal TypedConstant(ITypeSymbol type, ImmutableArray<TypedConstant> array)
            : this(type, TypedConstantKind.Array, array.IsDefault ? null : (object)array)
        {
        }

        /// <summary>
        /// The kind of the constant.
        /// </summary>
        public TypedConstantKind Kind
        {
            get { return _kind; }
        }

        /// <summary>
        /// Returns the <see cref="ITypeSymbol"/> of the constant, 
        /// or null if the type can't be determined (error).
        /// </summary>
        public ITypeSymbol Type
        {
            get { return _type; }
        }

        /// <summary>
        /// True if the constant represents a null reference.
        /// </summary>
        public bool IsNull
        {
            get
            {
                return _value == null;
            }
        }

        /// <summary>
        /// The value for a non-array constant.
        /// </summary>
        public object? Value
        {
            get
            {
                if (Kind == TypedConstantKind.Array)
                {
                    throw new InvalidOperationException("TypedConstant is an array. Use Values property.");
                }

                return _value;
            }
        }

        /// <summary>
        /// The value for a <see cref="TypedConstant"/> array. 
        /// </summary>
        public ImmutableArray<TypedConstant> Values
        {
            get
            {
                if (Kind != TypedConstantKind.Array)
                {
                    throw new InvalidOperationException("TypedConstant is not an array. Use Value property.");
                }

                if (this.IsNull)
                {
                    return default(ImmutableArray<TypedConstant>);
                }

                return (ImmutableArray<TypedConstant>)_value!;
            }
        }

        [return: MaybeNull]
        internal T DecodeValue<T>(SpecialType specialType)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'T' is a non-nullable reference type.
            TryDecodeValue(specialType, out T value);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'T' is a non-nullable reference type.
            return value;
        }

        internal bool TryDecodeValue<T>(SpecialType specialType, [MaybeNullWhen(returnValue: false)] out T value)
        {
            if (_kind == TypedConstantKind.Error)
            {
                value = default(T)!;
                return false;
            }

            if (_type.SpecialType == specialType || (_type.TypeKind == TypeKind.Enum && specialType == SpecialType.System_Enum))
            {
                value = (T)_value!;
                return true;
            }

            // the actual argument type doesn't match the type of the parameter - an error has already been reported by the binder
            value = default(T)!;
            return false;
        }

        /// <remarks>
        /// TypedConstant isn't computing its own kind from the type symbol because it doesn't
        /// have a way to recognize the well-known type System.Type.
        /// </remarks>
        internal static TypedConstantKind GetTypedConstantKind(ITypeSymbol type, Compilation compilation)
        {
            RoslynDebug.Assert(type != null);

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                case SpecialType.System_Object:
                    return TypedConstantKind.Primitive;
                default:
                    switch (type.TypeKind)
                    {
                        case TypeKind.Array:
                            return TypedConstantKind.Array;
                        case TypeKind.Enum:
                            return TypedConstantKind.Enum;
                        case TypeKind.Error:
                            return TypedConstantKind.Error;
                    }

                    if (compilation != null &&
                        compilation.IsSystemTypeReference(type))
                    {
                        return TypedConstantKind.Type;
                    }

                    return TypedConstantKind.Error;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is TypedConstant && Equals((TypedConstant)obj);
        }

        public bool Equals(TypedConstant other)
        {
            return _kind == other._kind
                && object.Equals(_value, other._value)
                && object.Equals(_type, other._type);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_value,
                   Hash.Combine(_type, (int)this.Kind));
        }

        #region Testing & Debugging
#if false
        /// <summary>
        /// Returns the System.String that represents the current TypedConstant.
        /// </summary>
        /// <returns>A System.String that represents the current TypedConstant.</returns>
        public override string ToString()
        {
            if (value.IsNull)
            {
                return "null";
            }

            if (kind == TypedConstantKind.Array)
            {
                return "{" + string.Join(", ", Values.Select(v => new TypedConstant(v).ToString())) + "}";
            }

            if (kind == TypedConstantKind.Type || type.SpecialType == SpecialType.System_Object)
            {
                return "typeof(" + value.Object.ToString() + ")";
            }

            if (kind == TypedConstantKind.Enum)
            {
                // TODO (tomat): replace with SymbolDisplay
                return DisplayEnumConstant();
            }

            return SymbolDisplay.FormatPrimitive(value.Object, quoteStrings: true, useHexadecimalNumbers: false);
        }

        // Decode the value of enum constant
        private string DisplayEnumConstant()
        {
            Debug.Assert(Kind == TypedConstantKind.Enum);

            // Create a ConstantValue of enum underlying type
            SpecialType splType = this.Type.GetEnumUnderlyingType().SpecialType;
            ConstantValue valueConstant = ConstantValue.Create(this.Value, splType);

            string typeName = this.Type.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
            if (valueConstant.IsUnsigned)
            {
                return DisplayUnsignedEnumConstant(splType, valueConstant.UInt64Value, typeName);
            }
            else
            {
                return DisplaySignedEnumConstant(splType, valueConstant.Int64Value, typeName);
            }
        }

        private string DisplayUnsignedEnumConstant(SpecialType specialType, ulong constantToDecode, string typeName)
        {
            // Specified valueConstant might have an exact matching enum field
            // or it might be a bitwise Or of multiple enum fields.
            // For the later case, we keep track of the current value of
            // bitwise Or of possible enum fields.
            ulong curValue = 0;

            // Initialize the value string to empty
            PooledStringBuilder pooledBuilder = null;
            StringBuilder valueStringBuilder = null;

            // Iterate through all the constant members in the enum type
            ImmutableArray<Symbol> members = this.Type.GetMembers();
            foreach (Symbol member in members)
            {
                var field = member as FieldSymbol;

                if ((object)field != null && field.HasConstantValue)
                {
                    ConstantValue memberConstant = ConstantValue.Create(field.ConstantValue, specialType);
                    ulong memberValue = memberConstant.UInt64Value;

                    // Do we have an exact matching enum field
                    if (memberValue == constantToDecode)
                    {
                        if (pooledBuilder != null)
                        {
                            pooledBuilder.Free();
                        }

                        return typeName + "." + field.Name;
                    }

                    // specifiedValue might be a bitwise Or of multiple enum fields
                    // Is the current member included in the specified value?
                    if ((memberValue & constantToDecode) == memberValue)
                    {
                        // update the current value
                        curValue = curValue | memberValue;

                        if (valueStringBuilder == null)
                        {
                            pooledBuilder = PooledStringBuilder.GetInstance();
                            valueStringBuilder = pooledBuilder.Builder;
                        }
                        else
                        {
                            valueStringBuilder.Append(" | ");
                        }

                        valueStringBuilder.Append(typeName);
                        valueStringBuilder.Append(".");
                        valueStringBuilder.Append(field.Name);
                    }
                }
            }

            if (pooledBuilder != null)
            {
                if (curValue == constantToDecode)
                {
                    // return decoded enum constant
                    return pooledBuilder.ToStringAndFree();
                }

                // Unable to decode the enum constant
                pooledBuilder.Free();
            }

            // Unable to decode the enum constant, just display the integral value
            return this.Value.ToString();
        }

        private string DisplaySignedEnumConstant(SpecialType specialType, long constantToDecode, string typeName)
        {
            // Specified valueConstant might have an exact matching enum field
            // or it might be a bitwise Or of multiple enum fields.
            // For the later case, we keep track of the current value of
            // bitwise Or of possible enum fields.
            long curValue = 0;

            // Initialize the value string to empty
            PooledStringBuilder pooledBuilder = null;
            StringBuilder valueStringBuilder = null;

            // Iterate through all the constant members in the enum type
            ImmutableArray<Symbol> members = this.Type.GetMembers();
            foreach (Symbol member in members)
            {
                var field = member as FieldSymbol;
                if ((object)field != null && field.HasConstantValue)
                {
                    ConstantValue memberConstant = ConstantValue.Create(field.ConstantValue, specialType);
                    long memberValue = memberConstant.Int64Value;

                    // Do we have an exact matching enum field
                    if (memberValue == constantToDecode)
                    {
                        if (pooledBuilder != null)
                        {
                            pooledBuilder.Free();
                        }

                        return typeName + "." + field.Name;
                    }

                    // specifiedValue might be a bitwise Or of multiple enum fields
                    // Is the current member included in the specified value?
                    if ((memberValue & constantToDecode) == memberValue)
                    {
                        // update the current value
                        curValue = curValue | memberValue;

                        if (valueStringBuilder == null)
                        {
                            pooledBuilder = PooledStringBuilder.GetInstance();
                            valueStringBuilder = pooledBuilder.Builder;
                        }
                        else
                        {
                            valueStringBuilder.Append(" | ");
                        }

                        valueStringBuilder.Append(typeName);
                        valueStringBuilder.Append(".");
                        valueStringBuilder.Append(field.Name);
                    }
                }
            }

            if (pooledBuilder != null)
            {
                if (curValue == constantToDecode)
                {
                    // return decoded enum constant
                    return pooledBuilder.ToStringAndFree();
                }

                // Unable to decode the enum constant
                pooledBuilder.Free();
            }

            // Unable to decode the enum constant, just display the integral value
            return this.Value.ToString();
        }
#endif
        #endregion
    }
}
