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
        var node = TryGetAncestorForLocation<BaseTypeDeclarationSyntax>(root, position);
        typeDeclaration = node;
        if (node == null)
            return false;

        var lastToken = (node as TypeDeclarationSyntax)?.TypeParameterList?.GetLastToken() ?? node.Identifier;
        if (fullHeader)
            lastToken = node.BaseList?.GetLastToken() ?? lastToken;

        return IsOnHeader(root, position, node, lastToken);
    }

    public override bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration)
    {
        var node = TryGetAncestorForLocation<PropertyDeclarationSyntax>(root, position);
        propertyDeclaration = node;
        if (propertyDeclaration == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node.Identifier);
    }

    public override bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter)
    {
        var node = TryGetAncestorForLocation<ParameterSyntax>(root, position);
        parameter = node;
        if (parameter == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node);
    }

    public override bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method)
    {
        var node = TryGetAncestorForLocation<MethodDeclarationSyntax>(root, position);
        method = node;
        if (method == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node.ParameterList);
    }

    public override bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction)
    {
        var node = TryGetAncestorForLocation<LocalFunctionStatementSyntax>(root, position);
        localFunction = node;
        if (localFunction == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node.ParameterList);
    }

    public override bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
    {
        var node = TryGetAncestorForLocation<LocalDeclarationStatementSyntax>(root, position);
        localDeclaration = node;
        if (localDeclaration == null)
        {
            return false;
        }

        var initializersExpressions = node!.Declaration.Variables
            .Where(v => v.Initializer != null)
            .SelectAsArray(initializedV => initializedV.Initializer!.Value);
        return IsOnHeader(root, position, node, node, holes: initializersExpressions);
    }

    public override bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement)
    {
        var node = TryGetAncestorForLocation<IfStatementSyntax>(root, position);
        ifStatement = node;
        if (ifStatement == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node.CloseParenToken);
    }

    public override bool IsOnWhileStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? whileStatement)
    {
        var node = TryGetAncestorForLocation<WhileStatementSyntax>(root, position);
        whileStatement = node;
        if (whileStatement == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node.CloseParenToken);
    }

    public override bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement)
    {
        var node = TryGetAncestorForLocation<ForEachStatementSyntax>(root, position);
        foreachStatement = node;
        if (foreachStatement == null)
        {
            return false;
        }

        RoslynDebug.AssertNotNull(node);
        return IsOnHeader(root, position, node, node.CloseParenToken);
    }
}
