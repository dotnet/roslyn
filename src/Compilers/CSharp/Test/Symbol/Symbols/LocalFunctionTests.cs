// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class LocalFunctionTests : CSharpTestBase
    {
        [Fact, WorkItem(27719, "https://github.com/dotnet/roslyn/issues/27719")]
        public void LocalFunctionIsNotStatic()
        {
            var source = @"
class C
{
    void M()
    {
        void local() {}
        local();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var local = semanticModel.GetDeclaredSymbol(localSyntax);
            Assert.False(local.IsStatic);
        }

        [Fact, WorkItem(27719, "https://github.com/dotnet/roslyn/issues/27719")]
        public void StaticLocalFunctionIsStatic()
        {
            var source = @"
class C
{
    void M()
    {
        static void local() {}
        local();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var local = semanticModel.GetDeclaredSymbol(localSyntax);
            Assert.True(local.IsStatic);
        }

        [Fact, WorkItem(27719, "https://github.com/dotnet/roslyn/issues/27719")]
        public void LocalFunctionInStaticMethodIsNotStatic()
        {
            var source = @"
class C
{
    static void M()
    {
        void local() {}
        local();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var local = semanticModel.GetDeclaredSymbol(localSyntax);
            Assert.False(local.IsStatic);
        }

        [Fact]
        public void LocalFunctionDoesNotRequireInstanceReceiver()
        {
            var source = @"
class C
{
    void M()
    {
        void local() {}
        static void staticLocal() {}
        local();
        staticLocal();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localsSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToArray();
            var local = semanticModel.GetDeclaredSymbol(localsSyntax[0]).GetSymbol<MethodSymbol>();
            Assert.False(local.RequiresInstanceReceiver);
            var staticLocal = semanticModel.GetDeclaredSymbol(localsSyntax[0]).GetSymbol<MethodSymbol>();
            Assert.False(staticLocal.RequiresInstanceReceiver);
        }

        [Fact]
        public void PartialStaticLocalFunction()
        {
            CreateCompilation("""
                public class C
                {
                    public void M()
                    {
                        partial static void local() { }
                    }
                }
                """).VerifyDiagnostics(
                // (5,9): error CS0103: The name 'partial' does not exist in the current context
                //         partial static void local() { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "partial").WithArguments("partial").WithLocation(5, 9),
                // (5,17): error CS1002: ; expected
                //         partial static void local() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 17),
                // (5,29): warning CS8321: The local function 'local' is declared but never used
                //         partial static void local() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(5, 29));
        }

        [Fact]
        public void StaticPartialLocalFunction()
        {
            CreateCompilation("""
                public class C
                {
                    public void M()
                    {
                        static partial void local() { }
                    }
                }
                """).VerifyDiagnostics(
                // (5,9): error CS0106: The modifier 'static' is not valid for this item
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(5, 9),
                // (5,16): error CS1031: Type expected
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(5, 16),
                // (5,16): error CS1525: Invalid expression term 'partial'
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(5, 16),
                // (5,16): error CS1002: ; expected
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(5, 16),
                // (5,16): error CS1513: } expected
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "partial").WithLocation(5, 16),
                // (5,29): error CS0759: No defining declaration found for implementing declaration of partial method 'C.local()'
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "local").WithArguments("C.local()").WithLocation(5, 29),
                // (5,29): error CS0751: A partial method must be declared within a partial type
                //         static partial void local() { }
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "local").WithLocation(5, 29),
                // (7,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(7, 1));
        }

        [Fact]
        public void PartialLocalFunction()
        {
            CreateCompilation("""
                public class C
                {
                    public void M()
                    {
                        partial void local() { }
                    }
                }
                """).VerifyDiagnostics(
                // (4,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 6),
                // (5,22): error CS0759: No defining declaration found for implementing declaration of partial method 'C.local()'
                //         partial void local() { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "local").WithArguments("C.local()").WithLocation(5, 22),
                // (5,22): error CS0751: A partial method must be declared within a partial type
                //         partial void local() { }
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "local").WithLocation(5, 22),
                // (7,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(7, 1));
        }
    }
}
