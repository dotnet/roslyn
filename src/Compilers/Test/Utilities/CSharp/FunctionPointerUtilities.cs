// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal static class FunctionPointerUtilities
    {
        internal static void CommonVerifyFunctionPointer(FunctionPointerTypeSymbol symbol)
        {
            verifyPointerType(symbol);
            verifySignature(symbol.Signature);
            foreach (var param in symbol.Signature.Parameters)
            {
                verifyParameter(param, symbol.Signature);
            }

            static void verifyPointerType(FunctionPointerTypeSymbol symbol)
            {
                Assert.Equal(SymbolKind.FunctionPointer, symbol.Kind);
                Assert.Equal(TypeKind.FunctionPointer, symbol.TypeKind);

                Assert.False(symbol.IsReferenceType);
                Assert.False(symbol.IsRefLikeType);
                Assert.False(symbol.IsReadOnly);
                Assert.False(symbol.IsStatic);
                Assert.False(symbol.IsAbstract);
                Assert.False(symbol.IsSealed);

                Assert.True(symbol.IsValueType);
                Assert.True(symbol.CanBeAssignedNull());

                Assert.Null(symbol.ContainingSymbol);
                Assert.Null(symbol.BaseTypeNoUseSiteDiagnostics);
                Assert.Null(symbol.ObsoleteAttributeData);

                Assert.Empty(symbol.Locations);
                Assert.Empty(symbol.DeclaringSyntaxReferences);
                Assert.Empty(symbol.GetMembers());
                Assert.Empty(symbol.GetTypeMembers());
                Assert.Empty(symbol.InterfacesNoUseSiteDiagnostics());
            }

            static void verifySignature(MethodSymbol symbol)
            {
                Assert.NotNull(symbol);

                Assert.Equal(MethodKind.FunctionPointerSignature, symbol.MethodKind);
                Assert.Equal(string.Empty, symbol.Name);
                Assert.Equal(0, symbol.Arity);
                Assert.Equal(default, symbol.ImplementationAttributes);
                Assert.Equal(Accessibility.NotApplicable, symbol.DeclaredAccessibility);
                Assert.Equal(FlowAnalysisAnnotations.None, symbol.ReturnTypeFlowAnalysisAnnotations);
                Assert.Equal(FlowAnalysisAnnotations.None, symbol.FlowAnalysisAnnotations);

                Assert.False(symbol.IsExtensionMethod);
                Assert.False(symbol.HidesBaseMethodsByName);
                Assert.False(symbol.IsStatic);
                Assert.False(symbol.IsAsync);
                Assert.False(symbol.IsVirtual);
                Assert.False(symbol.IsOverride);
                Assert.False(symbol.IsAbstract);
                Assert.False(symbol.IsExtern);
                Assert.False(symbol.IsExtensionMethod);
                Assert.False(symbol.IsSealed);
                Assert.False(symbol.IsExtern);
                Assert.False(symbol.HasSpecialName);
                Assert.False(symbol.HasDeclarativeSecurity);
                Assert.False(symbol.RequiresSecurityObject);
                Assert.False(symbol.IsDeclaredReadOnly);
                Assert.False(symbol.IsMetadataNewSlot(true));
                Assert.False(symbol.IsMetadataNewSlot(false));
                Assert.False(symbol.IsMetadataVirtual(true));
                Assert.False(symbol.IsMetadataVirtual(false));

                Assert.True(symbol.IsImplicitlyDeclared);

                Assert.Null(symbol.ContainingSymbol);
                Assert.Null(symbol.AssociatedSymbol);
                Assert.Null(symbol.ReturnValueMarshallingInformation);

                Assert.Empty(symbol.TypeParameters);
                Assert.Empty(symbol.ExplicitInterfaceImplementations);
                Assert.Empty(symbol.Locations);
                Assert.Empty(symbol.DeclaringSyntaxReferences);
                Assert.Empty(symbol.TypeArgumentsWithAnnotations);
                Assert.Empty(symbol.GetAppliedConditionalSymbols());
                Assert.Empty(symbol.ReturnNotNullIfParameterNotNull);
            }

            static void verifyParameter(ParameterSymbol symbol, MethodSymbol containing)
            {
                Assert.NotNull(symbol);

                Assert.Same(symbol.ContainingSymbol, containing);

                Assert.Equal(string.Empty, symbol.Name);
                Assert.Equal(FlowAnalysisAnnotations.None, symbol.FlowAnalysisAnnotations);

                Assert.False(symbol.IsDiscard);
                Assert.False(symbol.IsParams);
                Assert.False(symbol.IsMetadataOptional);
                Assert.False(symbol.IsIDispatchConstant);
                Assert.False(symbol.IsIUnknownConstant);
                Assert.False(symbol.IsCallerFilePath);
                Assert.False(symbol.IsCallerLineNumber);
                Assert.False(symbol.IsCallerFilePath);
                Assert.False(symbol.IsCallerMemberName);

                Assert.True(symbol.IsImplicitlyDeclared);

                Assert.Null(symbol.MarshallingInformation);
                Assert.Null(symbol.ExplicitDefaultConstantValue);

                Assert.Empty(symbol.Locations);
                Assert.Empty(symbol.DeclaringSyntaxReferences);
                Assert.Empty(symbol.NotNullIfParameterNotNull);
            }
        }

        public static void VerifyFunctionPointerSymbol(TypeSymbol type, CallingConvention expectedConvention, (RefKind RefKind, Action<TypeSymbol> TypeVerifier) returnVerifier, params (RefKind RefKind, Action<TypeSymbol> TypeVerifier)[] argumentVerifiers)
        {
            FunctionPointerTypeSymbol funcPtr = (FunctionPointerTypeSymbol)type;

            FunctionPointerUtilities.CommonVerifyFunctionPointer(funcPtr);

            var signature = funcPtr.Signature;
            Assert.Equal(expectedConvention, signature.CallingConvention);

            Assert.Equal(returnVerifier.RefKind, signature.RefKind);
            switch (signature.RefKind)
            {
                case RefKind.RefReadOnly:
                    Assert.True(CustomModifierUtils.HasInAttributeModifier(signature.RefCustomModifiers));
                    Assert.False(CustomModifierUtils.HasOutAttributeModifier(signature.RefCustomModifiers));
                    break;

                case RefKind.None:
                case RefKind.Ref:
                    Assert.False(CustomModifierUtils.HasInAttributeModifier(signature.RefCustomModifiers));
                    Assert.False(CustomModifierUtils.HasOutAttributeModifier(signature.RefCustomModifiers));
                    break;

                case RefKind.Out:
                default:
                    Assert.True(false, $"Cannot have a return ref kind of {signature.RefKind}");
                    break;
            }
            returnVerifier.TypeVerifier(signature.ReturnType);

            Assert.Equal(argumentVerifiers.Length, signature.ParameterCount);
            for (int i = 0; i < argumentVerifiers.Length; i++)
            {
                var parameter = signature.Parameters[i];
                Assert.Equal(argumentVerifiers[i].RefKind, parameter.RefKind);
                argumentVerifiers[i].TypeVerifier(parameter.Type);
                switch (parameter.RefKind)
                {
                    case RefKind.Out:
                        Assert.True(CustomModifierUtils.HasOutAttributeModifier(parameter.RefCustomModifiers));
                        Assert.False(CustomModifierUtils.HasInAttributeModifier(parameter.RefCustomModifiers));
                        break;

                    case RefKind.In:
                        Assert.True(CustomModifierUtils.HasInAttributeModifier(parameter.RefCustomModifiers));
                        Assert.False(CustomModifierUtils.HasOutAttributeModifier(parameter.RefCustomModifiers));
                        break;

                    case RefKind.Ref:
                    case RefKind.None:
                        Assert.False(CustomModifierUtils.HasInAttributeModifier(parameter.RefCustomModifiers));
                        Assert.False(CustomModifierUtils.HasOutAttributeModifier(parameter.RefCustomModifiers));
                        break;

                    default:
                        Assert.True(false, $"Cannot have a return ref kind of {parameter.RefKind}");
                        break;
                }
            }
        }

        public static Action<TypeSymbol> IsVoidType() => typeSymbol => Assert.True(typeSymbol.IsVoidType());

        public static Action<TypeSymbol> IsSpecialType(SpecialType specialType)
            => typeSymbol => Assert.Equal(specialType, typeSymbol.SpecialType);

        public static Action<TypeSymbol> IsTypeName(string typeName)
            => typeSymbol => Assert.Equal(typeName, typeSymbol.Name);

        public static Action<TypeSymbol> IsArrayType(Action<TypeSymbol> arrayTypeVerifier)
            => typeSymbol =>
            {
                Assert.True(typeSymbol.IsArray());
                arrayTypeVerifier(((ArrayTypeSymbol)typeSymbol).ElementType);
            };

        public static Action<TypeSymbol> IsUnsupportedType()
            => typeSymbol => Assert.True(typeSymbol is UnsupportedMetadataTypeSymbol);

        public static Action<TypeSymbol> IsFunctionPointerTypeSymbol(CallingConvention callingConvention, (RefKind, Action<TypeSymbol>) returnVerifier, params (RefKind, Action<TypeSymbol>)[] argumentVerifiers)
            => typeSymbol => VerifyFunctionPointerSymbol((FunctionPointerTypeSymbol)typeSymbol, callingConvention, returnVerifier, argumentVerifiers);

        public static Action<TypeSymbol> IsErrorType()
            => typeSymbol => Assert.True(typeSymbol.IsErrorType());

    }
}
