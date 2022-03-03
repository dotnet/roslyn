// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class MarshalAsAttributeDecoder<TWellKnownAttributeData, TAttributeSyntax, TAttributeData, TAttributeLocation>
        where TWellKnownAttributeData : WellKnownAttributeData, IMarshalAsAttributeTarget, new()
        where TAttributeSyntax : SyntaxNode
        where TAttributeData : AttributeData
    {
        internal static void Decode(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, AttributeTargets target, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            UnmanagedType unmanagedType = DecodeMarshalAsType(arguments.Attribute);

            switch (unmanagedType)
            {
                case Cci.Constants.UnmanagedType_CustomMarshaler:
                    DecodeMarshalAsCustom(ref arguments, messageProvider);
                    break;

                case UnmanagedType.Interface:
                case Cci.Constants.UnmanagedType_IDispatch:
                case UnmanagedType.IUnknown:
                    DecodeMarshalAsComInterface(ref arguments, unmanagedType, messageProvider);
                    break;

                case UnmanagedType.LPArray:
                    DecodeMarshalAsArray(ref arguments, messageProvider, isFixed: false);
                    break;

                case UnmanagedType.ByValArray:
                    if (target != AttributeTargets.Field)
                    {
                        messageProvider.ReportMarshalUnmanagedTypeOnlyValidForFields(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, "ByValArray", arguments.Attribute);
                    }
                    else
                    {
                        DecodeMarshalAsArray(ref arguments, messageProvider, isFixed: true);
                    }

                    break;

                case Cci.Constants.UnmanagedType_SafeArray:
                    DecodeMarshalAsSafeArray(ref arguments, messageProvider);
                    break;

                case UnmanagedType.ByValTStr:
                    if (target != AttributeTargets.Field)
                    {
                        messageProvider.ReportMarshalUnmanagedTypeOnlyValidForFields(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, "ByValTStr", arguments.Attribute);
                    }
                    else
                    {
                        DecodeMarshalAsFixedString(ref arguments, messageProvider);
                    }

                    break;

                case Cci.Constants.UnmanagedType_VBByRefStr:
                    if (target == AttributeTargets.Field)
                    {
                        messageProvider.ReportMarshalUnmanagedTypeNotValidForFields(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, "VBByRefStr", arguments.Attribute);
                    }
                    else
                    {
                        // named parameters ignored with no error
                        arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsSimpleType(unmanagedType);
                    }

                    break;

                default:
                    if ((int)unmanagedType < 0 || (int)unmanagedType > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                    {
                        // Dev10 reports CS0647: "Error emitting attribute ..."
                        messageProvider.ReportInvalidAttributeArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, arguments.Attribute);
                    }
                    else
                    {
                        // named parameters ignored with no error
                        arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsSimpleType(unmanagedType);
                    }

                    break;
            }
        }

        private static UnmanagedType DecodeMarshalAsType(AttributeData attribute)
        {
            UnmanagedType unmanagedType;
            if (attribute.AttributeConstructor.Parameters[0].Type.SpecialType == SpecialType.System_Int16)
            {
                unmanagedType = (UnmanagedType)attribute.CommonConstructorArguments[0].DecodeValue<short>(SpecialType.System_Int16);
            }
            else
            {
                unmanagedType = attribute.CommonConstructorArguments[0].DecodeValue<UnmanagedType>(SpecialType.System_Enum);
            }

            return unmanagedType;
        }

        private static void DecodeMarshalAsCustom(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            ITypeSymbolInternal typeSymbol = null;
            string typeName = null;
            string cookie = null;
            bool hasTypeName = false;
            bool hasTypeSymbol = false;
            bool hasErrors = false;

            int position = arguments.Attribute.CommonConstructorArguments.Length;
            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "MarshalType":
                        typeName = namedArg.Value.DecodeValue<string>(SpecialType.System_String);
                        if (!MetadataHelpers.IsValidUnicodeString(typeName))
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        hasTypeName = true; // even if MarshalType == null
                        break;

                    case "MarshalTypeRef":
                        typeSymbol = namedArg.Value.DecodeValue<ITypeSymbolInternal>(SpecialType.None);
                        hasTypeSymbol = true; // even if MarshalTypeRef == null
                        break;

                    case "MarshalCookie":
                        cookie = namedArg.Value.DecodeValue<string>(SpecialType.System_String);
                        if (!MetadataHelpers.IsValidUnicodeString(cookie))
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            if (!hasTypeName && !hasTypeSymbol)
            {
                // MarshalType or MarshalTypeRef must be specified:
                messageProvider.ReportAttributeParameterRequired(arguments.Diagnostics, arguments.AttributeSyntaxOpt, "MarshalType", "MarshalTypeRef");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsCustom(hasTypeName ? (object)typeName : typeSymbol, cookie);
            }
        }

        private static void DecodeMarshalAsComInterface(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, UnmanagedType unmanagedType, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            int? parameterIndex = null;
            int position = arguments.Attribute.CommonConstructorArguments.Length;
            bool hasErrors = false;

            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "IidParameterIndex":
                        parameterIndex = namedArg.Value.DecodeValue<int>(SpecialType.System_Int32);
                        if (parameterIndex < 0 || parameterIndex > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsComInterface(unmanagedType, parameterIndex);
            }
        }

        private static void DecodeMarshalAsArray(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, CommonMessageProvider messageProvider, bool isFixed)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            UnmanagedType? elementType = null;
            int? elementCount = isFixed ? 1 : (int?)null;
            short? parameterIndex = null;
            bool hasErrors = false;

            int position = arguments.Attribute.CommonConstructorArguments.Length;
            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    // array:
                    case "ArraySubType":
                        elementType = namedArg.Value.DecodeValue<UnmanagedType>(SpecialType.System_Enum);

                        // for some reason, Dev10 metadata writer disallows CustomMarshaler type as an element type of non-fixed arrays
                        if (!isFixed && elementType == Cci.Constants.UnmanagedType_CustomMarshaler ||
                            (int)elementType < 0 ||
                            (int)elementType > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;

                    case "SizeConst":
                        elementCount = namedArg.Value.DecodeValue<int>(SpecialType.System_Int32);
                        if (elementCount < 0 || elementCount > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;

                    case "SizeParamIndex":
                        if (isFixed)
                        {
                            goto case "SafeArraySubType";
                        }

                        parameterIndex = namedArg.Value.DecodeValue<short>(SpecialType.System_Int16);
                        if (parameterIndex < 0)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;

                    case "SafeArraySubType":
                        messageProvider.ReportParameterNotValidForType(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position);
                        hasErrors = true;
                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            if (!hasErrors)
            {
                var data = arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData();
                if (isFixed)
                {
                    data.SetMarshalAsFixedArray(elementType, elementCount);
                }
                else
                {
                    data.SetMarshalAsArray(elementType, elementCount, parameterIndex);
                }
            }
        }

        private static void DecodeMarshalAsSafeArray(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            Cci.VarEnum? elementTypeVariant = null;
            ITypeSymbolInternal elementTypeSymbol = null;
            int symbolIndex = -1;
            bool hasErrors = false;

            int position = arguments.Attribute.CommonConstructorArguments.Length;
            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "SafeArraySubType":
                        elementTypeVariant = namedArg.Value.DecodeValue<Cci.VarEnum>(SpecialType.System_Enum);
                        if (elementTypeVariant < 0 || (int)elementTypeVariant > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;

                    case "SafeArrayUserDefinedSubType":
                        elementTypeSymbol = namedArg.Value.DecodeValue<ITypeSymbolInternal>(SpecialType.None);
                        symbolIndex = position;
                        break;

                    case "ArraySubType":
                    case "SizeConst":
                    case "SizeParamIndex":
                        messageProvider.ReportParameterNotValidForType(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position);
                        hasErrors = true;
                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            switch (elementTypeVariant)
            {
                case Cci.VarEnum.VT_DISPATCH:
                case Cci.VarEnum.VT_UNKNOWN:
                case Cci.VarEnum.VT_RECORD:
                    // only these variants accept specification of user defined subtype
                    break;

                default:
                    if (elementTypeVariant != null && symbolIndex >= 0)
                    {
                        messageProvider.ReportParameterNotValidForType(arguments.Diagnostics, arguments.AttributeSyntaxOpt, symbolIndex);
                        hasErrors = true;
                    }
                    else
                    {
                        // type ignored:
                        elementTypeSymbol = null;
                    }

                    break;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsSafeArray(elementTypeVariant, elementTypeSymbol);
            }
        }

        private static void DecodeMarshalAsFixedString(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            int elementCount = -1;
            int position = arguments.Attribute.CommonConstructorArguments.Length;
            bool hasErrors = false;

            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "SizeConst":
                        elementCount = namedArg.Value.DecodeValue<int>(SpecialType.System_Int32);
                        if (elementCount < 0 || elementCount > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;

                    case "ArraySubType":
                    case "SizeParamIndex":
                        messageProvider.ReportParameterNotValidForType(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position);
                        hasErrors = true;
                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            if (elementCount < 0)
            {
                // SizeConst must be specified:
                messageProvider.ReportAttributeParameterRequired(arguments.Diagnostics, arguments.AttributeSyntaxOpt, "SizeConst");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsFixedString(elementCount);
            }
        }
    }
}
