// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractForLoopSnippetProvider : AbstractInlineStatementSnippetProvider
{
    protected override bool IsValidAccessingType(ITypeSymbol type, Compilation compilation)
    {
        if (IsSuitableIntegerType(type))
        {
            return true;
        }

        var hasLengthProperty = FindLengthProperty(type, compilation) is not null;
        var hasCountProperty = FindCountProperty(type, compilation) is not null;

        // We want to allow types, which have either `Length` or `Count` property, but not both to avoid ambiguity
        return hasLengthProperty ^ hasCountProperty;
    }

    protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        => syntaxFacts.IsForStatement;

    protected static bool IsSuitableIntegerType(ITypeSymbol type)
        => type.IsIntegralType() || type.IsNativeIntegerType;

    protected static IPropertySymbol? FindLengthProperty(ITypeSymbol type, Compilation compilation)
        => FindAccessibleIntegerProperty(type, compilation, "Length");

    protected static IPropertySymbol? FindCountProperty(ITypeSymbol type, Compilation compilation)
        => FindAccessibleIntegerProperty(type, compilation, "Count");

    private static IPropertySymbol? FindAccessibleIntegerProperty(ITypeSymbol type, Compilation compilation, string propertyName)
    {
        return type
            .GetAccessibleMembersInThisAndBaseTypes<IPropertySymbol>(propertyName, compilation.Assembly)
            .FirstOrDefault(p => p is { GetMethod: { } getMethod } && getMethod.IsAccessibleWithin(compilation.Assembly) && IsSuitableIntegerType(p.Type));
    }
}
