// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.InheritanceMargin
{
    [Trait(Traits.Feature, Traits.Features.InheritanceMargin)]
    [UseExportProvider]
    public class InheritanceMarginTests
    {
        private const string SearchAreaTag = "SeachTag";

        #region Helpers
        private static async Task VerifyInSingleDocumentAsync(
            string markup,
            string languageName,
            params TestInheritanceMemberItem[] memberItems)
        {
            var workspaceFile = $@"
<Workspace>
   <Project Language=""{languageName}"" CommonReferences=""true"">
       <Document>
            {markup}
       </Document>
   </Project>
</Workspace>";

            var cancellationToken = CancellationToken.None;

            using var testWorkspace = TestWorkspace.Create(
                workspaceFile,
                composition: EditorTestCompositions.EditorFeatures);

            testWorkspace.SetOptions(
                testWorkspace.Options.WithChangedOption(
                    InheritanceMarginOptions.ShowInheritanceMargin,
                    languageName,
                    true));

            var testDocumentHost = testWorkspace.Documents[0];
            var document = testWorkspace.CurrentSolution.GetRequiredDocument(testDocumentHost.Id);
            var service = document.GetRequiredLanguageService<IInheritanceMarginService>();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var searchingSpan = root.Span;
            // Look for the search span, if not found, then pass the whole document span to the service.
            if (testDocumentHost.AnnotatedSpans.TryGetValue(SearchAreaTag, out var spans) && spans.IsSingle())
            {
                searchingSpan = spans[0];
            }

            var actualItems = await service.GetInheritanceInfoAsync(
                document,
                searchingSpan,
                cancellationToken).ConfigureAwait(false);

            var sortedActualItems = actualItems.OrderBy(item => item.LineNumber).ToImmutableArray();
            var sortedExpectedItems = memberItems.OrderBy(item => item.LineNumber).ToImmutableArray();
            Assert.Equal(sortedExpectedItems.Length, sortedActualItems.Length);

            for (var i = 0; i < sortedActualItems.Length; i++)
            {
                VerifyInheritanceMember(testWorkspace, sortedExpectedItems[i], sortedActualItems[i]);
            }
        }

        private static void VerifyInheritanceMember(TestWorkspace testWorkspace, TestInheritanceMemberItem expectedItem, InheritanceMemberItem actualItem)
        {
            Assert.Equal(expectedItem.LineNumber, actualItem.LineNumber);
            Assert.Equal(expectedItem.MemberName, actualItem.MemberDisplayName);
            Assert.Equal(expectedItem.Targets.Length, actualItem.TargetItems.Length);
            var expectedTargets = expectedItem.Targets
                .SelectAsArray(info => TestInheritanceTargetItem.Create(info, testWorkspace));
            for (var i = 0; i < expectedTargets.Length; i++)
            {
                VerifyInheritanceTarget(expectedTargets[i], actualItem.TargetItems[i]);
            }
        }

        private static void VerifyInheritanceTarget(TestInheritanceTargetItem expectedTarget, InheritanceTargetItem actualTarget)
        {
            Assert.Equal(expectedTarget.TargetSymbolName, actualTarget.DefinitionItem.DisplayParts.JoinText());
            Assert.Equal(expectedTarget.RelationshipToMember, actualTarget.RelationToMember);

            var actualDocumentSpans = actualTarget.DefinitionItem.SourceSpans.OrderBy(documentSpan => documentSpan.SourceSpan.Start).ToImmutableArray();
            var expectedDocumentSpans = expectedTarget.DocumentSpans.OrderBy(documentSpan => documentSpan.SourceSpan.Start).ToImmutableArray();
            Assert.Equal(expectedDocumentSpans.Length, actualDocumentSpans.Length);
            for (var i = 0; i < actualDocumentSpans.Length; i++)
            {
                Assert.Equal(expectedDocumentSpans[i].SourceSpan, actualDocumentSpans[i].SourceSpan);
                Assert.Equal(expectedDocumentSpans[i].Document.FilePath, actualDocumentSpans[i].Document.FilePath);
            }
        }

        private readonly struct TestInheritanceMemberItem
        {
            public readonly int LineNumber;
            public readonly string MemberName;
            public readonly ImmutableArray<TargetInfo> Targets;

            public TestInheritanceMemberItem(
                int lineNumber,
                string memberName,
                ImmutableArray<TargetInfo> targets)
            {
                LineNumber = lineNumber;
                MemberName = memberName;
                Targets = targets;
            }
        }

        private readonly struct TargetInfo
        {
            public readonly string TargetSymbolDisplayName;
            public readonly string LocationTag;
            public readonly InheritanceRelationship Relationship;

            public TargetInfo(
                string targetSymbolDisplayName,
                string locationTag,
                InheritanceRelationship relationship)
            {
                TargetSymbolDisplayName = targetSymbolDisplayName;
                LocationTag = locationTag;
                Relationship = relationship;
            }
        }

        private readonly struct TestInheritanceTargetItem
        {
            public readonly string TargetSymbolName;
            public readonly InheritanceRelationship RelationshipToMember;
            public readonly ImmutableArray<DocumentSpan> DocumentSpans;

            private TestInheritanceTargetItem(
                string targetSymbolName,
                InheritanceRelationship relationshipToMember,
                ImmutableArray<DocumentSpan> documentSpans)
            {
                TargetSymbolName = targetSymbolName;
                RelationshipToMember = relationshipToMember;
                DocumentSpans = documentSpans;
            }

            public static TestInheritanceTargetItem Create(
                TargetInfo targetInfo,
                TestWorkspace testWorkspace)
            {
                using var _ = ArrayBuilder<DocumentSpan>.GetInstance(out var builder);
                foreach (var testHostDocument in testWorkspace.Documents)
                {
                    var annotatedSpans = testHostDocument.AnnotatedSpans;
                    if (annotatedSpans.TryGetValue(targetInfo.LocationTag, out var spans))
                    {
                        var document = testWorkspace.CurrentSolution.GetRequiredDocument(testHostDocument.Id);
                        builder.AddRange(spans.Select(span => new DocumentSpan(document, span)));
                    }
                }

                return new TestInheritanceTargetItem(
                    targetInfo.TargetSymbolDisplayName,
                    targetInfo.Relationship,
                    builder.ToImmutable());
            }
        }

        #endregion

        #region TestsForCSharp

        [Fact]
        public Task TestClassImplementingInterface()
        {
            var markup = @"
interface {|target1:IBar|} { }
public class {|target2:Bar|} : IBar
{
}
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3);
        }

        [Fact]
        public Task TestInterfaceImplemetingInterface()
        {
            var markup = @"
interface {|target1:IBar|} { }
interface {|target2:IBar2|} : IBar { }
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar2",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "IBar2",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3);
        }

        [Fact]
        public Task TestClassInheritsClass()
        {
            var markup = @"
class {|target2:A|} { }
class {|target1:B|} : A { }
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "A",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "class B",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented))
            );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "B",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "class A",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implementing))
            );

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3);
        }

        [Fact]
        public Task TestInterfaceMembers()
        {
            var markup = @"using System;
interface {|target1:IBar|}
{
    void {|target4:Foo|}();
    int {|target6:Poo|} { get; set; }
    event EventHandler {|target8:Eoo|};
}
public class {|target2:Bar|} : IBar
{
    public void {|target3:Foo|}() { }
    public int {|target5:Poo|} { get; set; }
    public event EventHandler {|target7:Eoo|};
}";

            var itemForEooInClass = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberName: "event EventHandler Bar.Eoo",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "event EventHandler IBar.Eoo",
                        locationTag: "target8",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForEooInInterface = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "event EventHandler IBar.Eoo",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "event EventHandler Bar.Eoo",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForPooInInterface = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "int IBar.Poo",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "int Bar.Poo { get; set; }",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForPooInClass = new TestInheritanceMemberItem(
                lineNumber: 11,
                memberName: "int Bar.Poo",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "int IBar.Poo { get; set; }",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForFooInInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void IBar.Foo()",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "void Bar.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberName: "void Bar.Foo()",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "void IBar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForEooInClass,
                itemForEooInInterface,
                itemForPooInInterface,
                itemForPooInClass,
                itemForFooInInterface,
                itemForFooInClass,
                itemForIBar,
                itemForBar);
        }

        [Theory]
        [InlineData("abstract")]
        [InlineData("virtual")]
        public Task TestAbstractClassMembers(string modifier)
        {
            var markup = $@"using System;
public abstract class {{|target2:Bar|}}
{{
    public {modifier} void {{|target4:Foo|}}();
    public {modifier} int {{|target6:Poo|}} {{ get; set; }}
    public {modifier} event EventHandler {{|target8:Eoo|}};
}}
public class {{|target1:Bar2|}} : Bar
{{
    public override void {{|target3:Foo|}}() {{ }}
    public override int {{|target5:Poo|}} {{ get; set; }}
    public override event EventHandler {{|target7:Eoo|}};
}}
            ";

            var itemForEooInClass = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberName: "event EventHandler Bar2.Eoo",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: $"{modifier} event EventHandler Bar.Eoo",
                        locationTag: "target8",
                        relationship: InheritanceRelationship.Overriding)));

            var itemForEooInAbstractClass = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "event EventHandler Bar.Eoo",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "override event EventHandler Bar2.Eoo",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.Overridden)));

            var itemForPooInClass = new TestInheritanceMemberItem(
                    lineNumber: 11,
                    memberName: "int Bar2.Poo",
                    targets: ImmutableArray<TargetInfo>.Empty
                        .Add(new TargetInfo(
                            targetSymbolDisplayName: $"{modifier} int Bar.Poo {{ get; set; }}",
                            locationTag: "target6",
                            relationship: InheritanceRelationship.Overriding)));

            var itemForPooInAbstractClass = new TestInheritanceMemberItem(
                    lineNumber: 5,
                    memberName: "int Bar.Poo",
                    targets: ImmutableArray<TargetInfo>.Empty
                        .Add(new TargetInfo(
                            targetSymbolDisplayName: "override int Bar2.Poo { get; set; }",
                            locationTag: "target5",
                            relationship: InheritanceRelationship.Overridden)));

            var itemForFooInAbstractClass = new TestInheritanceMemberItem(
                    lineNumber: 4,
                    memberName: "void Bar.Foo()",
                    targets: ImmutableArray<TargetInfo>.Empty
                        .Add(new TargetInfo(
                            targetSymbolDisplayName: "override void Bar2.Foo()",
                            locationTag: "target3",
                            relationship: InheritanceRelationship.Overridden)));

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberName: "void Bar2.Foo()",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: $"{modifier} void Bar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Overriding)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "class Bar2",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented)));

            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "Bar2",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForBar,
                itemForBar2,
                itemForFooInAbstractClass,
                itemForFooInClass,
                itemForPooInClass,
                itemForPooInAbstractClass,
                itemForEooInClass,
                itemForEooInAbstractClass);
        }
        #endregion
    }
}
