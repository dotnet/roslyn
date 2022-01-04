// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class ExplicitInterfaceMemberCompletionProvider
    {
        private static class CompletionSymbolDisplay
        {
            public static string ToDisplayString(ISymbol symbol)
                => symbol switch
                {
                    IEventSymbol eventSymbol => ToDisplayString(eventSymbol),
                    IPropertySymbol propertySymbol => ToDisplayString(propertySymbol),
                    IMethodSymbol methodSymbol => ToDisplayString(methodSymbol),
                    _ => "" // This shouldn't happen.
                };

            private static string ToDisplayString(IEventSymbol symbol)
                => symbol.Name;

            private static string ToDisplayString(IPropertySymbol symbol)
            {
                using var _ = PooledStringBuilder.GetInstance(out var builder);

                if (symbol.IsIndexer)
                {
                    builder.Append("this");
                }
                else
                {
                    builder.Append(symbol.Name);
                }

                if (symbol.Parameters.Length > 0)
                {
                    builder.Append('[');
                    AddParameters(symbol.Parameters, builder);
                    builder.Append(']');
                }

                return pooledBuilder.ToString();
            }

            private static string ToDisplayString(IMethodSymbol symbol)
            {
                using var _ = PooledStringBuilder.GetInstance(out var builder);
                switch (symbol.MethodKind)
                {
                    case MethodKind.Ordinary:
                        builder.Append(symbol.Name);
                        break;
                    case MethodKind.UserDefinedOperator:
                    case MethodKind.BuiltinOperator:
                        builder.Append("operator ");
                        builder.Append(SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(symbol.MetadataName)));
                        break;
                    case MethodKind.Conversion:
                        builder.Append("operator ");
                        AddType(symbol.ReturnType, builder);
                        break;
                }

                AddTypeArguments(symbol, builder);
                builder.Append('(');
                AddParameters(symbol.Parameters, builder);
                builder.Append(')');
                return pooledBuilder.ToString();
            }

            private static void AddParameters(ImmutableArray<IParameterSymbol> parameters, StringBuilder builder)
            {
                var first = true;
                foreach (var parameter in parameters)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    first = false;
                    builder.Append(parameter.RefKind switch
                    {
                        RefKind.Out => "out ",
                        RefKind.Ref => "ref ",
                        RefKind.In => "in ",
                        _ => ""
                    });

                    AddType(parameter.Type, builder);
                    builder.Append($" {parameter.Name}");
                }
            }

            private static void AddTypeArguments(IMethodSymbol symbol, StringBuilder builder)
            {
                if (symbol.TypeArguments.Length > 0)
                {
                    builder.Append('<');

                    var first = true;
                    foreach (var typeArgument in symbol.TypeArguments)
                    {
                        if (!first)
                        {
                            builder.Append(", ");
                        }

                        first = false;
                        builder.Append(typeArgument.Name);
                    }

                    builder.Append('>');
                }
            }

            private static void AddType(ITypeSymbol symbol, StringBuilder builder)
                => builder.Append(symbol.ToNameDisplayString());
        }
    }
}
