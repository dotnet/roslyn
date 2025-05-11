// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract class AbstractBlockFacts<TStatementSyntax> : IBlockFacts
    where TStatementSyntax : SyntaxNode
{
    public abstract bool IsScopeBlock([NotNullWhen(true)] SyntaxNode? node);
    public abstract bool IsExecutableBlock([NotNullWhen(true)] SyntaxNode? node);

    public abstract SyntaxNode? GetImmediateParentExecutableBlockForStatement(TStatementSyntax statement);

    public abstract IReadOnlyList<TStatementSyntax> GetExecutableBlockStatements(SyntaxNode? node);
    public abstract SyntaxNode? FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);
    public abstract bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node);
    public abstract IReadOnlyList<TStatementSyntax> GetStatementContainerStatements(SyntaxNode? node);

    SyntaxNode? IBlockFacts.GetImmediateParentExecutableBlockForStatement(SyntaxNode statement)
        => GetImmediateParentExecutableBlockForStatement((TStatementSyntax)statement);

    IReadOnlyList<SyntaxNode> IBlockFacts.GetExecutableBlockStatements(SyntaxNode? node)
        => GetExecutableBlockStatements(node);

    IReadOnlyList<SyntaxNode> IBlockFacts.GetStatementContainerStatements(SyntaxNode? node)
        => GetStatementContainerStatements(node);
}
