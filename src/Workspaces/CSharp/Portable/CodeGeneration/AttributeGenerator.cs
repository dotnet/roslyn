// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class AttributeGenerator
    {
        public static SyntaxList<AttributeListSyntax> GenerateAttributeLists(
            ImmutableArray<AttributeData> attributes,
            CodeGenerationOptions options,
            SyntaxToken? target = null)
        {
            if (options.MergeAttributes)
            {
                var attributeNodes = attributes.OrderBy(a => a.AttributeClass.Name).Select(a => GenerateAttribute(a, options)).WhereNotNull().ToList();
                return attributeNodes.Count == 0
                    ? default
                    : SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(
                        target.HasValue ? SyntaxFactory.AttributeTargetSpecifier(target.Value) : null,
                        SyntaxFactory.SeparatedList(attributeNodes)));
            }
            else
            {
                var attributeDeclarations = attributes.OrderBy(a => a.AttributeClass.Name).Select(a => GenerateAttributeDeclaration(a, target, options)).WhereNotNull().ToList();
                return attributeDeclarations.Count == 0
                    ? default
                    : SyntaxFactory.List<AttributeListSyntax>(attributeDeclarations);
            }
        }

        private static AttributeListSyntax GenerateAttributeDeclaration(
            AttributeData attribute, SyntaxToken? target, CodeGenerationOptions options)
        {
            var attributeSyntax = GenerateAttribute(attribute, options);
            return attributeSyntax == null
                ? null
                : SyntaxFactory.AttributeList(
                    target.HasValue ? SyntaxFactory.AttributeTargetSpecifier(target.Value) : null,
                    SyntaxFactory.SingletonSeparatedList(attributeSyntax));
        }

        private static AttributeSyntax GenerateAttribute(AttributeData attribute, CodeGenerationOptions options)
        {
            if (!options.MergeAttributes)
            {
                var reusableSyntax = GetReuseableSyntaxNodeForAttribute<AttributeSyntax>(attribute, options);
                if (reusableSyntax != null)
                {
                    return reusableSyntax;
                }
            }

            var attributeArguments = GenerateAttributeArgumentList(attribute);
            return !(attribute.AttributeClass.GenerateTypeSyntax() is NameSyntax nameSyntax) ? null : SyntaxFactory.Attribute(nameSyntax, attributeArguments);
        }

        private static AttributeArgumentListSyntax GenerateAttributeArgumentList(AttributeData attribute)
        {
            if (attribute is { ConstructorArguments: { Length: 0 }, NamedArguments: { Length: 0 } })
            {
                return null;
            }

            var arguments = new List<AttributeArgumentSyntax>();
            arguments.AddRange(attribute.ConstructorArguments.Select(c =>
                SyntaxFactory.AttributeArgument(ExpressionGenerator.GenerateExpression(c))));

            arguments.AddRange(attribute.NamedArguments.Select(kvp =>
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(kvp.Key)), null,
                    ExpressionGenerator.GenerateExpression(kvp.Value))));

            return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments));
        }
    }
}
