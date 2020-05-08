// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private ParameterListSyntax GenerateParameterList(ImmutableArray<IParameterSymbol> parameters)
        {
            using var _ = GetArrayBuilder<ParameterSyntax>(out var builder);

            foreach (var parameter in parameters)
                builder.Add(GenerateParameter(parameter));

            return ParameterList(SeparatedList(builder));
        }

        private BracketedParameterListSyntax GenerateBracketedParameterList(ImmutableArray<IParameterSymbol> parameters)
        {
            using var _ = GetArrayBuilder<ParameterSyntax>(out var builder);

            foreach (var parameter in parameters)
                builder.Add(GenerateParameter(parameter));

            return BracketedParameterList(SeparatedList(builder));
        }

        private ParameterSyntax GenerateParameter(IParameterSymbol parameter)
        {
            var expression = GenerateConstantExpression(
                parameter.Type,
                parameter.HasExplicitDefaultValue,
                parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null);
            var equalsValue = expression == null ? null : EqualsValueClause(expression);
            return Parameter(
                GenerateAttributeLists(parameter.GetAttributes()),
                GenerateModifiers(parameter),
                parameter.Type?.GenerateTypeSyntax(),
                Identifier(parameter.Name),
                equalsValue);
        }
    }
}
