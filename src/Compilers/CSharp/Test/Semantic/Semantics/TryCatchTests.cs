// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var catchClause = tree.GetCompilationUnitRoot().DescendantNodes().OfType<CatchClauseSyntax>().Single();
            var localSymbol = (ILocalSymbol)model.GetDeclaredSymbol(catchClause.Declaration);
            Assert.Equal("e", localSymbol.Name);
            Assert.Equal("System.IO.IOException", localSymbol.Type.ToDisplayString());

            var filterExprInfo = model.GetSymbolInfo(catchClause.Filter.FilterExpression);
            Assert.Equal("string.operator !=(string, string)", filterExprInfo.Symbol.ToDisplayString());
        }

        [Fact]
        public void CatchClauseValueType()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (int e)
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,16): error CS0155: The type caught or thrown must be derived from System.Exception
                //         catch (int e)
                Diagnostic(ErrorCode.ERR_BadExceptionType, "int").WithLocation(9, 16),
                // (9,20): warning CS0168: The variable 'e' is declared but never used
                //         catch (int e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(9, 20));
        }

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70948")]
        public void NestedAsyncThrow()
        {
            var source = """
                using System;
                using System.Threading.Tasks;

                class C
                {
                    private async Task<object> M(object o)
                    {
                        try
                        {
                        }
                        catch (Exception)
                        {
                            Func<Task> f = async () =>
                            {
                                try
                                {
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                                finally
                                {
                                }
                            };

                            await Task.CompletedTask;

                            throw;
                        }
                        finally
                        {
                        }

                        return null;
                    }
                }
                """;

            CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics();
        }
    }
}
