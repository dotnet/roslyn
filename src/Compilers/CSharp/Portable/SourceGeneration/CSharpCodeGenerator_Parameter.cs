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
    internal partial class CSharpCodeGenerator
    {
        private static ParameterListSyntax GenerateParameterList(ImmutableArray<IParameterSymbol> parameters)
        {
            using var _ = GetArrayBuilder<ParameterSyntax>(out var builder);

            foreach (var parameter in parameters)
                builder.Add(GenerateParameter(parameter));

            return ParameterList(SeparatedList(builder));
        }

        private static ParameterSyntax GenerateParameter(IParameterSymbol parameter)
        {
            var expression = GenerateConstantExpression(parameter.Type, parameter.HasExplicitDefaultValue, parameter.ExplicitDefaultValue);
            var equalsValue = expression == null ? null : EqualsValueClause(expression);
            return Parameter(
                GenerateAttributeLists(parameter.GetAttributes()),
                GenerateModifiers(Accessibility.NotApplicable, parameter.GetModifiers()),
                parameter.Type?.GenerateTypeSyntax(),
                Identifier(parameter.Name),
                equalsValue);
        }
    }
}
