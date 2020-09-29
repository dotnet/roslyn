// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
#nullable enable

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class FunctionPointerTypeSymbolKey
        {
            public static void Create(IFunctionPointerTypeSymbol symbol, SymbolKeyWriter visitor)
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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var callingConvention = (SignatureCallingConvention)reader.ReadInteger();

                var callingConventionModifiers = ImmutableArray<INamedTypeSymbol>.Empty;
                if (callingConvention == SignatureCallingConvention.Unmanaged)
                {
                    using var modifiersBuilder = reader.ReadSymbolKeyArray<INamedTypeSymbol>(out var conventionTypesFailureReason);
                    if (conventionTypesFailureReason != null)
                    {
                        failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} {nameof(callingConventionModifiers)} failed -> {conventionTypesFailureReason})";
                        return default;
                    }

                    callingConventionModifiers = modifiersBuilder.ToImmutable();
                }

                var returnRefKind = reader.ReadRefKind();
                var returnType = reader.ReadSymbolKey(out var returnTypeFailureReason);
                using var paramRefKinds = reader.ReadRefKindArray();
                using var parameterTypes = reader.ReadSymbolKeyArray<ITypeSymbol>(out var parameterTypesFailureReason);

                if (returnTypeFailureReason != null)
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} {nameof(returnType)} failed -> {returnTypeFailureReason})";
                    return default;
                }

                if (parameterTypesFailureReason != null)
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} {nameof(parameterTypes)} failed -> {parameterTypesFailureReason})";
                    return default;
                }

                if (parameterTypes.IsDefault)
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} no parameter types)";
                    return default;
                }

                if (!(returnType.GetAnySymbol() is ITypeSymbol returnTypeSymbol))
                {
                    failureReason = $"({nameof(FunctionPointerTypeSymbolKey)} no return type)";
                    return default;
                }

                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.CreateFunctionPointerTypeSymbol(
                    returnTypeSymbol, returnRefKind, parameterTypes.ToImmutable(), paramRefKinds.ToImmutable(), callingConvention, callingConventionModifiers));
            }
        }
    }
}
