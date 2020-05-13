// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class AttributeGenerator
    {
        public static (SyntaxList<AttributeListSyntax>, bool isNullable) GenerateAttributeLists(
            ImmutableArray<AttributeData> attributes,
            CodeGenerationOptions options,
            SyntaxToken? target = null)
        {
            if (options.MergeAttributes)
            {
                var pairs = attributes.OrderBy(a => a.AttributeClass.Name).Select(a => GenerateAttribute(a, options)).ToList();
                var isNullable = pairs.Any(t => t.isNullable);
                var attributeNodes = pairs.Select(p => p.syntax).WhereNotNull().ToList();

                var list = attributeNodes.Count == 0
                    ? default
                    : SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(
                        target.HasValue ? SyntaxFactory.AttributeTargetSpecifier(target.Value) : null,
                        SyntaxFactory.SeparatedList(attributeNodes)));
                return (list, isNullable);
            }
            else
            {
                var pairs = attributes.OrderBy(a => a.AttributeClass.Name).Select(a => GenerateAttributeDeclaration(a, target, options)).ToList();
                var isNullable = pairs.Any(t => t.isNullable);

                var list = SyntaxFactory.List(pairs.Select(t => t.syntax).WhereNotNull());
                return (list, isNullable);
            }
        }

        private static (AttributeListSyntax syntax, bool isNullable) GenerateAttributeDeclaration(
            AttributeData attribute, SyntaxToken? target, CodeGenerationOptions options)
        {
            var (attributeSyntax, isNullable) = GenerateAttribute(attribute, options);
            var resultSyntax = attributeSyntax == null
                ? null
                : SyntaxFactory.AttributeList(
                    target.HasValue ? SyntaxFactory.AttributeTargetSpecifier(target.Value) : null,
                    SyntaxFactory.SingletonSeparatedList(attributeSyntax));

            return (resultSyntax, isNullable);
        }

        private static (AttributeSyntax syntax, bool isNullable) GenerateAttribute(AttributeData attribute, CodeGenerationOptions options)
        {
            NullableAnnotation
            // Never add the internal nullable attributes the compiler generates.
            if (IsCompilerInternalNulllableAttribute(attribute))
                return (null, isNullable: true);

            if (!options.MergeAttributes)
            {
                var reusableSyntax = GetReuseableSyntaxNodeForAttribute<AttributeSyntax>(attribute, options);
                if (reusableSyntax != null)
                {
                    return (reusableSyntax, isNullable: false);
                }
            }

            var attributeArguments = GenerateAttributeArgumentList(attribute);
            var syntax = !(attribute.AttributeClass.GenerateTypeSyntax() is NameSyntax nameSyntax) ? null : SyntaxFactory.Attribute(nameSyntax, attributeArguments);

            return (syntax, isNullable: false);
        }

        private static bool IsCompilerInternalNulllableAttribute(AttributeData attribute)
        {
            // from https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md
            var attrClass = attribute.AttributeClass;
            var name = attrClass.Name;

            if (name != "NullableAttribute" && name != "NullableContextAttribute")
                return false;


            var ns = attrClass.ContainingNamespace;
            return ns?.Name == nameof(System.Runtime.CompilerServices) &&
                   ns.ContainingNamespace?.Name == nameof(System.Runtime) &&
                   ns.ContainingNamespace.ContainingNamespace?.Name == nameof(System) &&
                   ns.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
        }

        private static bool IsSystemRuntimeCompilerServicesNamespace(INamespaceSymbol containingNamespace)
        {
            throw new NotImplementedException();
        }

        private static AttributeArgumentListSyntax GenerateAttributeArgumentList(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 0 && attribute.NamedArguments.Length == 0)
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
