// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_TupleExpression_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void TupleAllTyped_Executes()
    {
        // (1, 2) has a natural type (int, int). It still routes through the typeless extension
        // path because BoundTupleLiteral has Type=null at the bound-tree level until target-typed.
        var source = """
            public static class Ext
            {
                public static int Sum(this (int A, int B) p) => p.A + p.B;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((3, 4).Sum());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "7").VerifyDiagnostics();
    }

    [Fact]
    public void TupleWithTypelessElement_Executes()
    {
        // The tuple has a typeless element (null), so the whole tuple has no natural type.
        // Target-typed against (string, int), it binds.
        var source = """
            public static class Ext
            {
                public static int Combine(this (string s, int n) p) => (p.s ?? "").Length + p.n;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((null, 42).Combine());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void InExtension_OnTypelessTupleReceiver_Executes()
    {
        // ValueTuple is a struct so an `in this` extension is valid. A typeless tuple
        // receiver (one with a `null` element) is converted to the target tuple struct
        // type and passed by readonly reference.
        var source = """
            public static class Ext
            {
                public static int Combine(in this (string s, int n) t) => (t.s ?? "x").Length + t.n;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((null, 41).Combine());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void TupleNoApplicableExtension_ReportsNoSuchMember()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (1, 2).DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,20): error CS1061: '(int, int)' does not contain a definition for 'DoesNotExist' and no accessible extension method 'DoesNotExist' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
            //         _ = (1, 2).DoesNotExist();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "DoesNotExist").WithArguments("(int, int)", "DoesNotExist").WithLocation(5, 20));
    }
}
