﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RefEscapingTests : CompilingTestBase
    {
        [Fact]
        public void RefStructSemanticModel()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
struct S1 { }
ref struct S2 { public S1 F1; }
enum E1 { }
class C<T>
{
    unsafe void M<U>() where U : unmanaged
    {
        Span<int> span = default;
        var s1 = new S1();
        var s2 = new S2();
        var i0 = 0;
        var e1 = new E1();
        var o1 = new object();
        var c1 = new C<int>();
        var t1 = default(T);
        var u1 = default(U);
        void* p1 = null;
        var a1 = new { X = 0 };
        var a2 = new int[1];
        var t2 = (0, 0);
    }
}", options: TestOptions.Regular7_3);
            var comp = CreateCompilationWithSpan(tree, TestOptions.UnsafeDebugDll);
            Assert.True(comp.GetDiagnostics().All(d => d.Severity != DiagnosticSeverity.Error));
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();

            Assert.True(getLocalType("span").IsRefLikeType);
            Assert.False(getLocalType("s1").IsRefLikeType);
            Assert.True(getLocalType("s2").IsRefLikeType);
            Assert.False(getLocalType("i0").IsRefLikeType);
            Assert.False(getLocalType("t1").IsRefLikeType);
            Assert.False(getLocalType("e1").IsRefLikeType);
            Assert.False(getLocalType("o1").IsRefLikeType);
            Assert.False(getLocalType("c1").IsRefLikeType);
            Assert.False(getLocalType("t1").IsRefLikeType);
            Assert.False(getLocalType("u1").IsRefLikeType);
            Assert.False(getLocalType("p1").IsRefLikeType);
            Assert.False(getLocalType("a1").IsRefLikeType);
            Assert.False(getLocalType("a2").IsRefLikeType);
            Assert.False(getLocalType("t2").IsRefLikeType);

            ITypeSymbol getLocalType(string name)
            {
                var decl = root.DescendantNodes()
                    .OfType<VariableDeclaratorSyntax>()
                    .Single(n => n.Identifier.ValueText == name);
                return ((ILocalSymbol)model.GetDeclaredSymbol(decl)).Type;
            }
        }

        [Fact]
        public void RefStructUsing()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
class C
{
    void M()
    {
        using (var x = GetRefStruct())
        {
        }
    }
    S2 GetRefStruct() => default;
    ref struct S2
    {
    }
}");
            comp.VerifyDiagnostics(
                // (6,16): error CS1674: 'C.S2': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using (var x = GetRefStruct())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var x = GetRefStruct()").WithArguments("C.S2").WithLocation(6, 16));
        }

        [Fact]
        public void RefStructAnonymous()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    object M()
    {
        Span<int> outer = new Span<int>(new int[10]);
        Span<int> inner = stackalloc int[10];

        return new { Outer = outer, Inner = inner };
    }
}", options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (10,22): error CS0828: Cannot assign 'Span<int>' to anonymous type property
                //         return new { Outer = outer, Inner = inner };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Outer = outer").WithArguments("System.Span<int>").WithLocation(10, 22),
                // (10,37): error CS0828: Cannot assign 'Span<int>' to anonymous type property
                //         return new { Outer = outer, Inner = inner };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "Inner = inner").WithArguments("System.Span<int>").WithLocation(10, 37));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefStructInFor(LanguageVersion languageVersion)
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M()
    {
        Span<int> outer;
        for (Span<int> inner = stackalloc int[10];; inner = outer)
        {
            outer = inner;
        }
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (10,21): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             outer = inner;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(10, 21));
        }

        [Fact]
        public void RefStructInLock()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class C
{
    void M()
    {
        Span<int> s = stackalloc int[10];
        lock (s)
        {
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,15): error CS0185: 'Span<int>' is not a reference type as required by the lock statement
                //         lock (s)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "s").WithArguments("System.Span<int>").WithLocation(9, 15));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefStructEscapeInIterator(LanguageVersion languageVersion)
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
using System.Collections;
class C
{
    IEnumerable F1()
    {
        Span<int> s1 = stackalloc int[10];
        yield return s1;
    }
    IEnumerable F2()
    {
        Span<int> s2 = default;
        yield return s2;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            // Note: an escape analysis error is not given here because we already gave a conversion error.
            comp.VerifyDiagnostics(
                // (9,22): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'object'
                //         yield return s1;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("System.Span<int>", "object").WithLocation(9, 22),
                // (14,22): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'object'
                //         yield return s2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s2").WithArguments("System.Span<int>", "object").WithLocation(14, 22));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeReturnEscape(LanguageVersion languageVersion)
        {
            var text = @"using System;


    class Program
    {
        static void Main()
        {
        }

        static ref int Test1(S1 arg)
        {
            return ref (new int[1])[0];
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        static ref int Test3()
        {
            Span<int> local = stackalloc int[1];
            return ref Test1(MayWrap(ref local));
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (23,42): error CS8526: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(23, 42),
                // (23,30): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref local)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(23, 30),
                // (23,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(MayWrap(ref local))").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(23, 24)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeReturnEscape1(LanguageVersion languageVersion)
        {
            var text = @"
    using System;

    class Program
    {
        static void Main()
        {
        }

        static ref int Test1(S1 arg)
        {
            return ref (new int[1])[0];
        }

        static S1 MayWrap(Span<int> arg)
        {
            return default;
        }

        static ref int Test3()
        {
            Span<int> local = stackalloc int[1];
            var sp = MayWrap(local);
            return ref Test1(sp);
        }

        ref struct S1
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (24,30): error CS8526: Cannot use variable 'sp' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sp").WithArguments("sp").WithLocation(24, 30),
                // (24,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(sp)").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(24, 24)
            );
        }

        [Fact]
        public void RefLikeReturnEscapeWithRefLikes()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        static ref int Test1(ref S1 arg)
        {
            return ref (new int[1])[0];
        }

        static S1 MayWrap(Span<int> arg)
        {
            return default;
        }

        static ref int Test3()
        {
            Span<int> local = stackalloc int[1];
            var sp = MayWrap(local);
            return ref Test1(ref sp);    // error1
        }

        static ref int Test4()
        {
            var sp = MayWrap(default);
            return ref Test1(ref sp);    // error2
        }

        ref struct S1
        {
        }
    }
";

            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (23,34): error CS8168: Cannot return local 'sp' by reference because it is not a ref local
                //             return ref Test1(ref sp);    // error1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "sp").WithArguments("sp").WithLocation(23, 34),
                // (23,24): error CS8347: Cannot use a result of 'Program.Test1(ref Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(ref sp);    // error1
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref sp)").WithArguments("Program.Test1(ref Program.S1)", "arg").WithLocation(23, 24),
                // (29,34): error CS8168: Cannot return local 'sp' by reference because it is not a ref local
                //             return ref Test1(ref sp);    // error2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "sp").WithArguments("sp").WithLocation(29, 34),
                // (29,24): error CS8347: Cannot use a result of 'Program.Test1(ref Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(ref sp);    // error2
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref sp)").WithArguments("Program.Test1(ref Program.S1)", "arg").WithLocation(29, 24)
                );

            comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (23,34): error CS8352: Cannot use variable 'sp' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(ref sp);    // error1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sp").WithArguments("sp").WithLocation(23, 34),
                // (23,24): error CS8347: Cannot use a result of 'Program.Test1(ref Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(ref sp);    // error1
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref sp)").WithArguments("Program.Test1(ref Program.S1)", "arg").WithLocation(23, 24),
                // (29,34): error CS8168: Cannot return local 'sp' by reference because it is not a ref local
                //             return ref Test1(ref sp);    // error2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "sp").WithArguments("sp").WithLocation(29, 34),
                // (29,24): error CS8347: Cannot use a result of 'Program.Test1(ref Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(ref sp);    // error2
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref sp)").WithArguments("Program.Test1(ref Program.S1)", "arg").WithLocation(29, 24)
                );
        }

        [Fact]
        public void RefLikeReturnEscapeWithRefLikes1()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        static ref Span<int> Test1(scoped ref S1 arg)
        {
            throw null;
        }

        static S1 MayWrap(Span<int> arg)
        {
            return default;
        }

        static void Test5()
        {
            var sp = MayWrap(default);

            // returnable. 
            var spR = MayWrap(Test1(ref sp));   

            Span<int> local = stackalloc int[1];
            var sp1 = MayWrap(local);

            // not returnable by value. (since it refers to a local data)
            var spNr = MayWrap(Test1(ref sp1));   

            // error
            spR = spNr;

            // ok, picks the narrowest val-escape
            var ternary =  true? spR: spNr;

            // error
            spR = ternary;
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text);
            comp.VerifyDiagnostics(
                // (33,19): error CS8526: Cannot use variable 'spNr' in this context because it may expose referenced variables outside of their declaration scope
                //             spR = spNr;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "spNr").WithArguments("spNr").WithLocation(33, 19),
                // (39,19): error CS8526: Cannot use variable 'ternary' in this context because it may expose referenced variables outside of their declaration scope
                //             spR = ternary;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "ternary").WithArguments("ternary").WithLocation(39, 19)
            );
        }

        [Fact]
        public void RefLikeReturnEscapeInParam()
        {
            var text = @"using System;

    class Program
    {
        static void Main()
        {
        }

        static ref int Test1(S1 arg)
        {
            return ref (new int[1])[0];
        }

        static S1 MayWrap(scoped in Span<int> arg)
        {
            return default;
        }

        static ref int Test3()
        {
            Span<int> local = stackalloc int[1];
            var sp = MayWrap(local);

            // not an error
            sp = MayWrap(local);

            // not an error
            sp = sp;

            // error here
            return ref Test1(sp);
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text);
            comp.VerifyDiagnostics(
                // (31,30): error CS8352: Cannot use variable 'sp' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sp").WithArguments("sp").WithLocation(31, 30),
                // (31,24): error CS8347: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(sp)").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(31, 24),
                // (28,13): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //             sp = sp;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "sp = sp").WithLocation(28, 13)
            );
        }

        [Fact()]
        public void RefLikeReturnEscapeInParamOptional()
        {
            var text = @"
    class Program
    {
        static void Main()
        {
        }

        static ref int Test1(S1 arg)
        {
            return ref (new int[1])[0];
        }

        static S1 MayNotWrap(in int arg = 123)
        {
            return default;
        }

        static ref int Test3()
        {
            // ok
            return ref Test1(MayNotWrap());
        }

        ref struct S1
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics();

            // In C#11, the rvalue from a method invocation that returns a ref struct is safe-to-escape
            // from ... the ref-safe-to-escape of all ref arguments, including in arguments and default values.
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (21,24): error CS8347: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayNotWrap());
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(MayNotWrap())").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(21, 24),
                // (21,30): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //             return ref Test1(MayNotWrap());
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "MayNotWrap()").WithLocation(21, 30),
                // (21,30): error CS8347: Cannot use a result of 'Program.MayNotWrap(in int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayNotWrap());
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayNotWrap()").WithArguments("Program.MayNotWrap(in int)", "arg").WithLocation(21, 30));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeScopeEscape(LanguageVersion languageVersion)
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
            Span<int> outer = default;

            S1 x = MayWrap(outer);

            {
                 Span<int> inner = stackalloc int[1];

                // valid
                x = MayWrap(outer);
    
                // error
                x = MayWrap(inner);
            }
        }

        static S1 MayWrap(Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (18,29): error CS8526: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(inner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(18, 29),
                // (18,21): error CS8521: Cannot use a result of 'Program.MayWrap(Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(inner)").WithArguments("Program.MayWrap(System.Span<int>)", "arg").WithLocation(18, 21)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeScopeEscapeVararg(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        Span<int> outer = default;

        S1 x = MayWrap(__arglist(outer));

        {
            Span<int> inner = stackalloc int[1];

            // valid
            x = MayWrap(__arglist(outer));

            // error
            x = MayWrap(__arglist(inner));
        }
    }

    static S1 MayWrap(__arglist)
    {
        return default;
    }

    ref struct S1
    {
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (18,35): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x = MayWrap(__arglist(inner));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(18, 35),
                // (18,17): error CS8347: Cannot use a result of 'Program.MayWrap(__arglist)' in this context because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //             x = MayWrap(__arglist(inner));
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(__arglist(inner))").WithArguments("Program.MayWrap(__arglist)", "__arglist").WithLocation(18, 17)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefScopeEscapeVararg(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {

    }    

    static ref int ReturnsRef()
    {
        int local = 42;

        // OK (also in 7.0)
        // __refvalue is not ref-returnable, so ref varargs can't come back
        return ref ReturnsRef1(__arglist(ref local));
    }

    static ref int ReturnsRef1(__arglist)
    {
        var ai = new ArgIterator(__arglist);

        // ERROR here. __refvalue is not ref-returnable
        return ref __refvalue(ai.GetNextArg(), int);
    }

    static ref int ReturnsRefSpan()
    {
        Span<int> local = stackalloc int[1];
        
        // error here;
        return ref ReturnsRefSpan1(__arglist(ref local));
    }

    static ref int ReturnsRefSpan1(__arglist)
    {
        var ai = new ArgIterator(__arglist);

        // this is ok
        return ref __refvalue(ai.GetNextArg(), Span<int>)[0];
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (24,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref __refvalue(ai.GetNextArg(), int);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "__refvalue(ai.GetNextArg(), int)").WithLocation(24, 20),
                // (32,50): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref ReturnsRefSpan1(__arglist(ref local));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(32, 50),
                // (32,20): error CS8347: Cannot use a result of 'Program.ReturnsRefSpan1(__arglist)' in this context because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         return ref ReturnsRefSpan1(__arglist(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRefSpan1(__arglist(ref local))").WithArguments("Program.ReturnsRefSpan1(__arglist)", "__arglist").WithLocation(32, 20)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ThrowExpression(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {

    }    

    static ref int M1() => throw null;

    static ref readonly int M2() => throw null;

    static ref Span<int> M3() => throw null;

    static ref readonly Span<int> M4() => throw null;

    static Span<int> M5() => throw null;

    static Span<int> M6() => M5().Length !=0 ? M5() : throw null;

}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UserDefinedLogical(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
    }

    S1 Test()
    {
        S1 global = default;
        S1 local = stackalloc int[100];

        // ok
        local = global && local;
        local = local && local;

        // ok
        global = global && global;

        // error
        global = local && global;

        // error
        return global || local;
    }
}

ref struct S1
{
    public static implicit operator S1(Span<int> o) => default;

    public static bool operator true(S1 o) => true;
    public static bool operator false(S1 o) => false;

    public static S1 operator &(S1 x, S1 y) => x;
    public static S1 operator |(S1 x, S1 y) => x;
}

";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (22,18): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local && global;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(22, 18),
                // (25,26): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         return global || local;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(25, 26)
             );
        }

        [Fact()]
        public void DiscardExpressionRef()
        {
            var text = @"

class Program
{
    static void Main()
    {

    }

    static ref int ReturnsRefTest()
    {
        return ref ReturnsRef1(out var _);
    }

    static ref int ReturnsRef1(out int x)
    {
        x = 42;
        return ref x;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (12,36): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref ReturnsRef1(out var _);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "var _").WithLocation(12, 36),
                // (12,20): error CS8347: Cannot use a result of 'Program.ReturnsRef1(out int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref ReturnsRef1(out var _);
                Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRef1(out var _)").WithArguments("Program.ReturnsRef1(out int)", "x").WithLocation(12, 20)
                );
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (18,20): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(18, 20)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionRef_UnsafeContext(LanguageVersion languageVersion)
        {
            var text = @"

unsafe class Program
{
    static ref int ReturnsRefTest()
    {
        return ref ReturnsRef1(out var _);
    }

    static ref int ReturnsRef1(out int x)
    {
        x = 42;
        return ref x;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp10)
            {
                comp.VerifyDiagnostics(
                    // (7,20): error CS8347: Cannot use a result of 'Program.ReturnsRef1(out int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                    //         return ref ReturnsRef1(out var _);
                    Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRef1(out var _)").WithArguments("Program.ReturnsRef1(out int)", "x").WithLocation(7, 20),
                    // (7,36): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                    //         return ref ReturnsRef1(out var _);
                    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "var _").WithLocation(7, 36));
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (13,20): warning CS9085: This returns a parameter by reference 'x' but it is scoped to the current method
                    //         return ref x;
                    Diagnostic(ErrorCode.WRN_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(13, 20));
            }
        }

        [Fact()]
        public void OrdinaryLocalAndOutRef()
        {
            var text = @"

class Program
{
    static void Main()
    {

    }

    static ref int ReturnsRefTest1()
    {       
        return ref ReturnsRef(out var z);
    }

    static ref int ReturnsRefTest2()
    {   int z;
        return ref ReturnsRef(out z);
    }


    static ref int ReturnsRef(out int x)
    {
        x = 42;
        return ref x;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (12,35): error CS8168: Cannot return local 'z' by reference because it is not a ref local
                //         return ref ReturnsRef(out var z);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "var z").WithArguments("z").WithLocation(12, 35),
                // (12,20): error CS8347: Cannot use a result of 'Program.ReturnsRef(out int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref ReturnsRef(out var z);
                Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRef(out var z)").WithArguments("Program.ReturnsRef(out int)", "x").WithLocation(12, 20),
                // (17,35): error CS8168: Cannot return local 'z' by reference because it is not a ref local
                //         return ref ReturnsRef(out z);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "z").WithArguments("z").WithLocation(17, 35),
                // (17,20): error CS8347: Cannot use a result of 'Program.ReturnsRef(out int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref ReturnsRef(out z);
                Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRef(out z)").WithArguments("Program.ReturnsRef(out int)", "x").WithLocation(17, 20)
                );
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (24,20): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(24, 20)
                );
        }

        [Fact]
        public void DiscardExpressionSpan_01()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {

    }

    static Span<int> Test1()
    {
        var s = ReturnsSpan(out var _);

        // ok
        return s;
    }

    static Span<int> Test2()
    {
        ref var s = ref ReturnsSpan(out var _);

        // error
        s = stackalloc int[1]; // 1

        // ok
        return s;
    }

    static void Test3()
    {
        // error
        ReturnsSpan(out var _ ) = stackalloc int[1]; // 2
    }

    static ref Span<int> ReturnsSpan(out Span<int> x)
    {
        x = default;
        return ref x; // 3
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (23,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s = stackalloc int[1]; // 1
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(23, 13),
                // (32,35): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out var _ ) = stackalloc int[1]; // 2
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(32, 35)
                );
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (23,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s = stackalloc int[1]; // 1
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(23, 13),
                // (32,35): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out var _ ) = stackalloc int[1]; // 2
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(32, 35),
                // (38,20): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return ref x; // 3
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(38, 20)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionSpan_UnsafeContext(LanguageVersion languageVersion)
        {
            var text = @"
using System;
unsafe class Program
{
    static Span<int> Test1()
    {
        var s = ReturnsSpan(out var _);
        return s;
    }

    static Span<int> Test2()
    {
        ref var s = ref ReturnsSpan(out var _);
        s = stackalloc int[1]; // 1
        return s;
    }

    static void Test3()
    {
        ReturnsSpan(out var _ ) = stackalloc int[1]; // 2
    }

    static ref Span<int> ReturnsSpan(out Span<int> x)
    {
        x = default;
        return ref x; // 3
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp10)
            {
                comp.VerifyDiagnostics(
                    // (14,13): warning CS9081: A result of a stackalloc expression of type 'Span<int>' in this context may be exposed outside of the containing method
                    //         s = stackalloc int[1]; // 1
                    Diagnostic(ErrorCode.WRN_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(14, 13),
                    // (20,35): warning CS9081: A result of a stackalloc expression of type 'Span<int>' in this context may be exposed outside of the containing method
                    //         ReturnsSpan(out var _ ) = stackalloc int[1]; // 2
                    Diagnostic(ErrorCode.WRN_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(20, 35));
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (14,13): warning CS9078: A result of a stackalloc expression of type 'Span<int>' in this context may be exposed outside of the containing method
                    //         s = stackalloc int[1]; // 1
                    Diagnostic(ErrorCode.WRN_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(14, 13),
                    // (20,35): warning CS9078: A result of a stackalloc expression of type 'Span<int>' in this context may be exposed outside of the containing method
                    //         ReturnsSpan(out var _ ) = stackalloc int[1]; // 2
                    Diagnostic(ErrorCode.WRN_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(20, 35),
                    // (26,20): warning CS9085: This returns a parameter by reference 'x' but it is scoped to the current method
                    //         return ref x; // 3
                    Diagnostic(ErrorCode.WRN_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(26, 20)
                    );
            }
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (System.Span<int>& V_0, //s
                System.Span<int> V_1,
                System.Span<int> V_2,
                System.Span<int> V_3)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_1
  IL_0003:  call       ""ref System.Span<int> Program.ReturnsSpan(out System.Span<int>)""
  IL_0008:  stloc.0
  IL_0009:  ldc.i4.4
  IL_000a:  conv.u
  IL_000b:  localloc
  IL_000d:  ldc.i4.1
  IL_000e:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_0013:  stloc.2
  IL_0014:  ldloc.0
  IL_0015:  ldloc.2
  IL_0016:  stobj      ""System.Span<int>""
  IL_001b:  ldloc.0
  IL_001c:  ldobj      ""System.Span<int>""
  IL_0021:  stloc.3
  IL_0022:  br.s       IL_0024
  IL_0024:  ldloc.3
  IL_0025:  ret
}
");
            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.Span<int> V_0,
                System.Span<int>& V_1,
                System.Span<int> V_2)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""ref System.Span<int> Program.ReturnsSpan(out System.Span<int>)""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.4
  IL_000a:  conv.u
  IL_000b:  localloc
  IL_000d:  ldc.i4.1
  IL_000e:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_0013:  stloc.2
  IL_0014:  ldloc.1
  IL_0015:  ldloc.2
  IL_0016:  stobj      ""System.Span<int>""
  IL_001b:  ret
}
");
        }

        // As above with 'out _' instead of 'out var _'.
        [WorkItem(65651, "https://github.com/dotnet/roslyn/issues/65651")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionSpan_02(LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class Program
                {
                    static Span<int> Test2A()
                    {
                        ref var s2A = ref ReturnsSpan(out _);
                        s2A = stackalloc int[1]; // 1
                        return s2A;
                    }
                    static Span<int> Test2B()
                    {
                        Span<int> _;
                        ref var s2B = ref ReturnsSpan(out _);
                        s2B = stackalloc int[1]; // 2
                        return s2B;
                    }
                    static void Test3A()
                    {
                        ReturnsSpan(out _ ) = stackalloc int[1]; // 3
                    }
                    static void Test3B()
                    {
                        Span<int> _;
                        ReturnsSpan(out _ ) = stackalloc int[1]; // 4
                    }
                    static ref Span<int> ReturnsSpan(out Span<int> x)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (7,15): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s2A = stackalloc int[1]; // 1
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(7, 15),
                // (14,15): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s2B = stackalloc int[1]; // 2
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(14, 15),
                // (19,31): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out _ ) = stackalloc int[1]; // 3
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(19, 31),
                // (24,31): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out _ ) = stackalloc int[1]; // 4
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(24, 31));
        }

        // ReturnsSpan() returns ref Span<int>, callers return Span<int> by value.
        [WorkItem(65651, "https://github.com/dotnet/roslyn/issues/65651")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionSpan_03(LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class Program
                {
                    static Span<int> Test1()
                    {
                        var s1 = ReturnsSpan(out _);
                        return s1;
                    }
                    static Span<int> Test2()
                    {
                        var s2 = ReturnsSpan(out var _);
                        return s2;
                    }
                    static Span<int> Test3()
                    {
                        var s3 = ReturnsSpan(out Span<int> _);
                        return s3;
                    }
                    static Span<int> Test4()
                    {
                        var s4 = ReturnsSpan(out var unused);
                        return s4;
                    }
                    static Span<int> Test5()
                    {
                        Span<int> _;
                        var s5 = ReturnsSpan(out _);
                        return s5;
                    }
                    static Span<int> Test6(out Span<int> _)
                    {
                        var s6 = ReturnsSpan(out _);
                        return s6;
                    }
                    static ref Span<int> ReturnsSpan(out Span<int> x)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics();
        }

        // ReturnsSpan() and callers return Span<int> by value.
        [WorkItem(65651, "https://github.com/dotnet/roslyn/issues/65651")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionSpan_04(LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class Program
                {
                    static Span<int> Test1()
                    {
                        var s1 = ReturnsSpan(out _);
                        return s1;
                    }
                    static Span<int> Test2()
                    {
                        var s2 = ReturnsSpan(out var _);
                        return s2;
                    }
                    static Span<int> Test3()
                    {
                        var s3 = ReturnsSpan(out Span<int> _);
                        return s3;
                    }
                    static Span<int> Test4()
                    {
                        var s4 = ReturnsSpan(out var unused);
                        return s4;
                    }
                    static Span<int> Test5()
                    {
                        Span<int> _;
                        var s5 = ReturnsSpan(out _);
                        return s5;
                    }
                    static Span<int> Test6(out Span<int> _)
                    {
                        var s6 = ReturnsSpan(out _);
                        return s6;
                    }
                    static Span<int> ReturnsSpan(out Span<int> x)
                    {
                        x = default;
                        return x;
                    }
                }
                """;
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics();
        }

        // ReturnsSpan() and callers return ref Span<int>.
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionSpan_05(LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class Program
                {
                    static ref Span<int> Test1()
                    {
                        var s1 = ReturnsSpan(out _);
                        return ref s1; // 1
                    }
                    static ref Span<int> Test2()
                    {
                        var s2 = ReturnsSpan(out var _);
                        return ref s2; // 2
                    }
                    static ref Span<int> Test3()
                    {
                        var s3 = ReturnsSpan(out Span<int> _);
                        return ref s3; // 3
                    }
                    static ref Span<int> Test4()
                    {
                        var s4 = ReturnsSpan(out var unused);
                        return ref s4; // 4
                    }
                    static ref Span<int> Test5()
                    {
                        Span<int> _;
                        var s5 = ReturnsSpan(out _);
                        return ref s5; // 5
                    }
                    static ref Span<int> Test6(out Span<int> _)
                    {
                        var s6 = ReturnsSpan(out _);
                        return ref s6; // 6
                    }
                    static ref Span<int> ReturnsSpan(out Span<int> x)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (7,20): error CS8168: Cannot return local 's1' by reference because it is not a ref local
                //         return ref s1; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s1").WithArguments("s1").WithLocation(7, 20),
                // (12,20): error CS8168: Cannot return local 's2' by reference because it is not a ref local
                //         return ref s2; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s2").WithArguments("s2").WithLocation(12, 20),
                // (17,20): error CS8168: Cannot return local 's3' by reference because it is not a ref local
                //         return ref s3; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s3").WithArguments("s3").WithLocation(17, 20),
                // (22,20): error CS8168: Cannot return local 's4' by reference because it is not a ref local
                //         return ref s4; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s4").WithArguments("s4").WithLocation(22, 20),
                // (28,20): error CS8168: Cannot return local 's5' by reference because it is not a ref local
                //         return ref s5; // 5
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s5").WithArguments("s5").WithLocation(28, 20),
                // (33,20): error CS8168: Cannot return local 's6' by reference because it is not a ref local
                //         return ref s6; // 6
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s6").WithArguments("s6").WithLocation(33, 20));
        }

        // ReturnsSpan() and callers return ref int.
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DiscardExpressionSpan_06(LanguageVersion languageVersion)
        {
            string source = """
                class Program
                {
                    static ref int Test1()
                    {
                        var s1 = ReturnsSpan(out _);
                        return ref s1; // 1
                    }
                    static ref int Test2()
                    {
                        var s2 = ReturnsSpan(out var _);
                        return ref s2; // 2
                    }
                    static ref int Test3()
                    {
                        var s3 = ReturnsSpan(out int _);
                        return ref s3; // 3
                    }
                    static ref int Test4()
                    {
                        var s4 = ReturnsSpan(out var unused);
                        return ref s4; // 4
                    }
                    static ref int Test5()
                    {
                        int _;
                        var s5 = ReturnsSpan(out _);
                        return ref s5; // 5
                    }
                    static ref int Test6(out int _)
                    {
                        var s6 = ReturnsSpan(out _);
                        return ref s6; // 6
                    }
                    static ref int ReturnsSpan(out int x)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (6,20): error CS8168: Cannot return local 's1' by reference because it is not a ref local
                //         return ref s1; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s1").WithArguments("s1").WithLocation(6, 20),
                // (11,20): error CS8168: Cannot return local 's2' by reference because it is not a ref local
                //         return ref s2; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s2").WithArguments("s2").WithLocation(11, 20),
                // (16,20): error CS8168: Cannot return local 's3' by reference because it is not a ref local
                //         return ref s3; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s3").WithArguments("s3").WithLocation(16, 20),
                // (21,20): error CS8168: Cannot return local 's4' by reference because it is not a ref local
                //         return ref s4; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s4").WithArguments("s4").WithLocation(21, 20),
                // (27,20): error CS8168: Cannot return local 's5' by reference because it is not a ref local
                //         return ref s5; // 5
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s5").WithArguments("s5").WithLocation(27, 20),
                // (32,20): error CS8168: Cannot return local 's6' by reference because it is not a ref local
                //         return ref s6; // 6
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s6").WithArguments("s6").WithLocation(32, 20));
        }

        [Theory]
        [InlineData("out var _")]
        [InlineData("out _")]
        [InlineData("out Span<int> _")]
        [InlineData("out var unused")]
        [InlineData("out Span<int> unused")]
        public void DiscardExpressionSpan_07(string outVarDeclaration)
        {
            string source = $$"""
                using System;
                using System.Diagnostics.CodeAnalysis;
                class Program
                {
                    static Span<int> Test1()
                    {
                        var s1 = ReturnsSpan({{outVarDeclaration}});
                        return s1;
                    }
                    static Span<int> Test2()
                    {
                        ref var s2 = ref ReturnsSpan({{outVarDeclaration}});
                        s2 = stackalloc int[1]; // 1
                        return s2;
                    }
                    static void Test3()
                    {
                        ReturnsSpan({{outVarDeclaration}}) =
                            stackalloc int[1]; // 2
                    }
                    static ref Span<int> ReturnsSpan([UnscopedRef] out Span<int> x)
                    {
                        x = default;
                        return ref x;
                    }
                }
                """;
            var comp = CreateCompilationWithMscorlibAndSpan(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,14): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s2 = stackalloc int[1]; // 1
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(13, 14),
                // (19,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //             stackalloc int[1]; // 2
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(19, 13));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Discard_01(LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class Program
                {
                    static void F1()
                    {
                        Span<int> s1 = stackalloc int[1];
                        _ = s1;
                    }
                    static void F2()
                    {
                        Span<int> s2 = stackalloc int[1];
                        Span<int> _;
                        _ = s2; // 1
                    }
                }
                """;
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (13,13): error CS8352: Cannot use variable 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         _ = s2; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s2").WithArguments("s2").WithLocation(13, 13));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Discard_02(LanguageVersion languageVersion)
        {
            string source = """
                class Program
                {
                    static void F1(ref int x1)
                    {
                        int y1 = 1;
                        _ = ref y1;
                        _ = ref x1;
                    }
                    static void F2(ref int x2)
                    {
                        int y2 = 2;
                        _ = ref x2;
                        _ = ref y2;
                    }
                    static void F3()
                    {
                        int y3 = 3;
                        ref int _ = ref y3;
                        _ = ref y3;
                    }
                    static void F4(ref int x4)
                    {
                        int y4 = 4;
                        ref int _ = ref x4;
                        _ = ref y4; // 1
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (25,9): error CS8374: Cannot ref-assign 'y4' to '_' because 'y4' has a narrower escape scope than '_'.
                //         _ = ref y4; // 1
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "_ = ref y4").WithArguments("_", "y4").WithLocation(25, 9));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Discard_03(LanguageVersion languageVersion)
        {
            var source =
@"class Program
{
    static void F1()
    {
        (var x1, _) = F();
        (var x2, var _) = F();
        (var x3, R _) = F();
        var (x4, _) = F();
    }
    static void F2()
    {
        R _;
        (var x5, _) = F();
    }
    static R F() => default;
}
ref struct R 
{
    public void Deconstruct(out R x, out R y) => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Discard_04(LanguageVersion languageVersion)
        {
            var source =
@"using System;
class Program
{
    static void F1()
    {
        Span<int> s1 = default;
        s1.Deconstruct(out s1, out _);
    }
    static void F2()
    {
        Span<int> s2 = default;
        (s2, _) = s2;
    }
    static void F3()
    {
        Span<int> s3 = default;
        s3.Deconstruct(out s3, out var _);
    }
    static void F4()
    {
        Span<int> s4 = default;
        (s4, var _) = s4;
    }
    static void F5()
    {
        Span<int> s5 = default;
        s5.Deconstruct(out s5, out Span<int> _);
    }
    static void F6()
    {
        Span<int> s6 = default;
        (s6, Span<int> _) = s6;
    }
    static void F7()
    {
        Span<int> s7 = default;
        s7.Deconstruct(out s7, out var unused);
    }
    static void F8()
    {
        Span<int> s8 = default;
        (s8, var unused) = s8;
    }
}
static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y)
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [WorkItem(65522, "https://github.com/dotnet/roslyn/issues/65522")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Discard_05(LanguageVersion languageVersion)
        {
            var source =
@"using System;
class Program
{
    static void F1()
    {
        Span<int> s1 = stackalloc int[10];
        s1.Deconstruct(out s1, out _);
    }
    static void F2()
    {
        Span<int> s2 = stackalloc int[10];
        (s2, _) = s2;
    }
    static void F3()
    {
        Span<int> s3 = stackalloc int[10];
        s3.Deconstruct(out s3, out var _);
    }
    static void F4()
    {
        Span<int> s4 = stackalloc int[10];
        (s4, var _) = s4;
    }
    static void F5()
    {
        Span<int> s5 = stackalloc int[10];
        s5.Deconstruct(out s5, out Span<int> _);
    }
    static void F6()
    {
        Span<int> s6 = stackalloc int[10];
        (s6, Span<int> _) = s6;
    }
    static void F7()
    {
        Span<int> s7 = stackalloc int[10];
        s7.Deconstruct(out s7, out var unused);
    }
    static void F8()
    {
        Span<int> s8 = stackalloc int[10];
        (s8, var unused) = s8;
    }
}
static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y)
    {
        throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Fact()]
        public void OrdinaryLocalAndOutSpan()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {

    }

    static Span<int> Test1()
    {
        var s = ReturnsSpan(out var z);

        // ok
        return s;
    }

    static Span<int> Test2()
    {
        ref var r = ref ReturnsSpan(out var z);

        // error
        r = stackalloc int[1];

        // ok
        return r;
    }

    static void Test3()
    {
        ReturnsSpan(out var z) = stackalloc int[1];
    }

    static Span<int> Test4()
    {
        Span<int> s;
        var r = ReturnsSpan(out s);

        // ok
        return r;
    }

    static Span<int> Test5()
    {
        Span<int> s;
        ref var r = ref ReturnsSpan(out s);

        // error
        r = stackalloc int[1];

        // ok
        return r;
    }

    static void Test6()
    {
        Span<int> s;

        // error
        ReturnsSpan(out s) = stackalloc int[1];
    }

    static ref Span<int> ReturnsSpan(out Span<int> x)
    {
        x = default;
        return ref x;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (23,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         r = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(23, 13),
                // (31,34): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out var z) = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(31, 34),
                // (49,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         r = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(49, 13),
                // (60,30): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out s) = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(60, 30)
                );
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (23,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         r = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(23, 13),
                // (31,34): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out var z) = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(31, 34),
                // (49,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         r = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(49, 13),
                // (60,30): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out s) = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(60, 30),
                // (66,20): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(66, 20)
                );
        }

        [Fact]
        public void RefLikeScopeEscapeReturnable()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
            Span<int> outer = default;

            // make x returnable
            S1 x = default;

            {
                Span<int> inner = stackalloc int[0];

                // valid
                x = MayWrap(ref outer);
    
                // error
                x = MayWrap(ref inner);
            }
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (19,33): error CS8526: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(19, 33),
                // (19,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(19, 21)
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (16,33): error CS8168: Cannot return local 'outer' by reference because it is not a ref local
                //                 x = MayWrap(ref outer);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "outer").WithArguments("outer").WithLocation(16, 33),
                // (16,21): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref outer);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref outer)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(16, 21),
                // (19,33): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(19, 33),
                // (19,21): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(19, 21)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeScopeEscapeThis(LanguageVersion languageVersion)
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
            Span<int> outer = default;

            S1 x = MayWrap(ref outer);

            {
                Span<int> inner = stackalloc int[1];

                // valid
                x = S1.NotSlice(1);

                // valid
                x = MayWrap(ref outer).Slice(1);
    
                // error
                x = MayWrap(ref inner).Slice(1);
            }
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
            public static S1 NotSlice(int x) => default;

            public S1 Slice(int x) => this;
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (21,33): error CS8526: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(ref inner).Slice(1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(21, 33),
                // (21,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner).Slice(1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(21, 21)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeScopeEscapeThisRef(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        Span<int> outer = default;

        ref S1 x = ref MayWrap(ref outer)[0];

        {
            Span<int> inner = stackalloc int[1];

            // valid
            x[0] = MayWrap(ref outer).Slice(1)[0];

            // error, technically rules for this case can be relaxed, 
            // but ref-like typed ref-returning properties are nearly impossible to implement in a useful way
            //
            x[0] = MayWrap(ref inner).Slice(1)[0];

            // error, technically rules for this case can be relaxed, 
            // but ref-like typed ref-returning properties are nearly impossible to implement in a useful way
            //
            x[x] = MayWrap(ref inner).Slice(1)[0];

            // error
            x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
        }
    }

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public ref S1 this[int i] => throw null;

        public ref S1 this[S1 i] => throw null;

        public ref S1 ReturnsRefArg(ref S1 arg) => throw null;

        public S1 Slice(int x) => this;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (20,32): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x[0] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(20, 32),
                // (20,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x[0] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(20, 20),
                // (25,32): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x[x] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(25, 32),
                // (25,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x[x] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(25, 20),
                // (28,50): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(28, 50),
                // (28,38): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(28, 38));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeScopeEscapeField(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        Span<int> outer = default;

        S1 x = MayWrap(outer);

        {
            Span<int> inner = stackalloc int[1];

            // valid
            x.field = MayWrap(outer).Slice(1).field;

            // error
            x.field = MayWrap(inner).Slice(1).field;
        }
    }

    static S1 MayWrap(Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public S0 field;

        public S1 Slice(int x) => this;
    }

    ref struct S0
    {
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (18,31): error CS8526: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x.field = MayWrap(inner).Slice(1).field;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(18, 31),
                // (18,23): error CS8521: Cannot use a result of 'Program.MayWrap(Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x.field = MayWrap(inner).Slice(1).field;
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(inner)").WithArguments("Program.MayWrap(System.Span<int>)", "arg").WithLocation(18, 23)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeEscapeParamsAndTopLevel(LanguageVersion languageVersion)
        {
            var text = @"
    class Program
    {
        static void Main()
        {
        }

        void Test1(int x)
        {
            int y = 1;

            var rx = MayWrap(ref x);
            var ry = MayWrap(ref y);

            // valid. parameter scope and the top local scope are the same.
            rx = ry;
            
            bool condition = true;
            rx = condition ? rx: ry;
        }

        static S1 MayWrap(ref int arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            // no diagnostics expected
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeEscapeMixingCallSameArgValue(LanguageVersion languageVersion)
        {
            var text = @"
    using System;
    public class Program
    {
        static void Main()
        {
        }

        void Test1()
        {
            S1 rOuter = default;

            Span<int> inner = stackalloc int[1];
            S1 rInner = MayWrap(inner);

            // valid
            MayAssign(ref rOuter);

            // valid
            MayAssign(ref rInner);
        }

        static void MayAssign(ref S1 arg1)
        {
            // valid
            arg1 = MayWrap(arg1.field);
        }

        static S1 MayWrap(Span<int> arg)
        {
            return default;
        }

        public ref struct S1
        {
            public Span<int> field;
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
        }

        [Fact]
        public void RefLikeEscapeMixingCall()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        void Test1()
        {
            S1 rOuter = default;

            Span<int> inner = stackalloc int[1];
            S1 rInner = MayWrap(ref inner);

            // valid
            MayAssign(ref rOuter, ref rOuter);

            // error
            MayAssign(ref rOuter, ref rInner);

            // error
            MayAssign(ref inner, ref rOuter);
        }

        static void MayAssign(ref Span<int> arg1, ref S1 arg2)
        {
            arg2 = MayWrap(ref arg1);
        }

        static void MayAssign(ref S1 arg1, ref S1 arg2)
        {
            arg1 = arg2;
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (20,39): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(20, 39),
                // (20,13): error CS8350: This combination of arguments to 'Program.MayAssign(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter, ref rInner)").WithArguments("Program.MayAssign(ref Program.S1, ref Program.S1)", "arg2").WithLocation(20, 13),
                // (23,27): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(23, 27),
                // (23,13): error CS8350: This combination of arguments to 'Program.MayAssign(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref inner, ref rOuter)").WithArguments("Program.MayAssign(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(23, 13));

            comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (20,39): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(20, 39),
                // (20,13): error CS8350: This combination of arguments to 'Program.MayAssign(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter, ref rInner)").WithArguments("Program.MayAssign(ref Program.S1, ref Program.S1)", "arg2").WithLocation(20, 13),
                // (23,27): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(23, 27),
                // (23,13): error CS8350: This combination of arguments to 'Program.MayAssign(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref inner, ref rOuter)").WithArguments("Program.MayAssign(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(23, 13),
                // (28,32): error CS9077: Cannot return a parameter by reference 'arg1' through a ref parameter; it can only be returned in a return statement
                //             arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "arg1").WithArguments("arg1").WithLocation(28, 32),
                // (28,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref arg1)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(28, 20)
            );
        }

        [Fact]
        public void RefLikeEscapeMixingCall_UnsafeContext()
        {
            var text = @"
using System;
class Program
{
    unsafe void Test1()
    {
        S1 rOuter = default;

        Span<int> inner = stackalloc int[1];
        S1 rInner = MayWrap(ref inner);

        // valid
        MayAssign(ref rOuter, ref rOuter);

        // warn
        MayAssign(ref rOuter, ref rInner);

        // warn
        MayAssign(ref inner, ref rOuter);
    }

    static unsafe void MayAssign(ref Span<int> arg1, ref S1 arg2)
    {
        arg2 = MayWrap(ref arg1);
    }

    static void MayAssign(ref S1 arg1, ref S1 arg2)
    {
        arg1 = arg2;
    }

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (16,35): warning CS9080: Use of variable 'rInner' in this context may expose referenced variables outside of their declaration scope
                //         MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.WRN_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(16, 35),
                // (19,23): warning CS9080: Use of variable 'inner' in this context may expose referenced variables outside of their declaration scope
                //         MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.WRN_EscapeVariable, "inner").WithArguments("inner").WithLocation(19, 23)
                );

            comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (16,35): warning CS9080: Use of variable 'rInner' in this context may expose referenced variables outside of their declaration scope
                //         MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.WRN_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(16, 35),
                // (19,23): warning CS9080: Use of variable 'inner' in this context may expose referenced variables outside of their declaration scope
                //         MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.WRN_EscapeVariable, "inner").WithArguments("inner").WithLocation(19, 23),
                // (24,28): warning CS9094: This returns a parameter by reference 'arg1' through a ref parameter; but it can only safely be returned in a return statement
                //         arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.WRN_RefReturnOnlyParameter, "arg1").WithArguments("arg1").WithLocation(24, 28)
                );
        }

        [Fact]
        public void RefLikeEscapeMixingCallVararg()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
    }

    void Test1()
    {
        S1 rOuter = default;

        Span<int> inner = stackalloc int[1];
        S1 rInner = MayWrap(ref inner);

        // valid
        MayAssign2(__arglist(ref rOuter, ref rOuter));

        // error
        MayAssign2(__arglist(ref rOuter, ref rInner));

        // error
        MayAssign1(__arglist(ref inner, ref rOuter));
    }

    static void MayAssign1(__arglist)
    {
        var ai = new ArgIterator(__arglist);

        ref var arg1 = ref __refvalue(ai.GetNextArg(), Span<int>);
        ref var arg2 = ref __refvalue(ai.GetNextArg(), S1);

        arg2 = MayWrap(ref arg1);
    }

    static void MayAssign2(__arglist)
    {
        var ai = new ArgIterator(__arglist);

        ref var arg1 = ref __refvalue(ai.GetNextArg(), S1);
        ref var arg2 = ref __refvalue(ai.GetNextArg(), S1);

        arg1 = arg2;
    }

    static S1 MayWrap(scoped ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
    }
}
";

            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (46,23): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static S1 MayWrap(scoped ref Span<int> arg)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(46, 23),
                // (20,46): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //         MayAssign2(__arglist(ref rOuter, ref rInner));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(20, 46),
                // (20,9): error CS8350: This combination of arguments to 'Program.MayAssign2(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         MayAssign2(__arglist(ref rOuter, ref rInner));
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign2(__arglist(ref rOuter, ref rInner))").WithArguments("Program.MayAssign2(__arglist)", "__arglist").WithLocation(20, 9),
                // (23,34): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         MayAssign1(__arglist(ref inner, ref rOuter));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(23, 34),
                // (23,9): error CS8350: This combination of arguments to 'Program.MayAssign1(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         MayAssign1(__arglist(ref inner, ref rOuter));
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign1(__arglist(ref inner, ref rOuter))").WithArguments("Program.MayAssign1(__arglist)", "__arglist").WithLocation(23, 9)
            );

            // Same errors modulo the scoped
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (20,46): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //         MayAssign2(__arglist(ref rOuter, ref rInner));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(20, 46),
                // (20,9): error CS8350: This combination of arguments to 'Program.MayAssign2(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         MayAssign2(__arglist(ref rOuter, ref rInner));
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign2(__arglist(ref rOuter, ref rInner))").WithArguments("Program.MayAssign2(__arglist)", "__arglist").WithLocation(20, 9),
                // (23,34): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         MayAssign1(__arglist(ref inner, ref rOuter));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(23, 34),
                // (23,9): error CS8350: This combination of arguments to 'Program.MayAssign1(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         MayAssign1(__arglist(ref inner, ref rOuter));
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign1(__arglist(ref inner, ref rOuter))").WithArguments("Program.MayAssign1(__arglist)", "__arglist").WithLocation(23, 9)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeEscapeMixingIndex(LanguageVersion languageVersion)
        {
            var text = @"
class Program
{
    static void Main()
    {
    }

    void Test1()
    {
        S1 rOuter = default;

        int inner = 1;
        S1 rInner = MayWrap(ref inner);

        // valid
        int dummy1 = this[rOuter, rOuter];

        // error
        int dummy2 = this[rOuter, rInner];

        // error
        int dummy3 = this[inner, rOuter];
    }

    int this[in int arg1, in S1 arg2]
    {
        get
        {
            // not possible
            // arg2 = MayWrap(ref arg1);
            return 0;
        }
    }

    int this[in S1 arg1, in S1 arg2]
    {
        get
        {
            // not possible
            // arg1 = arg2;
            return 0;
        }
    }

    static S1 MayWrap(ref int arg)
    {
        return default;
    }

    ref struct S1
    {
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            // no diagnostics
            );
        }

        [Fact]
        public void RefLikeEscapeMixingIndexOnRefLike()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
    }

    void Test1()
    {
        S1 rOuter = default;
        rOuter.field = default;

        Span<int> inner = stackalloc int[1];
        S1 rInner = MayWrap(inner);

        // valid
        int dummy1 = rOuter[rOuter];

        // valid
        int dummy2 = rInner[rOuter];

        // error
        int dummy3 = rOuter[rInner];

        // error
        int dummy4 = rOuter[inner];
    }

    static S1 MayWrap(in Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public Span<int> field;

        public int this[in Span<int> arg1]
        {
            get
            {
                // error: ref-safe-to-escape of arg1 is return-only
                this = MayWrap(arg1);
                return 0;
            }
        }

        public int this[in S1 arg1]
        {
            get
            {
                // error: ref-safe-to-escape of arg1 is return-only
                this = MayWrap(arg1.field);

                // ok: safe-to-escape of arg1 is calling method
                this = arg1;

                return 0;
            }
        }
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (24,29): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(24, 29),
                // (24,22): error CS8350: This combination of arguments to 'Program.S1.this[in Program.S1]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[rInner]").WithArguments("Program.S1.this[in Program.S1]", "arg1").WithLocation(24, 22),
                // (27,29): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(27, 29),
                // (27,22): error CS8350: This combination of arguments to 'Program.S1.this[in Span<int>]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[inner]").WithArguments("Program.S1.this[in System.Span<int>]", "arg1").WithLocation(27, 22)
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (24,29): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(24, 29),
                // (24,22): error CS8350: This combination of arguments to 'Program.S1.this[in Program.S1]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[rInner]").WithArguments("Program.S1.this[in Program.S1]", "arg1").WithLocation(24, 22),
                // (27,29): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(27, 29),
                // (27,22): error CS8350: This combination of arguments to 'Program.S1.this[in Span<int>]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[inner]").WithArguments("Program.S1.this[in System.Span<int>]", "arg1").WithLocation(27, 22),
                // (44,32): error CS9077: Cannot return a parameter by reference 'arg1' through a ref parameter; it can only be returned in a return statement
                //                 this = MayWrap(arg1);
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "arg1").WithArguments("arg1").WithLocation(44, 32),
                // (44,24): error CS8347: Cannot use a result of 'Program.MayWrap(in Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 this = MayWrap(arg1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(arg1)").WithArguments("Program.MayWrap(in System.Span<int>)", "arg").WithLocation(44, 24),
                // (54,32): error CS9078: Cannot return by reference a member of parameter 'arg1' through a ref parameter; it can only be returned in a return statement
                //                 this = MayWrap(arg1.field);
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter2, "arg1").WithArguments("arg1").WithLocation(54, 32),
                // (54,24): error CS8347: Cannot use a result of 'Program.MayWrap(in Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 this = MayWrap(arg1.field);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(arg1.field)").WithArguments("Program.MayWrap(in System.Span<int>)", "arg").WithLocation(54, 24)
            );
        }

        [Fact]
        public void RefLikeEscapeMixingCtor()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        delegate void D1(ref S1 arg1, ref S1 arg2);
        delegate void D2(ref Span<int> arg1, ref S1 arg2);

        void Test1()
        {
            S1 rOuter = default;

            Span<int> inner = stackalloc int[1];
            S1 rInner = MayWrap(ref inner);

            D1 MayAssignDel1 = MayAssign;
            D2 MayAssignDel2 = MayAssign;

            // valid
            MayAssignDel1(ref rOuter, ref rOuter);

            // error
            MayAssignDel1(ref rOuter, ref rInner);

            // error
            MayAssignDel2(ref inner, ref rOuter);
        }

        static void MayAssign(ref Span<int> arg1, ref S1 arg2)
        {
            arg2 = MayWrap(ref arg1);
        }

        static void MayAssign(ref S1 arg1, ref S1 arg2)
        {
            arg1 = arg2;
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (26,43): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(26, 43),
                // (26,13): error CS8350: This combination of arguments to 'Program.D1.Invoke(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel1(ref rOuter, ref rInner)").WithArguments("Program.D1.Invoke(ref Program.S1, ref Program.S1)", "arg2").WithLocation(26, 13),
                // (29,31): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(29, 31),
                // (29,13): error CS8350: This combination of arguments to 'Program.D2.Invoke(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel2(ref inner, ref rOuter)").WithArguments("Program.D2.Invoke(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(29, 13)
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (26,43): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(26, 43),
                // (26,13): error CS8350: This combination of arguments to 'Program.D1.Invoke(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel1(ref rOuter, ref rInner)").WithArguments("Program.D1.Invoke(ref Program.S1, ref Program.S1)", "arg2").WithLocation(26, 13),
                // (29,31): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(29, 31),
                // (29,13): error CS8350: This combination of arguments to 'Program.D2.Invoke(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel2(ref inner, ref rOuter)").WithArguments("Program.D2.Invoke(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(29, 13),
                // (34,32): error CS9077: Cannot return a parameter by reference 'arg1' through a ref parameter; it can only be returned in a return statement
                //             arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "arg1").WithArguments("arg1").WithLocation(34, 32),
                // (34,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref arg1)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(34, 20)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeObjInitializers(LanguageVersion languageVersion)
        {
            var text = @"
    using System;

    class Program
    {
        static void Main()
        {
        }

        static S2 Test1()
        {
            S1 outer = default;
            S1 inner = stackalloc int[1];

            // error
            return new S2() { Field1 = outer, Field2 = inner };
        }

        static S2 Test2()
        {
            S1 outer = default;
            S1 inner = stackalloc int[1];

            S2 result;

            // error
            result = new S2() { Field1 = inner, Field2 = outer };

            return result;
        }

        static S2 Test3()
        {
            S1 outer = default;
            S1 inner = stackalloc int[1];

            return new S2() { Field1 = outer, Field2 = outer };
        }

        public ref struct S1
        {
            public static implicit operator S1(Span<int> o) => default;
        }

        public ref struct S2
        {
            public S1 Field1;
            public S1 Field2;
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (16,47): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             return new S2() { Field1 = outer, Field2 = inner };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "Field2 = inner").WithArguments("inner").WithLocation(16, 47),
                // (27,33): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             result = new S2() { Field1 = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "Field1 = inner").WithArguments("inner").WithLocation(27, 33)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeObjInitializers1(LanguageVersion languageVersion)
        {
            var text = @"
    using System;

    class Program
    {
        static void Main()
        {
        }

        static S2 Test1()
        {
            S1 outer = default;
            S1 inner = stackalloc int[1];

            var x1 = new S2() { Field1 = outer, Field2 = inner };

            // error
            return x1;
        }

        static S2 Test2()
        {
            S1 outer = default;
            S1 inner = stackalloc int[1];

            var x2 = new S2() { Field1 = inner, Field2 = outer };

            // error
            return x2;
        }

        static S2 Test3()
        {
            S1 outer = default;
            S1 inner = stackalloc int[1];

            var x3 = new S2() { Field1 = outer, Field2 = outer };

            // ok
            return x3;
        }

        public ref struct S1
        {
            public static implicit operator S1(Span<int> o) => default;
        }

        public ref struct S2
        {
            public S1 Field1;
            public S1 Field2;
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (18,20): error CS8352: Cannot use variable 'x1' in this context because it may expose referenced variables outside of their declaration scope
                //             return x1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x1").WithArguments("x1").WithLocation(18, 20),
                // (29,20): error CS8352: Cannot use variable 'x2' in this context because it may expose referenced variables outside of their declaration scope
                //             return x2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x2").WithArguments("x2").WithLocation(29, 20)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeObjInitializersIndexer(LanguageVersion languageVersion)
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
    }

    static S2 Test1()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        // error
        return new S2() { [inner] = outer, Field2 = outer };
    }

    static S2 Test2()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        // error
        return new S2() { [outer] = inner, Field2 = outer };
    }

    static S2 Test3()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        return new S2() { [outer] = outer, Field2 = outer };
    }

    public ref struct S1
    {
        public static implicit operator S1(Span<int> o) => default;
    }

    public ref struct S2
    {
        private S1 field;

        public S1 this[S1 i]
        {
            get
            {
                return i;
            }
            set
            {
                field = i;
            }
        }

        public S1 Field2;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (16,28): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { [inner] = outer, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(16, 28),
                // (25,27): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { [outer] = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "[outer] = inner").WithArguments("inner").WithLocation(25, 27)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeObjInitializersIndexer1(LanguageVersion languageVersion)
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
    }

    static S2 Test1()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        var x1 =  new S2() { [inner] = outer, Field2 = outer };

        // error
        return x1;
    }

    static S2 Test2()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        S2 result;

        // error
        result = new S2() { [outer] = inner, Field2 = outer };

        return result;
    }

    static S2 Test3()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        var x3 = new S2() { [outer] = outer, Field2 = outer };

        // ok
        return x3;
    }

    public ref struct S1
    {
        public static implicit operator S1(Span<int> o) => default;
    }

    public ref struct S2
    {
        private S1 field;

        public S1 this[S1 i]
        {
            get
            {
                return i;
            }
            set
            {
                field = i;
            }
        }

        public S1 Field2;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (18,16): error CS8352: Cannot use variable 'x1' in this context because it may expose referenced variables outside of their declaration scope
                //         return x1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x1").WithArguments("x1").WithLocation(18, 16),
                // (29,29): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         result = new S2() { [outer] = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "[outer] = inner").WithArguments("inner").WithLocation(29, 29)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeObjInitializersNested(LanguageVersion languageVersion)
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
    }

    static S2 Nested1()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        return new S2() { Field2 = {[inner] = outer} };
    }

    static S2 Nested2()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        var x = new S2() { Field2 = {[inner] = outer } };

        return x;
    }

    static S2 Nested3()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        return new S2() { Field2 = {[outer] = inner} };
    }

    static S2 Nested4()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        var x = new S2() { Field2 = {[outer] = outer } };

        return x;  //ok
    }

    public ref struct S2
    {
        public S3 Field2;
    }

    public ref struct S3
    {
        private S1 field;

        public S1 this[S1 i]
        {
            get
            {
                return i;
            }
            set
            {
                field = i;
            }
        }

        public S1 Field2;
    }

    public ref struct S1
    {
        public static implicit operator S1(Span<int> o) => default;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (15,38): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { Field2 = {[inner] = outer} };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(15, 38),
                // (25,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(25, 16),
                // (33,37): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { Field2 = {[outer] = inner} };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "[outer] = inner").WithArguments("inner").WithLocation(33, 37),
                // (67,19): warning CS0649: Field 'Program.S3.Field2' is never assigned to, and will always have its default value 
                //         public S1 Field2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field2").WithArguments("Program.S3.Field2", "").WithLocation(67, 19)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeColInitializer(LanguageVersion languageVersion)
        {
            var text = @"
using System;
using System.Collections.Generic;

// X cannot be a ref-like type since it must implement IEnumerable
// that significantly reduces the number of scenarios that could be applicable to ref-like types
class X : List<int>
{
    void Add(Span<int> x, int y) { }

    static void Main()
    {
        Span<int> inner = stackalloc int[1];

        var z = new X { { inner, 12 } };
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
        }

        [Fact]
        public void RefLikeEscapeMixingDelegate()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        void Test1()
        {
            S1 rOuter = default;

            Span<int> inner = stackalloc int[2];
            S1 rInner = MayWrap(ref inner);

            // valid
            var dummy1 = new Program(ref rOuter, ref rOuter);

            // error
            var dummy2 = new Program(ref rOuter, ref rInner);

            // error
            var dummy3 = new Program(ref inner, ref rOuter);
        }

        Program(ref Span<int> arg1, ref S1 arg2)
        {
            arg2 = MayWrap(ref arg1);
        }

        Program(ref S1 arg1, ref S1 arg2)
        {
            arg1 = arg2;
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (20,54): error CS8526: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(20, 54),
                // (20,26): error CS8524: This combination of arguments to 'Program.Program(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref rOuter, ref rInner)").WithArguments("Program.Program(ref Program.S1, ref Program.S1)", "arg2").WithLocation(20, 26),
                // (23,42): error CS8526: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(23, 42),
                // (23,26): error CS8524: This combination of arguments to 'Program.Program(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref inner, ref rOuter)").WithArguments("Program.Program(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(23, 26)
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (20,54): error CS8352: Cannot use variable 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rInner").WithArguments("rInner").WithLocation(20, 54),
                // (20,26): error CS8350: This combination of arguments to 'Program.Program(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref rOuter, ref rInner)").WithArguments("Program.Program(ref Program.S1, ref Program.S1)", "arg2").WithLocation(20, 26),
                // (23,42): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(23, 42),
                // (23,26): error CS8350: This combination of arguments to 'Program.Program(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref inner, ref rOuter)").WithArguments("Program.Program(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(23, 26),
                // (28,32): error CS9077: Cannot return a parameter by reference 'arg1' through a ref parameter; it can only be returned in a return statement
                //             arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "arg1").WithArguments("arg1").WithLocation(28, 32),
                // (28,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             arg2 = MayWrap(ref arg1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref arg1)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(28, 20)
            );
        }

        [Fact]
        public void RefLikeEscapeMixingCallOptionalIn()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        void Test1()
        {
            S1 rOuter = default;

            Span<int> inner = stackalloc int[1];
            S1 rInner = MayWrap(inner);

            // valid, optional arg is of the same escape level
            MayAssign(ref rOuter);

            // valid, optional arg is of wider escape level
            MayAssign(ref rInner);
        }

        static void MayAssign(ref S1 arg1, in Span<int> arg2 = default)
        {
            arg1 = MayWrap(arg2);
        }

        static S1 MayWrap(in Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics();

            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (25,20): error CS8347: Cannot use a result of 'Program.MayWrap(in Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             arg1 = MayWrap(arg2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(arg2)").WithArguments("Program.MayWrap(in System.Span<int>)", "arg").WithLocation(25, 20),
                // (25,28): error CS9077: Cannot return a parameter by reference 'arg2' through a ref parameter; it can only be returned in a return statement
                //             arg1 = MayWrap(arg2);
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "arg2").WithArguments("arg2").WithLocation(25, 28));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void MismatchedRefTernaryEscape(LanguageVersion languageVersion)
        {
            var text = @"
class Program
{
    static void Main()
    {
    }

    public static int field = 1;

    bool flag = true;

    ref int Test1()
    {
        var local = 42;

        if (flag) 
            return ref true ? ref field : ref local;

        ref var lr = ref local;
        ref var fr = ref field;

        ref var ternary1 = ref true ? ref lr : ref fr;

        if (flag) 
            return ref ternary1;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (17,47): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref true ? ref field : ref local;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(17, 47),
                // (25,24): error CS8157: Cannot return 'ternary1' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref ternary1;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "ternary1").WithArguments("ternary1").WithLocation(25, 24),
                // (12,13): error CS0161: 'Program.Test1()': not all code paths return a value
                //     ref int Test1()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Test1").WithArguments("Program.Test1()").WithLocation(12, 13)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void MismatchedRefTernaryEscapeBlock(LanguageVersion languageVersion)
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
    }

    public static int field = 1;

    void Test1()
    {
        Span<int> outer = default;
        var sOuter = MayWrap(ref outer);

        {
            Span<int> inner = stackalloc int[1];
            var sInner = MayWrap(ref inner);

            ref var ternarySame1 = ref true ? ref sInner : ref sInner;
            ref var ternarySame2 = ref true ? ref sOuter : ref sOuter;

            // ok
            ternarySame2 = true ? sOuter : sOuter;

            // error
            ternarySame2 = true ? sOuter : sInner;

            // error
            ternarySame2 = true ? sInner : sOuter;

            // error, mixing val escapes
            ref var ternary1 = ref true ? ref sOuter : ref sInner;

            // error, mixing val escapes
            ref var ternary2 = ref true ? ref sInner : ref sOuter;

            // error, mixing val escapes
            ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;

            ref var ir = ref sInner[1];
            ref var or = ref sOuter[1];

            // no error, indexer cannot ref-return the instance, so ir and or are both safe to return
            ref var ternary4 = ref true ? ref ir : ref or;
        }
    }

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public ref int this[int i] => throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (27,44): error CS8526: Cannot use variable 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ternarySame2 = true ? sOuter : sInner;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(27, 44),
                // (30,35): error CS8526: Cannot use variable 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ternarySame2 = true ? sInner : sOuter;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(30, 35),
                // (33,60): error CS8526: Cannot use variable 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var ternary1 = ref true ? ref sOuter : ref sInner;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(33, 60),
                // (33,36): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var ternary1 = ref true ? ref sOuter : ref sInner;
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "true ? ref sOuter : ref sInner").WithLocation(33, 36),
                // (36,47): error CS8526: Cannot use variable 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var ternary2 = ref true ? ref sInner : ref sOuter;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(36, 47),
                // (36,36): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var ternary2 = ref true ? ref sInner : ref sOuter;
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "true ? ref sInner : ref sOuter").WithLocation(36, 36),
                // (39,47): error CS8526: Cannot use variable 'ternarySame1' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "ternarySame1").WithArguments("ternarySame1").WithLocation(39, 47),
                // (39,36): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "true ? ref ternarySame1 : ref ternarySame2").WithLocation(39, 36)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void MismatchedRefTernaryEscapeBlock_UnsafeContext(LanguageVersion languageVersion)
        {
            var text = @"
using System;
unsafe class Program
{
    static void Main()
    {
    }

    public static int field = 1;

    void Test1()
    {
        Span<int> outer = default;
        var sOuter = MayWrap(ref outer);

        {
            Span<int> inner = stackalloc int[1];
            var sInner = MayWrap(ref inner);

            ref var ternarySame1 = ref true ? ref sInner : ref sInner;
            ref var ternarySame2 = ref true ? ref sOuter : ref sOuter;

            // ok
            ternarySame2 = true ? sOuter : sOuter;

            // error
            ternarySame2 = true ? sOuter : sInner;

            // error
            ternarySame2 = true ? sInner : sOuter;

            // error, mixing val escapes
            ref var ternary1 = ref true ? ref sOuter : ref sInner;

            // error, mixing val escapes
            ref var ternary2 = ref true ? ref sInner : ref sOuter;

            // error, mixing val escapes
            ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;

            ref var ir = ref sInner[1];
            ref var or = ref sOuter[1];

            // no error, indexer cannot ref-return the instance, so ir and or are both safe to return
            ref var ternary4 = ref true ? ref ir : ref or;
        }
    }

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public ref int this[int i] => throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (27,44): warning CS9077: Use of variable 'sInner' in this context may expose referenced variables outside of their declaration scope
                //             ternarySame2 = true ? sOuter : sInner;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(27, 44),
                // (30,35): warning CS9077: Use of variable 'sInner' in this context may expose referenced variables outside of their declaration scope
                //             ternarySame2 = true ? sInner : sOuter;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(30, 35),
                // (33,60): warning CS9077: Use of variable 'sInner' in this context may expose referenced variables outside of their declaration scope
                //             ref var ternary1 = ref true ? ref sOuter : ref sInner;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(33, 60),
                // (33,36): warning CS9083: The branches of the ref conditional operator refer to variables with incompatible declaration scopes
                //             ref var ternary1 = ref true ? ref sOuter : ref sInner;
                Diagnostic(ErrorCode.WRN_MismatchedRefEscapeInTernary, "true ? ref sOuter : ref sInner").WithLocation(33, 36),
                // (36,47): warning CS9077: Use of variable 'sInner' in this context may expose referenced variables outside of their declaration scope
                //             ref var ternary2 = ref true ? ref sInner : ref sOuter;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "sInner").WithArguments("sInner").WithLocation(36, 47),
                // (36,36): warning CS9083: The branches of the ref conditional operator refer to variables with incompatible declaration scopes
                //             ref var ternary2 = ref true ? ref sInner : ref sOuter;
                Diagnostic(ErrorCode.WRN_MismatchedRefEscapeInTernary, "true ? ref sInner : ref sOuter").WithLocation(36, 36),
                // (39,47): warning CS9077: Use of variable 'ternarySame1' in this context may expose referenced variables outside of their declaration scope
                //             ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "ternarySame1").WithArguments("ternarySame1").WithLocation(39, 47),
                // (39,36): warning CS9083: The branches of the ref conditional operator refer to variables with incompatible declaration scopes
                //             ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;
                Diagnostic(ErrorCode.WRN_MismatchedRefEscapeInTernary, "true ? ref ternarySame1 : ref ternarySame2").WithLocation(39, 36)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void StackallocEscape(LanguageVersion languageVersion)
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {

        }

        Span<int> Test0()
        {
            // valid, baseline
            return default(Span<int>);
        }

        Span<int> Test3()
        {
            Span<int> local = stackalloc int[10];
            return true? local : default(Span<int>);
        }
      
        Span<int> Test4(Span<int> arg)
        {
            arg = stackalloc int[10];
            return arg;
        }

        Span<int> Test6()
        {
            Span<int> local = default;
            local = stackalloc int[10];
            return local;
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (19,26): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope 
                //             return true? local : default(Span<int>);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(19, 26),
                // (24,19): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //             arg = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[10]").WithArguments("System.Span<int>").WithLocation(24, 19),
                // (31,21): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //             local = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[10]").WithArguments("System.Span<int>").WithLocation(31, 21)
                );
        }

        [WorkItem(21831, "https://github.com/dotnet/roslyn/issues/21831")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void LocalWithNoInitializerEscape(LanguageVersion languageVersion)
        {
            var text = @"using System;

    class Program
    {
        static void Main()
        {
            // uninitialized
            S1 sp;

            // ok
            sp = default;
            
            Span<int> local = stackalloc int[1];

            // error
            sp = MayWrap(ref local);
        }

        static S1 SefReferringTest()
        {
            S1 sp1 = sp1;
            return sp1;
        }

        static S1 MayWrap(ref Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
            public ref int this[int i] => throw null;
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (16,30): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             sp = MayWrap(ref local);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(16, 30),
                // (16,18): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             sp = MayWrap(ref local);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref local)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(16, 18),
                // (22,20): error CS8352: Cannot use variable 'sp1' in this context because it may expose referenced variables outside of their declaration scope
                //             return sp1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "sp1").WithArguments("sp1").WithLocation(22, 20)
                );
        }

        [WorkItem(21858, "https://github.com/dotnet/roslyn/issues/21858")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FieldOfRefLikeEscape(LanguageVersion languageVersion)
        {
            var text = @"
    class Program
    {
        static void Main()
        {
        }

        ref struct S1
        {
            private S2 x;

            public S1(S2 arg) => x = arg;

            public S2 M1()
            {
                // ok
                return x;
            }

            public S2 M2()
            {
                var toReturn = x;

                // ok
                return toReturn;
            }

            public ref S2 M3()
            {
                // not ok
                return ref x;
            }
        }

        ref struct S2{}

    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (31,28): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //                 return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "x").WithLocation(31, 28)
                );
        }

        [WorkItem(21880, "https://github.com/dotnet/roslyn/issues/21880")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void MemberOfReadonlyRefLikeEscape(LanguageVersion languageVersion)
        {
            var text = @"
    using System;
    using System.Diagnostics.CodeAnalysis;
    public static class Program
    {
        public static void Main()
        {
            // OK, SR is readonly
            Span<int> value1 = stackalloc int[1];
            new SR().TryGet(out value1);

            // Ok, the new value can be copied into SW but not the 
            // ref to the value
            new SW().TryGet(out value1);

            // Error as the ref of this can escape into value2
            Span<int> value2 = default;
            new SW().TryGet2(out value2);
        }
    }

    public readonly ref struct SR
    {
        public void TryGet(out Span<int> result)
        {
            result = default;
        }
    }

    public ref struct SW
    {
        public void TryGet(out Span<int> result)
        {
            result = default;
        }

        [UnscopedRef]
        public void TryGet2(out Span<int> result)
        {
            result = default;
        }
    }
";
            if (languageVersion == LanguageVersion.CSharp10)
            {
                CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                    // (14,13): error CS8350: This combination of arguments to 'SW.TryGet(out Span<int>)' is disallowed because it may expose variables referenced by parameter 'result' outside of their declaration scope
                    //             new SW().TryGet(out value1);
                    Diagnostic(ErrorCode.ERR_CallArgMixing, "new SW().TryGet(out value1)").WithArguments("SW.TryGet(out System.Span<int>)", "result").WithLocation(14, 13),
                    // (14,33): error CS8352: Cannot use variable 'value1' in this context because it may expose referenced variables outside of their declaration scope
                    //             new SW().TryGet(out value1);
                    Diagnostic(ErrorCode.ERR_EscapeVariable, "value1").WithArguments("value1").WithLocation(14, 33)
                    );

            }
            else
            {
                CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                    // (18,13): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                    //             new SW().TryGet2(out value2);
                    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "new SW()").WithLocation(18, 13),
                    // (18,13): error CS8350: This combination of arguments to 'SW.TryGet2(out Span<int>)' is disallowed because it may expose variables referenced by parameter 'this' outside of their declaration scope
                    //             new SW().TryGet2(out value2);
                    Diagnostic(ErrorCode.ERR_CallArgMixing, "new SW().TryGet2(out value2)").WithArguments("SW.TryGet2(out System.Span<int>)", "this").WithLocation(18, 13)
                    );
            }
        }

        [WorkItem(21911, "https://github.com/dotnet/roslyn/issues/21911")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void MemberOfReadonlyRefLikeEscapeSpans(LanguageVersion languageVersion)
        {
            var text = @"
    using System;

    public static class Program
    {
        public static void Main()
        {
            Span<int> stackAllocated = stackalloc int[100];

            // OK, Span is a readonly struct
            new Span<int>().CopyTo(stackAllocated);

            // OK, ReadOnlySpan is a readonly struct
            new ReadOnlySpan<int>().CopyTo(stackAllocated);

            // not OK, Span is writeable
            new NotReadOnly<int>().CopyTo(stackAllocated);
        }
    }

    public ref struct NotReadOnly<T>
    {
        private Span<T> wrapped;

        public void CopyTo(Span<T> other)
        {
            wrapped = other;
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (17,43): error CS8352: Cannot use variable 'stackAllocated' in this context because it may expose referenced variables outside of their declaration scope
                //             new NotReadOnly<int>().CopyTo(stackAllocated);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "stackAllocated").WithArguments("stackAllocated").WithLocation(17, 43),
                // (17,13): error CS8350: This combination of arguments to 'NotReadOnly<int>.CopyTo(Span<int>)' is disallowed because it may expose variables referenced by parameter 'other' outside of their declaration scope
                //             new NotReadOnly<int>().CopyTo(stackAllocated);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new NotReadOnly<int>().CopyTo(stackAllocated)").WithArguments("NotReadOnly<int>.CopyTo(System.Span<int>)", "other").WithLocation(17, 13)
                );
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyRefStruct_Method_RefLikeStructParameter(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;

public readonly ref struct S<T>
{
    public void M(Span<T> x) { }

    public unsafe static void N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        b.M(x);
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics();
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyMethod_RefLikeStructParameter(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;

public ref struct S<T>
{
    public readonly void M(Span<T> x) { }

    public unsafe static void N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        b.M(x);
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics();
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyRefStruct_RefLikeProperty(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;

public readonly ref struct S<T>
{
    public Span<T> P { get => default; set {} }

    public unsafe static void N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        b.P = x;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (11,15): warning CS9080: Use of variable 'x' in this context may expose referenced variables outside of their declaration scope
                //         b.P = x;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "x").WithArguments("x").WithLocation(11, 15));
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("S<T>.N", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<byte> V_0, //x
                System.Span<byte> V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.5
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  ldc.i4.5
  IL_0006:  newobj     ""System.Span<byte>..ctor(void*, int)""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  stloc.0
  IL_000e:  ldarga.s   V_0
  IL_0010:  ldloc.0
  IL_0011:  call       ""void S<byte>.P.set""
  IL_0016:  nop
  IL_0017:  ret
}
");
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyRefLikeProperty_01(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;

public ref struct S<T>
{
    public readonly Span<T> P { get => default; set {} }

    public unsafe static void N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        b.P = x;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (11,15): warning CS9080: Use of variable 'x' in this context may expose referenced variables outside of their declaration scope
                //         b.P = x;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "x").WithArguments("x").WithLocation(11, 15));
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("S<T>.N", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<byte> V_0, //x
                System.Span<byte> V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.5
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  ldc.i4.5
  IL_0006:  newobj     ""System.Span<byte>..ctor(void*, int)""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  stloc.0
  IL_000e:  ldarga.s   V_0
  IL_0010:  ldloc.0
  IL_0011:  call       ""readonly void S<byte>.P.set""
  IL_0016:  nop
  IL_0017:  ret
}
");
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyRefLikeProperty_02(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;

public ref struct S<T>
{
    public Span<T> P { get => default; readonly set {} }

    public unsafe static void N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        b.P = x;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (11,15): warning CS9080: Use of variable 'x' in this context may expose referenced variables outside of their declaration scope
                //         b.P = x;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "x").WithArguments("x").WithLocation(11, 15));
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("S<T>.N", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<byte> V_0, //x
                System.Span<byte> V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.5
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  ldc.i4.5
  IL_0006:  newobj     ""System.Span<byte>..ctor(void*, int)""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  stloc.0
  IL_000e:  ldarga.s   V_0
  IL_0010:  ldloc.0
  IL_0011:  call       ""readonly void S<byte>.P.set""
  IL_0016:  nop
  IL_0017:  ret
}
");
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyIndexer_RefLikeStructParameter_01(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;

public ref struct S<T>
{
    public readonly Span<T> this[Span<T> span] { get => default; set {} }

    public unsafe static Span<byte> N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        _ = b[x];
        b[x] = x;
        return b[x];
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (13,18): warning CS9080: Use of variable 'x' in this context may expose referenced variables outside of their declaration scope
                //         return b[x];
                Diagnostic(ErrorCode.WRN_EscapeVariable, "x").WithArguments("x").WithLocation(13, 18));
        }

        [WorkItem(35146, "https://github.com/dotnet/roslyn/issues/35146")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyIndexer_RefLikeStructParameter_02(LanguageVersion languageVersion)
        {
            var csharp = @"
using System;
public ref struct S<T>
{
    public Span<T> this[Span<T> span] { get => default; readonly set {} }

    public unsafe static void N(S<byte> b)
    {
        Span<byte> x = stackalloc byte[5];
        _ = b[x];
        b[x] = x;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(csharp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (10,15): warning CS9080: Use of variable 'x' in this context may expose referenced variables outside of their declaration scope
                //         _ = b[x];
                Diagnostic(ErrorCode.WRN_EscapeVariable, "x").WithArguments("x").WithLocation(10, 15),
                // (11,11): warning CS9080: Use of variable 'x' in this context may expose referenced variables outside of their declaration scope
                //         b[x] = x;
                Diagnostic(ErrorCode.WRN_EscapeVariable, "x").WithArguments("x").WithLocation(11, 11));
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("S<T>.N", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (System.Span<byte> V_0, //x
                System.Span<byte> V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.5
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  ldc.i4.5
  IL_0006:  newobj     ""System.Span<byte>..ctor(void*, int)""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  stloc.0
  IL_000e:  ldarga.s   V_0
  IL_0010:  ldloc.0
  IL_0011:  call       ""System.Span<byte> S<byte>.this[System.Span<byte>].get""
  IL_0016:  pop
  IL_0017:  ldarga.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  ldloc.0
  IL_001b:  call       ""readonly void S<byte>.this[System.Span<byte>].set""
  IL_0020:  nop
  IL_0021:  ret
}
");
        }

        [WorkItem(22197, "https://github.com/dotnet/roslyn/issues/22197")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefTernaryMustMatchValEscapes(LanguageVersion languageVersion)
        {
            var text = @"
    using System;

    public class C
    {
        bool flag1 = true;
        bool flag2 = false;

        public void M(ref Span<int> global)
        {
            Span<int> local = stackalloc int[10];

            ref var r1 = ref (flag1 ? ref global : ref local);
            ref var r2 = ref (flag2 ? ref global : ref local);

	        // same as         global = local;   which would be an error.
            // but we can’t fail here, since r1 and r2 are basically the same, 
            // so should fail when making r1 and r2 above.
            r1 = r2;   
        }

        public static void Main()
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (13,56): error CS8526: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var r1 = ref (flag1 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(13, 56),
                // (13,31): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var r1 = ref (flag1 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "flag1 ? ref global : ref local").WithLocation(13, 31),
                // (14,56): error CS8526: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var r2 = ref (flag2 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(14, 56),
                // (14,31): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var r2 = ref (flag2 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "flag2 ? ref global : ref local").WithLocation(14, 31)
            );
        }

        [WorkItem(22197, "https://github.com/dotnet/roslyn/issues/22197")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefTernaryMustMatchValEscapes1(LanguageVersion languageVersion)
        {
            var text = @"
    using System;

    public class C
    {
        public void M(ref Span<int> global)
        {
            Span<int> local = stackalloc int[0];

            // ok
            (true ? ref local : ref local) = (false ? ref global : ref global);

            // also OK
            (true ? ref local : ref local) = (false ? global : local);
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics();

            var compiled = CompileAndVerify(comp, verify: Verification.Passes);
            compiled.VerifyIL("C.M(ref System.Span<int>)", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldobj      ""System.Span<int>""
  IL_0006:  pop
  IL_0007:  ret
}
");
        }

        [WorkItem(65522, "https://github.com/dotnet/roslyn/issues/65522")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentToGlobal(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        (global, global) = global;
        (global, global) = local; // error 1
        (global, local) = local; // error 2
        (local, local) = local;
        (global, _) = local; // error 3
        (local, _) = local;
        (global, _) = global;
    }
    public static void Main()
    {
    }
}
public static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y)
    {
        throw null;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (10,9): error CS8352: Cannot use variable '(global, global) = local' in this context because it may expose referenced variables outside of their declaration scope
                //         (global, global) = local; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(global, global) = local").WithArguments("(global, global) = local").WithLocation(10, 9),
                // (10,28): error CS8350: This combination of arguments to 'Extensions.Deconstruct(Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                //         (global, global) = local; // error 1
                Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(10, 28),
                // (11,9): error CS8352: Cannot use variable '(global, local) = local' in this context because it may expose referenced variables outside of their declaration scope
                //         (global, local) = local; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(global, local) = local").WithArguments("(global, local) = local").WithLocation(11, 9),
                // (11,27): error CS8350: This combination of arguments to 'Extensions.Deconstruct(Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                //         (global, local) = local; // error 2
                Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(11, 27),
                // (13,9): error CS8352: Cannot use variable '(global, _) = local' in this context because it may expose referenced variables outside of their declaration scope
                //         (global, _) = local; // error 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(global, _) = local").WithArguments("(global, _) = local").WithLocation(13, 9),
                // (13,23): error CS8350: This combination of arguments to 'Extensions.Deconstruct(Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                //         (global, _) = local; // error 3
                Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(13, 23));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentToRefMethods(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        (M(), M()) = local; // error
    }
    public static void Main() => throw null;
    public ref Span<int> M() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (9,9): error CS8352: Cannot use variable '(M(), M()) = local' in this context because it may expose referenced variables outside of their declaration scope
                //         (M(), M()) = local; // error
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(M(), M()) = local").WithArguments("(M(), M()) = local").WithLocation(9, 9),
                // (9,22): error CS8350: This combination of arguments to 'Extensions.Deconstruct(Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                //         (M(), M()) = local; // error
                Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(9, 22));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentWithRefExtension(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        (global, global) = global;
    }
    public static void Main() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(ref this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (8,9): error CS1510: A ref or out value must be an assignable variable
                //         (global, global) = global;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(global, global) = global").WithLocation(8, 9)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentWithRefExtension_UnsafeContext(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public unsafe class C
{
    public void M(ref Span<int> global)
    {
        (global, global) = global;
    }
    public static void Main() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(ref this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (8,9): error CS1510: A ref or out value must be an assignable variable
                //         (global, global) = global;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(global, global) = global").WithLocation(8, 9)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentWithRefReadonlyExtension(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        (global, global) = global;
        (global, global) = local; // error
    }
    public static void Main() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(in this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp10)
            {
                comp.VerifyDiagnostics(
                    // (10,9): error CS8352: Cannot use variable '(global, global) = local' in this context because it may expose referenced variables outside of their declaration scope
                    //         (global, global) = local; // error
                    Diagnostic(ErrorCode.ERR_EscapeVariable, "(global, global) = local").WithArguments("(global, global) = local").WithLocation(10, 9),
                    // (10,28): error CS8350: This combination of arguments to 'Extensions.Deconstruct(in Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                    //         (global, global) = local; // error
                    Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(in System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(10, 28));
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (9,9): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                    //         (global, global) = global;
                    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "(global, global) = global").WithLocation(9, 9),
                    // (9,28): error CS8350: This combination of arguments to 'Extensions.Deconstruct(in Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                    //         (global, global) = global;
                    Diagnostic(ErrorCode.ERR_CallArgMixing, "global").WithArguments("Extensions.Deconstruct(in System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(9, 28),
                    // (10,9): error CS8352: Cannot use variable '(global, global) = local' in this context because it may expose referenced variables outside of their declaration scope
                    //         (global, global) = local; // error
                    Diagnostic(ErrorCode.ERR_EscapeVariable, "(global, global) = local").WithArguments("(global, global) = local").WithLocation(10, 9),
                    // (10,28): error CS8350: This combination of arguments to 'Extensions.Deconstruct(in Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                    //         (global, global) = local; // error
                    Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(in System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(10, 28));
            }
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentWithRefReadonlyExtension_02(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    static void M()
    {
        Span<int> local = stackalloc int[10];
        Span<int> x1, y1;
        (x1, y1) = local; // 1
        var (x2, y2) = local;
        (var x3, var y3) = local;
        (Span<int> x4, Span<int> y4) = local;
    }
}
public static class Extensions
{
    public static void Deconstruct(in this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,9): error CS8352: Cannot use variable '(x1, y1) = local' in this context because it may expose referenced variables outside of their declaration scope
                //         (x1, y1) = local; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(x1, y1) = local").WithArguments("(x1, y1) = local").WithLocation(10, 9),
                // (10,20): error CS8350: This combination of arguments to 'Extensions.Deconstruct(in Span<int>, out Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                //         (x1, y1) = local; // 1
                Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(in System.Span<int>, out System.Span<int>, out System.Span<int>)", "self").WithLocation(10, 20));
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentWithRefReadonlyExtension_03(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    static void M()
    {
        ReadOnlySpan<int> local = stackalloc int[10];
        ReadOnlySpan<int> x1, y1;
        (x1, y1) = local; // 1
        var (x2, y2) = local;
        (var x3, var y3) = local;
        (ReadOnlySpan<int> x4, ReadOnlySpan<int> y4) = local;
    }
}
public static class Extensions
{
    public static void Deconstruct(in this ReadOnlySpan<int> self, out ReadOnlySpan<int> x, out ReadOnlySpan<int> y) => throw null;
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,9): error CS8352: Cannot use variable '(x1, y1) = local' in this context because it may expose referenced variables outside of their declaration scope
                //         (x1, y1) = local; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "(x1, y1) = local").WithArguments("(x1, y1) = local").WithLocation(10, 9),
                // (10,20): error CS8350: This combination of arguments to 'Extensions.Deconstruct(in ReadOnlySpan<int>, out ReadOnlySpan<int>, out ReadOnlySpan<int>)' is disallowed because it may expose variables referenced by parameter 'self' outside of their declaration scope
                //         (x1, y1) = local; // 1
                Diagnostic(ErrorCode.ERR_CallArgMixing, "local").WithArguments("Extensions.Deconstruct(in System.ReadOnlySpan<int>, out System.ReadOnlySpan<int>, out System.ReadOnlySpan<int>)", "self").WithLocation(10, 20));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentWithReturnValue(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        var t = ((global, global) = global); // error
    }
    public static void Main() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (8,19): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var t = ((global, global) = global); // error
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(8, 19),
                // (8,27): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var t = ((global, global) = global); // error
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(8, 27)
            );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentOfTuple(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        string s;
        C c;

        (global, global) = (local, local); // error 1

        (global, s) = (local, """"); // error 2
        (global, s) = (local, null); // error 3

        (local, s) = (global, """"); // error 4
        (local, s) = (global, null); // error 5

        (c, s) = (local, """"); // error 6
        (c, s) = (local, null);
    }
    public static void Main() => throw null;
    public static implicit operator C(Span<int> s) => throw null;
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            compilation.VerifyDiagnostics(
                // (12,29): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(12, 29),
                // (12,36): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(12, 36),
                // (14,24): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, s) = (local, ""); // error 2
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(14, 24),
                // (15,24): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, s) = (local, null); // error 3
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(15, 24),
                // (17,23): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (local, s) = (global, ""); // error 4
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(17, 23),
                // (18,23): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (local, s) = (global, null); // error 5
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(18, 23),
                // (20,19): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (c, s) = (local, ""); // error 6
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(20, 19)
            );

            // Check the Type and ConvertedType of tuples on the right-hand-side
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(3);
            Assert.Equal(@"(local, """")", tuple2.ToString());
            Assert.Equal(@"(global, s) = (local, """")", tuple2.Parent.ToString());
            Assert.Equal("(System.Span<int> local, string)", model.GetTypeInfo(tuple2).Type.ToString());
            Assert.Equal("(System.Span<int>, string)", model.GetTypeInfo(tuple2).ConvertedType.ToString());

            var tuple3 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(5);
            Assert.Equal(@"(local, null)", tuple3.ToString());
            Assert.Equal(@"(global, s) = (local, null)", tuple3.Parent.ToString());
            Assert.Null(model.GetTypeInfo(tuple3).Type);
            Assert.Equal("(System.Span<int>, string)", model.GetTypeInfo(tuple3).ConvertedType.ToString());

            var tuple6 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(11);
            Assert.Equal(@"(local, """")", tuple6.ToString());
            Assert.Equal(@"(c, s) = (local, """")", tuple6.Parent.ToString());
            Assert.Equal("(System.Span<int> local, string)", model.GetTypeInfo(tuple6).Type.ToString());
            Assert.Equal("(C, string)", model.GetTypeInfo(tuple6).ConvertedType.ToString());

            var tuple7 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(13);
            Assert.Equal("(local, null)", tuple7.ToString());
            Assert.Equal("(c, s) = (local, null)", tuple7.Parent.ToString());
            Assert.Null(model.GetTypeInfo(tuple7).Type);
            Assert.Equal("(C, string)", model.GetTypeInfo(tuple7).ConvertedType.ToString());
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentOfTuple_WithoutValueTuple(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        string s;
        C c;

        (global, global) = (local, local); // error 1

        (global, s) = (local, """"); // error 2
        (global, s) = (local, null); // error 3

        (local, s) = (global, """"); // error 4
        (local, s) = (global, null); // error 5

        (c, s) = (local, """"); // error 6
        (c, s) = (local, null); // error 7
    }
    public static void Main() => throw null;
    public static implicit operator C(Span<int> s) => throw null;
}
";
            var compilation = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            compilation.VerifyDiagnostics(
                // (12,28): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(local, local)").WithArguments("System.ValueTuple`2").WithLocation(12, 28),
                // (12,29): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(12, 29),
                // (12,36): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(12, 36),
                // (14,23): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (global, s) = (local, ""); // error 2
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, @"(local, """")").WithArguments("System.ValueTuple`2").WithLocation(14, 23),
                // (14,24): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, s) = (local, ""); // error 2
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(14, 24),
                // (15,23): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (global, s) = (local, null); // error 3
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(local, null)").WithArguments("System.ValueTuple`2").WithLocation(15, 23),
                // (17,22): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (local, s) = (global, ""); // error 4
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, @"(global, """")").WithArguments("System.ValueTuple`2").WithLocation(17, 22),
                // (17,23): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (local, s) = (global, ""); // error 4
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(17, 23),
                // (18,22): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (local, s) = (global, null); // error 5
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(global, null)").WithArguments("System.ValueTuple`2").WithLocation(18, 22),
                // (20,18): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (c, s) = (local, ""); // error 6
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, @"(local, """")").WithArguments("System.ValueTuple`2").WithLocation(20, 18),
                // (20,19): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (c, s) = (local, ""); // error 6
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(20, 19),
                // (21,18): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (c, s) = (local, null); // error 7
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(local, null)").WithArguments("System.ValueTuple`2").WithLocation(21, 18)
            );

            // Check the Type and ConvertedType of tuples on the right-hand-side
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var tuple2 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(3);
            Assert.Equal(@"(local, """")", tuple2.ToString());
            Assert.Equal(@"(global, s) = (local, """")", tuple2.Parent.ToString());
            Assert.Equal("(System.Span<int> local, string)", model.GetTypeInfo(tuple2).Type.ToString());
            Assert.Equal("(System.Span<int>, string)", model.GetTypeInfo(tuple2).ConvertedType.ToString());

            var tuple3 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(5);
            Assert.Equal(@"(local, null)", tuple3.ToString());
            Assert.Equal(@"(global, s) = (local, null)", tuple3.Parent.ToString());
            Assert.Null(model.GetTypeInfo(tuple3).Type);
            Assert.Equal("(System.Span<int>, string)", model.GetTypeInfo(tuple3).ConvertedType.ToString());

            var tuple6 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(11);
            Assert.Equal(@"(local, """")", tuple6.ToString());
            Assert.Equal(@"(c, s) = (local, """")", tuple6.Parent.ToString());
            Assert.Equal("(System.Span<int> local, string)", model.GetTypeInfo(tuple6).Type.ToString());
            Assert.Equal("(C, string)", model.GetTypeInfo(tuple6).ConvertedType.ToString());

            var tuple7 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ElementAt(13);
            Assert.Equal("(local, null)", tuple7.ToString());
            Assert.Equal("(c, s) = (local, null)", tuple7.Parent.ToString());
            Assert.Null(model.GetTypeInfo(tuple7).Type);
            Assert.Equal("(C, string)", model.GetTypeInfo(tuple7).ConvertedType.ToString());
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentOfRefLikeTuple(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        String s;
        C c;

        (global, global) = (local, local); // error 1

        (global, s) = (local, """"); // error 2
        (global, s) = (local, null); // error 3

        (local, s) = (global, """"); // error 4
        (local, s) = (global, null); // error 5

        (c, s) = (local, """"); // error 6
        (c, s) = (local, null);
    }
    public static void Main() => throw null;
    public static implicit operator C(Span<int> s) => throw null;
}
namespace System
{
    // Note: there is no way to make a ValueTuple type that will hold Spans and still be recognized as well-known type for tuples
    public ref struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            compilation.VerifyDiagnostics(
                // (12,29): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(12, 29),
                // (12,36): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, global) = (local, local); // error 1
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(12, 36),
                // (14,24): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, s) = (local, ""); // error 2
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(14, 24),
                // (15,24): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (global, s) = (local, null); // error 3
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(15, 24),
                // (17,23): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (local, s) = (global, ""); // error 4
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(17, 23),
                // (18,23): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (local, s) = (global, null); // error 5
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "global").WithArguments("System.Span<int>").WithLocation(18, 23),
                // (20,19): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         (c, s) = (local, ""); // error 6
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "local").WithArguments("System.Span<int>").WithLocation(20, 19));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionAssignmentToOuter(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M()
    {
        Span<int> outer = stackalloc int[10];

        {
            Span<int> local = stackalloc int[10]; // both stackallocs have the same escape scope
            (outer, outer) = outer;
            (outer, outer) = local;
            (outer, local) = local;
            (local, local) = local;
        }
    }
    public static void Main() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";

            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void DeconstructionDeclaration(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref Span<int> global)
    {
        Span<int> local = stackalloc int[10];
        var (local1, local2) = local;
        global = local1; // error 1
        global = local2; // error 2

        var (local3, local4) = global;
        global = local3;
        global = local4;
    }
    public static void Main() => throw null;
}
public static class Extensions
{
    public static void Deconstruct(this Span<int> self, out Span<int> x, out Span<int> y) => throw null;
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (10,18): error CS8352: Cannot use variable 'local1' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local1; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local1").WithArguments("local1").WithLocation(10, 18),
                // (11,18): error CS8352: Cannot use variable 'local2' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local2; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local2").WithArguments("local2").WithLocation(11, 18));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeForeach(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref S global)
    {
        S localCollection = stackalloc int[10];
        foreach (var local in localCollection)
        {
            global = local; // error
        }
    }
    public static void Main() => throw null;
}

public ref struct S
{
    public S GetEnumerator() => throw null;
    public bool MoveNext() => throw null;
    public S Current => throw null;
    public static implicit operator S(Span<int> s) => throw null;
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (11,22): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             global = local; // error
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(11, 22)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeDeconstructionForeach(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref S global)
    {
        S localCollection = stackalloc int[10];
        foreach (var (local1, local2) in localCollection)
        {
            global = local1; // error 1
            global = local2; // error 2
        }
    }
    public static void Main() => throw null;
}

public ref struct S
{
    public S GetEnumerator() => throw null;
    public bool MoveNext() => throw null;
    public S Current => throw null;
    public static implicit operator S(Span<int> s) => throw null;
    public void Deconstruct(out S s1, out S s2) => throw null;
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (11,22): error CS8352: Cannot use variable 'local1' in this context because it may expose referenced variables outside of their declaration scope
                //             global = local1; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local1").WithArguments("local1").WithLocation(11, 22),
                // (12,22): error CS8352: Cannot use variable 'local2' in this context because it may expose referenced variables outside of their declaration scope
                //             global = local2; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local2").WithArguments("local2").WithLocation(12, 22));
        }

        [Theory]
        [CombinatorialData]
        public void RefLikeDeconstruction(
            [CombinatorialValues(LanguageVersion.CSharp10, LanguageVersion.CSharp11)] LanguageVersion languageVersion,
            bool useReadOnly)
        {
            string refModifier = useReadOnly ? "readonly" : "";
            var text = $@"
using System;

public class C
{{
    public void M(ref S global)
    {{
        S localCollection = stackalloc int[10];
        var (local1, local2) = localCollection;
        global = local1; // error 1
        global = local2; // error 2
    }}
}}

public {refModifier} ref struct S
{{
    public static implicit operator S(Span<int> s) => throw null;
    public void Deconstruct(out S s1, out S s2) => throw null;
}}
";
            var comp = CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (10,18): error CS8352: Cannot use variable 'local1' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local1; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local1").WithArguments("local1").WithLocation(10, 18),
                // (11,18): error CS8352: Cannot use variable 'local2' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local2; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local2").WithArguments("local2").WithLocation(11, 18));
        }

        [WorkItem(22361, "https://github.com/dotnet/roslyn/issues/22361")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeOutVarFromLocal(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref S global)
    {
        S local1 = stackalloc int[10];
        local1.M(out S local2);
        local1 = local2; // ok
        global = local2; // error
    }
    public static void Main() => throw null;
}

public ref struct S
{
    public static implicit operator S(Span<int> s) => throw null;
    public void M(out S s) => throw null;
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (11,18): error CS8352: Cannot use variable 'local2' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local2; // error
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local2").WithArguments("local2").WithLocation(11, 18));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefLikeOutVarFromGlobal(LanguageVersion languageVersion)
        {
            var text = @"
using System;

public class C
{
    public void M(ref S global)
    {
        global.M(out S local2);
        global = local2;
    }
    public static void Main() => throw null;
}

public ref struct S
{
    public static implicit operator S(Span<int> s) => throw null;
    public void M(out S s) => throw null;
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
        }

        [WorkItem(22456, "https://github.com/dotnet/roslyn/issues/22456")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void InMatchesIn(LanguageVersion languageVersion)
        {
            var text = @"
public class C
{
    public static void Main() => throw null;

    static ref readonly int F1(in int x)
    {
        return ref x; 
    }

    static ref readonly int Test1(in int x)
    {
        return ref F1(in x);
    }

    static ref readonly int Test2(in int x)
    {
        ref readonly var t = ref F1(in x);
        return ref t;
    }

    static ref readonly int Test3()
    {
        return ref F1(in (new int[1])[0]);
    }

    static ref readonly int Test4()
    {
        ref readonly var t = ref F1(in (new int[1])[0]);
        return ref t;
    }
}

";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
        }

        [WorkItem(24776, "https://github.com/dotnet/roslyn/issues/24776")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void PointerElementAccess_RefStructPointer(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
public ref struct TestStruct
{
    public void M() { }
}
public class C
{
    public static unsafe void Test(TestStruct[] ar)
    {
        fixed (TestStruct* p = ar)
        {
            for (int i = 0; i < ar.Length; i++)
            {
                p[i].M();
            }
        }
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,36): error CS0611: Array elements cannot be of type 'TestStruct'
                //     public static unsafe void Test(TestStruct[] ar)
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "TestStruct").WithArguments("TestStruct").WithLocation(8, 36));
        }

        [WorkItem(24776, "https://github.com/dotnet/roslyn/issues/24776")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void PointerIndirectionOperator_RefStructPointer(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
public ref struct TestStruct
{
    public void M() { }
}
public class C
{
    public static unsafe void Test(TestStruct[] ar)
    {
        fixed (TestStruct* p = ar)
        {
            var x = *p;
        }
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,36): error CS0611: Array elements cannot be of type 'TestStruct'
                //     public static unsafe void Test(TestStruct[] ar)
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "TestStruct").WithArguments("TestStruct").WithLocation(8, 36));
        }

        [WorkItem(25398, "https://github.com/dotnet/roslyn/issues/25398")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void AwaitRefStruct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
using System.Threading.Tasks;

ref struct S { }

class C
{
    async Task M(Task<S> t)
    {
        _ = await t;

        var a = await t;

        var r = t.Result;
        M(await t, ref r);
    }

    void M(S t, ref S t1)
    {
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (8,26): error CS0306: The type 'S' may not be used as a type argument
                //     async Task M(Task<S> t)
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "t").WithArguments("S").WithLocation(8, 26),
                // (12,9): error CS4012: Parameters or locals of type 'S' cannot be declared in async methods or async lambda expressions.
                //         var a = await t;
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "var").WithArguments("S").WithLocation(12, 9),
                // (14,9): error CS4012: Parameters or locals of type 'S' cannot be declared in async methods or async lambda expressions.
                //         var r = t.Result;
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "var").WithArguments("S").WithLocation(14, 9),
                // (15,9): error CS8350: This combination of arguments to 'C.M(S, ref S)' is disallowed because it may expose variables referenced by parameter 't' outside of their declaration scope
                //         M(await t, ref r);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M(await t, ref r)").WithArguments("C.M(S, ref S)", "t").WithLocation(15, 9)
                );
        }

        [WorkItem(25398, "https://github.com/dotnet/roslyn/issues/25398")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void CoalesceRefStruct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
ref struct S { }

class C
{
    void M()
    {       
        _ = (S?)null ?? default;

        var a = (S?)null ?? default;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (8,14): error CS0306: The type 'S' may not be used as a type argument
                //         _ = (S?)null ?? default;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "S?").WithArguments("S").WithLocation(8, 14),
                // (10,18): error CS0306: The type 'S' may not be used as a type argument
                //         var a = (S?)null ?? default;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "S?").WithArguments("S").WithLocation(10, 18)
                );
        }

        [WorkItem(25398, "https://github.com/dotnet/roslyn/issues/25398")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ArrayAccessRefStruct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
ref struct S { }

class C
{
    void M()
    {       
        _ = ((S[])null)[0];

        var a = ((S[])null)[0];
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (8,15): error CS0611: Array elements cannot be of type 'S'
                //         _ = ((S[])null)[0];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "S").WithArguments("S").WithLocation(8, 15),
                // (10,19): error CS0611: Array elements cannot be of type 'S'
                //         var a = ((S[])null)[0];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "S").WithArguments("S").WithLocation(10, 19)
                );
        }

        [WorkItem(25398, "https://github.com/dotnet/roslyn/issues/25398")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ConditionalRefStruct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
ref struct S { }

class C
{
    void M()
    {       
        _ = ((C)null)?.Test();

        var a = ((C)null)?.Test();
    }
    
    S Test() => default;        
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (8,23): error CS8977: 'S' cannot be made nullable.
                //         _ = ((C)null)?.Test();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".Test()").WithArguments("S").WithLocation(8, 23),
                // (10,27): error CS8977: 'S' cannot be made nullable.
                //         var a = ((C)null)?.Test();
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, ".Test()").WithArguments("S").WithLocation(10, 27)
                );
        }

        [WorkItem(25485, "https://github.com/dotnet/roslyn/issues/25485")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ArrayAccess_CrashesEscapeRules(LanguageVersion languageVersion)
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
public class Class1
{
    public void Foo(Span<Thing>[] first, Thing[] second)
    {
        var x = first[0];
    }
}
public struct Thing
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (5,21): error CS0611: Array elements cannot be of type 'Span<Thing>'
                //     public void Foo(Span<Thing>[] first, Thing[] second)
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "Span<Thing>").WithArguments("System.Span<Thing>").WithLocation(5, 21));
        }

        [WorkItem(26457, "https://github.com/dotnet/roslyn/issues/26457")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefThisAssignment_Class(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
class Test
{
    public void M(ref Test obj)
    {
        this = ref this;
        obj = ref this;
        this = ref obj;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref this;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(6, 9),
                // (6,20): error CS1510: A ref or out value must be an assignable variable
                //         this = ref this;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "this").WithLocation(6, 20),
                // (7,19): error CS1510: A ref or out value must be an assignable variable
                //         obj = ref this;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "this").WithLocation(7, 19),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref obj;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(8, 9));
        }

        [WorkItem(26457, "https://github.com/dotnet/roslyn/issues/26457")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefThisAssignment_Struct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
struct Test
{
    public void M(ref Test obj)
    {
        this = ref this;
        obj = ref this;
        this = ref obj;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref this;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(6, 9),
                // (7,9): error CS8374: Cannot ref-assign 'this' to 'obj' because 'this' has a narrower escape scope than 'obj'.
                //         obj = ref this;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "obj = ref this").WithArguments("obj", "this").WithLocation(7, 9),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref obj;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(8, 9));
        }

        [WorkItem(26457, "https://github.com/dotnet/roslyn/issues/26457")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefThisAssignment_ReadOnlyStruct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
readonly struct Test
{
    public void M(ref Test obj)
    {
        this = ref this;
        obj = ref this;
        this = ref obj;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref this;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(6, 9),
                // (7,19): error CS1510: A ref or out value must be an assignable variable
                //         obj = ref this;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "this").WithLocation(7, 19),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref obj;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(8, 9));
        }

        [WorkItem(26457, "https://github.com/dotnet/roslyn/issues/26457")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefThisAssignment_RefStruct(LanguageVersion languageVersion)
        {
            var source = @"
ref struct Test
{
    public void M(ref Test obj)
    {
        this = ref this;
        obj = ref this;
        this = ref obj;
    }
}";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref this;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(6, 9),
                // (7,9): error CS8374: Cannot ref-assign 'this' to 'obj' because 'this' has a narrower escape scope than 'obj'.
                //         obj = ref this;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "obj = ref this").WithArguments("obj", "this").WithLocation(7, 9),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref obj;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(8, 9));
        }

        [WorkItem(26457, "https://github.com/dotnet/roslyn/issues/26457")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefThisAssignment_ReadOnlyRefStruct(LanguageVersion languageVersion)
        {
            CreateCompilation(@"
readonly ref struct Test
{
    public void M(ref Test obj)
    {
        this = ref this;
        obj = ref this;
        this = ref obj;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref this;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(6, 9),
                // (7,19): error CS1510: A ref or out value must be an assignable variable
                //         obj = ref this;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "this").WithLocation(7, 19),
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref obj;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(8, 9));
        }

        [WorkItem(29927, "https://github.com/dotnet/roslyn/issues/29927")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void CoalesceSpanReturn(LanguageVersion languageVersion)
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    Span<byte> M()
    {       
        return null ?? new Span<byte>();
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(29927, "https://github.com/dotnet/roslyn/issues/29927")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void CoalesceAssignSpanReturn(LanguageVersion languageVersion)
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    Span<byte> M()
    {       
        var x = null ?? new Span<byte>();
        return x;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(29927, "https://github.com/dotnet/roslyn/issues/29927")]
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void CoalesceRefSpanReturn(LanguageVersion languageVersion)
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    Span<byte> M()
    {       
        Span<byte> x = stackalloc byte[10];
        return null ?? x;
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (8,24): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return null ?? x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(8, 24)
                );
        }

        [WorkItem(62973, "https://github.com/dotnet/roslyn/issues/62973")]
        [Fact]
        public void RegressionTest62973()
        {
            var compilation = CreateCompilation(
"""
#nullable enable
using System.Collections.Generic;

System.Console.WriteLine("");

public class ArrayPool<T> { }
public readonly ref struct PooledArrayHandle<T>
{
    public void Dispose() { }
}

public static class Test
{
    public static PooledArrayHandle<T> RentArray<T>(this int length, out T[] array, ArrayPool<T>? pool = null) {
        throw null!;
    }

    public static IEnumerable<int> Iterator() {
        // Verify that the ref struct is usable
        using var handle = RentArray<int>(200, out var array);
  
        for (int i = 0; i < array.Length; i++) {
            yield return i;
        }
    }
}
""");
            compilation.VerifyEmitDiagnostics(
                // (20,19): error CS4013: Instance of type 'PooledArrayHandle<int>' cannot be used inside a nested function, query expression, iterator block or async method
                //         using var handle = RentArray<int>(200, out var array);
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "handle = RentArray<int>(200, out var array)").WithArguments("PooledArrayHandle<int>").WithLocation(20, 19));
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/40583")]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ConvertedSpanReturn(LanguageVersion languageVersion)
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    D M1() => stackalloc byte[10];
    D M2() { return stackalloc byte[10]; }
}
class D
{
    public static implicit operator D(Span<byte> span) => new D();
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(63384, "https://github.com/dotnet/roslyn/issues/63384")]
        [Theory]
        [InlineData("nuint")]
        [InlineData("nint")]
        public void NativeIntegerThis(string type)
        {
            var compilation = CreateCompilation(
    $$"""
        ref struct S
        {
            static int M({{type}} ptr) => ptr.GetHashCode();
        }
    """);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(63446, "https://github.com/dotnet/roslyn/issues/63446")]
        public void RefDiscardAssignment()
        {
            var source = @"
class Program
{
    static int dummy;

    static ref int F()
    {
        return ref dummy;
    }

    static void Main()
    {
        Test();
        System.Console.WriteLine(""Done"");
    }

    static void Test()
    {
        _ = ref F();
    }
}
";

            CompileAndVerify(source, expectedOutput: "Done").VerifyDiagnostics().
                VerifyIL("Program.Test",
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  call       ""ref int Program.F()""
  IL_0005:  pop
  IL_0006:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_01()
        {
            var source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
        // This refers to stack memory that has already been left out.
        ref Vec4 local = ref Test1();
        Console.WriteLine(local);
    }

    private static ref Vec4 Test1()
    {
        // Defensive copy occurs and it is placed in stack memory implicitly.
        // The method returns a reference to the copy, which happens invalid memory access.
        ref Vec4 xyzw1 = ref ReadOnlyVec.Self;
        return ref xyzw1;
    }

    private static ref Vec4 Test2()
    {
        var copy = ReadOnlyVec;
        ref Vec4 xyzw2 = ref copy.Self;
        return ref xyzw2;
    }

    private static ref Vec4 Test3()
    {
        ref Vec4 xyzw3 = ref ReadOnlyVec.Self2();
        return ref xyzw3;
    }

    private static ref Vec4 Test4()
    {
        var copy = ReadOnlyVec;
        ref Vec4 xyzw4 = ref copy.Self2();
        return ref xyzw4;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public ref Vec4 Self => ref this;

    [UnscopedRef]
    public ref Vec4 Self2() => ref this;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (22,20): error CS8157: Cannot return 'xyzw1' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw1;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw1").WithArguments("xyzw1").WithLocation(22, 20),
                // (29,20): error CS8157: Cannot return 'xyzw2' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw2;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw2").WithArguments("xyzw2").WithLocation(29, 20),
                // (35,20): error CS8157: Cannot return 'xyzw3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw3;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw3").WithArguments("xyzw3").WithLocation(35, 20),
                // (42,20): error CS8157: Cannot return 'xyzw4' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw4;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw4").WithArguments("xyzw4").WithLocation(42, 20)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_02()
        {
            var source =
@"#pragma warning disable CS8321 // The local function is declared but never used
using System.Diagnostics.CodeAnalysis;

var x = new Wrap { X = 1 };

ref var r = ref m1(x);
System.Console.WriteLine(r.X); // undefined value

static ref Wrap m1(in Wrap i)
{
    ref Wrap r1 = ref i.Self; // defensive copy
    return ref r1; // ref to the local copy
}

static ref Wrap m2(in Wrap i)
{
    var copy = i;
    ref Wrap r2 = ref copy.Self;
    return ref r2; // ref to the local copy
}

static ref Wrap m3(in Wrap i)
{
    ref Wrap r3 = ref i.Self2();
    return ref r3;
}

static ref Wrap m4(in Wrap i)
{
    var copy = i;
    ref Wrap r4 = ref copy.Self2();
    return ref r4; // ref to the local copy
}

struct Wrap
{
    public float X;

    [UnscopedRef]
    public ref Wrap Self => ref this;

    [UnscopedRef]
    public ref Wrap Self2() => ref this;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (12,16): error CS8157: Cannot return 'r1' by reference because it was initialized to a value that cannot be returned by reference
                //     return ref r1; // ref to the local copy
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r1").WithArguments("r1").WithLocation(12, 16),
                // (19,16): error CS8157: Cannot return 'r2' by reference because it was initialized to a value that cannot be returned by reference
                //     return ref r2; // ref to the local copy
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r2").WithArguments("r2").WithLocation(19, 16),
                // (25,16): error CS8157: Cannot return 'r3' by reference because it was initialized to a value that cannot be returned by reference
                //     return ref r3;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r3").WithArguments("r3").WithLocation(25, 16),
                // (32,16): error CS8157: Cannot return 'r4' by reference because it was initialized to a value that cannot be returned by reference
                //     return ref r4; // ref to the local copy
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r4").WithArguments("r4").WithLocation(32, 16)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_03()
        {
            var source =
@"
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static ref Vec4 Test3()
    {
        ref Vec4 xyzw3 = ref ReadOnlyVec.Self2();
        return ref xyzw3;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    readonly public ref Vec4 Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics().VerifyIL("Program.Test3",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsflda    ""Vec4 Program.ReadOnlyVec""
  IL_0005:  call       ""readonly ref Vec4 Vec4.Self2()""
  IL_000a:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_04()
        {
            var source =
@"
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static ref Vec4 Test1()
    {
        ref Vec4 xyzw1 = ref ReadOnlyVec.Self;
        return ref xyzw1;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    readonly public ref Vec4 Self => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics().VerifyIL("Program.Test1",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsflda    ""Vec4 Program.ReadOnlyVec""
  IL_0005:  call       ""readonly ref Vec4 Vec4.Self.get""
  IL_000a:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_05()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var xyzw2 = r2.Self;
        return xyzw2;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self
    {  get => throw null; set {}}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'xyzw1' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw1").WithArguments("xyzw1").WithLocation(16, 16),
                // (23,16): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(23, 16)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_06()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    readonly public Span<float> Self
    {  get => throw null; set {}}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics().VerifyIL("Program.Test1",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsflda    ""Vec4 Program.ReadOnlyVec""
  IL_0005:  call       ""readonly System.Span<float> Vec4.Self.get""
  IL_000a:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_07()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self
    { readonly get => throw null; set {}}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics().VerifyIL("Program.Test1",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsflda    ""Vec4 Program.ReadOnlyVec""
  IL_0005:  call       ""readonly System.Span<float> Vec4.Self.get""
  IL_000a:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_08()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var xyzw1 = ReadOnlyVec.Self;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var xyzw2 = r2.Self;
        return xyzw2;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self
    {  get => throw null; readonly set {}}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'xyzw1' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw1").WithArguments("xyzw1").WithLocation(16, 16),
                // (23,16): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(23, 16)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_09()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static void Test1()
    {
        ReadOnlyVec.Self = default;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self
    {  readonly get => throw null; set {}}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (15,9): error CS1650: Fields of static readonly field 'Program.ReadOnlyVec' cannot be assigned to (except in a static constructor or a variable initializer)
                //         ReadOnlyVec.Self = default;
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic2, "ReadOnlyVec.Self").WithArguments("Program.ReadOnlyVec").WithLocation(15, 9)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_10()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static void Test1()
    {
        ReadOnlyVec.Self = default;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self
    {  get => throw null; readonly set {}}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics().VerifyIL("Program.Test1",
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (System.Span<float> V_0)
  IL_0000:  ldsflda    ""Vec4 Program.ReadOnlyVec""
  IL_0005:  ldloca.s   V_0
  IL_0007:  initobj    ""System.Span<float>""
  IL_000d:  ldloc.0
  IL_000e:  call       ""readonly void Vec4.Self.set""
  IL_0013:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_11()
        {
            var source =
@"

using System.Diagnostics.CodeAnalysis;

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public ref Vec4 Self2() => ref this;

    [UnscopedRef]
    readonly public ref Vec4 Test3()
    {
        ref Vec4 xyzw3 = ref this.Self2();
        return ref xyzw3;
    }

    [UnscopedRef]
    readonly public ref Vec4 Test4()
    {
        var r = this;
        ref Vec4 xyzw4 = ref r.Self2();
        return ref xyzw4;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (16,30): warning CS8656: Call to non-readonly member 'Vec4.Self2()' from a 'readonly' member results in an implicit copy of 'this'.
                //         ref Vec4 xyzw3 = ref this.Self2();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("Vec4.Self2()", "this").WithLocation(16, 30),
                // (17,20): error CS8157: Cannot return 'xyzw3' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw3;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw3").WithArguments("xyzw3").WithLocation(17, 20),
                // (25,20): error CS8157: Cannot return 'xyzw4' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref xyzw4;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "xyzw4").WithArguments("xyzw4").WithLocation(25, 20)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_12()
        {
            var source =
@"
using System.Diagnostics.CodeAnalysis;

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public ref Vec4 Self2() => ref this;

    [UnscopedRef]
    public ref Vec4 Test3()
    {
        ref Vec4 xyzw3 = ref this.Self2();
        return ref xyzw3;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).
                VerifyDiagnostics().
                VerifyIL("Vec4.Test3",
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref Vec4 Vec4.Self2()""
  IL_0006:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_13()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    private Program(out Span<float> x)
    {
        var xyzw3 = ReadOnlyVec.Self2();
        x = xyzw3;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            CompileAndVerify(comp, verify: Verification.Skipped).
                VerifyDiagnostics().
                VerifyIL("Program..ctor",
@"
{
  // Code size       57 (0x39)
  .maxstack  5
  .locals init (System.Span<float> V_0) //xyzw3
  IL_0000:  ldarg.0
  IL_0001:  ldc.r4     1
  IL_0006:  ldc.r4     2
  IL_000b:  ldc.r4     3
  IL_0010:  ldc.r4     4
  IL_0015:  newobj     ""Vec4..ctor(float, float, float, float)""
  IL_001a:  stfld      ""Vec4 Program.ReadOnlyVec""
  IL_001f:  ldarg.0
  IL_0020:  call       ""object..ctor()""
  IL_0025:  ldarg.0
  IL_0026:  ldflda     ""Vec4 Program.ReadOnlyVec""
  IL_002b:  call       ""System.Span<float> Vec4.Self2()""
  IL_0030:  stloc.0
  IL_0031:  ldarg.1
  IL_0032:  ldloc.0
  IL_0033:  stobj      ""System.Span<float>""
  IL_0038:  ret
}
");
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_14()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    private Program()
    {
        var d = (out Span<float> x) =>
                {
                    var xyzw1 = ReadOnlyVec.Self2();
                    x = xyzw1;
                };

        d = local;

        void local(out Span<float> x)
        {
            var xyzw2 = ReadOnlyVec.Self2();
            x = xyzw2;
        }
    }

    private void Test3(out Span<float> x)
    {
        var xyzw3 = ReadOnlyVec.Self2();
        x = xyzw3;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (14,25): error CS8352: Cannot use variable 'xyzw1' in this context because it may expose referenced variables outside of their declaration scope
                //                     x = xyzw1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw1").WithArguments("xyzw1").WithLocation(14, 25),
                // (22,17): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //             x = xyzw2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(22, 17),
                // (29,13): error CS8352: Cannot use variable 'xyzw3' in this context because it may expose referenced variables outside of their declaration scope
                //         x = xyzw3;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw3").WithArguments("xyzw3").WithLocation(29, 13)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_15()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);
    private static S s;

    int F1 = GetInt(s = new S(ReadOnlyVec.Self2()));
    int F2 = GetInt(() => s = new S(ReadOnlyVec.Self2()));
    static int F3 = GetInt(s = new S(ReadOnlyVec.Self2()));

    static int GetInt(S s) => 0;
    static int GetInt(System.Action a) => 0;
}

ref struct S
{
    public S (Span<float> x) {}
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8345: Field or auto-implemented property cannot be of type 'S' unless it is an instance member of a ref struct.
                //     private static S s;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "S").WithArguments("S").WithLocation(8, 20),
                // (10,31): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Program.ReadOnlyVec'
                //     int F1 = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ReadOnlyVec").WithArguments("Program.ReadOnlyVec").WithLocation(10, 31),
                // (11,37): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Program.ReadOnlyVec'
                //     int F2 = GetInt(() => s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ReadOnlyVec").WithArguments("Program.ReadOnlyVec").WithLocation(11, 37),
                // (12,38): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Program.ReadOnlyVec'
                //     static int F3 = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ReadOnlyVec").WithArguments("Program.ReadOnlyVec").WithLocation(12, 38)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_16()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);
    private static S s;

    int P1 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));
    int P2 {get;} = GetInt(() => s = new S(ReadOnlyVec.Self2()));
    static int P3 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));

    static int GetInt(S s) => 0;
    static int GetInt(System.Action a) => 0;
}

ref struct S
{
    public S (Span<float> x) {}
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8345: Field or auto-implemented property cannot be of type 'S' unless it is an instance member of a ref struct.
                //     private static S s;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "S").WithArguments("S").WithLocation(8, 20),
                // (10,38): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Program.ReadOnlyVec'
                //     int P1 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ReadOnlyVec").WithArguments("Program.ReadOnlyVec").WithLocation(10, 38),
                // (11,44): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Program.ReadOnlyVec'
                //     int P2 {get;} = GetInt(() => s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ReadOnlyVec").WithArguments("Program.ReadOnlyVec").WithLocation(11, 44),
                // (12,45): error CS0236: A field initializer cannot reference the non-static field, method, or property 'Program.ReadOnlyVec'
                //     static int P3 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "ReadOnlyVec").WithArguments("Program.ReadOnlyVec").WithLocation(12, 45)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_17()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);
    private static S s;

    static Program()
    {
        var xyzw1 = ReadOnlyVec.Self2();
        s = new S(xyzw1);

        var d = static () =>
                {
                    var xyzw2 = ReadOnlyVec.Self2();
                    s = new S(xyzw2);
                };

        d = local;

        static void local()
        {
            var xyzw3 = ReadOnlyVec.Self2();
            s = new S(xyzw3);
        }
    }

    static void Test4()
    {
        var xyzw4 = ReadOnlyVec.Self2();
        s = new S(xyzw4);
    }
}

ref struct S
{
    public S (Span<float> x) {}
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8345: Field or auto-implemented property cannot be of type 'S' unless it is an instance member of a ref struct.
                //     private static S s;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "S").WithArguments("S").WithLocation(8, 20),
                // (18,25): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //                     s = new S(xyzw2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(xyzw2)").WithArguments("S.S(System.Span<float>)", "x").WithLocation(18, 25),
                // (18,31): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //                     s = new S(xyzw2);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(18, 31),
                // (26,17): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //             s = new S(xyzw3);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(xyzw3)").WithArguments("S.S(System.Span<float>)", "x").WithLocation(26, 17),
                // (26,23): error CS8352: Cannot use variable 'xyzw3' in this context because it may expose referenced variables outside of their declaration scope
                //             s = new S(xyzw3);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw3").WithArguments("xyzw3").WithLocation(26, 23),
                // (33,13): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = new S(xyzw4);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(xyzw4)").WithArguments("S.S(System.Span<float>)", "x").WithLocation(33, 13),
                // (33,19): error CS8352: Cannot use variable 'xyzw4' in this context because it may expose referenced variables outside of their declaration scope
                //         s = new S(xyzw4);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw4").WithArguments("xyzw4").WithLocation(33, 19)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_18()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);
    private static S s;

    static int F1 = GetInt(s = new S(ReadOnlyVec.Self2()));
    static int F2 = GetInt(static () => s = new S(ReadOnlyVec.Self2()));
    int F3 = GetInt(s = new S(ReadOnlyVec.Self2()));

    static int GetInt(S s) => 0;
    static int GetInt(System.Action a) => 0;
}

ref struct S
{
    public S (Span<float> x) {}
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8345: Field or auto-implemented property cannot be of type 'S' unless it is an instance member of a ref struct.
                //     private static S s;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "S").WithArguments("S").WithLocation(8, 20),
                // (11,45): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     static int F2 = GetInt(static () => s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(ReadOnlyVec.Self2())").WithArguments("S.S(System.Span<float>)", "x").WithLocation(11, 45),
                // (11,51): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int F2 = GetInt(static () => s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "ReadOnlyVec").WithLocation(11, 51),
                // (12,25): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     int F3 = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(ReadOnlyVec.Self2())").WithArguments("S.S(System.Span<float>)", "x").WithLocation(12, 25),
                // (12,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     int F3 = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "ReadOnlyVec").WithLocation(12, 31)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_19()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);
    private static S s;

    static int P1 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));
    static int P2 {get;} = GetInt(static () => s = new S(ReadOnlyVec.Self2()));
    int P3 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));

    static int GetInt(S s) => 0;
    static int GetInt(System.Action a) => 0;
}

ref struct S
{
    public S (Span<float> x) {}
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8345: Field or auto-implemented property cannot be of type 'S' unless it is an instance member of a ref struct.
                //     private static S s;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "S").WithArguments("S").WithLocation(8, 20),
                // (11,52): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     static int P2 {get;} = GetInt(static () => s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(ReadOnlyVec.Self2())").WithArguments("S.S(System.Span<float>)", "x").WithLocation(11, 52),
                // (11,58): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int P2 {get;} = GetInt(static () => s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "ReadOnlyVec").WithLocation(11, 58),
                // (12,32): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     int P3 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(ReadOnlyVec.Self2())").WithArguments("S.S(System.Span<float>)", "x").WithLocation(12, 32),
                // (12,38): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     int P3 {get;} = GetInt(s = new S(ReadOnlyVec.Self2()));
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "ReadOnlyVec").WithLocation(12, 38)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_20()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);
    private static S s;

    int P
    {
        get => 0;
        init
        {
            var xyz1 = ReadOnlyVec.Self2(); 
            s = new S(xyz1);

            var d = () =>
                    {
                        var xyz2 = ReadOnlyVec.Self2(); 
                        s = new S(xyz2);
                    };

            d = local;

            void local()
            {
                var xyz3 = ReadOnlyVec.Self2(); 
                s = new S(xyz3);
            }
        }
    }
}

ref struct S
{
    public S (Span<float> x) {}
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public Span<float> Self2() => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8345: Field or auto-implemented property cannot be of type 'S' unless it is an instance member of a ref struct.
                //     private static S s;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "S").WithArguments("S").WithLocation(8, 20),
                // (21,29): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //                         s = new S(xyz2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(xyz2)").WithArguments("S.S(System.Span<float>)", "x").WithLocation(21, 29),
                // (21,35): error CS8352: Cannot use variable 'xyz2' in this context because it may expose referenced variables outside of their declaration scope
                //                         s = new S(xyz2);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyz2").WithArguments("xyz2").WithLocation(21, 35),
                // (29,21): error CS8347: Cannot use a result of 'S.S(Span<float>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //                 s = new S(xyz3);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new S(xyz3)").WithArguments("S.S(System.Span<float>)", "x").WithLocation(29, 21),
                // (29,27): error CS8352: Cannot use variable 'xyz3' in this context because it may expose referenced variables outside of their declaration scope
                //                 s = new S(xyz3);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyz3").WithArguments("xyz3").WithLocation(29, 27)
                );
        }

        [Fact]
        [WorkItem(64776, "https://github.com/dotnet/roslyn/issues/64776")]
        public void DefensiveCopy_21()
        {
            var source =
@"
using System;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static readonly Vec4 ReadOnlyVec = new Vec4(1, 2, 3, 4);

    static void Main()
    {
    }

    private static Span<float> Test1()
    {
        var (xyzw1, _) = ReadOnlyVec;
        return xyzw1;
    }

    private static Span<float> Test2()
    {
        var r2 = ReadOnlyVec;
        var (xyzw2, _) = r2;
        return xyzw2;
    }

    private static Span<float> Test3()
    {
        ReadOnlyVec.Deconstruct(out var xyzw3, out _);
        return xyzw3;
    }

    private static Span<float> Test4()
    {
        var r4 = ReadOnlyVec;
        r4.Deconstruct(out var xyzw4, out _);
        return xyzw4;
    }
}

public struct Vec4
{
    public float X, Y, Z, W;
    public Vec4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    [UnscopedRef]
    public void Deconstruct(out Span<float> x, out int i) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'xyzw1' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw1").WithArguments("xyzw1").WithLocation(16, 16),
                // (23,16): error CS8352: Cannot use variable 'xyzw2' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw2").WithArguments("xyzw2").WithLocation(23, 16),
                // (29,16): error CS8352: Cannot use variable 'xyzw3' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw3;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw3").WithArguments("xyzw3").WithLocation(29, 16),
                // (36,16): error CS8352: Cannot use variable 'xyzw4' in this context because it may expose referenced variables outside of their declaration scope
                //         return xyzw4;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "xyzw4").WithArguments("xyzw4").WithLocation(36, 16)
                );
        }

        [Fact]
        public void LocalScope_DeclarationExpression_01()
        {
            var source = """
                ref struct RS
                {
                    public RS(ref RS rs) => throw null!;
                }

                class Program
                {
                    static void M0(ref RS rs1, out RS rs2)
                    {
                        // ok. RSTE of rs1 is ReturnOnly. STE of rs2 is ReturnOnly.
                        rs2 = new RS(ref rs1);
                    }

                    static RS M1(scoped ref RS rs3)
                    {
                        // RSTE of rs3 is CurrentMethod
                        // STE of rs4 (local variable) is also CurrentMethod
                        M0(ref rs3, out var rs4);
                        return rs4; // 1
                    }

                    static RS M2(scoped ref RS rs3)
                    {
                        M0(ref rs3, out RS rs4);
                        return rs4; // 2
                    }

                    static RS M3(scoped ref RS rs3)
                    {
                        RS rs4;
                        M0(ref rs3, out rs4); // 3
                        return rs4;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (19,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(19, 16),
                // (25,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(25, 16),
                // (31,9): error CS8350: This combination of arguments to 'Program.M0(ref RS, out RS)' is disallowed because it may expose variables referenced by parameter 'rs1' outside of their declaration scope
                //         M0(ref rs3, out rs4); // 3
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M0(ref rs3, out rs4)").WithArguments("Program.M0(ref RS, out RS)", "rs1").WithLocation(31, 9),
                // (31,16): error CS9075: Cannot return a parameter by reference 'rs3' because it is scoped to the current method
                //         M0(ref rs3, out rs4); // 3
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "rs3").WithArguments("rs3").WithLocation(31, 16));
        }

        [Fact]
        public void LocalScope_DeclarationExpression_02()
        {
            var source = """
                ref struct RS { }

                class Program
                {
                    static void M0(RS rs1, out RS rs2)
                    {
                        // ok. STE of rs1 is CallingMethod. STE of rs2 is ReturnOnly.
                        rs2 = rs1;
                    }

                    static RS M1(scoped RS rs3)
                    {
                        // STE of rs3 is CurrentMethod
                        // STE of rs4 (local variable) is also CurrentMethod
                        M0(rs3, out var rs4);
                        return rs4; // 1
                    }

                    static RS M2(scoped RS rs3)
                    {
                        M0(rs3, out RS rs4);
                        return rs4; // 2
                    }

                    static RS M3(scoped RS rs3)
                    {
                        RS rs4;
                        M0(rs3, out rs4); // 3
                        return rs4;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(16, 16),
                // (22,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(22, 16),
                // (28,9): error CS8350: This combination of arguments to 'Program.M0(RS, out RS)' is disallowed because it may expose variables referenced by parameter 'rs1' outside of their declaration scope
                //         M0(rs3, out rs4); // 3
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M0(rs3, out rs4)").WithArguments("Program.M0(RS, out RS)", "rs1").WithLocation(28, 9),
                // (28,12): error CS8352: Cannot use variable 'scoped RS rs3' in this context because it may expose referenced variables outside of their declaration scope
                //         M0(rs3, out rs4); // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs3").WithArguments("scoped RS rs3").WithLocation(28, 12));
        }

        [Fact]
        public void LocalScope_DeclarationExpression_03()
        {
            var source = """
                ref struct RS { }
                struct S { }

                class Program
                {
                    static void M0(RS rs1, out S s1) => throw null!;

                    static S M1(scoped RS rs2)
                    {
                        // STE of s2 is CallingMethod because it is not ref struct
                        M0(rs2, out var s2);
                        return s2;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LocalScope_DeclarationExpression_04()
        {
            var source0 = """
                public ref struct RS
                {
                    public RS(ref int i) => throw null!;
                }

                public class Util
                {
                    public static void M0(ref int i1, out RS rs1)
                    {
                        // RSTE of i1 is ReturnOnly. STE of rs1 is ReturnOnly in C# 11, but CallingMethod in C# 10.
                        rs1 = new RS(ref i1);
                    }
                }
                """;

            var source1 = """
                class Program
                {
                    static void M1(ref int i2, ref RS rs2)
                    {
                        // STE of rs3 (local variable) is ReturnOnly in C# 11, but CallingMethod in C# 10.
                        Util.M0(ref i2, out var rs3);

                        // STE of rs2 is CallingMethod. Therefore the assignment is permitted in C# 10 but not C# 11.
                        rs2 = rs3; // 1
                    }
                }
                """;

            var source1DiagnosticsWhenSource0IsCSharp11 = new[]
            {
                // (9,15): error CS8352: Cannot use variable 'rs3' in this context because it may expose referenced variables outside of their declaration scope
                //         rs2 = rs3; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs3").WithArguments("rs3").WithLocation(9, 15)
            };

            var comp = CreateCompilation(new[] { source0, source1 }, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(source1DiagnosticsWhenSource0IsCSharp11);

            comp = CreateCompilation(new[] { source0, source1 }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();

            // Reference C# 10, consume from 11
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular10);
            comp0.VerifyDiagnostics();

            var comp1 = CreateCompilation(source1, references: new[] { comp0.ToMetadataReference() }, parseOptions: TestOptions.Regular11);
            comp1.VerifyDiagnostics();

            comp1 = CreateCompilation(source1, references: new[] { comp0.EmitToImageReference() }, parseOptions: TestOptions.Regular11);
            comp1.VerifyDiagnostics();

            // Reference C# 11, consume from 10
            comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular11);
            comp0.VerifyDiagnostics();

            comp1 = CreateCompilation(source1, references: new[] { comp0.ToMetadataReference() }, parseOptions: TestOptions.Regular10);
            comp1.VerifyDiagnostics(source1DiagnosticsWhenSource0IsCSharp11);

            comp1 = CreateCompilation(source1, references: new[] { comp0.EmitToImageReference() }, parseOptions: TestOptions.Regular10);
            comp1.VerifyDiagnostics(source1DiagnosticsWhenSource0IsCSharp11);
        }

        [Fact]
        public void LocalScope_DeclarationExpression_05()
        {
            var source = """
                using System;

                ref struct RS
                {
                    public RS(ref int i) => throw null!;
                }

                class Program
                {
                    static void M0(out RS rs1, __arglist)
                    {
                        // STE of __refvalue (i.e. values in __arglist) is CallingMethod.
                        // RSTE of __refvalue is CurrentMethod.
                        // STE of rs1 is ReturnOnly.
                        var ai = new ArgIterator(__arglist);
                        rs1 = __refvalue(ai.GetNextArg(), RS);
                        rs1 = new RS(ref __refvalue(ai.GetNextArg(), int)); // 1
                    }

                    static RS M1(scoped RS rs2)
                    {
                        M0(out var rs3, __arglist(rs2));
                        return rs3; // 2
                    }

                    static RS M2(scoped RS rs4)
                    {
                        M0(out var rs5, __arglist(ref rs4));
                        return rs5; // 3
                    }

                    static RS M3(ref int i1)
                    {
                        M0(out var rs5, __arglist(ref i1));
                        return rs5;
                    }
                }
                """;

            var comp = CreateCompilationWithMscorlibAndSpan(source);
            comp.VerifyDiagnostics(
                // (17,15): error CS8347: Cannot use a result of 'RS.RS(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         rs1 = new RS(ref __refvalue(ai.GetNextArg(), int)); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "new RS(ref __refvalue(ai.GetNextArg(), int))").WithArguments("RS.RS(ref int)", "i").WithLocation(17, 15),
                // (17,26): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         rs1 = new RS(ref __refvalue(ai.GetNextArg(), int)); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "__refvalue(ai.GetNextArg(), int)").WithLocation(17, 26),
                // (23,16): error CS8352: Cannot use variable 'rs3' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs3; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs3").WithArguments("rs3").WithLocation(23, 16),
                // (29,16): error CS8352: Cannot use variable 'rs5' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs5; // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs5").WithArguments("rs5").WithLocation(29, 16));
        }

        [Fact]
        public void LocalScope_DeclarationExpression_06()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                ref struct RS
                {
                    public RS(ref RS rs) => throw null!;

                    [UnscopedRef]
                    void M0(out RS rs2)
                    {
                        // ok. RSTE of `this` is ReturnOnly. STE of rs2 is ReturnOnly.
                        rs2 = new RS(ref this);
                    }

                    RS M1()
                    {
                        // RSTE of `this` is CurrentMethod
                        // STE of rs4 (local variable) is also CurrentMethod
                        M0(out var rs4);
                        return rs4; // 1
                    }

                    [UnscopedRef]
                    RS M2()
                    {
                        M0(out var rs4);
                        return rs4;
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics(
                // (19,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(19, 16));
        }

        [Fact]
        public void LocalScope_DeclarationExpression_07()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                class Program
                {
                    static ref int F1([UnscopedRef] out int i)
                    {
                        i = 0;
                        return ref i;
                    }
                    static ref int F2()
                    {
                        return ref F1(out int i); // 1
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics(
                // (12,20): error CS8347: Cannot use a result of 'Program.F1(out int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return ref F1(out int i); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "F1(out int i)").WithArguments("Program.F1(out int)", "i").WithLocation(12, 20),
                // (12,27): error CS8168: Cannot return local 'i' by reference because it is not a ref local
                //         return ref F1(out int i); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "int i").WithArguments("i").WithLocation(12, 27));
        }

        [Fact]
        public void LocalScope_DeclarationExpression_08()
        {
            var source = """
                ref struct RS
                {
                    public RS(ref RS rs) => throw null!;
                }

                class Program
                {
                    static void M0(ref RS rs1, out RS rs2)
                    {
                        // ok. RSTE of rs1 is ReturnOnly. STE of rs2 is ReturnOnly.
                        rs2 = new RS(ref rs1);
                    }

                    static RS M1(ref RS rs3)
                    {
                        // RSTE of rs3 is ReturnOnly.
                        // However, since rs4 is 'scoped', its STE should be narrowed to CurrentMethod
                        M0(ref rs3, out scoped var rs4);
                        return rs4; // 1
                    }

                    static RS M2(ref RS rs5)
                    {
                        // RSTE of rs5 is ReturnOnly.
                        // However, since rs6 is 'scoped', its STE should be narrowed to CurrentMethod
                        M0(ref rs5, out scoped RS rs6);
                        return rs6; // 2
                    }
                
                    static RS M12(ref RS rs3)
                    {
                        // RSTE of rs3 is ReturnOnly.
                        // However, since rs4 is 'scoped', its STE should be narrowed to CurrentMethod
                        scoped RS rs4;
                        M0(ref rs3, out rs4);
                        return rs4; // 3
                    }
                
                    static RS M22(ref RS rs5)
                    {
                        // RSTE of rs5 is ReturnOnly.
                        // However, since rs6 is 'scoped', its STE should be narrowed to CurrentMethod
                        scoped RS rs6;
                        M0(ref rs5, out rs6);
                        return rs6; // 4
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (19,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(19, 16),
                // (27,16): error CS8352: Cannot use variable 'rs6' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs6; // 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs6").WithArguments("rs6").WithLocation(27, 16),
                // (36,16): error CS8352: Cannot use variable 'rs4' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs4; // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs4").WithArguments("rs4").WithLocation(36, 16),
                // (45,16): error CS8352: Cannot use variable 'rs6' in this context because it may expose referenced variables outside of their declaration scope
                //         return rs6; // 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs6").WithArguments("rs6").WithLocation(45, 16)
                );
        }

        [Fact, WorkItem(64783, "https://github.com/dotnet/roslyn/issues/64783")]
        public void OutArgumentsDoNotContributeValEscape_01()
        {
            var source = """
                using System;

                class Program
                {
                    static Span<byte> M1()
                    {
                        Span<byte> a = stackalloc byte[42];
                        var ret = OneOutSpanReturnsSpan(out a);
                        return ret;
                    }

                    static Span<byte> M2()
                    {
                        Span<byte> a = stackalloc byte[42];
                        TwoOutSpans(out a, out Span<byte> b);
                        return b;
                    }

                    static Span<byte> OneOutSpanReturnsSpan(out Span<byte> a)
                    {
                        // 'return a' is illegal until it is overwritten
                        a = default;
                        return default;
                    }

                    static void TwoOutSpans(out Span<byte> a, out Span<byte> b)
                    {
                        // 'a = b' and 'b = a' are illegal until one has already been written
                        a = b = default;
                    }
                }

                """;

            var comp = CreateCompilationWithSpan(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(56587, "https://github.com/dotnet/roslyn/issues/56587")]
        public void OutArgumentsDoNotContributeValEscape_02()
        {
            // Test that out discard arguments are not treated as inputs.
            // This means we don't need to take special care to zero-out the variable used for a discard argument between uses.
            var source = """
                using System;

                class Program
                {
                    static Span<byte> M1()
                    {
                        Span<byte> a = stackalloc byte[42];
                        TwoOutSpans(out a, out _);
                        TwoOutSpans(out _, out Span<byte> c);
                        return c;
                    }

                    static void TwoOutSpans(out Span<byte> a, out Span<byte> b)
                    {
                        // 'a = b' and 'b = a' are illegal until one has already been written
                        a = b = default;
                    }
                }
                """;

            var comp = CreateCompilationWithSpan(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Local_UsingStatementExpression()
        {
            string source = """
                using System;
                struct S : IDisposable
                {
                    public void Dispose() { }
                }
                ref struct R
                {
                    public ref int F;
                    public R(ref int i) { F = ref i; }
                    public static implicit operator S(R r) => default;
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 0;
                        var x = new R(ref i);
                        using (x switch { R y => (S)y })
                        {
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }

        [WorkItem(67493, "https://github.com/dotnet/roslyn/issues/67493")]
        [Fact]
        public void Local_SwitchStatementExpression()
        {
            string source = """
                ref struct R1
                {
                    public R2 F;
                    public R1(ref int i) { F = new R2(ref i); }
                }
                ref struct R2
                {
                    ref int _i;
                    public R2(ref int i) { _i = ref i; }
                }
                class Program
                {
                    static R2 F()
                    {
                        int i = 0;
                        var x = new R1(ref i);
                        switch (x switch { { F: R2 y } => y })
                        {
                            case R2 z:
                                return z;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (20,24): error CS8352: Cannot use variable 'z' in this context because it may expose referenced variables outside of their declaration scope
                //                 return z;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "z").WithArguments("z").WithLocation(20, 24));
        }

        [WorkItem(67493, "https://github.com/dotnet/roslyn/issues/67493")]
        [Fact]
        public void Local_ForEachExpression()
        {
            string source = """
                ref struct R
                {
                    public ref int F;
                    public R(ref int i) { F = ref i; }
                }
                ref struct Enumerable
                {
                    public ref int F;
                    public Enumerable(ref int i) { F = ref i; }
                    public Enumerator GetEnumerator() => new Enumerator(ref F);
                }
                ref struct Enumerator
                {
                    public ref int F;
                    public Enumerator(ref int i) { F = ref i; }
                    public R Current => new R(ref F);
                    public bool MoveNext() => false;
                }
                class Program
                {
                    static R F()
                    {
                        foreach (var y in 1 switch { int x => new Enumerable(ref x) })
                        {
                            return y;
                        }
                        return default;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (25,20): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //             return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(25, 20));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ParameterEscape()
        {
            var source = """
                using System;
                ref struct R
                {
                    public R(Span<int> s) { }
                    public void F(ReadOnlySpan<int> s) { }
                }
                class Program
                {
                    static void M(ReadOnlySpan<int> s)
                    {
                        R r = new R(stackalloc int[2]);
                        while (true)
                        {
                            r.F(s);
                            r.F(s.Slice(0, 1));
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics();
        }
    }
}
