using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    public class CSharpGoToNextAndPreviousMemberTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void EmptyFile()
        {
            var code = @"$$";
            Assert.Null(GetTargetPosition(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void ClassWithNoMembers()
        {
            var code = @"class C
{
$$
}";
            Assert.Null(GetTargetPosition(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void BeforeClassWithMember()
        {
            var code = @"$$
class C
{
    [||]void M() { }
}";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void AfterClassWithMember()
        {
            var code = @"
class C
{
    [||]void M() { }
}

$$";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void BetweenClasses()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void FromFirstMemberToSecond()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]void M2() { }
}";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void FromSecondToFirst()
        {
            var code = @"
class C
{
    [||]void M1() { }
    $$void M2() { }
}";

            AssertNavigated(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void NextWraps()
        {
            var code = @"
class C
{
    [||]void M1() { }
    $$void M2() { }
}";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void PreviousWraps()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]void M2() { }
}";

            AssertNavigated(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void DescendsIntoNestedType()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtConstructor()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]public C() { }
}";
            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtDestructor()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]~C() { }
}";
            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtOperator()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]static C operator+(C left, C right) { throw new System.NotImplementedException(); }
}";
            AssertNavigated(code, next: true);
        }
        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtField()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]int F;
}";
            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtFieldlikeEvent()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]event System.EventHandler E;
}";
            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtAutoProperty()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]int P { get; set ; }
}";
            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtPropertyWithAccessors()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void SkipsPropertyAccessors()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void FromInsideAccessor()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtIndexerWithAccessors()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void SkipsIndexerAccessors()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtEventWithAddRemove()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void SkipsEventAddRemove()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void FromInsideMethod()
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

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void NextFromBetweenMethods()
        {
            var code = @"
class C
{
    void M1() { }

    $$

    [||]void M2() { }
}";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void PreviousFromBetweenMethods()
        {
            var code = @"
class C
{
    [||]void M1() { }

    $$

    void M2() { }
}";

            AssertNavigated(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void NextFromBetweenMethodsInTrailingTrivia()
        {
            var code = @"
class C
{
    void M1()
    {
    } $$

    [||]void M2() { }
}";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void PreviousFromBetweenMethodsInTrailingTrivia()
        {
            var code = @"
class C
{
    [||]void M1()
    {
    } $$

    void M2() { }
}";

            AssertNavigated(code, next: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void StopsAtExpressionBodiedMember()
        {
            var code = @"
class C
{
    int M1() => $$42;

    [||]int M2() => 42;
}";

            AssertNavigated(code, next: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void NextInScript()
        {
            var code = @"
$$void M1() { }

[||]void M2() { }";

            AssertNavigated(code, next: true, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        [WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")]
        public void PrevInScript()
        {
            var code = @"
[||]void M1() { }

$$void M2() { }";

            AssertNavigated(code, next: false, sourceCodeKind: SourceCodeKind.Script);
        }

        private static void AssertNavigated(string code, bool next, SourceCodeKind? sourceCodeKind = null)
        {
            var kinds = sourceCodeKind != null
                ? SpecializedCollections.SingletonEnumerable(sourceCodeKind.Value)
                : new[] { SourceCodeKind.Regular, SourceCodeKind.Script };
            foreach (var kind in kinds)
            {
                using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                    LanguageNames.CSharp,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    CSharpParseOptions.Default.WithKind(kind),
                    code))
                {
                    var hostDocument = workspace.DocumentWithCursor;
                    var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                    Assert.Empty(document.GetSyntaxTreeAsync().Result.GetDiagnostics());
                    var targetPosition = GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                        document,
                        hostDocument.CursorPosition.Value,
                        next,
                        CancellationToken.None);

                    Assert.NotNull(targetPosition);
                    Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value);
                }
            }
        }

        private static int? GetTargetPosition(string code, bool next)
        {
            using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.CSharp,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                CSharpParseOptions.Default,
                code))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                Assert.Empty(document.GetSyntaxTreeAsync().Result.GetDiagnostics());
                return GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    document,
                    hostDocument.CursorPosition.Value,
                    next,
                    CancellationToken.None);
            }
        }
    }
}
