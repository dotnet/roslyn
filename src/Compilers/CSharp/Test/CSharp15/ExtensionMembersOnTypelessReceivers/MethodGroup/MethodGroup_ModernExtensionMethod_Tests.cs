// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_MethodGroup_ModernExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void StaticMethodGroup_FuncTarget_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension(Func<int, int> f)
                {
                    public int RunIt(int arg) => f(arg);
                }
            }

            public class Goo
            {
                public static int Square(int x) => x * x;

                public static void Main()
                {
                    System.Console.Write(Square.RunIt(6));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "36").VerifyDiagnostics();
    }

    [Fact]
    public void GenericExtensionBlock_OnFunc_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension<T>(Func<T, T> f)
                {
                    public T Apply(T arg) => f(arg);
                }
            }

            public class Goo
            {
                public static int Negate(int x) => -x;

                public static void Main()
                {
                    System.Console.Write(Negate.Apply(8));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "-8").VerifyDiagnostics();
    }

    [Fact]
    public void NoApplicableExtension_ReportsNoSuchMember()
    {
        var source = """
            public class Goo
            {
                public static int Square(int x) => x * x;
                public static void M()
                {
                    _ = Square.DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,20): error CS0117: 'method group' does not contain a definition for 'DoesNotExist'
            //         _ = Square.DoesNotExist();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "DoesNotExist").WithArguments("method group", "DoesNotExist").WithLocation(6, 20));
    }
}
