// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RefEscapingTests : CompilingTestBase
    {
        [Fact()]
        public void RefLikeReturnEscape()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (23,42): error CS8526: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(23, 42),
                // (23,30): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref local)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(23, 30),
                // (23,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(MayWrap(ref local))").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(23, 24)
            );
        }

        [Fact()]
        public void RefLikeReturnEscape1()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (24,30): error CS8526: Cannot use local 'sp' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sp").WithArguments("sp").WithLocation(24, 30),
                // (24,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(sp)").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(24, 24)
            );
        }

        [Fact()]
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (23,34): error CS8168: Cannot return local 'sp' by reference because it is not a ref local
                //             return ref Test1(ref sp);    // error1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "sp").WithArguments("sp").WithLocation(23, 34),
                // (23,24): error CS8521: Cannot use a result of 'Program.Test1(ref Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(ref sp);    // error1
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref sp)").WithArguments("Program.Test1(ref Program.S1)", "arg").WithLocation(23, 24),
                // (29,34): error CS8168: Cannot return local 'sp' by reference because it is not a ref local
                //             return ref Test1(ref sp);    // error2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "sp").WithArguments("sp").WithLocation(29, 34),
                // (29,24): error CS8521: Cannot use a result of 'Program.Test1(ref Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(ref sp);    // error2
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref sp)").WithArguments("Program.Test1(ref Program.S1)", "arg").WithLocation(29, 24)
            );
        }

        [Fact()]
        public void RefLikeReturnEscapeWithRefLikes1()
        {
            var text = @"
    using System;
    class Program
    {
        static void Main()
        {
        }

        static ref Span<int> Test1(ref S1 arg)
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (33,19): error CS8526: Cannot use local 'spNr' in this context because it may expose referenced variables outside of their declaration scope
                //             spR = spNr;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "spNr").WithArguments("spNr").WithLocation(33, 19),
                // (39,19): error CS8526: Cannot use local 'ternary' in this context because it may expose referenced variables outside of their declaration scope
                //             spR = ternary;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "ternary").WithArguments("ternary").WithLocation(39, 19)
            );
        }
        
        [Fact()]
        public void RefLikeReturnEscapeInParam()
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

        static S1 MayWrap(ref readonly Span<int> arg)
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (31,30): error CS8352: Cannot use local 'sp' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sp").WithArguments("sp").WithLocation(31, 30),
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

        static S1 MayNotWrap(ref readonly int arg = 123)
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics();
        }

        [Fact()]
        public void RefLikeScopeEscape()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (18,29): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(inner);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(18, 29),
                // (18,21): error CS8521: Cannot use a result of 'Program.MayWrap(Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(inner)").WithArguments("Program.MayWrap(System.Span<int>)", "arg").WithLocation(18, 21)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeVararg()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (18,35): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x = MayWrap(__arglist(inner));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(18, 35),
                // (18,17): error CS8347: Cannot use a result of 'Program.MayWrap(__arglist)' in this context because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //             x = MayWrap(__arglist(inner));
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(__arglist(inner))").WithArguments("Program.MayWrap(__arglist)", "__arglist").WithLocation(18, 17)
            );
        }

        [Fact()]
        public void RefScopeEscapeVararg()
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
        return ref ReturnsRef1(__arglist(ref local));
    }

    static ref int ReturnsRefSpan1(__arglist)
    {
        var ai = new ArgIterator(__arglist);

        // this is ok
        return ref __refvalue(ai.GetNextArg(), Span<int>)[0];
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (24,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref __refvalue(ai.GetNextArg(), int);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "__refvalue(ai.GetNextArg(), int)").WithLocation(24, 20),
                // (32,46): error CS8352: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         return ref ReturnsRef1(__arglist(ref local));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(32, 46),
                // (32,20): error CS8347: Cannot use a result of 'Program.ReturnsRef1(__arglist)' in this context because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         return ref ReturnsRef1(__arglist(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRef1(__arglist(ref local))").WithArguments("Program.ReturnsRef1(__arglist)", "__arglist").WithLocation(32, 20)
            );
        }

        [Fact()]
        public void ThrowExpression()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics();
        }


        [Fact()]
        public void UserDefinedLogical()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (22,18): error CS8352: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local && global;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(22, 18),
                // (25,26): error CS8352: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         return global || local;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(25, 26)
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (12,36): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref ReturnsRef1(out var _);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "var _").WithLocation(12, 36),
                // (12,20): error CS8347: Cannot use a result of 'Program.ReturnsRef1(out int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref ReturnsRef1(out var _);
                Diagnostic(ErrorCode.ERR_EscapeCall, "ReturnsRef1(out var _)").WithArguments("Program.ReturnsRef1(out int)", "x").WithLocation(12, 20)
                );
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
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
        }

        [Fact()]
        public void DiscardExpressionSpan()
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
        s = stackalloc int[1];

        // ok
        return s;
    }

    static void Test3()
    {
        // error
        ReturnsSpan(out var _ ) = stackalloc int[1];
    }

    static ref Span<int> ReturnsSpan(out Span<int> x)
    {
        x = default;
        return ref x;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (23,13): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(23, 13),
                // (32,35): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         ReturnsSpan(out var _ ) = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(32, 35)
                );
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
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(60, 30)
                );
        }

        [Fact()]
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (19,33): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(19, 33),
                // (19,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(19, 21)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeThis()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (21,33): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 x = MayWrap(ref inner).Slice(1);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(21, 33),
                // (21,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner).Slice(1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(21, 21)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeThisRef()
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

        public ref S1 ReturnsRefArg(ref S1 arg) => ref arg;

        public S1 Slice(int x) => this;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (20,32): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x[0] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(20, 32),
                // (20,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x[0] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(20, 20),
                // (25,32): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x[x] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(25, 32),
                // (25,20): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x[x] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(25, 20),
                // (28,50): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(28, 50),
                // (28,38): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(28, 38)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeField()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (18,31): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             x.field = MayWrap(inner).Slice(1).field;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(18, 31),
                // (18,23): error CS8521: Cannot use a result of 'Program.MayWrap(Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x.field = MayWrap(inner).Slice(1).field;
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(inner)").WithArguments("Program.MayWrap(System.Span<int>)", "arg").WithLocation(18, 23)
            );
        }

        [Fact()]
        public void RefLikeEscapeParamsAndTopLevel()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // no diagnostics expected
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingCallSameArgValue()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics();
        }

        [Fact()]
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (20,39): error CS8526: Cannot use local 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "rInner").WithArguments("rInner").WithLocation(20, 39),
                // (20,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter, ref rInner)").WithArguments("Program.MayAssign(ref Program.S1, ref Program.S1)", "arg2").WithLocation(20, 13),
                // (23,27): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(23, 27),
                // (23,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref inner, ref rOuter)").WithArguments("Program.MayAssign(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(23, 13)
            );
        }

        [Fact()]
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

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (20,46): error CS8352: Cannot use local 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //         MayAssign2(__arglist(ref rOuter, ref rInner));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "rInner").WithArguments("rInner").WithLocation(20, 46),
                // (20,9): error CS8350: This combination of arguments to 'Program.MayAssign2(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         MayAssign2(__arglist(ref rOuter, ref rInner));
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign2(__arglist(ref rOuter, ref rInner))").WithArguments("Program.MayAssign2(__arglist)", "__arglist").WithLocation(20, 9),
                // (23,34): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         MayAssign1(__arglist(ref inner, ref rOuter));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(23, 34),
                // (23,9): error CS8350: This combination of arguments to 'Program.MayAssign1(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         MayAssign1(__arglist(ref inner, ref rOuter));
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign1(__arglist(ref inner, ref rOuter))").WithArguments("Program.MayAssign1(__arglist)", "__arglist").WithLocation(23, 9)
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingIndex()
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

    int this[ref readonly int arg1, ref readonly S1 arg2]
    {
        get
        {
            // not possible
            // arg2 = MayWrap(ref arg1);
            return 0;
        }
    }

    int this[ref readonly S1 arg1, ref readonly S1 arg2]
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // no diagnostics
            );
        }

        [Fact()]
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

    static S1 MayWrap(ref readonly Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public Span<int> field;

        public int this[ref readonly Span<int> arg1]
        {
            get
            {
                // should be an error, arg1 is not ref-returnable, 'this' is val-returnable
                this = MayWrap(arg1);
                return 0;
            }
        }

        public int this[ref readonly S1 arg1]
        {
            get
            {
                // ok
                this = MayWrap(arg1.field);

                // this is actually OK and thus the errors in corresponding Test1 scenarios.
                this = arg1;

                return 0;
            }
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (24,29): error CS8526: Cannot use local 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "rInner").WithArguments("rInner").WithLocation(24, 29),
                // (24,22): error CS8524: This combination of arguments to 'Program.S1.this[ref readonly Program.S1]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[rInner]").WithArguments("Program.S1.this[ref readonly Program.S1]", "arg1").WithLocation(24, 22),
                // (27,29): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(27, 29),
                // (27,22): error CS8524: This combination of arguments to 'Program.S1.this[ref readonly Span<int>]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[inner]").WithArguments("Program.S1.this[ref readonly System.Span<int>]", "arg1").WithLocation(27, 22)
            );
        }

        [Fact()]
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (26,43): error CS8526: Cannot use local 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "rInner").WithArguments("rInner").WithLocation(26, 43),
                // (26,13): error CS8524: This combination of arguments to 'Program.D1.Invoke(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel1(ref rOuter, ref rInner)").WithArguments("Program.D1.Invoke(ref Program.S1, ref Program.S1)", "arg2").WithLocation(26, 13),
                // (29,31): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(29, 31),
                // (29,13): error CS8524: This combination of arguments to 'Program.D2.Invoke(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel2(ref inner, ref rOuter)").WithArguments("Program.D2.Invoke(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(29, 13)
            );
        }

        [Fact()]
        public void RefLikeObjInitializers()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (16,47): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             return new S2() { Field1 = outer, Field2 = inner };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "Field2 = inner").WithArguments("inner").WithLocation(16, 47),
                // (27,33): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             result = new S2() { Field1 = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "Field1 = inner").WithArguments("inner").WithLocation(27, 33)
            );
        }

        [Fact()]
        public void RefLikeObjInitializers1()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (18,20): error CS8352: Cannot use local 'x1' in this context because it may expose referenced variables outside of their declaration scope
                //             return x1;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "x1").WithArguments("x1").WithLocation(18, 20),
                // (29,20): error CS8352: Cannot use local 'x2' in this context because it may expose referenced variables outside of their declaration scope
                //             return x2;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "x2").WithArguments("x2").WithLocation(29, 20)
            );
        }

        [Fact()]
        public void RefLikeObjInitializersIndexer()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (16,28): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { [inner] = outer, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(16, 28),
                // (25,27): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { [outer] = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "[outer] = inner").WithArguments("inner").WithLocation(25, 27)
                );
        }

        [Fact()]
        public void RefLikeObjInitializersIndexer1()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (18,16): error CS8352: Cannot use local 'x1' in this context because it may expose referenced variables outside of their declaration scope
                //         return x1;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "x1").WithArguments("x1").WithLocation(18, 16),
                // (29,29): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         result = new S2() { [outer] = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "[outer] = inner").WithArguments("inner").WithLocation(29, 29)
                );
        }

        [Fact()]
        public void RefLikeObjInitializersNested()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (15,38): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { Field2 = {[inner] = outer} };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(15, 38),
                // (25,16): error CS8352: Cannot use local 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "x").WithArguments("x").WithLocation(25, 16),
                // (33,37): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() { Field2 = {[outer] = inner} };
                Diagnostic(ErrorCode.ERR_EscapeLocal, "[outer] = inner").WithArguments("inner").WithLocation(33, 37),
                // (67,19): warning CS0649: Field 'Program.S3.Field2' is never assigned to, and will always have its default value 
                //         public S1 Field2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field2").WithArguments("Program.S3.Field2", "").WithLocation(67, 19)
                );
        }

        [Fact()]
        public void RefLikeColInitializer()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics();
        }

        [Fact()]
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (20,54): error CS8526: Cannot use local 'rInner' in this context because it may expose referenced variables outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "rInner").WithArguments("rInner").WithLocation(20, 54),
                // (20,26): error CS8524: This combination of arguments to 'Program.Program(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg2' outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref rOuter, ref rInner)").WithArguments("Program.Program(ref Program.S1, ref Program.S1)", "arg2").WithLocation(20, 26),
                // (23,42): error CS8526: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(23, 42),
                // (23,26): error CS8524: This combination of arguments to 'Program.Program(ref Span<int>, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref inner, ref rOuter)").WithArguments("Program.Program(ref System.Span<int>, ref Program.S1)", "arg1").WithLocation(23, 26)
            );
        }

        [Fact()]
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

        static void MayAssign(ref S1 arg1, ref readonly Span<int> arg2 = default)
        {
            arg1 = MayWrap(arg2);
        }

        static S1 MayWrap(ref readonly Span<int> arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics();
        }

        [Fact()]
        public void MismatchedRefTernaryEscape()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
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

        [Fact()]
        public void MismatchedRefTernaryEscapeBlock()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (27,44): error CS8526: Cannot use local 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ternarySame2 = true ? sOuter : sInner;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sInner").WithArguments("sInner").WithLocation(27, 44),
                // (30,35): error CS8526: Cannot use local 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ternarySame2 = true ? sInner : sOuter;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sInner").WithArguments("sInner").WithLocation(30, 35),
                // (33,60): error CS8526: Cannot use local 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var ternary1 = ref true ? ref sOuter : ref sInner;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sInner").WithArguments("sInner").WithLocation(33, 60),
                // (33,36): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var ternary1 = ref true ? ref sOuter : ref sInner;
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "true ? ref sOuter : ref sInner").WithLocation(33, 36),
                // (36,47): error CS8526: Cannot use local 'sInner' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var ternary2 = ref true ? ref sInner : ref sOuter;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sInner").WithArguments("sInner").WithLocation(36, 47),
                // (36,36): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var ternary2 = ref true ? ref sInner : ref sOuter;
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "true ? ref sInner : ref sOuter").WithLocation(36, 36),
                // (39,47): error CS8526: Cannot use local 'ternarySame1' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "ternarySame1").WithArguments("ternarySame1").WithLocation(39, 47),
                // (39,36): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var ternary3 = ref true ? ref ternarySame1 : ref ternarySame2;
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "true ? ref ternarySame1 : ref ternarySame2").WithLocation(39, 36)
            );
        }

        [Fact()]
        public void StackallocEscape()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (19,26): error CS8352: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope 
                //             return true? local : default(Span<int>);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(19, 26),
                // (24,19): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //             arg = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[10]").WithArguments("System.Span<int>").WithLocation(24, 19),
                // (31,21): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //             local = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[10]").WithArguments("System.Span<int>").WithLocation(31, 21)
                );
        }

        [WorkItem(21831, "https://github.com/dotnet/roslyn/issues/21831")]
        [Fact()]
        public void LocalWithNoInitializerEscape()
        {
            var text = @"
    using System;
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (16,30): error CS8526: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             sp = MayWrap(ref local);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(16, 30),
                // (16,18): error CS8521: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             sp = MayWrap(ref local);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref local)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(16, 18),
                // (22,20): error CS8526: Cannot use local 'sp1' in this context because it may expose referenced variables outside of their declaration scope
                //             return sp1;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sp1").WithArguments("sp1").WithLocation(22, 20)
                );
        }

        [WorkItem(21858, "https://github.com/dotnet/roslyn/issues/21858")]
        [Fact()]
        public void FieldOfRefLikeEscape()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (31,28): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //                 return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "x").WithArguments("this").WithLocation(31, 28)
                );
        }

        [WorkItem(21880, "https://github.com/dotnet/roslyn/issues/21880")]
        [Fact()]
        public void MemberOfReadonlyRefLikeEscape()
        {
            var text = @"
    using System;
    public static class Program
    {
        public static void Main()
        {
            // OK, SR is readonly
            Span<int> value1 = stackalloc int[1];
            new SR().TryGet(out value1);

            // error, TryGet can write into the instance
            new SW().TryGet(out value1);
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
    }
";
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (12,33): error CS8526: Cannot use local 'value1' in this context because it may expose referenced variables outside of their declaration scope
                //             new SW().TryGet(out value1);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "value1").WithArguments("value1").WithLocation(12, 33),
                // (12,13): error CS8524: This combination of arguments to 'SW.TryGet(out Span<int>)' is disallowed because it may expose variables referenced by parameter 'result' outside of their declaration scope
                //             new SW().TryGet(out value1);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new SW().TryGet(out value1)").WithArguments("SW.TryGet(out System.Span<int>)", "result").WithLocation(12, 13)
                );
        }

        [WorkItem(21911, "https://github.com/dotnet/roslyn/issues/21911")]
        [Fact()]
        public void MemberOfReadonlyRefLikeEscapeSpans()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (17,43): error CS8352: Cannot use local 'stackAllocated' in this context because it may expose referenced variables outside of their declaration scope
                //             new NotReadOnly<int>().CopyTo(stackAllocated);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "stackAllocated").WithArguments("stackAllocated").WithLocation(17, 43),
                // (17,13): error CS8350: This combination of arguments to 'NotReadOnly<int>.CopyTo(Span<int>)' is disallowed because it may expose variables referenced by parameter 'other' outside of their declaration scope
                //             new NotReadOnly<int>().CopyTo(stackAllocated);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new NotReadOnly<int>().CopyTo(stackAllocated)").WithArguments("NotReadOnly<int>.CopyTo(System.Span<int>)", "other").WithLocation(17, 13)
                );
        }

        [WorkItem(22197, "https://github.com/dotnet/roslyn/issues/22197")]
        [Fact()]
        public void RefTernaryMustMatchValEscapes()
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
            CreateCompilationWithMscorlibAndSpan(text).VerifyDiagnostics(
                // (13,56): error CS8526: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var r1 = ref (flag1 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(13, 56),
                // (13,31): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var r1 = ref (flag1 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "flag1 ? ref global : ref local").WithLocation(13, 31),
                // (14,56): error CS8526: Cannot use local 'local' in this context because it may expose referenced variables outside of their declaration scope
                //             ref var r2 = ref (flag2 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "local").WithArguments("local").WithLocation(14, 56),
                // (14,31): error CS8525: Branches of a ref ternary operator cannot refer to variables with incompatible declaration scopes
                //             ref var r2 = ref (flag2 ? ref global : ref local);
                Diagnostic(ErrorCode.ERR_MismatchedRefEscapeInTernary, "flag2 ? ref global : ref local").WithLocation(14, 31)
            );
        }

        [WorkItem(22197, "https://github.com/dotnet/roslyn/issues/22197")]
        [Fact()]
        public void RefTernaryMustMatchValEscapes1()
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

        public static void Main()
        {
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSpan(text);
            comp.VerifyDiagnostics();

            var compiled = CompileAndVerify(comp,expectedOutput: "", verify: false);
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
    }
}
