// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Emit;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from <see cref="MarshalAsAttribute"/>.
    /// </summary>
    internal sealed class MarshalPseudoCustomAttributeData : Cci.IMarshallingInformation
    {
        private UnmanagedType marshalType;
        private int marshalArrayElementType;      // safe array: VarEnum; array: UnmanagedType
        private int marshalArrayElementCount;     // number of elements in an array, length of a string, or Unspecified
        private int marshalParameterIndex;        // index of parameter that specifies array size (short) or IID (int), or Unspecified
        private object marshalTypeNameOrSymbol;   // custom marshaller: string or ITypeSymbol; safe array: element type symbol
        private string marshalCookie;

        internal const int Invalid = -1;
        private const UnmanagedType InvalidUnmanagedType = (UnmanagedType)Invalid;
        private const VarEnum InvalidVariantType = (VarEnum)Invalid;
        internal const int MaxMarshalInteger = 0x1fffffff;

        #region Initialization

        public MarshalPseudoCustomAttributeData()
        {
        }

        internal void SetMarshalAsCustom(object typeSymbolOrName, string cookie)
        {
            this.marshalType = Cci.Constants.UnmanagedType_CustomMarshaler;
            this.marshalTypeNameOrSymbol = typeSymbolOrName;
            this.marshalCookie = cookie;
        }

        internal void SetMarshalAsComInterface(UnmanagedType unmanagedType, int? parameterIndex)
        {
            Debug.Assert(parameterIndex == null || parameterIndex >= 0 && parameterIndex <= MaxMarshalInteger);

            this.marshalType = unmanagedType;
            this.marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsArray(UnmanagedType? elementType, int? elementCount, short? parameterIndex)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(parameterIndex == null || parameterIndex >= 0);

            this.marshalType = UnmanagedType.LPArray;
            this.marshalArrayElementType = (int)(elementType ?? (UnmanagedType)0x50);
            this.marshalArrayElementCount = elementCount ?? Invalid;
            this.marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsFixedArray(UnmanagedType? elementType, int? elementCount)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            this.marshalType = UnmanagedType.ByValArray;
            this.marshalArrayElementType = (int)(elementType ?? InvalidUnmanagedType);
            this.marshalArrayElementCount = elementCount ?? Invalid;
        }

        internal void SetMarshalAsSafeArray(VarEnum? elementType, ITypeSymbol elementTypeSymbol)
        {
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            this.marshalType = UnmanagedType.SafeArray;
            this.marshalArrayElementType = (int)(elementType ?? InvalidVariantType);
            this.marshalTypeNameOrSymbol = elementTypeSymbol;
        }

        internal void SetMarshalAsFixedString(int elementCount)
        {
            Debug.Assert(elementCount >= 0 && elementCount <= MaxMarshalInteger);

            this.marshalType = UnmanagedType.ByValTStr;
            this.marshalArrayElementCount = elementCount;
        }

        internal void SetMarshalAsSimpleType(UnmanagedType type)
        {
            Debug.Assert(type >= 0 && (int)type <= MaxMarshalInteger);
            this.marshalType = type;
        }

        #endregion

        public UnmanagedType UnmanagedType
        {
            get { return marshalType; }
        }

        int Cci.IMarshallingInformation.IidParameterIndex
        {
            get
            {
                Debug.Assert(
                    marshalType == UnmanagedType.Interface ||
                    marshalType == UnmanagedType.IUnknown ||
                    marshalType == UnmanagedType.IDispatch);

                return marshalParameterIndex;
            }
        }

        object Cci.IMarshallingInformation.GetCustomMarshaller(EmitContext context)
        {
            Debug.Assert(marshalType == Cci.Constants.UnmanagedType_CustomMarshaler);
            var typeSymbol = marshalTypeNameOrSymbol as ITypeSymbol;
            if (typeSymbol != null)
            {
                return ((CommonPEModuleBuilder)context.Module).Translate(typeSymbol, context.SyntaxNodeOpt, context.Diagnostics);
            }
            else
            {
                Debug.Assert(marshalTypeNameOrSymbol == null || marshalTypeNameOrSymbol is string);
                return marshalTypeNameOrSymbol;
            }
        }

        string Cci.IMarshallingInformation.CustomMarshallerRuntimeArgument
        {
            get
            {
                Debug.Assert(marshalType == Cci.Constants.UnmanagedType_CustomMarshaler);
                return marshalCookie;
            }
        }

        int Cci.IMarshallingInformation.NumberOfElements
        {
            get
            {
                Debug.Assert(marshalType == UnmanagedType.ByValTStr || marshalType == UnmanagedType.LPArray || marshalType == UnmanagedType.SafeArray || marshalType == UnmanagedType.ByValArray);
                return marshalArrayElementCount;
            }
        }

        short Cci.IMarshallingInformation.ParamIndex
        {
            get
            {
                Debug.Assert(marshalType == UnmanagedType.LPArray && marshalParameterIndex <= short.MaxValue);
                return (short)marshalParameterIndex;
            }
        }

        UnmanagedType Cci.IMarshallingInformation.ElementType
        {
            get
            {
                Debug.Assert(marshalType == UnmanagedType.LPArray || marshalType == UnmanagedType.ByValArray);
                return (UnmanagedType)marshalArrayElementType;
            }
        }

        VarEnum Cci.IMarshallingInformation.SafeArrayElementSubtype
        {
            get
            {
                Debug.Assert(marshalType == UnmanagedType.SafeArray);
                return (VarEnum)marshalArrayElementType;
            }
        }

        Cci.ITypeReference Cci.IMarshallingInformation.GetSafeArrayElementUserDefinedSubtype(EmitContext context)
        {
            Debug.Assert(marshalType == UnmanagedType.SafeArray);

            if (marshalTypeNameOrSymbol == null)
            {
                return null;
            }

            return ((CommonPEModuleBuilder)context.Module).Translate((ITypeSymbol)marshalTypeNameOrSymbol, context.SyntaxNodeOpt, context.Diagnostics);
        }

        /// <summary>
        /// Returns an instance of <see cref="MarshalPseudoCustomAttributeData"/> with all types replaced by types returned by specified translator.
        /// Returns this instance if it doesn't hold on any types.
        /// </summary>
        internal MarshalPseudoCustomAttributeData WithTranslatedTypes<TTypeSymbol, TArg>(
            Func<TTypeSymbol, TArg, TTypeSymbol> translator, TArg arg)
            where TTypeSymbol : ITypeSymbol
        {
            if (marshalType != UnmanagedType.SafeArray || marshalTypeNameOrSymbol == null)
            {
                return this;
            }

            var translatedType = translator((TTypeSymbol)marshalTypeNameOrSymbol, arg);
            if ((object)translatedType == (object)marshalTypeNameOrSymbol)
            {
                return this;
            }

            var result = new MarshalPseudoCustomAttributeData();
            result.SetMarshalAsSafeArray((VarEnum)marshalArrayElementType, translatedType);
            return result;
        }

        // testing only
        internal ITypeSymbol TryGetSafeArrayElementUserDefinedSubtype()
        {
            return marshalTypeNameOrSymbol as ITypeSymbol;
        }
    }
}
