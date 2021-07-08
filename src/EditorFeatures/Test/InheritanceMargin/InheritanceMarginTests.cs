﻿// Licensed to the .NET Foundation under one or more agreements.
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

        private static Task VerifyInSingleDocumentAsync(
            string markup,
            string languageName,
            params TestInheritanceMemberItem[] memberItems)
        {
            markup = @$"<![CDATA[
{markup}]]>";

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

            var testHostDocument = testWorkspace.Documents[0];
            return VerifyTestMemberInDocumentAsync(testWorkspace, testHostDocument, memberItems, cancellationToken);
        }

        private static async Task VerifyTestMemberInDocumentAsync(
            TestWorkspace testWorkspace,
            TestHostDocument testHostDocument,
            TestInheritanceMemberItem[] memberItems,
            CancellationToken cancellationToken)
        {
            var document = testWorkspace.CurrentSolution.GetRequiredDocument(testHostDocument.Id);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var searchingSpan = root.Span;
            // Look for the search span, if not found, then pass the whole document span to the service.
            if (testHostDocument.AnnotatedSpans.TryGetValue(SearchAreaTag, out var spans) && spans.IsSingle())
            {
                searchingSpan = spans[0];
            }

            var service = document.GetRequiredLanguageService<IInheritanceMarginService>();
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
                .Select(info => TestInheritanceTargetItem.Create(info, testWorkspace))
                .OrderBy(target => target.TargetSymbolName)
                .ToImmutableArray();
            var sortedActualTargets = actualItem.TargetItems.OrderBy(target => target.DefinitionItem.DisplayParts.JoinText())
                .ToImmutableArray();
            for (var i = 0; i < expectedTargets.Length; i++)
            {
                VerifyInheritanceTarget(expectedTargets[i], sortedActualTargets[i]);
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

        /// <summary>
        /// Project of markup1 is referencing project of markup2
        /// </summary>
        private static async Task VerifyInDifferentProjectsAsync(
            (string markupInProject1, string languageName) markup1,
            (string markupInProject2, string languageName) markup2,
            TestInheritanceMemberItem[] memberItemsInMarkup1,
            TestInheritanceMemberItem[] memberItemsInMarkup2)
        {
            var workspaceFile =
                $@"
<Workspace>
    <Project Language=""{markup1.languageName}"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <ProjectReference>Assembly2</ProjectReference>
        <Document>
            <![CDATA[
                {markup1.markupInProject1}]]>
        </Document>
    </Project>
    <Project Language=""{markup2.languageName}"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
            <![CDATA[
                {markup2.markupInProject2}]]>
        </Document>
    </Project>
</Workspace>";

            var cancellationToken = CancellationToken.None;
            using var testWorkspace = TestWorkspace.Create(
                workspaceFile,
                composition: EditorTestCompositions.EditorFeatures);

            var testHostDocument1 = testWorkspace.Documents.Single(doc => doc.Project.AssemblyName.Equals("Assembly1"));
            var testHostDocument2 = testWorkspace.Documents.Single(doc => doc.Project.AssemblyName.Equals("Assembly2"));
            await VerifyTestMemberInDocumentAsync(testWorkspace, testHostDocument1, memberItemsInMarkup1, cancellationToken).ConfigureAwait(false);
            await VerifyTestMemberInDocumentAsync(testWorkspace, testHostDocument2, memberItemsInMarkup2, cancellationToken).ConfigureAwait(false);
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
        public Task TestCSharpClassWithErrorBaseType()
        {
            var markup = @"
public class Bar : SomethingUnknown
{
}";
            return VerifyNoItemForDocumentAsync(markup, LanguageNames.CSharp);
        }

        [Fact]
        public Task TestCSharpReferencingMetadata()
        {
            var markup = @"
using System.Collections;
public class Bar : IEnumerable
{
    public IEnumerator GetEnumerator () { return null };
}";
            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IEnumerable",
                        relationship: InheritanceRelationship.ImplementedInterface,
                        inMetadata: true)));

            var itemForGetEnumerator = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "IEnumerator Bar.GetEnumerator()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "IEnumerator IEnumerable.GetEnumerator()",
                        relationship: InheritanceRelationship.ImplementedMember,
                        inMetadata: true)));

            return VerifyInSingleDocumentAsync(markup, LanguageNames.CSharp, itemForBar, itemForGetEnumerator);
        }

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
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementedInterface)));

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
                        relationship: InheritanceRelationship.ImplementingType))
                );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "interface IBar2",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.InheritedInterface))
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
                        relationship: InheritanceRelationship.DerivedType))
            );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "class B",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class A",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.BaseType))
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
                            relationship: InheritanceRelationship.BaseType))));
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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForEventInInterface = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "event EventHandler IBar.e",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler Bar.e",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForEventInClass = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "event EventHandler Bar.e",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler IBar.e",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.ImplementedMember)));

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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForE1InInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "event EventHandler IBar.e1",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler Bar.e1",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForE2InInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "event EventHandler IBar.e2",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler Bar.e2",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForE1InClass = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "event EventHandler Bar.e1",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler IBar.e1",
                    locationTag: "target5",
                    relationship: InheritanceRelationship.ImplementedMember)));

            var itemForE2InClass = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "event EventHandler Bar.e2",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "event EventHandler IBar.e2",
                    locationTag: "target6",
                    relationship: InheritanceRelationship.ImplementedMember)));

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
                        relationship: InheritanceRelationship.ImplementedMember))
                );

            var itemForEooInInterface = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "event EventHandler IBar.Eoo",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "event EventHandler Bar.Eoo",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.ImplementingMember))
                );

            var itemForPooInInterface = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "int IBar.Poo { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int Bar.Poo { get; set; }",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.ImplementingMember))
                );

            var itemForPooInClass = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberName: "int Bar.Poo { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int IBar.Poo { get; set; }",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember))
                );

            var itemForFooInInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void IBar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void Bar.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementingMember))
                );

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 11,
                memberName: "void Bar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedMember))
                );

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementingType))
                );

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementedInterface))
                );

            var itemForIndexerInClass = new TestInheritanceMemberItem(
                lineNumber: 14,
                memberName: "int Bar.this[int] { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int IBar.this[int] { get; set; }",
                        locationTag: "target9",
                        relationship: InheritanceRelationship.ImplementedMember))
                );

            var itemForIndexerInInterface = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "int IBar.this[int] { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int Bar.this[int] { get; set; }",
                        locationTag: "target10",
                        relationship: InheritanceRelationship.ImplementingMember))
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
                        relationship: InheritanceRelationship.OverriddenMember)));

            var itemForEooInAbstractClass = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: $"{modifier} event EventHandler Bar.Eoo",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "override event EventHandler Bar2.Eoo",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.OverridingMember)));

            var itemForPooInClass = new TestInheritanceMemberItem(
                    lineNumber: 11,
                    memberName: "override int Bar2.Poo { get; set; }",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: $"{modifier} int Bar.Poo {{ get; set; }}",
                            locationTag: "target6",
                            relationship: InheritanceRelationship.OverriddenMember)));

            var itemForPooInAbstractClass = new TestInheritanceMemberItem(
                    lineNumber: 5,
                    memberName: $"{modifier} int Bar.Poo {{ get; set; }}",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "override int Bar2.Poo { get; set; }",
                            locationTag: "target5",
                            relationship: InheritanceRelationship.OverridingMember)));

            var itemForFooInAbstractClass = new TestInheritanceMemberItem(
                    lineNumber: 4,
                    memberName: $"{modifier} void Bar.Foo()",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "override void Bar2.Foo()",
                            locationTag: "target3",
                            relationship: InheritanceRelationship.OverridingMember)));

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberName: "override void Bar2.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: $"{modifier} void Bar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.OverriddenMember)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar2",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.DerivedType)));

            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "class Bar2",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.BaseType)));

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

        [Theory]
        [CombinatorialData]
        public Task TestCSharpOverrideMemberCanFindImplementingInterface(bool testDuplicate)
        {
            var markup1 = @"using System;
        public interface {|target4:IBar|}
        {
            void {|target6:Foo|}();
        }
        public class {|target1:Bar1|} : IBar
        {
            public virtual void {|target2:Foo|}() { }
        }
        public class {|target5:Bar2|} : Bar1
        {
            public override void {|target3:Foo|}() { }
        }";

            var markup2 = @"using System;
        public interface {|target4:IBar|}
        {
            void {|target6:Foo|}();
        }
        public class {|target1:Bar1|} : IBar
        {
            public virtual void {|target2:Foo|}() { }
        }
        public class {|target5:Bar2|} : Bar1, IBar
        {
            public override void {|target3:Foo|}() { }
        }";

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar1",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementingType),
                new TargetInfo(
                    targetSymbolDisplayName: "class Bar2",
                    locationTag: "target5",
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInIBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void IBar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "virtual void Bar1.Foo()",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementingMember),
                    new TargetInfo(
                        targetSymbolDisplayName: "override void Bar2.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForBar1 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "class Bar1",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedInterface),
                    new TargetInfo(
                        targetSymbolDisplayName: "class Bar2",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.DerivedType)));

            var itemForFooInBar1 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "virtual void Bar1.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar.Foo()",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember),
                    new TargetInfo(
                        targetSymbolDisplayName: "override void Bar2.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.OverridingMember)));

            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberName: "class Bar2",
                targets: ImmutableArray.Create(
                    new TargetInfo(
                        targetSymbolDisplayName: "class Bar1",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.BaseType),
                    new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForFooInBar2 = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberName: "override void Bar2.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar.Foo()",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember),
                    new TargetInfo(
                        targetSymbolDisplayName: "virtual void Bar1.Foo()",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.OverriddenMember)));

            return VerifyInSingleDocumentAsync(
                testDuplicate ? markup2 : markup1,
                LanguageNames.CSharp,
                itemForIBar,
                itemForFooInIBar,
                itemForBar1,
                itemForFooInBar1,
                itemForBar2,
                itemForFooInBar2);
        }

        [Fact]
        public Task TestCSharpFindGenericsBaseType()
        {
            var markup = @"
public interface {|target2:IBar|}<T>
{
    void {|target4:Foo|}();
}

public class {|target1:Bar2|} : IBar<int>, IBar<string>
{
    public void {|target3:Foo|}();
}";

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar<T>",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar2",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInIBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void IBar<T>.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void Bar2.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementingMember)));

            // Only have one IBar<T> item
            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "class Bar2",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar<T>",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            // Only have one IBar<T>.Foo item
            var itemForFooInBar2 = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "void Bar2.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar<T>.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedMember)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForIBar,
                itemForFooInIBar,
                itemForBar2,
                itemForFooInBar2);
        }

        [Fact]
        public Task TestCSharpExplicitInterfaceImplementation()
        {
            var markup = @"
interface {|target2:IBar|}<T>
{
    void {|target3:Foo|}(T t);
}

abstract class {|target1:AbsBar|} : IBar<int>
{
    void IBar<int>.{|target4:Foo|}(int t)
    {
        throw new System.NotImplementedException();
    }
}";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface IBar<T>",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class AbsBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInIBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void IBar<T>.Foo(T)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void AbsBar.IBar<int>.Foo(int)",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForAbsBar = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "class AbsBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar<T>",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForFooInAbsBar = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "void AbsBar.IBar<int>.Foo(int)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar<T>.Foo(T)",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementedMember)
                ));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForIBar,
                itemForFooInIBar,
                itemForAbsBar,
                itemForFooInAbsBar);
        }

        [Fact]
        public Task TestStaticAbstractMemberInterface()
        {
            var markup = @"
interface {|target5:I1|}<T> where T : I1<T>
{
    static abstract void {|target4:M1|}();
    static abstract int {|target7:P1|} { get; set; }
    static abstract event EventHandler {|target9:e1|};
    static abstract int operator {|target11:+|}(T i1);
    static abstract implicit operator {|target12:int|}(T i1);
}

public class {|target1:Class1|} : I1<Class1>
{
    public static void {|target2:M1|}() {}
    public static int {|target6:P1|} { get => 1; set { } }
    public static event EventHandler {|target8:e1|};
    public static int operator {|target10:+|}(Class1 i) => 1;
    public static implicit operator {|target13:int|}(Class1 i) => 0;
}";
            var itemForI1 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "interface I1<T>",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Class1",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemForM1InI1 = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "void I1<T>.M1()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "static void Class1.M1()",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForAbsClass1 = new TestInheritanceMemberItem(
                lineNumber: 11,
                memberName: "class Class1",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface I1<T>",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForM1InClass1 = new TestInheritanceMemberItem(
                lineNumber: 13,
                memberName: "static void Class1.M1()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void I1<T>.M1()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForP1InI1 = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "int I1<T>.P1 { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "static int Class1.P1 { get; set; }",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForP1InClass1 = new TestInheritanceMemberItem(
                lineNumber: 14,
                memberName: "static int Class1.P1 { get; set; }",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int I1<T>.P1 { get; set; }",
                        locationTag: "target7",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForE1InI1 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "event EventHandler I1<T>.e1",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "static event EventHandler Class1.e1",
                        locationTag: "target8",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForE1InClass1 = new TestInheritanceMemberItem(
                lineNumber: 15,
                memberName: "static event EventHandler Class1.e1",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "event EventHandler I1<T>.e1",
                        locationTag: "target9",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForPlusOperatorInI1 = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "int I1<T>.operator +(T)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "static int Class1.operator +(Class1)",
                        locationTag: "target10",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForPlusOperatorInClass1 = new TestInheritanceMemberItem(
                lineNumber: 16,
                memberName: "static int Class1.operator +(Class1)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "int I1<T>.operator +(T)",
                        locationTag: "target11",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForIntOperatorInI1 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "I1<T>.implicit operator int(T)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "static Class1.implicit operator int(Class1)",
                        locationTag: "target13",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForIntOperatorInClass1 = new TestInheritanceMemberItem(
                lineNumber: 17,
                memberName: "static Class1.implicit operator int(Class1)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "I1<T>.implicit operator int(T)",
                        locationTag: "target12",
                        relationship: InheritanceRelationship.ImplementedMember)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemForI1,
                itemForAbsClass1,
                itemForM1InI1,
                itemForM1InClass1,
                itemForP1InI1,
                itemForP1InClass1,
                itemForE1InI1,
                itemForE1InClass1,
                itemForPlusOperatorInI1,
                itemForPlusOperatorInClass1,
                itemForIntOperatorInI1,
                itemForIntOperatorInClass1);
        }

        #endregion

        #region TestsForVisualBasic

        [Fact]
        public Task TestVisualBasicWithErrorBaseType()
        {
            var markup = @"
        Namespace MyNamespace
            Public Class Bar
                Implements SomethingNotExist
            End Class
        End Namespace";

            return VerifyNoItemForDocumentAsync(markup, LanguageNames.VisualBasic);
        }

        [Fact]
        public Task TestVisualBasicReferencingMetadata()
        {
            var markup = @"
        Namespace MyNamespace
            Public Class Bar
                Implements System.Collections.IEnumerable
                Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
                    Throw New NotImplementedException()
                End Function
            End Class
        End Namespace";
            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Interface IEnumerable",
                        relationship: InheritanceRelationship.ImplementedInterface,
                        inMetadata: true)));

            var itemForGetEnumerator = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "Function Bar.GetEnumerator() As IEnumerator",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Function IEnumerable.GetEnumerator() As IEnumerator",
                        relationship: InheritanceRelationship.ImplementedMember,
                        inMetadata: true)));

            return VerifyInSingleDocumentAsync(markup, LanguageNames.VisualBasic, itemForBar, itemForGetEnumerator);
        }

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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.ImplementedInterface)));

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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Interface IBar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar2",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.InheritedInterface)));
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
                    relationship: InheritanceRelationship.DerivedType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Class Bar2",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.BaseType)));
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
                            relationship: InheritanceRelationship.ImplementedInterface,
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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForEventInInterface = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Event IBar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event Bar.e As EventHandler",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForEventInClass = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Event Bar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event IBar.e As EventHandler",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.ImplementedMember)));

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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "Class Bar",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForEventInInterface = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Event IBar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event Bar.e As EventHandler",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForEventInClass = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Event Bar.e As EventHandler",
                ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Event IBar.e As EventHandler",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.ImplementedMember)));

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
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Interface IBar",
                    locationTag: "target2",
                    relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForPooInInterface = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Property IBar.Poo As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Property Bar.Poo As Integer",
                    locationTag: "target3",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForPooInClass = new TestInheritanceMemberItem(
                lineNumber: 9,
                memberName: "Property Bar.Poo As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Property IBar.Poo As Integer",
                    locationTag: "target4",
                    relationship: InheritanceRelationship.ImplementedMember)));

            var itemForFooInInterface = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Function IBar.Foo() As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Function Bar.Foo() As Integer",
                    locationTag: "target5",
                    relationship: InheritanceRelationship.ImplementingMember)));

            var itemForFooInClass = new TestInheritanceMemberItem(
                lineNumber: 16,
                memberName: "Function Bar.Foo() As Integer",
                targets: ImmutableArray.Create(new TargetInfo(
                    targetSymbolDisplayName: "Function IBar.Foo() As Integer",
                    locationTag: "target6",
                    relationship: InheritanceRelationship.ImplementedMember)));

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
                        relationship: InheritanceRelationship.DerivedType)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "Class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Class Bar1",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.BaseType)));

            var itemForFooInBar1 = new TestInheritanceMemberItem(
                    lineNumber: 3,
                    memberName: "MustOverride Sub Bar1.Foo()",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "Overrides Sub Bar.Foo()",
                            locationTag: "target3",
                            relationship: InheritanceRelationship.OverridingMember)));

            var itemForFooInBar = new TestInheritanceMemberItem(
                    lineNumber: 8,
                    memberName: "Overrides Sub Bar.Foo()",
                    targets: ImmutableArray.Create(new TargetInfo(
                            targetSymbolDisplayName: "MustOverride Sub Bar1.Foo()",
                            locationTag: "target4",
                            relationship: InheritanceRelationship.OverriddenMember)));

            return VerifyInSingleDocumentAsync(
                markup,
                 LanguageNames.VisualBasic,
                itemForBar1,
                 itemForBar,
                itemForFooInBar1,
                itemForFooInBar);
        }

        [Theory]
        [CombinatorialData]
        public Task TestVisualBasicOverrideMemberCanFindImplementingInterface(bool testDuplicate)
        {
            var markup1 = @"
        Interface {|target4:IBar|}
            Sub {|target6:Foo|}()
        End Interface

        Class {|target1:Bar1|}
            Implements IBar
            Public Overridable Sub {|target2:Foo|}() Implements IBar.Foo
            End Sub
        End Class

        Class {|target5:Bar2|}
            Inherits Bar1
            Public Overrides Sub {|target3:Foo|}()
            End Sub
        End Class";

            var markup2 = @"
        Interface {|target4:IBar|}
            Sub {|target6:Foo|}()
        End Interface

        Class {|target1:Bar1|}
            Implements IBar
            Public Overridable Sub {|target2:Foo|}() Implements IBar.Foo
            End Sub
        End Class

        Class {|target5:Bar2|}
            Inherits Bar1
            Public Overrides Sub {|target3:Foo|}()
            End Sub
        End Class";
            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Class Bar1",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementingType),
                new TargetInfo(
                    targetSymbolDisplayName: "Class Bar2",
                    locationTag: "target5",
                    relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInIBar = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Sub IBar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Overridable Sub Bar1.Foo()",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementingMember),
                    new TargetInfo(
                        targetSymbolDisplayName: "Overrides Sub Bar2.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementingMember)));

            var itemForBar1 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "Class Bar1",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Interface IBar",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedInterface),
                    new TargetInfo(
                        targetSymbolDisplayName: "Class Bar2",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.DerivedType)));

            var itemForFooInBar1 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberName: "Overridable Sub Bar1.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub IBar.Foo()",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember),
                    new TargetInfo(
                        targetSymbolDisplayName: "Overrides Sub Bar2.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.OverridingMember)));

            var itemForBar2 = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberName: "Class Bar2",
                targets: ImmutableArray.Create(
                    new TargetInfo(
                        targetSymbolDisplayName: "Class Bar1",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.BaseType),
                    new TargetInfo(
                        targetSymbolDisplayName: "Interface IBar",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForFooInBar2 = new TestInheritanceMemberItem(
                lineNumber: 14,
                memberName: "Overrides Sub Bar2.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub IBar.Foo()",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember),
                    new TargetInfo(
                        targetSymbolDisplayName: "Overridable Sub Bar1.Foo()",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.OverriddenMember)));

            return VerifyInSingleDocumentAsync(
                testDuplicate ? markup2 : markup1,
                LanguageNames.VisualBasic,
                itemForIBar,
                itemForFooInIBar,
                itemForBar1,
                itemForFooInBar1,
                itemForBar2,
                itemForFooInBar2);
        }

        [Fact]
        public Task TestVisualBasicFindGenericsBaseType()
        {
            var markup = @"
        Public Interface {|target5:IBar|}(Of T)
            Sub {|target6:Foo|}()
        End Interface

        Public Class {|target1:Bar|}
            Implements IBar(Of Integer)
            Implements IBar(Of String)

            Public Sub {|target3:Foo|}() Implements IBar(Of Integer).Foo
                Throw New NotImplementedException()
            End Sub

            Private Sub {|target4:IBar_Foo|}() Implements IBar(Of String).Foo
                Throw New NotImplementedException()
            End Sub
        End Class";

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberName: "Interface IBar(Of T)",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Class Bar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInIBar = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Sub IBar(Of T).Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub Bar.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementingMember),
                        new TargetInfo(
                            targetSymbolDisplayName: "Sub Bar.IBar_Foo()",
                            locationTag: "target4",
                            relationship: InheritanceRelationship.ImplementingMember)));

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "Class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Interface IBar(Of T)",
                        locationTag: "target5",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForFooInBar = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberName: "Sub Bar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub IBar(Of T).Foo()",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForIBar_FooInBar = new TestInheritanceMemberItem(
                lineNumber: 14,
                memberName: "Sub Bar.IBar_Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub IBar(Of T).Foo()",
                        locationTag: "target6",
                        relationship: InheritanceRelationship.ImplementedMember)));

            return VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.VisualBasic,
                itemForIBar,
                itemForFooInIBar,
                itemForBar,
                itemForFooInBar,
                itemForIBar_FooInBar);
        }

        #endregion

        [Fact]
        public Task TestCSharpProjectReferencingVisualBasicProject()
        {
            var markup1 = @"
        using MyNamespace;
        namespace BarNs
        {
            public class {|target2:Bar|} : IBar
            {
                public void {|target4:Foo|}() { }
            }
        }";

            var markup2 = @"
        Namespace MyNamespace
            Public Interface {|target1:IBar|}
                Sub {|target3:Foo|}()
            End Interface
        End Namespace";

            var itemForBar = new TestInheritanceMemberItem(
                lineNumber: 5,
                memberName: "class Bar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForFooInMarkup1 = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "void Bar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub IBar.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberName: "Interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "class Bar",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInMarkup2 = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Sub IBar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void Bar.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementingMember)));

            return VerifyInDifferentProjectsAsync(
                (markup1, LanguageNames.CSharp),
                (markup2, LanguageNames.VisualBasic),
                new[] { itemForBar, itemForFooInMarkup1 },
                new[] { itemForIBar, itemForFooInMarkup2 });
        }

        [Fact]
        public Task TestVisualBasicProjectReferencingCSharpProject()
        {
            var markup1 = @"
        Imports BarNs
        Namespace MyNamespace
            Public Class {|target2:Bar44|}
                Implements IBar

                Public Sub {|target4:Foo|}() Implements IBar.Foo
                End Sub
            End Class
        End Namespace";

            var markup2 = @"
        namespace BarNs
        {
            public interface {|target1:IBar|}
            {
                void {|target3:Foo|}();
            }
        }";

            var itemForBar44 = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "Class Bar44",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "interface IBar",
                        locationTag: "target1",
                        relationship: InheritanceRelationship.ImplementedInterface)));

            var itemForFooInMarkup1 = new TestInheritanceMemberItem(
                lineNumber: 7,
                memberName: "Sub Bar44.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "void IBar.Foo()",
                        locationTag: "target3",
                        relationship: InheritanceRelationship.ImplementedMember)));

            var itemForIBar = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberName: "interface IBar",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Class Bar44",
                        locationTag: "target2",
                        relationship: InheritanceRelationship.ImplementingType)));

            var itemForFooInMarkup2 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberName: "void IBar.Foo()",
                targets: ImmutableArray.Create(new TargetInfo(
                        targetSymbolDisplayName: "Sub Bar44.Foo()",
                        locationTag: "target4",
                        relationship: InheritanceRelationship.ImplementingMember)));

            return VerifyInDifferentProjectsAsync(
                (markup1, LanguageNames.VisualBasic),
                (markup2, LanguageNames.CSharp),
                new[] { itemForBar44, itemForFooInMarkup1 },
                new[] { itemForIBar, itemForFooInMarkup2 });
        }
    }
}
