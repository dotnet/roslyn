// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IParameterSymbolExtensions
    {
        public static bool IsRefOrOut(this IParameterSymbol symbol)
        {
            switch (symbol.RefKind)
            {
                case RefKind.Ref:
                case RefKind.Out:
                    return true;
                default:
                    return false;
            }
        }

        public static IParameterSymbol RenameParameter(this IParameterSymbol parameter, string parameterName)
        {
            return parameter.Name == parameterName
                ? parameter
                : CodeGenerationSymbolFactory.CreateParameterSymbol(
                        parameter.GetAttributes(),
                        parameter.RefKind,
                        parameter.IsParams,
                        parameter.Type,
                        parameterName,
                        parameter.IsOptional,
                        parameter.HasExplicitDefaultValue,
                        parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null);
        }

        public static IParameterSymbol WithAttributes(this IParameterSymbol parameter, ImmutableArray<AttributeData> attributes)
        {
            return parameter.GetAttributes() == attributes
                ? parameter
                : CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes,
                        parameter.RefKind,
                        parameter.IsParams,
                        parameter.Type,
                        parameter.Name,
                        parameter.IsOptional,
                        parameter.HasExplicitDefaultValue,
                        parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null);
        }

        public static ImmutableArray<IParameterSymbol> WithAttributesToBeCopied(
            this ImmutableArray<IParameterSymbol> parameters, INamedTypeSymbol containingType)
            => parameters.SelectAsArray(
                p => p.WithAttributes(p.GetAttributes().WhereAsArray(a => a.ShouldKeepAttribute(containingType))));

        public static ImmutableArray<IParameterSymbol> RenameParameters(this IList<IParameterSymbol> parameters, IList<string> parameterNames)
        {
            var result = ArrayBuilder<IParameterSymbol>.GetInstance();
            for (var i = 0; i < parameterNames.Count; i++)
            {
                result.Add(parameters[i].RenameParameter(parameterNames[i]));
            }

            return result.ToImmutableAndFree();
        }

        private static bool ShouldKeepAttribute(this AttributeData attributeData, INamedTypeSymbol containingType)
            => attributeData.AttributeClass.IsAccessibleWithin(containingType);
    }
}
