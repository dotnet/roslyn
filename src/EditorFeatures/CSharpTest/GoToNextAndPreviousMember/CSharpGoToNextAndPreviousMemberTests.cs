using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    public class CSharpGoToNextAndPreviousMemberTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        public void EmptyFile()
        {
            var code = @"$$";
            Assert.Null(GetTargetPosition(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        public void ClassWithNoMembers()
        {
            var code = @"class C
{
$$
}";
            Assert.Null(GetTargetPosition(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
        public void BeforeClassWithMember()
        {
            var code = @"$$
class C
{
    void M() { }
}";

            // TODO: Should this navigate to M?
            Assert.Null(GetTargetPosition(code, next: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
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
        public void StopsAtOperator()
        {
            var code = @"
class C
{
    $$void M1() { }
    [||]static C operator+(C left, C right) { throw System.NotImplementedException(); }
}";
            AssertNavigated(code, next: true);
        }
        [Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)]
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

        private static void AssertNavigated(string code, bool next)
        {
            using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.CSharp,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                CSharpParseOptions.Default,
                code))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var targetPosition = GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    workspace.CurrentSolution.GetDocument(hostDocument.Id),
                    hostDocument.CursorPosition.Value,
                    next,
                    CancellationToken.None);

                Assert.NotNull(targetPosition);
                Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value);
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

                return GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    workspace.CurrentSolution.GetDocument(hostDocument.Id),
                    hostDocument.CursorPosition.Value,
                    next,
                    CancellationToken.None);
            }
        }
    }
}
