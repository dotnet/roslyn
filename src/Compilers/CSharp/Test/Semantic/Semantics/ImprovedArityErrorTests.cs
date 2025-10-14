using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ImprovedArityErrorTests : CSharpTestBase
    {
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24406")]
        public void TestGenericAndNonGenericType()
        {
            var text = """
                class MyExpression { }
                class MyExpression<T> { }

                class Test
                {
                    void M()
                    {
                        MyExpression<int, string> x;
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (8,9): error CS0305: Using the generic type 'MyExpression<T>' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "MyExpression<int, string>").WithArguments("MyExpression<T>", "type", "1").WithLocation(8, 9),
                // (8,35): warning CS0168: The variable 'x' is declared but never used
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(8, 35));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24406")]
        public void TestGenericAndNonGenericType_SingleTypeArgument()
        {
            var text = """
                class MyExpression { }
                class MyExpression<T> { }

                class Test
                {
                    void M()
                    {
                        MyExpression<int> x = null;
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (8,27): warning CS0219: The variable 'x' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 27));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24406")]
        public void TestNonGenericTypeOnly()
        {
            var text = """
                class MyExpression { }

                class Test
                {
                    void M()
                    {
                        MyExpression<int> x;
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (7,9): error CS0308: The non-generic type 'MyExpression' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "MyExpression<int>").WithArguments("MyExpression", "type").WithLocation(7, 9),
                // (7,27): warning CS0168: The variable 'x' is declared but never used
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 27));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24406")]
        public void TestGenericTypeOnly()
        {
            var text = """
                class MyExpression<T> { }

                class Test
                {
                    void M()
                    {
                        MyExpression<int, string> x;
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (7,9): error CS0305: Using the generic type 'MyExpression<T>' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "MyExpression<int, string>").WithArguments("MyExpression<T>", "type", "1").WithLocation(7, 9),
                // (7,35): warning CS0168: The variable 'x' is declared but never used
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 35));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24406")]
        public void TestMultipleGenericTypes()
        {
            var text = """
                class MyExpression { }
                class MyExpression<T> { }
                class MyExpression<T1, T2> { }

                class Test
                {
                    void M()
                    {
                        MyExpression<int, string, bool> x;
                    }
                }
                """;
            // With multiple generic types and non-generic, should prefer the closest match
            // In this case, we look for arity 3, find generic with arity 2, that's better than non-generic
            CreateCompilation(text).VerifyDiagnostics(
                // (9,9): error CS0305: Using the generic type 'MyExpression<T>' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "MyExpression<int, string, bool>").WithArguments("MyExpression<T>", "type", "1").WithLocation(9, 9),
                // (9,41): warning CS0168: The variable 'x' is declared but never used
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(9, 41));
        }
    }
}
