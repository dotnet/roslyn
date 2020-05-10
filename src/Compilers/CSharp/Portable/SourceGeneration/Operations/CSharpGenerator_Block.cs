// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SourceGeneration;
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
            => TryGenerateBlock(block, SyntaxType.Statement);

        private BlockSyntax? TryGenerateBlock(IBlockOperation? block, SyntaxType type)
        {
            if (block == null)
                return null;

            if (type == SyntaxType.Expression)
                throw new ArgumentException("Block operation cannot be converted to an expression");

            using var _ = GetArrayBuilder<StatementSyntax>(out var statements);

            foreach (var statement in block.Operations)
                statements.AddIfNotNull(TryGenerateStatement(statement));

            return Block(List(statements));
        }

        private IBlockOperation? WrapWithBlock(IOperation? operation)
            => operation == null ? null :
               operation is IBlockOperation block ? block :
                CodeGenerator.Block(operations: ImmutableArray.Create(operation));
    }
}
