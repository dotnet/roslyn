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
    class Program
    {
        static void Main()
        {
        }

        static ref int Test1(S1 arg)
        {
            return ref (new int[1])[0];
        }

        static S1 MayWrap(ref int arg)
        {
            return default;
        }

        static ref int Test3()
        {
            int local = 42;
            return ref Test1(MayWrap(ref local));
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (21,42): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(21, 42),
                // (21,30): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref local)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(21, 30),
                // (21,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(MayWrap(ref local))").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(21, 24)
            );
        }

        [Fact()]
        public void RefLikeReturnEscape1()
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

        static S1 MayWrap(ref int arg)
        {
            return default;
        }

        static ref int Test3()
        {
            int local = 42;
            var sp = MayWrap(ref local);
            return ref Test1(sp);
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (22,30): error CS8520: Cannot use local 'sp' in this context because it may expose referenced variables outside of their declaration scope 
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sp").WithArguments("sp").WithLocation(22, 30)
            );
        }

        [Fact()]
        public void RefLikeReturnEscapeInParam()
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

        static S1 MayWrap(in int arg)
        {
            return default;
        }

        static ref int Test3()
        {
            int local = 42;
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (30,30): error CS8520: Cannot use local 'sp' in this context because it may expose referenced variables outside of their declaration scope 
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "sp").WithArguments("sp").WithLocation(30, 30),
                // (27,13): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //             sp = sp;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "sp = sp").WithLocation(27, 13)
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

        static S1 MayWrap(in int arg = 123)
        {
            return default;
        }

        static ref int Test3()
        {
            // error here
            return ref Test1(MayWrap());
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (21,30): error CS8521: Cannot use a result of 'Program.MayWrap(in int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap());
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap()").WithArguments("Program.MayWrap(in int)", "arg").WithLocation(21, 30),
                // (21,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(MayWrap());
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(MayWrap())").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(21, 24)
            );
        }

        [Fact()]
        public void RefLikeScopeEscape()
        {
            var text = @"
    class Program
    {
        static void Main()
        {
            int outer = 1;

            S1 x = MayWrap(ref outer);

            {
                int inner = 1;

                // valid
                x = MayWrap(ref outer);
    
                // error
                x = MayWrap(ref inner);
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (17,33): error CS8520: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope 
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(17, 33),
                // (17,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(17, 21)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeReturnable()
        {
            var text = @"
    class Program
    {
        static void Main()
        {
            int outer = 1;

            // make x returnable
            S1 x = default;

            {
                int inner = 1;

                // valid
                x = MayWrap(ref outer);
    
                // error
                x = MayWrap(ref inner);
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (15,33): error CS8168: Cannot return local 'outer' by reference because it is not a ref local
                //                 x = MayWrap(ref outer);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "outer").WithArguments("outer").WithLocation(15, 33),
                // (15,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref outer);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref outer)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(15, 21),
                // (18,33): error CS8168: Cannot return local 'inner' by reference because it is not a ref local
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "inner").WithArguments("inner").WithLocation(18, 33),
                // (18,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(18, 21)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeThis()
        {
            var text = @"
    class Program
    {
        static void Main()
        {
            int outer = 1;

            S1 x = MayWrap(ref outer);

            {
                int inner = 1;

                // valid
                x = S1.NotSlice(1);

                // valid
                x = MayWrap(ref outer).Slice(1);
    
                // error
                x = MayWrap(ref inner).Slice(1);
            }
        }

        static S1 MayWrap(ref int arg)
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (20,33): error CS8520: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope 
                //                 x = MayWrap(ref inner).Slice(1);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(20, 33),
                // (20,21): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = MayWrap(ref inner).Slice(1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(20, 21)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeThisRef()
        {
            var text = @"
class Program
{
    static void Main()
    {
        int outer = 1;

        ref S1 x = ref MayWrap(ref outer)[0];

        {
            int inner = 1;

            // valid
            x[0] = MayWrap(ref outer).Slice(1)[0];

            // PROTOTYPE(span): we may relax this. The LHS indexer cannot possibly return itself byref
            //                  ref-returning calls do not need to include receiver
            // error
            x[0] = MayWrap(ref inner).Slice(1)[0];

            // PROTOTYPE(span): we may relax this. The LHS indexer cannot possibly return itself byref
            //                  ref-returning calls do not need to include receiver
            // error
            x[x] = MayWrap(ref inner).Slice(1)[0];

            // error
            x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
        }
    }

    static S1 MayWrap(ref int arg)
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (19,32): error CS8520: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope 
                //             x[0] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(19, 32),
                // (19,20): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x[0] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(19, 20),
                // (24,32): error CS8520: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope 
                //             x[x] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(24, 32),
                // (24,20): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x[x] = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(24, 20),
                // (27,50): error CS8520: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope 
                //             x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(27, 50),
                // (27,38): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x.ReturnsRefArg(ref x) = MayWrap(ref inner).Slice(1)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(27, 38)
            );
        }

        [Fact()]
        public void RefLikeScopeEscapeField()
        {
            var text = @"
class Program
{
    static void Main()
    {
        int outer = 1;

        S1 x = MayWrap(ref outer);

        {
            int inner = 1;

            // valid
            x.field = MayWrap(ref outer).Slice(1).field;

            // error
            x.field = MayWrap(ref inner).Slice(1).field;
        }
    }

    static S1 MayWrap(ref int arg)
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (17,35): error CS8520: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope 
                //             x.field = MayWrap(ref inner).Slice(1).field;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(17, 35),
                // (17,23): error CS8521: Cannot use a result of 'Program.MayWrap(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             x.field = MayWrap(ref inner).Slice(1).field;
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref inner)").WithArguments("Program.MayWrap(ref int)", "arg").WithLocation(17, 23)
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // no diagnostics expected
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingCallSameArgValue()
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
            MayAssign(ref rOuter);

            // valid
            MayAssign(ref rInner);
        }

        static void MayAssign(ref S1 arg1)
        {
            // should be an error
            arg1 = MayWrap(ref arg1.field);
        }

        static S1 MayWrap(ref int arg)
        {
            return default;
        }

        ref struct S1
        {
            public int field;
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (16,27): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             MayAssign(ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(16, 27),
                // (16,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter)").WithArguments("Program.MayAssign(ref Program.S1)", "arg1").WithLocation(16, 13)
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingCall()
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
            MayAssign(ref rOuter, ref rOuter);

            // error
            MayAssign(ref rOuter, ref rInner);

            // error
            MayAssign(ref inner, ref rOuter);
        }

        static void MayAssign(ref int arg1, ref S1 arg2)
        {
            arg2 = MayWrap(ref arg1);
        }

        static void MayAssign(ref S1 arg1, ref S1 arg2)
        {
            arg1 = arg2;
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (16,27): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             MayAssign(ref rOuter, ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(16, 27),
                // (16,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref rOuter, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter, ref rOuter)").WithArguments("Program.MayAssign(ref Program.S1, ref Program.S1)", "arg1").WithLocation(16, 13),
                // (19,27): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(19, 27),
                // (19,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter, ref rInner)").WithArguments("Program.MayAssign(ref Program.S1, ref Program.S1)", "arg1").WithLocation(19, 13),
                // (22,27): error CS8168: Cannot return local 'inner' by reference because it is not a ref local
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "inner").WithArguments("inner").WithLocation(22, 27),
                // (22,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref int, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref inner, ref rOuter)").WithArguments("Program.MayAssign(ref int, ref Program.S1)", "arg1").WithLocation(22, 13)
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // no diagnostics
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingIndexOnRefLike()
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
        rOuter.field = 1;

        int inner = 1;
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

    static S1 MayWrap(in int arg)
    {
        return default;
    }

    ref struct S1
    {
        public int field;

        public int this[in int arg1]
        {
            get
            {
                this = MayWrap(arg1);
                return 0;
            }
        }

        public int this[in S1 arg1]
        {
            get
            {
                this = MayWrap(arg1.field);
                this = arg1;
                return 0;
            }
        }
    }
}
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (17,29): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //         int dummy1 = rOuter[rOuter];
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(17, 29),
                // (17,22): error CS8524: This combination of arguments to 'Program.S1.this[in Program.S1]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy1 = rOuter[rOuter];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[rOuter]").WithArguments("Program.S1.this[in Program.S1]", "arg1").WithLocation(17, 22),
                // (23,29): error CS8168: Cannot return local 'rInner' by reference because it is not a ref local
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rInner").WithArguments("rInner").WithLocation(23, 29),
                // (23,22): error CS8524: This combination of arguments to 'Program.S1.this[in Program.S1]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy3 = rOuter[rInner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[rInner]").WithArguments("Program.S1.this[in Program.S1]", "arg1").WithLocation(23, 22),
                // (26,29): error CS8168: Cannot return local 'inner' by reference because it is not a ref local
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "inner").WithArguments("inner").WithLocation(26, 29),
                // (26,22): error CS8524: This combination of arguments to 'Program.S1.this[in int]' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //         int dummy4 = rOuter[inner];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "rOuter[inner]").WithArguments("Program.S1.this[in int]", "arg1").WithLocation(26, 22)
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingCtor()
        {
            var text = @"
    class Program
    {
        static void Main()
        {
        }

        delegate void D1(ref S1 arg1, ref S1 arg2);
        delegate void D2(ref int arg1, ref S1 arg2);

        void Test1()
        {
            S1 rOuter = default;

            int inner = 1;
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

        static void MayAssign(ref int arg1, ref S1 arg2)
        {
            arg2 = MayWrap(ref arg1);
        }

        static void MayAssign(ref S1 arg1, ref S1 arg2)
        {
            arg1 = arg2;
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (22,31): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             MayAssignDel1(ref rOuter, ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(22, 31),
                // (22,13): error CS8524: This combination of arguments to 'Program.D1.Invoke(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel1(ref rOuter, ref rOuter)").WithArguments("Program.D1.Invoke(ref Program.S1, ref Program.S1)", "arg1").WithLocation(22, 13),
                // (25,31): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(25, 31),
                // (25,13): error CS8524: This combination of arguments to 'Program.D1.Invoke(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssignDel1(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel1(ref rOuter, ref rInner)").WithArguments("Program.D1.Invoke(ref Program.S1, ref Program.S1)", "arg1").WithLocation(25, 13),
                // (28,31): error CS8168: Cannot return local 'inner' by reference because it is not a ref local
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "inner").WithArguments("inner").WithLocation(28, 31),
                // (28,13): error CS8524: This combination of arguments to 'Program.D2.Invoke(ref int, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssignDel2(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssignDel2(ref inner, ref rOuter)").WithArguments("Program.D2.Invoke(ref int, ref Program.S1)", "arg1").WithLocation(28, 13)
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingDelegate()
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
            var dummy1 = new Program(ref rOuter, ref rOuter);

            // error
            var dummy2 = new Program(ref rOuter, ref rInner);

            // error
            var dummy3 = new Program(ref inner, ref rOuter);
        }

        Program(ref int arg1, ref S1 arg2)
        {
            arg2 = MayWrap(ref arg1);
        }

        Program(ref S1 arg1, ref S1 arg2)
        {
            arg1 = arg2;
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
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (16,42): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             var dummy1 = new Program(ref rOuter, ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(16, 42),
                // (16,26): error CS8524: This combination of arguments to 'Program.Program(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             var dummy1 = new Program(ref rOuter, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref rOuter, ref rOuter)").WithArguments("Program.Program(ref Program.S1, ref Program.S1)", "arg1").WithLocation(16, 26),
                // (19,42): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(19, 42),
                // (19,26): error CS8524: This combination of arguments to 'Program.Program(ref Program.S1, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             var dummy2 = new Program(ref rOuter, ref rInner);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref rOuter, ref rInner)").WithArguments("Program.Program(ref Program.S1, ref Program.S1)", "arg1").WithLocation(19, 26),
                // (22,42): error CS8168: Cannot return local 'inner' by reference because it is not a ref local
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "inner").WithArguments("inner").WithLocation(22, 42),
                // (22,26): error CS8524: This combination of arguments to 'Program.Program(ref int, ref Program.S1)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             var dummy3 = new Program(ref inner, ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new Program(ref inner, ref rOuter)").WithArguments("Program.Program(ref int, ref Program.S1)", "arg1").WithLocation(22, 26)
            );
        }

        [Fact()]
        public void RefLikeEscapeMixingCallOptionalIn()
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
            S1 rInner = MayWrap(inner);

            // error
            MayAssign(ref rOuter);

            // valid, optional arg is of the same escape level
            MayAssign(ref rInner);
        }

        static void MayAssign(ref S1 arg1, in int arg2 = 42)
        {
            arg1 = MayWrap(arg2);
        }

        static S1 MayWrap(in int arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (16,27): error CS8168: Cannot return local 'rOuter' by reference because it is not a ref local
                //             MayAssign(ref rOuter);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "rOuter").WithArguments("rOuter").WithLocation(16, 27),
                // (16,13): error CS8524: This combination of arguments to 'Program.MayAssign(ref Program.S1, in int)' is disallowed because it may expose variables referenced by parameter 'arg1' outside of their declaration scope
                //             MayAssign(ref rOuter);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MayAssign(ref rOuter)").WithArguments("Program.MayAssign(ref Program.S1, in int)", "arg1").WithLocation(16, 13)
            );
        }
    }
}
