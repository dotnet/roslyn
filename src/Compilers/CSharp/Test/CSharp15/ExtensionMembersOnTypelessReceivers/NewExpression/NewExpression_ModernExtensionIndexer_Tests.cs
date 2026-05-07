// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NewExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnNewParameterless_Executes()
    {
        var source = """
            public class C
            {
                public int Value = 42;
            }

            public static class Ext
            {
                extension(C c)
                {
                    public int this[long _] => c.Value;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(new()[0L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_OnNewWithArgs_Executes()
    {
        var source = """
            public class C
            {
                public int Value;
                public C(int v) { Value = v; }
            }

            public static class Ext
            {
                extension(C c)
                {
                    public int this[long offset] => c.Value + (int)offset;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(new(7)[3L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToImplicitObjectCreationNoTargetType()
    {
        // No extension indexer is in scope. Helper returns null; legacy implicit-object-creation
        // rejection surfaces.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = new()[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS8754: There is no target type for 'new()'
            //         _ = new()[0];
            Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(5, 13));
    }
}
