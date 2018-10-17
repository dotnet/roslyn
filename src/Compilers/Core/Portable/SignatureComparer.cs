// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RuntimeMembers
{
    /// <summary>
    /// Helper class to match signatures in format of 
    /// MemberDescriptor.Signature to members.
    /// </summary>
    internal abstract class SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol>
        where MethodSymbol : class
        where FieldSymbol : class
        where PropertySymbol : class
        where TypeSymbol : class
        where ParameterSymbol : class
    {
        /// <summary>
        /// Returns true if signature matches signature of the field.
        /// Signature should be in format described in MemberDescriptor.
        /// </summary>
        public bool MatchFieldSignature(FieldSymbol field, ImmutableArray<byte> signature)
        {
            int position = 0;

            // get the type
            bool result = MatchType(GetFieldType(field), signature, ref position);

            Debug.Assert(!result || position == signature.Length);
            return result;
        }

        /// <summary>
        /// Returns true if signature matches signature of the property.
        /// Signature should be in format described in MemberDescriptor.
        /// </summary>
        public bool MatchPropertySignature(PropertySymbol property, ImmutableArray<byte> signature)
        {
            int position = 0;

            // Get the parameter count
            int paramCount = signature[position++];

            // Get all of the parameters.
            ImmutableArray<ParameterSymbol> parameters = GetParameters(property);

            if (paramCount != parameters.Length)
            {
                return false;
            }

            bool isByRef = IsByRef(signature, ref position);
            if (IsByRefProperty(property) != isByRef)
            {
                return false;
            }

            // get the property type
            if (!MatchType(GetPropertyType(property), signature, ref position))
            {
                return false;
            }

            // Match parameters
            foreach (ParameterSymbol parameter in parameters)
            {
                if (!MatchParameter(parameter, signature, ref position))
                {
                    return false;
                }
            }

            Debug.Assert(position == signature.Length);
            return true;
        }

        /// <summary>
        /// Returns true if signature matches signature of the method.
        /// Signature should be in format described in MemberDescriptor.
        /// </summary>
        public bool MatchMethodSignature(MethodSymbol method, ImmutableArray<byte> signature)
        {
            int position = 0;

            // Get the parameter count
            int paramCount = signature[position++];

            // Get all of the parameters.
            ImmutableArray<ParameterSymbol> parameters = GetParameters(method);

            if (paramCount != parameters.Length)
            {
                return false;
            }

            bool isByRef = IsByRef(signature, ref position);

            if (IsByRefMethod(method) != isByRef)
            {
                return false;
            }

            // get the return type
            if (!MatchType(GetReturnType(method), signature, ref position))
            {
                return false;
            }

            // Match parameters
            foreach (ParameterSymbol parameter in parameters)
            {
                if (!MatchParameter(parameter, signature, ref position))
                {
                    return false;
                }
            }

            Debug.Assert(position == signature.Length);
            return true;
        }

        private bool MatchParameter(ParameterSymbol parameter, ImmutableArray<byte> signature, ref int position)
        {
            bool isByRef = IsByRef(signature, ref position);

            if (IsByRefParam(parameter) != isByRef)
            {
                return false;
            }

            return MatchType(GetParamType(parameter), signature, ref position);
        }

        private static bool IsByRef(ImmutableArray<byte> signature, ref int position)
        {
            SignatureTypeCode typeCode = (SignatureTypeCode)signature[position];

            if (typeCode == SignatureTypeCode.ByReference)
            {
                position++;
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Does pretty much the same thing as MetadataDecoder.DecodeType only instead of 
        /// producing a type symbol it compares encoded type to the target.
        /// 
        /// Signature should be in format described in MemberDescriptor.
        /// </summary>
        private bool MatchType(TypeSymbol type, ImmutableArray<byte> signature, ref int position)
        {
            if (type == null)
            {
                return false;
            }

            int paramPosition;

            // Get the type.
            SignatureTypeCode typeCode = (SignatureTypeCode)signature[position++];

            // Switch on the type.
            switch (typeCode)
            {
                case SignatureTypeCode.TypeHandle:
                    // Ecma-335 spec:
                    // 23.1.16 Element types used in signatures
                    // ...
                    // ELEMENT_TYPE_VALUETYPE 0x11 Followed by TypeDef or TypeRef token
                    // ELEMENT_TYPE_CLASS 0x12 Followed by TypeDef or TypeRef token
                    short expectedType = ReadTypeId(signature, ref position);
                    return MatchTypeToTypeId(type, expectedType);

                case SignatureTypeCode.Array:
                    if (!MatchType(GetMDArrayElementType(type), signature, ref position))
                    {
                        return false;
                    }

                    int countOfDimensions = signature[position++];

                    return MatchArrayRank(type, countOfDimensions);

                case SignatureTypeCode.SZArray:
                    return MatchType(GetSZArrayElementType(type), signature, ref position);

                case SignatureTypeCode.Pointer:
                    return MatchType(GetPointedToType(type), signature, ref position);

                case SignatureTypeCode.GenericTypeParameter:
                    paramPosition = signature[position++];
                    return IsGenericTypeParam(type, paramPosition);

                case SignatureTypeCode.GenericMethodParameter:
                    paramPosition = signature[position++];
                    return IsGenericMethodTypeParam(type, paramPosition);

                case SignatureTypeCode.GenericTypeInstance:
                    if (!MatchType(GetGenericTypeDefinition(type), signature, ref position))
                    {
                        return false;
                    }

                    int argumentCount = signature[position++];

                    for (int argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
                    {
                        if (!MatchType(GetGenericTypeArgument(type, argumentIndex), signature, ref position))
                        {
                            return false;
                        }
                    }

                    return true;

                default:
                    throw ExceptionUtilities.UnexpectedValue(typeCode);
            }
        }

        /// <summary>
        /// Read a type Id from the signature.
        /// This may consume one or two bytes, and therefore increment the position correspondingly.
        /// </summary>
        private static short ReadTypeId(ImmutableArray<byte> signature, ref int position)
        {
            var firstByte = signature[position++];
            if (firstByte == (byte)WellKnownType.ExtSentinel)
            {
                return (short)(signature[position++] + WellKnownType.ExtSentinel);
            }
            else
            {
                return firstByte;
            }
        }

        /// <summary>
        /// Should return null in case of error.
        /// </summary>
        protected abstract TypeSymbol GetGenericTypeArgument(TypeSymbol type, int argumentIndex);

        /// <summary>
        /// Should return null in case of error.
        /// </summary>
        protected abstract TypeSymbol GetGenericTypeDefinition(TypeSymbol type);

        protected abstract bool IsGenericMethodTypeParam(TypeSymbol type, int paramPosition);

        protected abstract bool IsGenericTypeParam(TypeSymbol type, int paramPosition);

        /// <summary>
        /// Should only accept Pointer types.
        /// Should return null in case of error.
        /// </summary>
        protected abstract TypeSymbol GetPointedToType(TypeSymbol type);

        /// <summary>
        /// Should return null in case of error.
        /// </summary>
        protected abstract TypeSymbol GetSZArrayElementType(TypeSymbol type);

        /// <summary>
        /// Should only accept multi-dimensional arrays.
        /// </summary>
        protected abstract bool MatchArrayRank(TypeSymbol type, int countOfDimensions);

        /// <summary>
        /// Should only accept multi-dimensional arrays.
        /// Should return null in case of error.
        /// </summary>
        protected abstract TypeSymbol GetMDArrayElementType(TypeSymbol type);

        protected abstract bool MatchTypeToTypeId(TypeSymbol type, int typeId);

        protected abstract TypeSymbol GetReturnType(MethodSymbol method);
        protected abstract ImmutableArray<ParameterSymbol> GetParameters(MethodSymbol method);

        protected abstract TypeSymbol GetPropertyType(PropertySymbol property);
        protected abstract ImmutableArray<ParameterSymbol> GetParameters(PropertySymbol property);

        protected abstract TypeSymbol GetParamType(ParameterSymbol parameter);

        protected abstract bool IsByRefParam(ParameterSymbol parameter);
        protected abstract bool IsByRefMethod(MethodSymbol method);
        protected abstract bool IsByRefProperty(PropertySymbol property);

        protected abstract TypeSymbol GetFieldType(FieldSymbol field);
    }
}
