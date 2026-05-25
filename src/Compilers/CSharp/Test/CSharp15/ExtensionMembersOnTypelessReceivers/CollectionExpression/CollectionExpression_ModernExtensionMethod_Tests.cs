// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_CollectionExpression_ModernExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void Method_OnCollectionExpression_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension(IEnumerable<int> source)
                {
                    public int Sum()
                    {
                        int s = 0;
                        foreach (var x in source) s += x;
                        return s;
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Sum());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "6").VerifyDiagnostics();
    }

    [Fact]
    public void GenericMethod_OnCollectionExpression_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IEnumerable<T> source)
                {
                    public int CountIt()
                    {
                        int n = 0;
                        foreach (var _ in source) n++;
                        return n;
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].CountIt());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }
}
