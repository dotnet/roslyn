// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class StatementGenerator
    {
        internal static SyntaxList<StatementSyntax> GenerateStatements(IEnumerable<SyntaxNode> statements)
        {
            return statements.OfType<StatementSyntax>().ToSyntaxList();
        }

        internal static BlockSyntax GenerateBlock(IMethodSymbol method)
        {
            return SyntaxFactory.Block(
                StatementGenerator.GenerateStatements(CodeGenerationMethodInfo.GetStatements(method)));
        }
    }
}
