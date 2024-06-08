// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.GoToAdjacentMember;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GoToAdjacentMember;

[Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
public class CSharpGoToAdjacentMemberTests : AbstractGoToAdjacentMemberTests
{
    protected override string LanguageName => LanguageNames.CSharp;
    protected override ParseOptions DefaultParseOptions => CSharpParseOptions.Default;

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task EmptyFile()
    {
        var code = @"$$";
        Assert.Null(await GetTargetPositionAsync(code, next: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task ClassWithNoMembers()
    {
        var code = """
            class C
            {
            $$
            }
            """;
        Assert.Null(await GetTargetPositionAsync(code, next: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task BeforeClassWithMember()
    {
        var code = """
            $$
            class C
            {
                [||]void M() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task AfterClassWithMember()
    {
        var code = """
            class C
            {
                [||]void M() { }
            }

            $$
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task BetweenClasses()
    {
        var code = """
            class C1
            {
                void M() { }
            }

            $$

            class C2
            {
                [||]void M() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task BetweenClassesPrevious()
    {
        var code = """
            class C1
            {
                [||]void M() { }
            }

            $$

            class C2
            {
                void M() { }
            }
            """;

        await AssertNavigatedAsync(code, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromFirstMemberToSecond()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromSecondToFirst()
    {
        var code = """
            class C
            {
                [||]void M1() { }
                $$void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextWraps()
    {
        var code = """
            class C
            {
                [||]void M1() { }
                $$void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PreviousWraps()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task DescendsIntoNestedType()
    {
        var code = """
            class C
            {
                $$void M1() { }

                class N
                {
                    [||]void M2() { }
                }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtConstructor()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]public C() { }
            }
            """;
        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtDestructor()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]~C() { }
            }
            """;
        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtOperator()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]static C operator+(C left, C right) { throw new System.NotImplementedException(); }
            }
            """;
        await AssertNavigatedAsync(code, next: true);
    }
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtField()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]int F;
            }
            """;
        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtFieldlikeEvent()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]event System.EventHandler E;
            }
            """;
        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtAutoProperty()
    {
        var code = """
            class C
            {
                $$void M1() { }
                [||]int P { get; set ; }
            }
            """;
        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtPropertyWithAccessors()
    {
        var code = """
            class C
            {
                $$void M1() { }

                [||]int P
                {
                    get { return 42; }
                    set { }
                }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task SkipsPropertyAccessors()
    {
        var code = """
            class C
            {
                void M1() { }

                $$int P
                {
                    get { return 42; }
                    set { }
                }

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromInsideAccessor()
    {
        var code = """
            class C
            {
                void M1() { }

                int P
                {
                    get { return $$42; }
                    set { }
                }

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtIndexerWithAccessors()
    {
        var code = """
            class C
            {
                $$void M1() { }

                [||]int this[int i]
                {
                    get { return 42; }
                    set { }
                }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task SkipsIndexerAccessors()
    {
        var code = """
            class C
            {
                void M1() { }

                $$int this[int i]
                {
                    get { return 42; }
                    set { }
                }

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtEventWithAddRemove()
    {
        var code = """
            class C
            {
                $$void M1() { }

                [||]event EventHandler E
                {
                    add { }
                    remove { }
                }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task SkipsEventAddRemove()
    {
        var code = """
            class C
            {
                void M1() { }

                $$event EventHandler E
                {
                    add { }
                    remove { }
                }

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromInsideMethod()
    {
        var code = """
            class C
            {
                void M1()
                {
                    $$System.Console.WriteLine();
                }

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextFromBetweenMethods()
    {
        var code = """
            class C
            {
                void M1() { }

                $$

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PreviousFromBetweenMethods()
    {
        var code = """
            class C
            {
                [||]void M1() { }

                $$

                void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextFromBetweenMethodsInTrailingTrivia()
    {
        var code = """
            class C
            {
                void M1()
                {
                } $$

                [||]void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PreviousFromBetweenMethodsInTrailingTrivia()
    {
        var code = """
            class C
            {
                [||]void M1()
                {
                } $$

                void M2() { }
            }
            """;

        await AssertNavigatedAsync(code, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtExpressionBodiedMember()
    {
        var code = """
            class C
            {
                int M1() => $$42;

                [||]int M2() => 42;
            }
            """;

        await AssertNavigatedAsync(code, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/10588")]
    public async Task PreviousFromInsideCurrent()
    {
        var code = """
            class C
            {
                [||]void M1()
                {
                    Console.WriteLine($$);
                }

                void M2()
                {
                }
            }
            """;

        await AssertNavigatedAsync(code, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextInScript()
    {
        var code = """
            $$void M1() { }

            [||]void M2() { }
            """;

        await AssertNavigatedAsync(code, next: true, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PrevInScript()
    {
        var code = """
            [||]void M1() { }

            $$void M2() { }
            """;

        await AssertNavigatedAsync(code, next: false, sourceCodeKind: SourceCodeKind.Script);
    }
}
