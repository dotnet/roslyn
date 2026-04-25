// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NewExpression_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void NewParenless_Executes()
    {
        var source = """
            public class Bag
            {
                public int N;
            }

            public static class Ext
            {
                public static int Doubled(this Bag b) => b.N * 2;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(new().Doubled());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact]
    public void NewWithArgs_Executes()
    {
        var source = """
            public class Bag
            {
                public int N;
                public Bag(int n) { N = n; }
            }

            public static class Ext
            {
                public static int Doubled(this Bag b) => b.N * 2;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(new(7).Doubled());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "14").VerifyDiagnostics();
    }

    [Fact]
    public void NewWithArgs_ConstructorOverloadResolution_Executes()
    {
        var source = """
            public class Bag
            {
                public int N;
                public Bag() { N = 0; }
                public Bag(int n, int m) { N = n + m; }
            }

            public static class Ext
            {
                public static int N(this Bag b) => b.N;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(new(3, 4).N());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "7").VerifyDiagnostics();
    }

    [Fact]
    public void NoApplicableExtension_ReportsNoSuchMember()
    {
        // No extension is in scope. The receiver enters extension lookup via the typeless path,
        // finds nothing, and reports "new() does not contain a definition for ...".
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = new().DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,19): error CS0117: 'new()' does not contain a definition for 'DoesNotExist'
            //         _ = new().DoesNotExist();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "DoesNotExist").WithArguments("new()", "DoesNotExist").WithLocation(5, 19));
    }
}
