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

        private static Task VerifyNoItemForDocumentAsync(string markup, string languageName)
            => VerifyInSingleDocumentAsync(markup, languageName);

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

            var actualItems = await service.GetInheritanceMemberItemsAsync(
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

        private static void VerifyInheritanceMember(TestWorkspace testWorkspace, TestInheritanceMemberItem expectedItem, InheritanceMarginItem actualItem)
        {
            Assert.Equal(expectedItem.LineNumber, actualItem.LineNumber);
            Assert.Equal(expectedItem.MemberName, actualItem.DisplayTexts.JoinText());
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

            if (expectedTarget.IsInMetadata)
            {
                Assert.True(actualTarget.DefinitionItem.Properties.ContainsKey("MetadataSymbolKey"));
                Assert.True(actualTarget.DefinitionItem.SourceSpans.IsEmpty);
            }
            else
            {
                var actualDocumentSpans = actualTarget.DefinitionItem.SourceSpans.OrderBy(documentSpan => documentSpan.SourceSpan.Start).ToImmutableArray();
                var expectedDocumentSpans = expectedTarget.DocumentSpans.OrderBy(documentSpan => documentSpan.SourceSpan.Start).ToImmutableArray();
                Assert.Equal(expectedDocumentSpans.Length, actualDocumentSpans.Length);
                for (var i = 0; i < actualDocumentSpans.Length; i++)
                {
                    Assert.Equal(expectedDocumentSpans[i].SourceSpan, actualDocumentSpans[i].SourceSpan);
                    Assert.Equal(expectedDocumentSpans[i].Document.FilePath, actualDocumentSpans[i].Document.FilePath);
                }
            }
        }

        private class TestInheritanceMemberItem
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

        private class TargetInfo
        {
            public readonly string TargetSymbolDisplayName;
            public readonly string? LocationTag;
            public readonly InheritanceRelationship Relationship;
            public readonly bool InMetadata;

            public TargetInfo(
                string targetSymbolDisplayName,
                string locationTag,
                InheritanceRelationship relationship)
            {
                TargetSymbolDisplayName = targetSymbolDisplayName;
                LocationTag = locationTag;
                Relationship = relationship;
                InMetadata = false;
            }

            public TargetInfo(
                string targetSymbolDisplayName,
                InheritanceRelationship relationship,
                bool inMetadata)
            {
                TargetSymbolDisplayName = targetSymbolDisplayName;
                Relationship = relationship;
                InMetadata = inMetadata;
                LocationTag = null;
            }
        }

        private class TestInheritanceTargetItem
        {
            public readonly string TargetSymbolName;
            public readonly InheritanceRelationship RelationshipToMember;
            public readonly ImmutableArray<DocumentSpan> DocumentSpans;
            public readonly bool IsInMetadata;

            public TestInheritanceTargetItem(
                string targetSymbolName,
                InheritanceRelationship relationshipToMember,
                 ImmutableArray<DocumentSpan> documentSpans,
                  bool isInMetadata)
            {
                TargetSymbolName = targetSymbolName;
                RelationshipToMember = relationshipToMember;
                DocumentSpans = documentSpans;
                IsInMetadata = isInMetadata;
            }

            public static TestInheritanceTargetItem Create(
                TargetInfo targetInfo,
                TestWorkspace testWorkspace)
            {
                if (targetInfo.InMetadata)
                {
                    return new TestInheritanceTargetItem(
                        targetInfo.TargetSymbolDisplayName,
                        targetInfo.Relationship,
                        ImmutableArray<DocumentSpan>.Empty,
                        isInMetadata: true);
                }
                else
                {
                    using var _ = ArrayBuilder<DocumentSpan>.GetInstance(out var builder);
                    // If the target is not in metadata, there must be a location tag to give the span!
                    Assert.True(targetInfo.LocationTag != null);
                    foreach (var testHostDocument in testWorkspace.Documents)
                    {
                        if (targetInfo.LocationTag != null)
                        {
                            var annotatedSpans = testHostDocument.AnnotatedSpans;
                            if (annotatedSpans.TryGetValue(targetInfo.LocationTag, out var spans))
                            {
                                var document = testWorkspace.CurrentSolution.GetRequiredDocument(testHostDocument.Id);
                                builder.AddRange(spans.Select(span => new DocumentSpan(document, span)));
                            }
                        }
                    }

                    return new TestInheritanceTargetItem(
                        targetInfo.TargetSymbolDisplayName,
                        targetInfo.Relationship,
                        builder.ToImmutable(),
                        isInMetadata: false);
                }
            }
        }

        #endregion

        #region TestsForCSharp

        [Fact]
        public Task TestCSharpClassImplementingInterface()
        {
            var markup = @"
interface {|target1:IBar|} { }
public class {|target2:Bar|} : IBar
{
}
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented)));

            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3);
        }

        [Fact]
        public Task TestCSharpInterfaceImplementingInterface()
        {
            var markup = @"
interface {|target1:IBar|} { }
interface {|target2:IBar2|} : IBar { }
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar2",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "interface IBar2",
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
        public Task TestCSharpClassInheritsClass()
        {
            var markup = @"
class {|target2:A|} { }
class {|target1:B|} : A { }
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "class A",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class B",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented))
            );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "class B",
                targets: ImmutableArray.Create(new TargetInfo(
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

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("enum")]
        [InlineData("interface")]
        public Task TestCSharpTypeWithoutBaseType(string typeName)
        {
            var markup = $@"
public {typeName} Bar
{{
}}";
            return VerifyNoItemForDocumentAsync(markup, LanguageNames.CSharp);
        }

        [Theory]
        [InlineData("public Bar() { }")]
        [InlineData("public static void Bar3() { }")]
        [InlineData("public static void ~Bar() { }")]
        [InlineData("public static Bar operator +(Bar a, Bar b) => new Bar();")]
        public Task TestCSharpSpecialMember(string memberDeclaration)
        {
            var markup = $@"
public abstract class {{|target1:Bar1|}}
{{}}
public class Bar : Bar1
{{
    {{|{SearchAreaTag}:{memberDeclaration}|}}
}}";
            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                new TestInheritanceMemberItem(
                    lineNumber: 4,
                    memberName: "class Bar",
                    targets: ImmutableArray.Create(
                        new TargetInfo(
                            targetSymbolDisplayName: "class Bar1",
                            locationTag: "target1",
                            relationship: InheritanceRelationship.Implementing))));
        }

        [Fact]
        public Task TestCSharpMetadataInterface()
        {
            var markup = @"
using System.Collections;
public class Bar : IEnumerable
{
}";
            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                new TestInheritanceMemberItem(
                    lineNumber: 3,
                    memberName: "class Bar",
                    targets: ImmutableArray.Create(
                        new TargetInfo(
                                targetSymbolDisplayName: "interface IEnumerable",
                                relationship: InheritanceRelationship.Implementing,
                                inMetadata: true))));
        }

        [Fact]
        public Task TestCSharpEventDeclaration()
        {
            var markup = @"
using System;
interface {|target2:IBar|}
{
    event EventHandler {|target4:e|};
}
public class {|target1:Bar|} : IBar
{
    public event EventHandler {|target3:e|}
    {
        add {} remove {}
    }
}";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForEventInInterface = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "event EventHandler IBar.e",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler Bar.e",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForEventInClass = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "event EventHandler Bar.e",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler IBar.e",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForIBar,
                itemForBar,
                itemForEventInInterface,
                itemForEventInClass);
        }

        [Fact]
        public Task TestCSharpEventFieldDeclarations()
        {
            var markup = @"using System;
interface {|target2:IBar|}
{
    event EventHandler {|target5:e1|}, {|target6:e2|};
}
public class {|target1:Bar|} : IBar
{
    public event EventHandler {|target3:e1|}, {|target4:e2|};
}";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForE1InInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "event EventHandler IBar.e1",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler Bar.e1",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForE2InInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "event EventHandler IBar.e2",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler Bar.e2",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForE1InClass = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "event EventHandler Bar.e1",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler IBar.e1",
                    locationTag: "target5",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForE2InClass = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "event EventHandler Bar.e2",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler IBar.e2",
                    locationTag: "target6",
                    relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForIBar,
                itemForBar,
                itemForE1InInterface,
                itemForE2InInterface,
                itemForE1InClass,
                itemForE2InClass);
        }

        [Fact]
        public Task TestCSharpInterfaceMembers()
        {
            var markup = @"using System;
interface {|target1:IBar|}
{
    void {|target4:Foo|}();
    int {|target6:Poo|} { get; set; }
    event EventHandler {|target8:Eoo|};
    int {|target9:this|}[int i] { get; set; }
}
public class {|target2:Bar|} : IBar
{
    public void {|target3:Foo|}() { }
    public int {|target5:Poo|} { get; set; }
    public event EventHandler {|target7:Eoo|};
    public int {|target10:this|}[int i] { get => 1; set { } }
}";
            var itemForEooInClass = new TestInheritanceMemberItem(
                lineNumber: 13,
                memberName: "event EventHandler Bar.Eoo",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "event EventHandler IBar.Eoo",
                        locationTag: "target8",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForEooInInterface = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "event EventHandler IBar.Eoo",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "event EventHandler Bar.Eoo",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForPooInInterface = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "int IBar.Poo { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int Bar.Poo { get; set; }",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForPooInClass = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberName: "int Bar.Poo { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int IBar.Poo { get; set; }",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForFooInInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void IBar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void Bar.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 11,
                memberName: "void Bar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForIndexerInClass = new TestInheritanceMemberItem(
                lineNumber: 14,
                memberName: "int Bar.this[int] { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int IBar.this[int] { get; set; }",
                        locationTag: "target9",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemForIndexerInInterface = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "int IBar.this[int] { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int Bar.this[int] { get; set; }",
                        locationTag: "target10",
                        relationship: InheritanceRelationship.Implemented))
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
                itemForBar,
                itemForIndexerInInterface,
                itemForIndexerInClass);
        }

        [Theory]
        [InlineData("abstract")]
        [InlineData("virtual")]
        public Task TestCSharpAbstractClassMembers(string modifier)
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
                memberName: "override event EventHandler Bar2.Eoo",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: $"{modifier} event EventHandler Bar.Eoo",
                        locationTag: "target8",
                        relationship: InheritanceRelationship.Overriding)));

            var itemForEooInAbstractClass = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: $"{modifier} event EventHandler Bar.Eoo",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "override event EventHandler Bar2.Eoo",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.Overridden)));

            var itemForPooInClass = new TestInheritanceMemberItem(
                    lineNumber: 11,
                    memberName: "override int Bar2.Poo { get; set; }",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: $"{modifier} int Bar.Poo {{ get; set; }}",
                            locationTag: "target6",
                            relationship: InheritanceRelationship.Overriding)));

            var itemForPooInAbstractClass = new TestInheritanceMemberItem(
                    lineNumber: 5,
                    memberName: $"{modifier} int Bar.Poo {{ get; set; }}",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "override int Bar2.Poo { get; set; }",
                            locationTag: "target5",
                            relationship: InheritanceRelationship.Overridden)));

            var itemForFooInAbstractClass = new TestInheritanceMemberItem(
                    lineNumber: 4,
                    memberName: $"{modifier} void Bar.Foo()",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "override void Bar2.Foo()",
                            locationTag: "target3",
                            relationship: InheritanceRelationship.Overridden)));

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberName: "override void Bar2.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: $"{modifier} void Bar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Overriding)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar2",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented)));

            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "class Bar2",
                targets: ImmutableArray.Create(new TargetInfo(
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

        #region TestsForVisualBasic

        [Fact]
        public Task TestVisualBasicClassImplementingInterface()
        {
            var markup = @"
Interface {|target2:IBar|}
End Interface
Class {|target1:Bar|}
    Implements IBar
End Class";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.VisualBasic,
                itemForIBar,
                itemForBar);
        }

        [Fact]
        public Task TestVisualBasicInterfaceImplementingInterface()
        {
            var markup = @"
Interface {|target2:IBar2|}
End Interface
Interface {|target1:IBar|}
    Inherits IBar2
End Interface";

            var itemForIBar2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar2",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar2",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));
            return VerifyInSingleDocumentAsync(markup, LanguageNames.VisualBasic, itemForIBar2, itemForIBar);
        }

        [Fact]
        public Task TestVisualBasicClassInheritsClass()
        {
            var markup = @"
Class {|target2:Bar2|}
End Class
Class {|target1:Bar|}
    Inherits Bar2
End Class";

            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Class Bar2",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar2",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));
            return VerifyInSingleDocumentAsync(markup, LanguageNames.VisualBasic, itemForBar2, itemForBar);
        }

        [Theory]
        [InlineData("Class")]
        [InlineData("Structure")]
        [InlineData("Enum")]
        [InlineData("Interface")]
        public Task TestVisualBasicTypeWithoutBaseType(string typeName)
        {
            var markup = $@"
{typeName} Bar
End {typeName}";

            return VerifyNoItemForDocumentAsync(markup, LanguageNames.VisualBasic);
        }

        [Fact]
        public Task TestVisualBasicMetadataInterface()
        {
            var markup = @"
Imports System.Collections
Class Bar
    Implements IEnumerable
End Class";
            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.VisualBasic,
                new TestInheritanceMemberItem(
                    lineNumber: 3,
                    memberName: "Class Bar",
                    targets: ImmutableArray.Create(
                        new TargetInfo(
                            targetSymbolDisplayName: "Interface IEnumerable",
                            relationship: InheritanceRelationship.Implementing,
                            inMetadata: true))));
        }

        [Fact]
        public Task TestVisualBasicEventStatement()
        {
            var markup = @"
Interface {|target2:IBar|}
    Event {|target4:e|} As EventHandler
End Interface
Class {|target1:Bar|}
    Implements IBar
    Public Event {|target3:e|} As EventHandler Implements IBar.e
End Class";

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForEventInInterface = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Event IBar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event Bar.e As EventHandler",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForEventInClass = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Event Bar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event IBar.e As EventHandler",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.VisualBasic,
                itemForIBar,
                itemForBar,
                itemForEventInInterface,
                itemForEventInClass);
        }

        [Fact]
        public Task TestVisualBasicEventBlock()
        {
            var markup = @"
Interface {|target2:IBar|}
    Event {|target4:e|} As EventHandler
End Interface
Class {|target1:Bar|}
    Implements IBar
    Public Custom Event {|target3:e|} As EventHandler Implements IBar.e
    End Event
End Class";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForEventInInterface = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Event IBar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event Bar.e As EventHandler",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForEventInClass = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Event Bar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event IBar.e As EventHandler",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.VisualBasic,
                itemForIBar,
                itemForBar,
                itemForEventInInterface,
                itemForEventInClass);
        }

        [Fact]
        public Task TestVisualBasicInterfaceMembers()
        {
            var markup = @"
Interface {|target2:IBar|}
    Property {|target4:Poo|} As Integer
    Function {|target6:Foo|}() As Integer
End Interface

Class {|target1:Bar|}
    Implements IBar
    Public Property {|target3:Poo|} As Integer Implements IBar.Poo
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Function {|target5:Foo|}() As Integer Implements IBar.Foo
        Return 1
    End Function
End Class";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar",
                    locationTag: "target1",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForPooInInterface = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Property IBar.Poo As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Property Bar.Poo As Integer",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForPooInClass = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "Property Bar.Poo As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Property IBar.Poo As Integer",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.Implementing)));

            var itemForFooInInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Function IBar.Foo() As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Function Bar.Foo() As Integer",
                    locationTag: "target5",
                    relationship: InheritanceRelationship.Implemented)));

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 16,
                memberName: "Function Bar.Foo() As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Function IBar.Foo() As Integer",
                    locationTag: "target6",
                    relationship: InheritanceRelationship.Implementing)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.VisualBasic,
                itemForIBar,
                itemForBar,
                itemForPooInInterface,
                itemForPooInClass,
                itemForFooInInterface,
                itemForFooInClass);
        }

        [Fact]
        public Task TestVisualBasicMustInheritClassMember()
        {
            var markup = @"
MustInherit Class {|target2:Bar1|}
    Public MustOverride Sub {|target4:Foo|}()
End Class

Class {|target1:Bar|}
    Inherits Bar1
    Public Overrides Sub {|target3:Foo|}()
    End Sub
End Class";
            var itemForBar1 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Class Bar1",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: $"Class Bar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "Class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Class Bar1",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implementing)));

            var itemForFooInBar1 = new TestInheritanceMemberItem(
                    lineNumber: 3,
                    memberName: "MustOverride Sub Bar1.Foo()",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "Overrides Sub Bar.Foo()",
                            locationTag: "target3",
                            relationship: InheritanceRelationship.Overridden)));

            var itemForFooInBar = new TestInheritanceMemberItem(
                    lineNumber: 8,
                    memberName: "Overrides Sub Bar.Foo()",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "MustOverride Sub Bar1.Foo()",
                            locationTag: "target4",
                            relationship: InheritanceRelationship.Overriding)));

            return VerifyInSingleDocumentAsync(
                markup,
                 LanguageNames.VisualBasic,
                itemForBar1,
                 itemForBar,
                itemForFooInBar1,
                itemForFooInBar);
        }
        #endregion
    }
}
