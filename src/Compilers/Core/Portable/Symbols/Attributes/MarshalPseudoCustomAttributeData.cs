// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
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
        private UnmanagedType _marshalType;
        private int _marshalArrayElementType;      // safe array: VarEnum; array: UnmanagedType
        private int _marshalArrayElementCount;     // number of elements in an array, length of a string, or Unspecified
        private int _marshalParameterIndex;        // index of parameter that specifies array size (short) or IID (int), or Unspecified
        private object _marshalTypeNameOrSymbol;   // custom marshaller: string or ITypeSymbolInternal; safe array: element type symbol
        private string _marshalCookie;

        internal const int Invalid = -1;
        private const UnmanagedType InvalidUnmanagedType = (UnmanagedType)Invalid;
        private const Cci.VarEnum InvalidVariantType = (Cci.VarEnum)Invalid;
        internal const int MaxMarshalInteger = 0x1fffffff;

        #region Initialization

        public MarshalPseudoCustomAttributeData()
        {
        }

        internal void SetMarshalAsCustom(object typeSymbolOrName, string cookie)
        {
            _marshalType = Cci.Constants.UnmanagedType_CustomMarshaler;
            _marshalTypeNameOrSymbol = typeSymbolOrName;
            _marshalCookie = cookie;
        }

        internal void SetMarshalAsComInterface(UnmanagedType unmanagedType, int? parameterIndex)
        {
            Debug.Assert(parameterIndex == null || parameterIndex >= 0 && parameterIndex <= MaxMarshalInteger);

            _marshalType = unmanagedType;
            _marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsArray(UnmanagedType? elementType, int? elementCount, short? parameterIndex)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(parameterIndex == null || parameterIndex >= 0);

            _marshalType = UnmanagedType.LPArray;
            _marshalArrayElementType = (int)(elementType ?? (UnmanagedType)0x50);
            _marshalArrayElementCount = elementCount ?? Invalid;
            _marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsFixedArray(UnmanagedType? elementType, int? elementCount)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            _marshalType = UnmanagedType.ByValArray;
            _marshalArrayElementType = (int)(elementType ?? InvalidUnmanagedType);
            _marshalArrayElementCount = elementCount ?? Invalid;
        }

        internal void SetMarshalAsSafeArray(Cci.VarEnum? elementType, ITypeSymbolInternal elementTypeSymbol)
        {
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            _marshalType = Cci.Constants.UnmanagedType_SafeArray;
            _marshalArrayElementType = (int)(elementType ?? InvalidVariantType);
            _marshalTypeNameOrSymbol = elementTypeSymbol;
        }

        internal void SetMarshalAsFixedString(int elementCount)
        {
            Debug.Assert(elementCount >= 0 && elementCount <= MaxMarshalInteger);

            _marshalType = UnmanagedType.ByValTStr;
            _marshalArrayElementCount = elementCount;
        }

        internal void SetMarshalAsSimpleType(UnmanagedType type)
        {
            Debug.Assert(type >= 0 && (int)type <= MaxMarshalInteger);
            _marshalType = type;
        }

        #endregion

        public UnmanagedType UnmanagedType
        {
            get { return _marshalType; }
        }

        int Cci.IMarshallingInformation.IidParameterIndex
        {
            get
            {
                Debug.Assert(
                    _marshalType == UnmanagedType.Interface ||
                    _marshalType == UnmanagedType.IUnknown ||
                    _marshalType == Cci.Constants.UnmanagedType_IDispatch);

                return _marshalParameterIndex;
            }
        }

        object Cci.IMarshallingInformation.GetCustomMarshaller(EmitContext context)
        {
            Debug.Assert(_marshalType == Cci.Constants.UnmanagedType_CustomMarshaler);
            var typeSymbol = _marshalTypeNameOrSymbol as ITypeSymbolInternal;
            if (typeSymbol != null)
            {
                return ((CommonPEModuleBuilder)context.Module).Translate(typeSymbol, context.SyntaxNode, context.Diagnostics);
            }
            else
            {
                Debug.Assert(_marshalTypeNameOrSymbol == null || _marshalTypeNameOrSymbol is string);
                return _marshalTypeNameOrSymbol;
            }
        }

        string Cci.IMarshallingInformation.CustomMarshallerRuntimeArgument
        {
            get
            {
                Debug.Assert(_marshalType == Cci.Constants.UnmanagedType_CustomMarshaler);
                return _marshalCookie;
            }
        }

        int Cci.IMarshallingInformation.NumberOfElements
        {
            get
            {
                Debug.Assert(_marshalType == UnmanagedType.ByValTStr || _marshalType == UnmanagedType.LPArray || _marshalType == Cci.Constants.UnmanagedType_SafeArray || _marshalType == UnmanagedType.ByValArray);
                return _marshalArrayElementCount;
            }
        }

        short Cci.IMarshallingInformation.ParamIndex
        {
            get
            {
                Debug.Assert(_marshalType == UnmanagedType.LPArray && _marshalParameterIndex <= short.MaxValue);
                return (short)_marshalParameterIndex;
            }
        }

        UnmanagedType Cci.IMarshallingInformation.ElementType
        {
            get
            {
                Debug.Assert(_marshalType == UnmanagedType.LPArray || _marshalType == UnmanagedType.ByValArray);
                return (UnmanagedType)_marshalArrayElementType;
            }
        }

        Cci.VarEnum Cci.IMarshallingInformation.SafeArrayElementSubtype
        {
            get
            {
                Debug.Assert(_marshalType == Cci.Constants.UnmanagedType_SafeArray);
                return (Cci.VarEnum)_marshalArrayElementType;
            }
        }

        Cci.ITypeReference Cci.IMarshallingInformation.GetSafeArrayElementUserDefinedSubtype(EmitContext context)
        {
            Debug.Assert(_marshalType == Cci.Constants.UnmanagedType_SafeArray);

            if (_marshalTypeNameOrSymbol == null)
            {
                return null;
            }

            return ((CommonPEModuleBuilder)context.Module).Translate((ITypeSymbolInternal)_marshalTypeNameOrSymbol, context.SyntaxNode, context.Diagnostics);
        }

        /// <summary>
        /// Returns an instance of <see cref="MarshalPseudoCustomAttributeData"/> with all types replaced by types returned by specified translator.
        /// Returns this instance if it doesn't hold on any types.
        /// </summary>
        internal MarshalPseudoCustomAttributeData WithTranslatedTypes<TTypeSymbol, TArg>(
            TArg arg,
            Func<TTypeSymbol, TArg, TTypeSymbol> translator)
            where TTypeSymbol : ITypeSymbolInternal
        {
            if (_marshalType != Cci.Constants.UnmanagedType_SafeArray || _marshalTypeNameOrSymbol == null)
            {
                return this;
            }

            var translatedType = translator((TTypeSymbol)_marshalTypeNameOrSymbol, arg);
            if ((object)translatedType == (object)_marshalTypeNameOrSymbol)
            {
                return this;
            }

            var result = new MarshalPseudoCustomAttributeData();
            result.SetMarshalAsSafeArray((Cci.VarEnum)_marshalArrayElementType, translatedType);
            return result;
        }

        // testing only
        internal ITypeSymbolInternal TryGetSafeArrayElementUserDefinedSubtype()
        {
            return _marshalTypeNameOrSymbol as ITypeSymbolInternal;
        }
    }
}
