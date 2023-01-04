// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private sealed class FunctionPointerTypeSymbolKey : AbstractSymbolKey<IFunctionPointerTypeSymbol>
        {
            public static readonly FunctionPointerTypeSymbolKey Instance = new();

            public sealed override void Create(IFunctionPointerTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                var callingConvention = symbol.Signature.CallingConvention;
                visitor.WriteInteger((int)callingConvention);

                if (callingConvention == SignatureCallingConvention.Unmanaged)
                {
                    visitor.WriteSymbolKeyArray(symbol.Signature.UnmanagedCallingConventionTypes);
                }

                visitor.WriteRefKind(symbol.Signature.RefKind);
                visitor.WriteSymbolKey(symbol.Signature.ReturnType);
                visitor.WriteRefKindArray(symbol.Signature.Parameters);
                visitor.WriteParameterTypesArray(symbol.Signature.Parameters);
            }

            protected sealed override SymbolKeyResolution Resolve(
                SymbolKeyReader reader, IFunctionPointerTypeSymbol? contextualSymbol, out string? failureReason)
            {
                var callingConvention = (SignatureCallingConvention)reader.ReadInteger();

                var callingConventionModifiers = ImmutableArray<INamedTypeSymbol>.Empty;
                if (callingConvention == SignatureCallingConvention.Unmanaged)
                {
                    using var modifiersBuilder = reader.ReadSymbolKeyArray<IFunctionPointerTypeSymbol, INamedTypeSymbol>(
                        contextualSymbol,
                        static (contextualSymbol, i) => SafeGet(contextualSymbol.Signature.UnmanagedCallingConventionTypes, i),
                        out var conventionTypesFailureReason);
                    if (conventionTypesFailureReason != null)
                    {
                        failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} {nameof(callingConventionModifiers)} failed -> {conventionTypesFailureReason})";
                        return default;
                    }

                    callingConventionModifiers = modifiersBuilder.ToImmutable();
                }

                var returnRefKind = reader.ReadRefKind();
                var returnType = reader.ReadSymbolKey(contextualSymbol?.Signature.ReturnType, out var returnTypeFailureReason);
                using var paramRefKinds = reader.ReadRefKindArray();
                using var parameterTypes = reader.ReadSymbolKeyArray<IFunctionPointerTypeSymbol, ITypeSymbol>(
                    contextualSymbol,
                    static (contextualSymbol, i) => SafeGet(contextualSymbol.Signature.Parameters, i)?.Type,
                    out var parameterTypesFailureReason);

                if (returnTypeFailureReason != null)
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} {nameof(returnType)} failed -> {returnTypeFailureReason})";
                    return default;
                }

                if (parameterTypesFailureReason != null)
                {
                    Contract.ThrowIfFalse(parameterTypes.IsDefault);
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} {nameof(parameterTypes)} failed -> {parameterTypesFailureReason})";
                    return default;
                }

                Contract.ThrowIfTrue(parameterTypes.IsDefault);

                if (returnType.GetAnySymbol() is not ITypeSymbol returnTypeSymbol)
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} no return type)";
                    return default;
                }

                if (reader.Compilation.Language == LanguageNames.VisualBasic)
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} is not supported in {LanguageNames.VisualBasic})";
                    return default;
                }

                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.CreateFunctionPointerTypeSymbol(
                    returnTypeSymbol, returnRefKind, parameterTypes.ToImmutable(), paramRefKinds.ToImmutable(), callingConvention, callingConventionModifiers));
            }
        }
    }
}
