// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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

        [ConditionalFact(typeof(x86))]
        [WorkItem(7030, "https://github.com/dotnet/roslyn/issues/7030")]
        public void Issue7030()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        int i = 3;
        do
        {
            try
            {
                throw new Exception();
            }
            catch (Exception) when (--i < 0)
            {
                Console.Write(""e"");
                break;
            }
            catch (Exception)
            {
                Console.Write(""h"");
            }
        } while (true);
    }
}";

            CompileAndVerify(source, expectedOutput: "hhhe");
        }
    }
}
