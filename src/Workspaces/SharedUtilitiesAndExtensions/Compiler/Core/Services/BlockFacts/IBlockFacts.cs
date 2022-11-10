// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageService
{
    /// <summary>
    /// Provides helpers for working across "blocks" of statements in an agnostic fashion across VB and C#.  Both
    /// languages have quirks here that this API attempts to smooth out.  For example, many things in VB are 'blocks'
    /// (like ClassBlocks and MethodBlocks).  However, only a subset of those can have executable statements. Similarly,
    /// C# has actual BlockSyntax nodes (<c>{ ... }</c>), but it can also have sequences of executable statements not
    /// contained by those (for example statements in a case-clause in a switch-statement).
    /// </summary>
    internal interface IBlockFacts
    {
        /// <summary>
        /// A block that has no semantics other than introducing a new scope. That is only C# BlockSyntax.
        /// </summary>
        bool IsScopeBlock([NotNullWhen(true)] SyntaxNode? node);

        /// <summary>
        /// A node that contains a list of statements. In C#, this is BlockSyntax and SwitchSectionSyntax. In VB, this
        /// includes all block statements such as a MultiLineIfBlockSyntax.
        /// </summary>
        bool IsExecutableBlock([NotNullWhen(true)] SyntaxNode? node);

        IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode? node);
        SyntaxNode? FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);

        /// <summary>
        /// A node that can host a list of statements or a single statement. In addition to every "executable block",
        /// this also includes C# embedded statement owners.
        /// </summary>
        bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node);

        IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode? node);
    }
}
