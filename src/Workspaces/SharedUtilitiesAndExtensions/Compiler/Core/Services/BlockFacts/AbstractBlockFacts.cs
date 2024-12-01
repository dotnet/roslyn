// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract class AbstractBlockFacts : IBlockFacts
{
    public abstract bool IsScopeBlock([NotNullWhen(true)] SyntaxNode? node);
    public abstract bool IsExecutableBlock([NotNullWhen(true)] SyntaxNode? node);
    public abstract IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode? node);
    public abstract SyntaxNode? FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);
    public abstract bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node);
    public abstract IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode? node);
}
