// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpSimplifyLinqTypeCheckAndCastDiagnosticAnalyzer,
    CSharpSimplifyLinqTypeCheckAndCastCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqTypeCheckAndCast)]
public sealed partial class CSharpSimplifyLinqTypeCheckAndCast
{
    [Fact]
    public async Task TestCastMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).[|Cast|]<int>();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                
                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.[|OfType|]<int>();
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSelectWithCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).[|Select|](a => (int)a);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                
                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.[|OfType|]<int>();
                    }
                }
                """,
        }.RunAsync();
    }
}
