// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpSimplifyLinqTypeCheckAndCastDiagnosticAnalyzer,
    CSharpSimplifyLinqTypeCheckAndCastCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqTypeCheckAndCast)]
public sealed partial class CSharpSimplifyLinqTypeCheckAndCast
{
    [Fact]
    public Task TestCastMethod()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCastMethod_Incorrect1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.{|CS1061:Where1|}(a => a is int).Cast<int>();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestCastMethod_Incorrect2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => {|CS0103:b|} is int).Cast<int>();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestCastMethod_Incorrect3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is string).Cast<int>();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestCastMethod_Incorrect4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).{|CS1061:Cast1<int>|}();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestCastMethod_Incorrect5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).{|CS1501:Cast<int>|}(0);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestCastMethod_Incorrect6()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).{|CS0411:Cast|}();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestCastMethod_Incorrect7()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where({|CS0103:s|}).Cast<int>();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestSelectWithCast_Incorrect1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.{|CS1061:Where1|}(a => a is int).Select(a => (int)a);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast_Incorrect2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => {|CS0103:b|} is int).Select(a => (int)a);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast_Incorrect3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is string).Select(a => (int)a);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast_Incorrect4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).{|CS1061:Select1|}(a => (int)a);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast_Incorrect5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).Select(a => (int){|CS0103:b|});
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast_Incorrect6()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).Select({|CS0103:a|});
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestSelectWithCast_Incorrect7()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;

                class C
                {
                    void M(object[] objs)
                    {
                        var y = objs.Where(a => a is int).Select<object, int>(a => (int)a);
                    }
                }
                """,
        }.RunAsync();
}
