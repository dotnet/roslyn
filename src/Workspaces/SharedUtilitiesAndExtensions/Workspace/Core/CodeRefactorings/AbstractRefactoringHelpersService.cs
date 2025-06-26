// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal abstract class AbstractRefactoringHelpersService<TExpressionSyntax, TArgumentSyntax, TExpressionStatementSyntax> : IRefactoringHelpersService
    where TExpressionSyntax : SyntaxNode
    where TArgumentSyntax : SyntaxNode
    where TExpressionStatementSyntax : SyntaxNode
{
    protected abstract IRefactoringHelpers RefactoringHelpers { get; }

    public bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
        => this.RefactoringHelpers.IsBetweenTypeMembers(sourceText, root, position, out typeDeclaration);

    public void AddRelevantNodes<TSyntaxNode>(SourceText sourceText, SyntaxNode root, TextSpan selection, bool allowEmptyNodes, int maxCount, ref TemporaryArray<TSyntaxNode> result, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        => this.RefactoringHelpers.AddRelevantNodes(sourceText, root, selection, allowEmptyNodes, maxCount, ref result, cancellationToken);

    public bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
        => this.RefactoringHelpers.IsOnTypeHeader(root, position, fullHeader, out typeDeclaration);

    public bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration)
        => this.RefactoringHelpers.IsOnPropertyDeclarationHeader(root, position, out propertyDeclaration);

    public bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter)
        => this.RefactoringHelpers.IsOnParameterHeader(root, position, out parameter);

    public bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method)
        => this.RefactoringHelpers.IsOnMethodHeader(root, position, out method);

    public bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction)
        => this.RefactoringHelpers.IsOnLocalFunctionHeader(root, position, out localFunction);

    public bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
        => this.RefactoringHelpers.IsOnLocalDeclarationHeader(root, position, out localDeclaration);

    public bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement)
        => this.RefactoringHelpers.IsOnIfStatementHeader(root, position, out ifStatement);

    public bool IsOnWhileStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? whileStatement)
        => this.RefactoringHelpers.IsOnWhileStatementHeader(root, position, out whileStatement);

    public bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement)
        => this.RefactoringHelpers.IsOnForeachHeader(root, position, out foreachStatement);
}
