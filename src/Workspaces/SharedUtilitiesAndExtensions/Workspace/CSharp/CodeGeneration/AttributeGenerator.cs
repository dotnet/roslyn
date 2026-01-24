// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static SyntaxFactory;

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
                : [AttributeList(
                    target.HasValue ? AttributeTargetSpecifier(target.Value) : null,
                    [.. attributeNodes])];
        }
        else
        {
            var attributeDeclarations =
                attributes.OrderBy(a => a.AttributeClass?.Name)
                          .Select(a => TryGenerateAttributeDeclaration(a, target, info))
                          .WhereNotNull().ToList();
            return [.. attributeDeclarations];
        }
    }

    private static AttributeListSyntax? TryGenerateAttributeDeclaration(
        AttributeData attribute, SyntaxToken? target, CSharpCodeGenerationContextInfo info)
    {
        var attributeSyntax = TryGenerateAttribute(attribute, info);
        return attributeSyntax == null
            ? null
            : AttributeList(
                target.HasValue
                    ? AttributeTargetSpecifier(target.Value)
                    : null,
                [attributeSyntax]);
    }

    private static AttributeSyntax? TryGenerateAttribute(AttributeData attribute, CSharpCodeGenerationContextInfo info)
    {
        if (IsCompilerInternalAttribute(attribute))
            return null;

        var reusableSyntax = GetReuseableSyntaxNodeForAttribute<AttributeSyntax>(attribute);
        if (info.Context.ReuseSyntax && reusableSyntax != null)
            return reusableSyntax;

        if (attribute.AttributeClass == null)
            return null;

        var attributeArguments = GenerateAttributeArgumentList(attribute, reusableSyntax);
        return attribute.AttributeClass.GenerateTypeSyntax() is NameSyntax nameSyntax
            ? Attribute(nameSyntax, attributeArguments)
            : null;
    }

    private static AttributeArgumentListSyntax? GenerateAttributeArgumentList(
        AttributeData attribute, AttributeSyntax? existingSyntax)
    {
        if (attribute.ConstructorArguments.Length == 0 && attribute.NamedArguments.Length == 0)
            return null;

        using var _ = ArrayBuilder<AttributeArgumentSyntax>.GetInstance(out var arguments);

        foreach (var argument in attribute.ConstructorArguments)
            arguments.Add(AttributeArgument(GenerateAttributeSyntax(argument)));

        foreach (var argument in attribute.NamedArguments)
        {
            arguments.Add(AttributeArgument(
                NameEquals(IdentifierName(argument.Key)),
                nameColon: null,
                GenerateAttributeSyntax(argument.Value)));
        }

        return AttributeArgumentList(SeparatedList(arguments));

        ExpressionSyntax GenerateAttributeSyntax(TypedConstant constant)
        {
            // In the case of a string constant with value "x", see if the originating syntax was a `nameof(x)`
            // expression and attempt to preserve that.
            if (existingSyntax?.ArgumentList != null && constant is { Kind: not TypedConstantKind.Array, Value: string stringValue })
            {
                foreach (var existingArgument in existingSyntax.ArgumentList.Arguments)
                {
                    if (existingArgument.Expression is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: var nameofArgument }] } invocation &&
                        invocation.IsNameOfInvocation())
                    {
                        var inferredName = nameofArgument.TryGetInferredMemberName();
                        if (inferredName == stringValue)
                            return existingArgument.Expression.WithoutTrivia();
                    }
                }
            }

            return ExpressionGenerator.GenerateExpression(constant);
        }
    }
}
