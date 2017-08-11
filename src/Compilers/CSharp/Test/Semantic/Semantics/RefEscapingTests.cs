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
        public void SimpleRefLikeReturnEscape()
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

        static S1 Test2(ref int arg)
        {
            return default;
        }

        static ref int Test3()
        {
            int local = 42;
            return ref Test1(Test2(ref local));
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (21,40): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref Test1(Test2(ref local));
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(21, 40),
                // (21,30): error CS8521: Cannot use a result of 'Program.Test2(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(Test2(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test2(ref local)").WithArguments("Program.Test2(ref int)", "arg").WithLocation(21, 30),
                // (21,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(Test2(ref local));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(Test2(ref local))").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(21, 24)
            );
        }

        [Fact()]
        public void SimpleRefLikeReturnEscape1()
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

        static S1 Test2(ref int arg)
        {
            return default;
        }

        static ref int Test3()
        {
            int local = 42;
            var sp = Test2(ref local);
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
        public void SimpleRefLikeReturnEscapeInParam()
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

        static S1 Test2(in int arg)
        {
            return default;
        }

        static ref int Test3()
        {
            int local = 42;
            var sp = Test2(local);

            // not an error
            sp = Test2(local);

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
        public void SimpleRefLikeReturnEscapeInParamOptional()
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

        static S1 Test2(in int arg = 123)
        {
            return default;
        }

        static ref int Test3()
        {
            // error here
            return ref Test1(Test2());
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (21,30): error CS8521: Cannot use a result of 'Program.Test2(in int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(Test2());
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test2()").WithArguments("Program.Test2(in int)", "arg").WithLocation(21, 30),
                // (21,24): error CS8521: Cannot use a result of 'Program.Test1(Program.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Test1(Test2());
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(Test2())").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(21, 24)
            );
        }

        [Fact()]
        public void SimpleRefLikeScopeEscape()
        {
            var text = @"
    class Program
    {
        static void Main()
        {
            S1 x = default;

            int outer = 1;

            {
                int inner = 1;

                // valid
                x = Test1(ref outer);
    
                // error
                x = Test1(ref inner);
            }
        }

        static S1 Test1(ref int arg)
        {
            return default;
        }

        ref struct S1
        {
        }
    }
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (14,31): error CS8168: Cannot return local 'outer' by reference because it is not a ref local
                //                 x = Test1(ref outer);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "outer").WithArguments("outer").WithLocation(14, 31),
                // (14,21): error CS8521: Cannot use a result of 'Program.Test1(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = Test1(ref outer);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref outer)").WithArguments("Program.Test1(ref int)", "arg").WithLocation(14, 21),
                // (17,31): error CS8168: Cannot return local 'inner' by reference because it is not a ref local
                //                 x = Test1(ref inner);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "inner").WithArguments("inner").WithLocation(17, 31),
                // (17,21): error CS8521: Cannot use a result of 'Program.Test1(ref int)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //                 x = Test1(ref inner);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Test1(ref inner)").WithArguments("Program.Test1(ref int)", "arg").WithLocation(17, 21)

            );
        }
    }
}
