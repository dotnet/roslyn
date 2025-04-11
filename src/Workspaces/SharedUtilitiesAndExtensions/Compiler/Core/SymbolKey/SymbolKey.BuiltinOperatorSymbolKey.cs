// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class BuiltinOperatorSymbolKey : AbstractSymbolKey<IMethodSymbol>
    {
        public static readonly BuiltinOperatorSymbolKey Instance = new();

        public sealed override void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.Name);
            visitor.WriteSymbolKey(symbol.ReturnType);
            visitor.WriteParameterTypesArray(symbol.Parameters);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IMethodSymbol? contextualSymbol, out string? failureReason)
        {
            var name = reader.ReadRequiredString();

            var returnType = reader.ReadSymbolKey(contextualSymbol?.ReturnType, out var returnTypeFailureReason);
            using var parameterTypes = reader.ReadSymbolKeyArray<IMethodSymbol, ITypeSymbol>(
                contextualSymbol,
                static (contextualSymbol, i) => SafeGet(contextualSymbol.Parameters, i)?.Type,
                out var parameterTypesFailureReason);

            if (returnTypeFailureReason != null)
            {
                failureReason = $"({nameof(BuiltinOperatorSymbolKey)} {nameof(returnType)} failed -> {returnTypeFailureReason})";
                return default;
            }

            if (parameterTypesFailureReason != null)
            {
                failureReason = $"({nameof(BuiltinOperatorSymbolKey)} {nameof(parameterTypes)} failed -> {parameterTypesFailureReason})";
                return default;
            }

            var returnTypeSymbol = (ITypeSymbol?)returnType.GetAnySymbol();
            Contract.ThrowIfNull(returnTypeSymbol);

            try
            {
                switch (parameterTypes.Count)
                {
                    case 1:
                        failureReason = null;
                        var unaryOperator = reader.Compilation.CreateBuiltinOperator(name, returnTypeSymbol, parameterTypes[0]);
                        return new SymbolKeyResolution(unaryOperator);
                    case 2:
                        failureReason = null;
                        var binaryOperator = reader.Compilation.CreateBuiltinOperator(name, returnTypeSymbol, parameterTypes[0], parameterTypes[1]);
                        return new SymbolKeyResolution(binaryOperator);

                    default:
                        failureReason = $"({nameof(BuiltinOperatorSymbolKey)} {nameof(parameterTypes)} failed -> count was {parameterTypes.Count})";
                        return default;
                }
            }
            catch (ArgumentException ex)
            {
                failureReason = $"({nameof(BuiltinOperatorSymbolKey)} failed -> {ex.Message})";
                return default;
            }
        }
    }
}
