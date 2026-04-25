// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_Lambda_ModernExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void FuncTarget_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension(Func<int, int> f)
                {
                    public int Apply(int arg) => f(arg);
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((x => x * 3).Apply(4));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void GenericExtensionBlock_Executes()
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
                public static void Main()
                {
                    System.Console.Write(((int x) => x + 100).Apply(5));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "105").VerifyDiagnostics();
    }

    [Fact]
    public void ActionExtension_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension(Action a)
                {
                    public void RunIt() { a(); a(); a(); }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    int n = 0;
                    (() => n++).RunIt();
                    System.Console.Write(n);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void NoApplicableExtension_ReportsNoSuchMember()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (x => x).DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,22): error CS0117: 'lambda expression' does not contain a definition for 'DoesNotExist'
            //         _ = (x => x).DoesNotExist();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "DoesNotExist").WithArguments("lambda expression", "DoesNotExist").WithLocation(5, 22));
    }
}
