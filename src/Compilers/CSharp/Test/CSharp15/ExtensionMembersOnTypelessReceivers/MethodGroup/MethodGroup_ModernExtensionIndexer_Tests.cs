// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_MethodGroup_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnMethodGroup_Executes()
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
                public static int Square(int x) => x * x;

                public static void Main()
                {
                    System.Console.Write(Square[4]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "16").VerifyDiagnostics();
    }

    [Fact]
    public void GenericIndexer_OnMethodGroup_Executes()
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
                public static int Negate(int x) => -x;

                public static void Main()
                {
                    System.Console.Write(Negate[7]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "-7").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToBadIndexLHS()
    {
        // No extension indexer is in scope. Helper returns null; legacy method-group rejection
        // surfaces ERR_BadIndexLHS.
        var source = """
            public class Goo
            {
                public static int Square(int x) => x * x;

                public static void M()
                {
                    _ = Square[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (7,13): error CS0021: Cannot apply indexing with [] to an expression of type 'method group'
            //         _ = Square[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "Square[0]").WithArguments("method group").WithLocation(7, 13));
    }
}
