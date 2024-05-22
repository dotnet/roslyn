// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature;

internal readonly struct UnifiedArgumentSyntax : IUnifiedArgumentSyntax
{
    private readonly SyntaxNode _argument;

    private UnifiedArgumentSyntax(SyntaxNode argument)
    {
        Debug.Assert(argument.Kind() is SyntaxKind.Argument or SyntaxKind.AttributeArgument);
        _argument = argument;
    }

    public static IUnifiedArgumentSyntax Create(ArgumentSyntax argument)
        => new UnifiedArgumentSyntax(argument);

    public static IUnifiedArgumentSyntax Create(AttributeArgumentSyntax argument)
        => new UnifiedArgumentSyntax(argument);

    public SyntaxNode NameColon
    {
        get
        {
            return _argument is ArgumentSyntax argument
                ? argument.NameColon
                : ((AttributeArgumentSyntax)_argument).NameColon;
        }
    }

    public IUnifiedArgumentSyntax WithNameColon(SyntaxNode nameColonSyntax)
    {
        Debug.Assert(nameColonSyntax is NameColonSyntax);

        return _argument is ArgumentSyntax argument
            ? Create(argument.WithNameColon((NameColonSyntax)nameColonSyntax))
            : Create(((AttributeArgumentSyntax)_argument).WithNameColon((NameColonSyntax)nameColonSyntax));
    }

    public string GetName()
        => NameColon == null ? string.Empty : ((NameColonSyntax)NameColon).Name.Identifier.ValueText;

    public IUnifiedArgumentSyntax WithName(string name)
    {
        return _argument is ArgumentSyntax argument
                ? Create(argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))))
                : Create(((AttributeArgumentSyntax)_argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))));
    }

    public IUnifiedArgumentSyntax WithAdditionalAnnotations(SyntaxAnnotation annotation)
        => new UnifiedArgumentSyntax(_argument.WithAdditionalAnnotations(annotation));

    public SyntaxNode Expression
    {
        get
        {
            return _argument is ArgumentSyntax argument
                ? argument.Expression
                : ((AttributeArgumentSyntax)_argument).Expression;
        }
    }

    public bool IsDefault
    {
        get
        {
            return _argument == null;
        }
    }

    public bool IsNamed
    {
        get
        {
            return NameColon != null;
        }
    }

    public static explicit operator SyntaxNode(UnifiedArgumentSyntax unified)
        => unified._argument;
}
