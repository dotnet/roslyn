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
                // (21,30): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //             return ref Test1(Test2(ref local));
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "Test2(ref local)").WithLocation(21, 30),
                // (21,24): error CS8164: Cannot return by reference a result of 'Program.Test1(Program.S1)' because the argument passed to parameter 'arg' cannot be returned by reference
                //             return ref Test1(Test2(ref local));
                Diagnostic(ErrorCode.ERR_RefReturnCall, "Test1(Test2(ref local))").WithArguments("Program.Test1(Program.S1)", "arg").WithLocation(21, 24)
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
                // (22,30): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //             return ref Test1(sp);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "sp").WithLocation(22, 30)
            );
        }
    }
}
