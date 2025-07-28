// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching;

[Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
public sealed class CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzerTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer(), new CSharpIsAndCastCheckWithoutNameCodeFixProvider());

    [Fact]
    public Task TestBinaryExpression()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return [||]obj is TestFile && ((TestFile)obj).i > 0;
                }
            }
            """,
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return obj is TestFile {|Rename:file|} && file.i > 0;
                }
            }
            """);

    [Fact]
    public Task TestNotInCSharp6()
        => TestMissingAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return [||]obj is TestFile && ((TestFile)obj).i > 0;
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Fact]
    public Task TestExpressionBody()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                    => [||]obj is TestFile && ((TestFile)obj).i > 0;
            }
            """,

            """
            class TestFile
            {
                int i;
                bool M(object obj)
                    => obj is TestFile {|Rename:file|} && file.i > 0;
            }
            """);

    [Fact]
    public Task TestField()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                static object obj;

                bool M = [||]obj is TestFile && ((TestFile)obj).i > 0;
            }
            """,

            """
            class TestFile
            {
                int i;
                static object obj;

                bool M = obj is TestFile {|Rename:file|} && file.i > 0;
            }
            """);

    [Fact]
    public Task TestLambdaBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class TestFile
            {
                int i;

                void Goo(Func<bool> f) { }

                bool M(object obj)
                    => Goo(() => [||]obj is TestFile && ((TestFile)obj).i > 0, () => obj is TestFile && ((TestFile)obj).i > 0);
            }
            """,
            """
            using System;

            class TestFile
            {
                int i;

                void Goo(Func<bool> f) { }

                bool M(object obj)
                    => Goo(() => obj is TestFile {|Rename:file|} && file.i > 0, () => obj is TestFile && ((TestFile)obj).i > 0);
            }
            """);

    [Fact]
    public Task TestDefiniteAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if ([||]obj is TestFile)
                    {
                        M(((TestFile)obj).i);
                        M(((TestFile)obj).i);
                    }
                    else
                    {
                        M(((TestFile)obj).i);
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if (obj is TestFile {|Rename:file|})
                    {
                        M(file.i);
                        M(file.i);
                    }
                    else
                    {
                        M(((TestFile)obj).i);
                        M(((TestFile)obj).i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestDefiniteAssignment2()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if (!([||]obj is TestFile))
                    {
                        M(((TestFile)obj).i);
                        M(((TestFile)obj).i);
                    }
                    else
                    {
                        M(((TestFile)obj).i);
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if (!(obj is TestFile {|Rename:file|}))
                    {
                        M(((TestFile)obj).i);
                        M(((TestFile)obj).i);
                    }
                    else
                    {
                        M(file.i);
                        M(file.i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNotOnAnalyzerMatch()
        => TestMissingAsync(
            """
            class TestFile
            {
                bool M(object obj)
                {
                    if ([||]obj is TestFile)
                    {
                        var file = (TestFile)obj;
                    }
                }
            }
            """);

    [Fact]
    public Task TestNotOnNullable()
        => TestMissingAsync(
            """
            struct TestFile
            {
                bool M(object obj)
                {
                    if ([||]obj is TestFile?)
                    {
                        var i = ((TestFile?)obj).Value;
                    }
                }
            }
            """);

    [Fact]
    public Task TestComplexMatch()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return [||]M(null) is TestFile && ((TestFile)M(null)).i > 0;
                }
            }
            """,

            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return M(null) is TestFile {|Rename:file|} && file.i > 0;
                }
            }
            """);

    [Fact]
    public Task TestTrivia()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return [||]obj is TestFile && /*before*/ ((TestFile)obj) /*after*/.i > 0;
                }
            }
            """,

            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return obj is TestFile {|Rename:file|} && /*before*/ file /*after*/.i > 0;
                }
            }
            """);

    [Fact]
    public Task TestFixOnlyAfterIsCheck()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return ((TestFile)obj).i > 0 && [||]obj is TestFile && ((TestFile)obj).i > 0;
                }
            }
            """,

            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return ((TestFile)obj).i > 0 && obj is TestFile {|Rename:file|} && file.i > 0;
                }
            }
            """);

    [Fact]
    public Task TestArrayNaming()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return [||]obj is int[] && ((int[])obj) > 0;
                }
            }
            """,

            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    return obj is int[] {|Rename:v|} && v > 0;
                }
            }
            """);

    [Fact]
    public Task TestNamingConflict1()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    TestFile file = null;
                    return [||]obj is TestFile && ((TestFile)obj).i > 0;
                }
            }
            """,

            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    TestFile file = null;
                    return obj is TestFile {|Rename:file1|} && file1.i > 0;
                }
            }
            """);

    [Fact]
    public Task TestNamingConflict2()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if ([||]obj is TestFile)
                    {
                        TestFile file = null;
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if (obj is TestFile {|Rename:file1|})
                    {
                        TestFile file = null;
                        M(file1.i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNamingNoConflict1()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if ([||]obj is TestFile)
                    {
                        var v = new { file = 0 };
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if (obj is TestFile {|Rename:file|})
                    {
                        var v = new { file = 0 };
                        M(file.i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNamingNoConflict2()
        => TestInRegularAndScriptAsync(
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if ([||]obj is TestFile)
                    {
                        var v = (file: 0, x: 1);
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class TestFile
            {
                int i;
                bool M(object obj)
                {
                    if (obj is TestFile {|Rename:file|})
                    {
                        var v = (file: 0, x: 1);
                        M(file.i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNamingNoConflict3()
        => TestInRegularAndScriptAsync(
            """
            class X { public int file; }

            class TestFile
            {
                int i;
                bool M(object obj, X x)
                {
                    if ([||]obj is TestFile)
                    {
                        var v = new { x.file };
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class X { public int file; }

            class TestFile
            {
                int i;
                bool M(object obj, X x)
                {
                    if (obj is TestFile {|Rename:file|})
                    {
                        var v = new { x.file };
                        M(file.i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNamingNoConflict4()
        => TestInRegularAndScriptAsync(
            """
            class X { public int file; }

            class TestFile
            {
                int i;
                bool M(object obj, X x)
                {
                    if ([||]obj is TestFile)
                    {
                        var v = (x.file, 0);
                        M(((TestFile)obj).i);
                    }
                }
            }
            """,
            """
            class X { public int file; }

            class TestFile
            {
                int i;
                bool M(object obj, X x)
                {
                    if (obj is TestFile {|Rename:file|})
                    {
                        var v = (x.file, 0);
                        M(file.i);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51340")]
    public Task TestNoDiagnosticWhenCS0103Happens()
        => TestDiagnosticMissingAsync(
            """
            using System.Linq;
            class Bar
            {
                private void Foo()
                {
                    var objects = new SpecificThingType[100];
                    var d = from obj in objects
                            let aGenericThing = obj.Prop
                            where aGenericTh[||]ing is SpecificThingType
                            let specificThing = (SpecificThingType)aGenericThing
                            select (obj, specificThing);
                }
            }
            class SpecificThingType
            {
                public SpecificThingType Prop { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58558")]
    public Task TestInExpressionTree1()
        => TestMissingAsync(
            """
            using System.Linq.Expressions;

            object? o = null;
            Expression<Func<bool>> test = () => [||]o is int && (int)o > 5;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58558")]
    public Task TestInExpressionTree2()
        => TestMissingAsync(
            """
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    object? o = null;
                    Expression<Func<bool>> test = () => [||]o is int && (int)o > 5;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68051")]
    public Task TestNotWhenCrossingStaticLambda()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void Main(object o)
                {
                    if ([||]o is string)
                    {
                        M(static (object o) =>
                        {
                            var s = (string)o;
                        });
                    }
                }
                private void M(Action<object> value)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68051")]
    public Task TestNotWhenCrossingInstanceLambdaThatReferencesDifferentVariable()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void Main(object o)
                {
                    if ([||]o is string)
                    {
                        M((object o) =>
                        {
                            var s = (string)o;
                        });
                    }
                }
                private void M(Action<object> value)
                {
                }
            }
            """);
}
