// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static SyntaxList<AttributeListSyntax> GenerateAttributeLists(
            ImmutableArray<AttributeData> attributes,
            CSharpCodeGenerationContextInfo info,
            SyntaxToken? target = null)
        {
            if (info.Context.MergeAttributes)
            {
                var attributeNodes =
                    attributes.OrderBy(a => a.AttributeClass?.Name)
                              .Select(a => TryGenerateAttribute(a, info))
                              .WhereNotNull().ToList();
                return attributeNodes.Count == 0
                    ? default
                    : SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(
                        target.HasValue ? SyntaxFactory.AttributeTargetSpecifier(target.Value) : null,
                        SyntaxFactory.SeparatedList(attributeNodes)));
            }
            else
            {
                var attributeDeclarations =
                    attributes.OrderBy(a => a.AttributeClass?.Name)
                              .Select(a => TryGenerateAttributeDeclaration(a, target, info))
                              .WhereNotNull().ToList();
                return attributeDeclarations.Count == 0
                    ? default
                    : SyntaxFactory.List<AttributeListSyntax>(attributeDeclarations);
            }
        }

        private static AttributeListSyntax? TryGenerateAttributeDeclaration(
            AttributeData attribute, SyntaxToken? target, CSharpCodeGenerationContextInfo info)
        {
            var attributeSyntax = TryGenerateAttribute(attribute, info);
            return attributeSyntax == null
                ? null
                : SyntaxFactory.AttributeList(
                    target.HasValue
                        ? SyntaxFactory.AttributeTargetSpecifier(target.Value)
                        : null,
                    SyntaxFactory.SingletonSeparatedList(attributeSyntax));
        }

        private static AttributeSyntax? TryGenerateAttribute(AttributeData attribute, CSharpCodeGenerationContextInfo info)
        {
            if (IsCompilerInternalAttribute(attribute))
                return null;

            if (!info.Context.MergeAttributes)
            {
                var reusableSyntax = GetReuseableSyntaxNodeForAttribute<AttributeSyntax>(attribute, info);
                if (reusableSyntax != null)
                {
                    return reusableSyntax;
                }
            }

            if (attribute.AttributeClass == null)
                return null;

            var attributeArguments = GenerateAttributeArgumentList(attribute);
            return attribute.AttributeClass.GenerateTypeSyntax() is NameSyntax nameSyntax
                ? SyntaxFactory.Attribute(nameSyntax, attributeArguments)
                : null;
        }

        private static bool IsCompilerInternalAttribute(AttributeData attribute)
        {
            // from https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md
            var attrClass = attribute.AttributeClass;
            if (attrClass == null)
                return false;

            var name = attrClass.Name;

            if (name is not "NullableAttribute" and
                not "NullableContextAttribute" and
                not "NativeIntegerAttribute" and
                not "DynamicAttribute")
            {
                return false;
            }

            var ns = attrClass.ContainingNamespace;
            return ns?.Name == nameof(System.Runtime.CompilerServices) &&
                   ns.ContainingNamespace?.Name == nameof(System.Runtime) &&
                   ns.ContainingNamespace.ContainingNamespace?.Name == nameof(System) &&
                   ns.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
        }

        private static AttributeArgumentListSyntax? GenerateAttributeArgumentList(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 0 && attribute.NamedArguments.Length == 0)
                return null;

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
