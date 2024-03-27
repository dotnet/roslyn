// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
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
                Assert.Equal(SymbolKind.FunctionPointerType, symbol.Kind);
                Assert.Equal(TypeKind.FunctionPointer, symbol.TypeKind);

                Assert.False(symbol.IsReferenceType);
                Assert.False(symbol.IsRefLikeType);
                Assert.False(symbol.IsReadOnly);
                Assert.False(symbol.IsStatic);
                Assert.False(symbol.IsAbstract);
                Assert.False(symbol.IsSealed);
                Assert.False(symbol.CanBeReferencedByName);
                Assert.True(symbol.IsTypeOrTypeAlias());

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

                Assert.Equal(symbol.IsVararg, symbol.CallingConvention.IsCallingConvention(CallingConvention.ExtraArguments));

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
                Assert.False(symbol.IsParamsArray);
                Assert.False(symbol.IsParamsCollection);
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

        public static void VerifyFunctionPointerSemanticInfo(
            SemanticModel model,
            SyntaxNode syntax,
            string expectedSyntax,
            string? expectedType,
            string? expectedConvertedType = null,
            string? expectedSymbol = null,
            CandidateReason expectedCandidateReason = CandidateReason.None,
            string[]? expectedSymbolCandidates = null)
        {
            AssertEx.Equal(expectedSyntax, syntax.ToString());
            var semanticInfo = model.GetSemanticInfoSummary(syntax);
            ITypeSymbol? exprType;

            if (expectedType is null)
            {
                exprType = semanticInfo.ConvertedType;
                Assert.Null(semanticInfo.Type);
            }
            else
            {
                exprType = semanticInfo.Type;
                AssertEx.Equal(expectedType, semanticInfo.Type.ToTestDisplayString(includeNonNullable: false));
            }

            if (expectedConvertedType is null)
            {
                Assert.Equal(semanticInfo.Type, semanticInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability);
            }
            else
            {
                AssertEx.Equal(expectedConvertedType, semanticInfo.ConvertedType.ToTestDisplayString(includeNonNullable: false));
            }

            verifySymbolInfo(expectedSymbol, expectedCandidateReason, expectedSymbolCandidates, semanticInfo);

            if (exprType is IFunctionPointerTypeSymbol ptrType)
            {
                CommonVerifyFunctionPointer(ptrType.GetSymbol());
            }

            switch (syntax)
            {
                case FunctionPointerTypeSyntax { ParameterList: { Parameters: var paramSyntaxes } }:
                    verifyNestedFunctionPointerSyntaxSemanticInfo(model, (IFunctionPointerTypeSymbol)exprType, paramSyntaxes);
                    break;

                case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.AddressOfExpression, Operand: var operand }:
                    // Members should only be accessible from the underlying operand
                    Assert.Empty(semanticInfo.MemberGroup);
                    var expectedConversionKind = (expectedType, expectedConvertedType, expectedSymbol) switch
                    {
                        (null, null, _) => ConversionKind.Identity,
                        (_, _, null) => ConversionKind.NoConversion,
                        (_, _, _) => ConversionKind.MethodGroup,
                    };
                    Assert.Equal(expectedConversionKind, semanticInfo.ImplicitConversion.Kind);

                    semanticInfo = model.GetSemanticInfoSummary(operand);
                    Assert.Null(semanticInfo.Type);
                    Assert.Null(semanticInfo.ConvertedType);
                    if (expectedSymbolCandidates != null)
                    {
                        AssertEx.Equal(expectedSymbolCandidates, semanticInfo.MemberGroup.Select(s => s.ToTestDisplayString(includeNonNullable: false)));
                    }
                    else
                    {
                        Assert.Contains(semanticInfo.MemberGroup, actual => actual.ToTestDisplayString(includeNonNullable: false) == expectedSymbol);
                    }

                    verifySymbolInfo(expectedSymbol, expectedCandidateReason, expectedSymbolCandidates, semanticInfo);

                    break;
            }

            static void verifySymbolInfo(
                string? expectedSymbol,
                CandidateReason expectedReason,
                string[]? expectedSymbolCandidates,
                CompilationUtils.SemanticInfoSummary semanticInfo)
            {
                if (expectedSymbol is object)
                {
                    Assert.Empty(semanticInfo.CandidateSymbols);
                    AssertEx.Equal(expectedSymbol, semanticInfo.Symbol.ToTestDisplayString(includeNonNullable: false));
                    Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
                }
                else if (expectedSymbolCandidates is object)
                {
                    Assert.Null(semanticInfo.Symbol);
                    Assert.Equal(expectedReason, semanticInfo.CandidateReason);
                    AssertEx.Equal(expectedSymbolCandidates, semanticInfo.CandidateSymbols.Select(s => s.ToTestDisplayString(includeNonNullable: false)));
                }
                else
                {
                    Assert.Null(semanticInfo.Symbol);
                    Assert.Empty(semanticInfo.CandidateSymbols);
                    Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
                }
            }

            static void verifyNestedFunctionPointerSyntaxSemanticInfo(SemanticModel model, IFunctionPointerTypeSymbol ptrType, SeparatedSyntaxList<FunctionPointerParameterSyntax> paramSyntaxes)
            {
                // https://github.com/dotnet/roslyn/issues/43321 Nullability in type syntaxes that don't have an origin bound node
                // can differ.
                var signature = ptrType.Signature;
                for (int i = 0; i < paramSyntaxes.Count - 1; i++)
                {
                    var paramSyntax = paramSyntaxes[i].Type!;
                    ITypeSymbol signatureParamType = signature.Parameters[i].Type;
                    assertEqualSemanticInformation(model, paramSyntax, signatureParamType);
                }

                var returnParam = paramSyntaxes[^1].Type;
                assertEqualSemanticInformation(model, returnParam!, signature.ReturnType);
            }

            static void assertEqualSemanticInformation(SemanticModel model, TypeSyntax typeSyntax, ITypeSymbol signatureType)
            {
                var semanticInfo = model.GetSemanticInfoSummary(typeSyntax);
                Assert.Equal<ISymbol>(signatureType, semanticInfo.Type, SymbolEqualityComparer.Default);
                Assert.Equal(semanticInfo.Type, semanticInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability);

                Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
                Assert.Equal(signatureType, semanticInfo.Type, SymbolEqualityComparer.Default);
                Assert.Empty(semanticInfo.CandidateSymbols);

                if (typeSyntax is FunctionPointerTypeSyntax { ParameterList: { Parameters: var paramSyntaxes } })
                {
                    var paramPtrType = (IFunctionPointerTypeSymbol)semanticInfo.Type!;
                    CommonVerifyFunctionPointer(paramPtrType.GetSymbol());
                    verifyNestedFunctionPointerSyntaxSemanticInfo(model, paramPtrType, paramSyntaxes);
                }
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
