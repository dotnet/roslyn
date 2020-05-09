// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private SyntaxToken Semicolon(BlockSyntax? block)
            => block == null ? Token(SyntaxKind.SemicolonToken) : default;

        public (BlockSyntax?, ArrowExpressionClauseSyntax?, SyntaxToken) GenerateBodyParts(IBlockOperation? body)
        {
            var block = GenerateBlock(body);
            return (block, null, Semicolon(block));
        }

        public BlockSyntax? GenerateBlock(IBlockOperation? block)
        {
            if (block == null)
                return null;

            using var _ = GetArrayBuilder<StatementSyntax>(out var statements);

            foreach (var statement in block.Operations)
                statements.AddIfNotNull(TryGenerateStatement(statement));

            return Block(List(statements));
        }
    }
}
