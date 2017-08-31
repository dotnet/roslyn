// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.TargetEnumeration)]
    public class TargetTypedEnumTests : CSharpTestBase
    {
        [Fact]
        public void TestGoodTargetTypedEnums()
        {
            var source = @"
using static System.Console;
enum E { A, B }
class C
{
    public static void Main()
    {
        void Test(E e, bool b = true)
        {
            switch (e)
            {
                case A: Write(1); break;
                case B: Write(2); break;
            }

            switch (e)
            {
                case A when b: Write(3); break;
                case B when b: Write(4); break;
            }

            if (e == A) Write(5);
            else if (B == e) Write(6);

            if (e != B) Write(7);
            else if (A != e) Write(8);
        }

        void TestNullable(E? e, bool b = true)
        {
            switch (e)
            {
                case A: Write(1); break;
                case B: Write(2); break;
            }

            switch (e)
            {
                case A when b: Write(3); break;
                case B when b: Write(4); break;
            }

            if (e == A) Write(5);
            else if (B == e) Write(6);

            if (e != B) Write(7);
            else if (A != e) Write(8);
        }

        Test(E.A);
        TestNullable(E.B);
    }

}
";

            var compilation = CreateStandardCompilation(
                source,
                options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "13572468");
        }

        [Fact]
        public void TestMissingNames()
        {
            var source = @"
enum E { A, B }
class C
{
    public static void Main()
    {
        void Test(E e, bool b = true)
        {
            switch (e)
            {
                case err1: break;
            }

            switch (e)
            {
                case err2 when b: break;
            }

            if (err3 == err4) {}

            if (err5 != err6) {}
        }

        Test(E.A);
    }
}
";

            var compilation = CreateStandardCompilation(
                source,
                options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (11,22): error CS0103: The name 'err1' does not exist in the current context
                //                 case err1: break;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "err1").WithArguments("err1").WithLocation(11, 22),
                // (16,22): error CS0103: The name 'err2' does not exist in the current context
                //                 case err2 when b: break;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "err2").WithArguments("err2").WithLocation(16, 22),
                // (19,17): error CS0103: The name 'err3' does not exist in the current context
                //             if (err3 == err4) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "err3").WithArguments("err3").WithLocation(19, 17),
                // (19,25): error CS0103: The name 'err4' does not exist in the current context
                //             if (err3 == err4) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "err4").WithArguments("err4").WithLocation(19, 25),
                // (21,17): error CS0103: The name 'err5' does not exist in the current context
                //             if (err5 != err6) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "err5").WithArguments("err5").WithLocation(21, 17),
                // (21,25): error CS0103: The name 'err6' does not exist in the current context
                //             if (err5 != err6) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "err6").WithArguments("err6").WithLocation(21, 25),
                // (16,35): warning CS0162: Unreachable code detected
                //                 case err2 when b: break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(16, 35)
                );
        }

        [Fact]
        public void TestExistingNames()
        {
            var source = @"
enum E { A, B }
class A {}
class C
{
    public static void Main()
    {
        void Test(E e)
        {
            switch(e)
            {
                case A: break;
            }
        }
        Test(E.A);
    }
}
";

            var compilation = CreateStandardCompilation(
                source,
                options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (13,22): error CS0119: 'A' is a type, which is not valid in the given context
                //                 case A: break;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "A").WithArguments("A", "type").WithLocation(12, 22)
                );
        }
    }
}
