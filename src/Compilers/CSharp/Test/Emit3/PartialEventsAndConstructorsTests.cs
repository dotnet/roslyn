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
    }
}
