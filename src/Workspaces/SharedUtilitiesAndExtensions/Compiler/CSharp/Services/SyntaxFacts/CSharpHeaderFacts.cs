// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal class CSharpHeaderFacts : AbstractHeaderFacts
{
    public static readonly IHeaderFacts Instance = new CSharpHeaderFacts();

    protected CSharpHeaderFacts()
    {
    }

    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    public override bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
    {
        var node = TryGetAncestorForLocation<BaseTypeDeclarationSyntax>(root, position, out typeDeclaration);
        return node != null && IsOnHeader(root, position, node, GetLastToken());

        SyntaxToken GetLastToken()
        {
            if (fullHeader && node.BaseList != null)
                return node.BaseList.GetLastToken();

            if (node is TypeDeclarationSyntax { TypeParameterList.GreaterThanToken: var greaterThanToken })
                return greaterThanToken;

            // .Identifier may be default in the case of an extension type.
            if (node.Identifier != default)
                return node.Identifier;

            return node switch
            {
                TypeDeclarationSyntax typeDeclaration => typeDeclaration.Keyword,
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.EnumKeyword,
                _ => throw ExceptionUtilities.Unreachable(),
            };
        }
    }

    public override bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration)
    {
        var node = TryGetAncestorForLocation<PropertyDeclarationSyntax>(root, position, out propertyDeclaration);
        return node != null && IsOnHeader(root, position, node, node.Identifier);
    }

    public override bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter)
    {
        var node = TryGetAncestorForLocation<ParameterSyntax>(root, position, out parameter);
        return node != null && IsOnHeader(root, position, node, node);
    }

    public override bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method)
    {
        var node = TryGetAncestorForLocation<MethodDeclarationSyntax>(root, position, out method);
        return node != null && IsOnHeader(root, position, node, node.ParameterList);
    }

    public override bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction)
    {
        var node = TryGetAncestorForLocation<LocalFunctionStatementSyntax>(root, position, out localFunction);
        return node != null && IsOnHeader(root, position, node, node.ParameterList);
    }

    public override bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
    {
        var node = TryGetAncestorForLocation<LocalDeclarationStatementSyntax>(root, position, out localDeclaration);
        return node != null && IsOnHeader(root, position, node, node, holes: node.Declaration.Variables
            .SelectAsArray(
                predicate: v => v.Initializer != null,
                selector: initializedV => initializedV.Initializer!.Value));
    }

    public override bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement)
    {
        var node = TryGetAncestorForLocation<IfStatementSyntax>(root, position, out ifStatement);
        return node != null && IsOnHeader(root, position, node, node.CloseParenToken);
    }

    public override bool IsOnWhileStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? whileStatement)
    {
        var node = TryGetAncestorForLocation<WhileStatementSyntax>(root, position, out whileStatement);
        return node != null && IsOnHeader(root, position, node, node.CloseParenToken);
    }

    public override bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement)
    {
        var node = TryGetAncestorForLocation<ForEachStatementSyntax>(root, position, out foreachStatement);
        return node != null && IsOnHeader(root, position, node, node.CloseParenToken);
    }
}
