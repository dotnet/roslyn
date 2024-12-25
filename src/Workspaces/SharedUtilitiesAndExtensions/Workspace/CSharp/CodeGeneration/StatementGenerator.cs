// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal static class StatementGenerator
{
    internal static SyntaxList<StatementSyntax> GenerateStatements(IEnumerable<SyntaxNode> statements)
        => [.. statements.OfType<StatementSyntax>()];

    internal static BlockSyntax GenerateBlock(IMethodSymbol method)
    {
        return SyntaxFactory.Block(
            StatementGenerator.GenerateStatements(CodeGenerationMethodInfo.GetStatements(method)));
    }
}
