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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task BeforeClassWithMember()
        => AssertNavigatedAsync("""
            $$
            class C
            {
                [||]void M() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task AfterClassWithMember()
        => AssertNavigatedAsync("""
            class C
            {
                [||]void M() { }
            }

            $$
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task BetweenClasses()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task BetweenClassesPrevious()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task FromFirstMemberToSecond()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]void M2() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task FromSecondToFirst()
        => AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }
                $$void M2() { }
            }
            """, next: false);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task NextWraps()
        => AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }
                $$void M2() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task PreviousWraps()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]void M2() { }
            }
            """, next: false);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task DescendsIntoNestedType()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }

                class N
                {
                    [||]void M2() { }
                }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtConstructor()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]public C() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtDestructor()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]~C() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtOperator()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]static C operator+(C left, C right) { throw new System.NotImplementedException(); }
            }
            """, next: true);
    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtField()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]int F;
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtFieldlikeEvent()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]event System.EventHandler E;
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtAutoProperty()
        => AssertNavigatedAsync("""
            class C
            {
                $$void M1() { }
                [||]int P { get; set ; }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtPropertyWithAccessors()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task SkipsPropertyAccessors()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task FromInsideAccessor()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtIndexerWithAccessors()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task SkipsIndexerAccessors()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtEventWithAddRemove()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task SkipsEventAddRemove()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task FromInsideMethod()
        => AssertNavigatedAsync("""
            class C
            {
                void M1()
                {
                    $$System.Console.WriteLine();
                }

                [||]void M2() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task NextFromBetweenMethods()
        => AssertNavigatedAsync("""
            class C
            {
                void M1() { }

                $$

                [||]void M2() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task PreviousFromBetweenMethods()
        => AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }

                $$

                void M2() { }
            }
            """, next: false);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task NextFromBetweenMethodsInTrailingTrivia()
        => AssertNavigatedAsync("""
            class C
            {
                void M1()
                {
                } $$

                [||]void M2() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task PreviousFromBetweenMethodsInTrailingTrivia()
        => AssertNavigatedAsync("""
            class C
            {
                [||]void M1()
                {
                } $$

                void M2() { }
            }
            """, next: false);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task StopsAtExpressionBodiedMember()
        => AssertNavigatedAsync("""
            class C
            {
                int M1() => $$42;

                [||]int M2() => 42;
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/10588")]
    public Task PreviousFromInsideCurrent()
        => AssertNavigatedAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task NextInScript()
        => AssertNavigatedAsync("""
            $$void M1() { }

            [||]void M2() { }
            """, next: true, sourceCodeKind: SourceCodeKind.Script);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")]
    public Task PrevInScript()
        => AssertNavigatedAsync("""
            [||]void M1() { }

            $$void M2() { }
            """, next: false, sourceCodeKind: SourceCodeKind.Script);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77393")]
    public Task SelectionNextMethod()
        => AssertNavigatedAsync("""
            class C
            {
                {|selection:$$void M1() { }|}
                [||]void M2() { }
            }
            """, next: true);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77393")]
    public Task SelectionPreviousMethod()
        => AssertNavigatedAsync("""
            class C
            {
                [||]void M1() { }
                {|selection:$$void M2() { }|}
            }
            """, next: false);
}
