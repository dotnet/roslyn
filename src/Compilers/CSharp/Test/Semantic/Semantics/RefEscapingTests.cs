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
            );
        }
    }
}
