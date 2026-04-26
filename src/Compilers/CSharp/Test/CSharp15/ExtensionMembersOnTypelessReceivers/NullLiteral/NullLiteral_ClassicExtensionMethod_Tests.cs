// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NullLiteral_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void NullToReferenceType_Executes()
    {
        var source = """
            public static class Ext
            {
                public static string OrEmpty(this string s) => s ?? "empty";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null.OrEmpty());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void NullToNullableValueType_Executes()
    {
        var source = """
            public static class Ext
            {
                public static int OrZero(this int? n) => n ?? 0;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null.OrZero());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_PrefersMoreSpecific()
    {
        // null is convertible to both string and object; overload resolution picks the more
        // specific candidate (string).
        var source = """
            public static class Ext
            {
                public static string Tag(this object o) => "object";
                public static string Tag(this string s) => "string";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null.Tag());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "string").VerifyDiagnostics();
    }

    [Fact]
    public void Ambiguous_TwoUnrelatedReferenceTypes()
    {
        // null is convertible to two unrelated reference types; overload resolution reports an
        // ambiguity.
        var source = """
            public class Bag1 { }
            public class Bag2 { }

            public static class ExtA
            {
                public static int M(this Bag1 b) => 1;
            }
            public static class ExtB
            {
                public static int M(this Bag2 b) => 2;
            }

            public class Goo
            {
                public static void Main()
                {
                    _ = null.M();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (17,18): error CS0121: The call is ambiguous between the following methods or properties: 'ExtA.M(Bag1)' and 'ExtB.M(Bag2)'
            //         _ = null.M();
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("ExtA.M(Bag1)", "ExtB.M(Bag2)").WithLocation(17, 18));
    }

    [Fact]
    public void NoExtensionInScope_FallsBackToBadUnaryOp()
    {
        // No extension method is in scope. The typeless-receiver feature only engages when at
        // least one extension candidate exists; without one, the helper returns null and the
        // legacy "operator '.' cannot be applied to <null>" diagnostic fires.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = null.DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
            //         _ = null.DoesNotExist();
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.DoesNotExist").WithArguments(".", "<null>").WithLocation(5, 13));
    }

    [Fact]
    public void NullLiteralReceiver_NullableEnabled_NoNRTWarning()
    {
        // Inside a nullable-enabled context, `null.Ext()` should not warn about possible
        // null dereference: extension method calls do not dereference the receiver and the
        // user explicitly wrote `null`.
        var source = """
            #nullable enable
            public static class Ext
            {
                public static string OrEmpty(this string? s) => s ?? "empty";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null.OrEmpty());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void NullConditionalOnNullLiteral_RejectedByConditionalAccess()
    {
        // `null?.Ext()` is rejected at the `?.` operator level - the null-conditional
        // operator itself disallows a literal-null operand and emits CS0023 before the
        // typeless-receiver feature path is consulted.
        var source = """
            public static class Ext
            {
                public static int Side(this string s) => 1;
            }

            public class Goo
            {
                public static void M()
                {
                    int? r = null?.Side();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (10,22): error CS0023: Operator '?' cannot be applied to operand of type '<null>'
            //         int? r = null?.Side();
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "<null>").WithLocation(10, 22));
    }

    [Fact]
    public void NullSuppressed_OnNullLiteralReceiver_Executes()
    {
        // The null-forgiving operator `!` on a null-literal receiver doesn't change the
        // receiver kind - it still binds as a typeless null literal and the extension call
        // succeeds in a nullable-enabled context.
        var source = """
            #nullable enable
            public static class Ext
            {
                public static string OrEmpty(this string? s) => s ?? "empty";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null!.OrEmpty());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }
}
