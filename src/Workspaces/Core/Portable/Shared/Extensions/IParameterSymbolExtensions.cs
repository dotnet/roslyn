// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IParameterSymbolExtensions
    {
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

        public static ImmutableArray<IParameterSymbol> RenameParameters(this IList<IParameterSymbol> parameters, ImmutableArray<string> parameterNames)
        {
            var result = ArrayBuilder<IParameterSymbol>.GetInstance();
            for (var i = 0; i < parameterNames.Length; i++)
            {
                result.Add(parameters[i].RenameParameter(parameterNames[i]));
            }

            return result.ToImmutableAndFree();
        }

        public static IPropertySymbol? GetAssociatedSynthesizedRecordProperty(this IParameterSymbol parameter, CancellationToken cancellationToken)
        {
            if (parameter is
                {
                    DeclaringSyntaxReferences.Length: > 0,
                    ContainingSymbol: IMethodSymbol
                    {
                        MethodKind: MethodKind.Constructor,
                        DeclaringSyntaxReferences.Length: > 0,
                        ContainingType: { IsRecord: true } containingType,
                    } constructor,
                })
            {
                // ok, we have a record constructor.  This might be the primary constructor or not.
                var parameterSyntax = parameter.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                var constructorSyntax = constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (containingType.DeclaringSyntaxReferences.Any(r => r.GetSyntax(cancellationToken) == constructorSyntax))
                {
                    // this was a primary constructor. see if we can map this parameter to a corresponding synthesized property 
                    foreach (var member in containingType.GetMembers(parameter.Name))
                    {
                        if (member is IPropertySymbol { DeclaringSyntaxReferences.Length: > 0 } property &&
                            property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) == parameterSyntax)
                        {
                            return property;
                        }
                    }
                }
            }

            return null;
        }
    }
}
