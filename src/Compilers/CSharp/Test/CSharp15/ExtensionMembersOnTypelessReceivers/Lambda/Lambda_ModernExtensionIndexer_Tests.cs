// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_Lambda_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnFunc_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension(Func<int, int> f)
                {
                    public int this[int arg] => f(arg);
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((int x) => x * 2)[5]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void GenericIndexer_OnFunc_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension<T>(Func<T, T> f)
                {
                    public T this[T arg] => f(arg);
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((int x) => x + 7)[3]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToBadUnaryOp()
    {
        // No extension indexer is in scope. Helper returns null; legacy unbound-lambda rejection
        // produces ERR_BadUnaryOp.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (x => x)[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0023: Operator '[]' cannot be applied to operand of type 'lambda expression'
            //         _ = (x => x)[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "(x => x)[0]").WithArguments("lambda expression").WithLocation(5, 13));
    }
}
