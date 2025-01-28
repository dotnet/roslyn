// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class PartialEventsAndConstructorsTests : CSharpTestBase
{
    [Fact]
    public void ReturningPartialType_LocalFunction_InMethod()
    {
        var source = """
            class @partial
            {
                static void Main()
                {
                    System.Console.Write(F().GetType().Name);
                    partial F() => new();
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: "partial").VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (5,30): error CS0103: The name 'F' does not exist in the current context
            //         System.Console.Write(F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "F").WithArguments("F").WithLocation(5, 30),
            // (5,50): error CS1513: } expected
            //         System.Console.Write(F().GetType().Name);
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 50),
            // (6,9): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method or property return type.
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(6, 9),
            // (6,17): error CS1520: Method must have a return type
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "F").WithLocation(6, 17),
            // (6,24): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "new()").WithLocation(6, 24),
            // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReturningPartialType_LocalFunction_TopLevel()
    {
        var source = """
            System.Console.Write(F().GetType().Name);
            partial F() => new();
            class @partial;
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: "partial").VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (1,22): error CS0103: The name 'F' does not exist in the current context
            // System.Console.Write(F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "F").WithArguments("F").WithLocation(1, 22),
            // (2,9): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
            // partial F() => new();
            Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "F").WithLocation(2, 9),
            // (2,10): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "() => new()").WithLocation(2, 10)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReturningPartialType_Method()
    {
        var source = """
            class C
            {
                partial F() => new();
                static void Main()
                {
                    System.Console.Write(new C().F().GetType().Name);
                }
            }
            class @partial;
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: "partial").VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method or property return type.
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (3,13): error CS1520: Method must have a return type
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "F").WithLocation(3, 13),
            // (3,20): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "new()").WithLocation(3, 20),
            // (6,38): error CS1061: 'C' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            //         System.Console.Write(new C().F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("C", "F").WithLocation(6, 38)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }
}
