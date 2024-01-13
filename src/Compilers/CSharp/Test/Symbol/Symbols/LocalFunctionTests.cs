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

        [Fact, WorkItem(65938, "https://github.com/dotnet/roslyn/issues/65938")]
        public void StaticLocalFunction_CapturingMethodGroup()
        {
            CreateCompilation("""
                using System;

                var c = new C();
                LocalFunc();
                NonStatic();

                static void LocalFunc()
                {
                    var x1 = c.MyExtension;
                    var y1 = new Func<string>(c.MyExtension);
                }

                void NonStatic()
                {
                    Action f = static () =>
                    {
                        var x2 = c.MyExtension;
                        var y2 = new Func<string>(c.MyExtension);
                    };
                }

                public class C
                {
                }

                public static class Extensions
                {
                    public static string MyExtension(this C c)
                        => string.Empty;
                }
                """).VerifyDiagnostics(
                    // (9,14): error CS8421: A static local function cannot contain a reference to 'c'.
                    //     var x1 = c.MyExtension;
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "c").WithArguments("c").WithLocation(9, 14),
                    // (10,31): error CS8421: A static local function cannot contain a reference to 'c'.
                    //     var y1 = new Func<string>(c.MyExtension);
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "c").WithArguments("c").WithLocation(10, 31),
                    // (17,18): error CS8820: A static anonymous function cannot contain a reference to 'c'.
                    //         var x2 = c.MyExtension;
                    Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "c").WithArguments("c").WithLocation(17, 18),
                    // (18,35): error CS8820: A static anonymous function cannot contain a reference to 'c'.
                    //         var y2 = new Func<string>(c.MyExtension);
                    Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "c").WithArguments("c").WithLocation(18, 35));
        }

        [Fact]
        public void StaticLocalFunction_CapturingMethodGroup2()
        {
            CreateCompilation("""
                using System;

                var c = new C();
                LocalFunc();
                NonStatic();

                static void LocalFunc()
                {
                    var x1 = Extensions.MyExtension;
                    var y1 = new Func<C, string>(Extensions.MyExtension);
                }

                void NonStatic()
                {
                    Action f = static () =>
                    {
                        var x2 = Extensions.MyExtension;
                        var y2 = new Func<C, string>(Extensions.MyExtension);
                    };
                }

                public class C
                {
                }

                public static class Extensions
                {
                    public static string MyExtension(this C c)
                        => string.Empty;
                }
                """).VerifyEmitDiagnostics();
        }

        [Fact]
        public void StaticLocalFunction_CapturingMethodGroup3()
        {
            CreateCompilation("""
                using System;
                public class Base { }

                public class C : Base
                {
                    public void M()
                    {
                        LocalFunc();

                        static void LocalFunc()
                        {
                            var x1 = this.MyExtension;
                            var x2 = new Func<string>(this.MyExtension);
                            var y1 = base.MyExtension;
                            var y2 = new Func<string>(base.MyExtension);
                        }
                    }
                }

                internal static class Extensions
                {
                    public static string MyExtension(this Base c)
                        => string.Empty;
                }
                """).VerifyDiagnostics(
                    // (12,22): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //             var x1 = this.MyExtension;
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "this").WithLocation(12, 22),
                    // (13,39): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //             var x2 = new Func<string>(this.MyExtension);
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "this").WithLocation(13, 39),
                    // (14,22): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //             var y1 = base.MyExtension;
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "base").WithLocation(14, 22),
                    // (14,27): error CS0117: 'Base' does not contain a definition for 'MyExtension'
                    //             var y1 = base.MyExtension;
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "MyExtension").WithArguments("Base", "MyExtension").WithLocation(14, 27),
                    // (15,39): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //             var y2 = new Func<string>(base.MyExtension);
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "base").WithLocation(15, 39),
                    // (15,44): error CS0117: 'Base' does not contain a definition for 'MyExtension'
                    //             var y2 = new Func<string>(base.MyExtension);
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "MyExtension").WithArguments("Base", "MyExtension").WithLocation(15, 44));
        }

        [Fact]
        public void StaticLocalFunction_CapturingMethodGroup4()
        {
            CreateCompilation("""
                using System;
                public class Base { }

                public class C : Base
                {
                    public void M()
                    {
                        NonStatic();
                
                        void NonStatic()
                        {
                            Action f = static () =>
                            {
                                var x1 = this.MyExtension;
                                var x2 = new Func<string>(this.MyExtension);
                            };
                        }
                    }
                }

                internal static class Extensions
                {
                    public static string MyExtension(this Base c)
                        => string.Empty;
                }
                """).VerifyDiagnostics(
                    // (14,26): error CS8821: A static anonymous function cannot contain a reference to 'this' or 'base'.
                    //                 var x1 = this.MyExtension;
                    Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "this").WithLocation(14, 26),
                    // (15,43): error CS8821: A static anonymous function cannot contain a reference to 'this' or 'base'.
                    //                 var x2 = new Func<string>(this.MyExtension);
                    Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "this").WithLocation(15, 43));
        }

        [Fact]
        public void StaticLocalFunction_CapturingMethodGroup5()
        {
            CreateCompilation("""
                using System;
                public class Base { }

                public class C : Base
                {
                    public void M()
                    {
                        NonStatic();
                
                        void NonStatic()
                        {
                            Action f = static () =>
                            {
                                var x1 = base.MyExtension;
                                var x2 = new Func<string>(base.MyExtension);
                            };
                        }
                    }
                }

                internal static class Extensions
                {
                    public static string MyExtension(this Base c)
                        => string.Empty;
                }
                """).VerifyDiagnostics(
                    // (14,31): error CS0117: 'Base' does not contain a definition for 'MyExtension'
                    //                 var x1 = base.MyExtension;
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "MyExtension").WithArguments("Base", "MyExtension").WithLocation(14, 31),
                    // (15,48): error CS0117: 'Base' does not contain a definition for 'MyExtension'
                    //                 var x2 = new Func<string>(base.MyExtension);
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "MyExtension").WithArguments("Base", "MyExtension").WithLocation(15, 48));
        }

        [Fact]
        public void StaticLocalFunction_CapturingMethodGroup6()
        {
            CreateCompilation("""
                using System;

                static class Extensions
                {
                    static void F()
                    {
                        LocalFunc();
                        static void LocalFunc()
                        {
                            var x = MyExtension;
                            var y = new Func<object, string>(MyExtension);
                        }
                    }
                    static string MyExtension(this object o)
                        => string.Empty;
                }
                """).VerifyEmitDiagnostics();
        }
    }
}
