﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) try/catch statements.
    /// </summary>
    public class TryCatchTests : CompilingTestBase
    {
        [Fact]
        public void SemanticModel()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (System.IO.IOException e) when (e.Message != null)
        {
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var catchClause = tree.GetCompilationUnitRoot().DescendantNodes().OfType<CatchClauseSyntax>().Single();
            var localSymbol = (LocalSymbol)model.GetDeclaredSymbol(catchClause.Declaration);
            Assert.Equal("e", localSymbol.Name);
            Assert.Equal("System.IO.IOException", localSymbol.Type.ToDisplayString());

            var filterExprInfo = model.GetSymbolInfo(catchClause.Filter.FilterExpression);
            Assert.Equal("string.operator !=(string, string)", filterExprInfo.Symbol.ToDisplayString());
        }
    }
}
