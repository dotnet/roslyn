// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static SyntaxList<AttributeListSyntax> GenerateAttributeLists(ImmutableArray<AttributeData> attributes)
        {
            using var _ = GetArrayBuilder<AttributeListSyntax>(out var result);

            foreach (var attributeData in attributes)
                result.AddIfNotNull(GenerateAttributeList(attributeData));

            return List(result);
        }

        private static AttributeListSyntax GenerateAttributeList(AttributeData attributeData)
        {
            using var _ = GetArrayBuilder<AttributeSyntax>(out var result);

            result.Add(GenerateAttribute(attributeData));

            return AttributeList(SeparatedList(result));
        }

        private static AttributeSyntax GenerateAttribute(AttributeData attributeData)
        {
            using var _ = GetArrayBuilder<AttributeArgumentSyntax>(out var arguments);

            foreach (var arg in attributeData.ConstructorArguments)
                arguments.Add(AttributeArgument(GenerateConstantExpression(arg)));

            foreach (var arg in attributeData.NamedArguments)
            {
                arguments.Add(AttributeArgument(
                    NameEquals(IdentifierName(arg.Key)),
                    nameColon: null,
                    GenerateConstantExpression(arg.Value)));
            }

            return Attribute(
                attributeData.AttributeClass.GenerateNameSyntax(),
                AttributeArgumentList(SeparatedList(arguments)));
        }
    }
}
