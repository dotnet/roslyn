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
public sealed class CSharpGoToAdjacentMemberTests : AbstractGoToAdjacentMemberTests
{
    protected override string LanguageName => LanguageNames.CSharp;
    protected override ParseOptions DefaultParseOptions => CSharpParseOptions.Default;

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task EmptyFile()
    {
        Assert.Null(await GetTargetPositionAsync(@"$$", next: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task ClassWithNoMembers()
    {
        Assert.Null(await GetTargetPositionAsync("""
            class C
            {
            $$
            }
            """, next: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task BeforeClassWithMember()
    {
        await AssertNavigatedAsync("""
            $$
            class C
            {
                [||]void M() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task AfterClassWithMember()
    {
        await AssertNavigatedAsync("""
            class C
            {
                [||]void M() { }
            }

            $$
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task BetweenClasses()
    {
        await AssertNavigatedAsync("""
            class C1
            {
                void M() { }
            }

            $$

            class C2
            {
                [||]void M() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task BetweenClassesPrevious()
    {
        await AssertNavigatedAsync("""
            class C1
            {
                [||]void M() { }
            }

            $$

            class C2
            {
                void M() { }
            }
            """, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromFirstMemberToSecond()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]void M2() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromSecondToFirst()
    {
        await AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }
                $$void M2() { }
            }
            """, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextWraps()
    {
        await AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }
                $$void M2() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PreviousWraps()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]void M2() { }
            }
            """, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task DescendsIntoNestedType()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }

                class N
                {
                    [||]void M2() { }
                }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtConstructor()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]public C() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtDestructor()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]~C() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtOperator()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]static C operator+(C left, C right) { throw new System.NotImplementedException(); }
            }
            """, next: true);
    }
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtField()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]int F;
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtFieldlikeEvent()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]event System.EventHandler E;
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtAutoProperty()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]int P { get; set ; }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtPropertyWithAccessors()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }

                [||]int P
                {
                    get { return 42; }
                    set { }
                }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task SkipsPropertyAccessors()
    {
        await AssertNavigatedAsync("""
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
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromInsideAccessor()
    {
        await AssertNavigatedAsync("""
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
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtIndexerWithAccessors()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }

                [||]int this[int i]
                {
                    get { return 42; }
                    set { }
                }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task SkipsIndexerAccessors()
    {
        await AssertNavigatedAsync("""
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
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtEventWithAddRemove()
    {
        await AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }

                [||]event EventHandler E
                {
                    add { }
                    remove { }
                }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task SkipsEventAddRemove()
    {
        await AssertNavigatedAsync("""
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
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task FromInsideMethod()
    {
        await AssertNavigatedAsync("""
            class C
            {
                void M1()
                {
                    $$System.Console.WriteLine();
                }

                [||]void M2() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextFromBetweenMethods()
    {
        await AssertNavigatedAsync("""
            class C
            {
                void M1() { }

                $$

                [||]void M2() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PreviousFromBetweenMethods()
    {
        await AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }

                $$

                void M2() { }
            }
            """, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextFromBetweenMethodsInTrailingTrivia()
    {
        await AssertNavigatedAsync("""
            class C
            {
                void M1()
                {
                } $$

                [||]void M2() { }
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PreviousFromBetweenMethodsInTrailingTrivia()
    {
        await AssertNavigatedAsync("""
            class C
            {
                [||]void M1()
                {
                } $$

                void M2() { }
            }
            """, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task StopsAtExpressionBodiedMember()
    {
        await AssertNavigatedAsync("""
            class C
            {
                int M1() => $$42;

                [||]int M2() => 42;
            }
            """, next: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/10588")]
    public async Task PreviousFromInsideCurrent()
    {
        await AssertNavigatedAsync("""
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
            """, next: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task NextInScript()
    {
        await AssertNavigatedAsync("""
            $$void M1() { }

            [||]void M2() { }
            """, next: true, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public async Task PrevInScript()
    {
        await AssertNavigatedAsync("""
            [||]void M1() { }

            $$void M2() { }
            """, next: false, sourceCodeKind: SourceCodeKind.Script);
    }
}
