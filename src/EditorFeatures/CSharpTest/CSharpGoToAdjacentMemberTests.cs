// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    public class CSharpGoToAdjacentMemberTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task EmptyFile()
        {
            var code = @"$$";
            Assert.Null(await GetTargetPositionAsync(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task ClassWithNoMembers()
        {
            var code = @"class C
{
$$
}";
            Assert.Null(await GetTargetPositionAsync(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task BeforeClassWithMember()
        {
            var code = @"$$
class C
{
    [||]void M() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task AfterClassWithMember()
        {
            var code = @"
class C
{
    [||]void M() { }
}

$$";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task BetweenClasses()
        {
            var code = @"
class C1
{
    void M() { }
}

$$

class C2
{
    [||]void M() { }
} ";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task BetweenClassesPrevious()
        {
            var code = @"
class C1
{
    [||]void M() { }
}

$$

class C2
{
    void M() { }
} ";

            await AssertNavigatedAsync(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task FromFirstMemberToSecond()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task FromSecondToFirst()
        {
            var code = @"
class C
{
    [||]void M1() { }
    $$void M2() { }
}";

            await AssertNavigatedAsync(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task NextWraps()
        {
            var code = @"
class C
{
    [||]void M1() { }
    $$void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task PreviousWraps()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task DescendsIntoNestedType()
        {
            var code = @"
class C
{
    $$void M1() { }

    class N
    {
        [||]void M2() { }
    }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtConstructor()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]public C() { }
}";
            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtDestructor()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]~C() { }
}";
            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtOperator()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]static C operator+(C left, C right) { throw new System.NotImplementedException(); }
}";
            await AssertNavigatedAsync(code, next: true);
        }
        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtField()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]int F;
}";
            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtFieldlikeEvent()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]event System.EventHandler E;
}";
            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtAutoProperty()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]int P { get; set ; }
}";
            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtPropertyWithAccessors()
        {
            var code = @"
class C
{
    $$void M1() { }

    [||]int P
    {
        get { return 42; }
        set { }
    }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task SkipsPropertyAccessors()
        {
            var code = @"
class C
{
    void M1() { }

    $$int P
    {
        get { return 42; }
        set { }
    }

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task FromInsideAccessor()
        {
            var code = @"
class C
{
    void M1() { }

    int P
    {
        get { return $$42; }
        set { }
    }

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtIndexerWithAccessors()
        {
            var code = @"
class C
{
    $$void M1() { }

    [||]int this[int i]
    {
        get { return 42; }
        set { }
    }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task SkipsIndexerAccessors()
        {
            var code = @"
class C
{
    void M1() { }

    $$int this[int i]
    {
        get { return 42; }
        set { }
    }

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtEventWithAddRemove()
        {
            var code = @"
class C
{
    $$void M1() { }

    [||]event EventHandler E
    {
        add { }
        remove { }
    }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task SkipsEventAddRemove()
        {
            var code = @"
class C
{
    void M1() { }

    $$event EventHandler E
    {
        add { }
        remove { }
    }

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task FromInsideMethod()
        {
            var code = @"
class C
{
    void M1()
    {
        $$System.Console.WriteLine();
    }

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task NextFromBetweenMethods()
        {
            var code = @"
class C
{
    void M1() { }

    $$

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task PreviousFromBetweenMethods()
        {
            var code = @"
class C
{
    [||]void M1() { }

    $$

    void M2() { }
}";

            await AssertNavigatedAsync(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task NextFromBetweenMethodsInTrailingTrivia()
        {
            var code = @"
class C
{
    void M1()
    {
    } $$

    [||]void M2() { }
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task PreviousFromBetweenMethodsInTrailingTrivia()
        {
            var code = @"
class C
{
    [||]void M1()
    {
    } $$

    void M2() { }
}";

            await AssertNavigatedAsync(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task StopsAtExpressionBodiedMember()
        {
            var code = @"
class C
{
    int M1() => $$42;

    [||]int M2() => 42;
}";

            await AssertNavigatedAsync(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task NextInScript()
        {
            var code = @"
$$void M1() { }

[||]void M2() { }";

            await AssertNavigatedAsync(code, next: true, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public async Task PrevInScript()
        {
            var code = @"
[||]void M1() { }

$$void M2() { }";

            await AssertNavigatedAsync(code, next: false, sourceCodeKind: SourceCodeKind.Script);
        }

        private static async Task AssertNavigatedAsync(string code, bool next, SourceCodeKind? sourceCodeKind = null)
        {
            var kinds = sourceCodeKind != null
                ? SpecializedCollections.SingletonEnumerable(sourceCodeKind.Value)
                : new[] { SourceCodeKind.Regular, SourceCodeKind.Script };
            foreach (var kind in kinds)
            {
                using (var workspace = await TestWorkspaceFactory.CreateWorkspaceFromLinesAsync(
                    LanguageNames.CSharp,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    CSharpParseOptions.Default.WithKind(kind),
                    code))
                {
                    var hostDocument = workspace.DocumentWithCursor;
                    var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                    Assert.Empty((await document.GetSyntaxTreeAsync()).GetDiagnostics());
                    var targetPosition = await GoToAdjacentMemberCommandHandler.GetTargetPositionAsync(
                        document,
                        hostDocument.CursorPosition.Value,
                        next,
                        CancellationToken.None);

                    Assert.NotNull(targetPosition);
                    Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value);
                }
            }
        }

        private static async Task<int?> GetTargetPositionAsync(string code, bool next)
        {
            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceFromLinesAsync(
                LanguageNames.CSharp,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                CSharpParseOptions.Default,
                code))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                Assert.Empty((await document.GetSyntaxTreeAsync()).GetDiagnostics());
                return await GoToAdjacentMemberCommandHandler.GetTargetPositionAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    next,
                    CancellationToken.None);
            }
        }
    }
}
