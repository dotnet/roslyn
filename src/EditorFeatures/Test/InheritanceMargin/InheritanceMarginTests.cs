// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.InheritanceMargin
{
    [Trait(Traits.Feature, Traits.Features.InheritanceMargin)]
    [UseExportProvider]
    public class InheritanceMarginTests
    {
        private const string Interface = TextTags.Interface;
        private const string Class = TextTags.Class;
        private const string Method = TextTags.Method;
        private const string Property = TextTags.Property;
        private const string Event = TextTags.Event;

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
            var actualItems = await service.GetInheritanceInfoAsync(document, cancellationToken).ConfigureAwait(false);

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
            Assert.Equal(expectedItem.MemberName, actualItem.MemberDescription.Text);
            Assert.Equal(expectedItem.MemberSymbolKind, actualItem.MemberDescription.Tag);
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
            Assert.Equal(expectedTarget.TargetSymbolName, actualTarget.TargetDescription.Text);
            Assert.Equal(expectedTarget.TargetSymbolKind, actualTarget.TargetDescription.Tag);
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
            public readonly string MemberSymbolKind;
            public readonly string MemberName;
            public readonly ImmutableArray<TargetInfo> Targets;

            public TestInheritanceMemberItem(
                int lineNumber,
                string memberSymbolKind,
                string memberName,
                ImmutableArray<TargetInfo> targets)
            {
                LineNumber = lineNumber;
                MemberSymbolKind = memberSymbolKind;
                MemberName = memberName;
                Targets = targets;
            }
        }

        private readonly struct TargetInfo
        {
            public readonly string TargetSymbolName;
            public readonly string TargetSymbolKind;
            public readonly string LocationTag;
            public readonly InheritanceRelationship Relationship;

            public TargetInfo(
                string targetSymbolName,
                string targetSymbolKind,
                string locationTag,
                InheritanceRelationship relationship)
            {
                TargetSymbolName = targetSymbolName;
                TargetSymbolKind = targetSymbolKind;
                LocationTag = locationTag;
                Relationship = relationship;
            }
        }

        private readonly struct TestInheritanceTargetItem
        {
            public readonly string TargetSymbolName;
            public readonly string TargetSymbolKind;
            public readonly InheritanceRelationship RelationshipToMember;
            public readonly ImmutableArray<DocumentSpan> DocumentSpans;

            private TestInheritanceTargetItem(
                string targetSymbolName,
                string targetSymbolKind,
                InheritanceRelationship relationshipToMember,
                ImmutableArray<DocumentSpan> documentSpans)
            {
                TargetSymbolName = targetSymbolName;
                TargetSymbolKind = targetSymbolKind;
                RelationshipToMember = relationshipToMember;
                DocumentSpans = documentSpans;
            }

            public static TestInheritanceTargetItem Create(
                TargetInfo targetInfo,
                TestWorkspace testWorkspace)
            {
                using var _ = PooledObjects.ArrayBuilder<DocumentSpan>.GetInstance(out var builder);
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
                    targetInfo.TargetSymbolName,
                    targetInfo.TargetSymbolKind,
                    targetInfo.Relationship,
                    builder.ToImmutable());
            }
        }
        #endregion

        #region TestsForCSharp

        [Fact]
        public async Task TestClassImplementingInterface()
        {
            var markup = @"
interface {|target1:IBar|} { }
public class {|target2:Bar|} : IBar
{
}
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberSymbolKind: Interface,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberSymbolKind: Class,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "IBar",
                        targetSymbolKind: Interface,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            await VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestInterfaceImplemetingInterface()
        {
            var markup = @"
interface {|target1:IBar|} { }
interface {|target2:IBar2|} : IBar { }
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberSymbolKind: Interface,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberSymbolKind: Class,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "IBar",
                        targetSymbolKind: Interface,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            await VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestClassInheritsClass()
        {
            var markup = @"
class {target2:A} { }
class {target1:B} : A { }
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberSymbolKind: Class,
                memberName: "A",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
            );
            var itemOnLine3 = new TestInheritanceMemberItem(
                lineNumber: 3,
                memberSymbolKind: Class,
                memberName: "B",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "A",
                        targetSymbolKind: Class,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
            );

            await VerifyInSingleDocumentAsync(
                markup,
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine3).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("void {|target4:Foo|}();", "public void {|target3:Foo|}() { }", "Foo", Method)]
        [InlineData("int {|target4:Property|} { get; set; }", "public int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("int {|target4:Property|} { get; }", "public int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("int {|target4:Property|} { set; }", "public int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("event EventHandler {target4:|e|};", "event EventHandler {target3:|e|}", "e", Event)]
        public async Task TestSingleInterfaceMember(string interfaceDeclaration, string classDeclaration, string name, string memberKind)
        {
            var markupTemplate = @"using System;
interface {|target1:IBar|}
{
    {0}
}
public class {|target2:Bar|} : IBar
{
    {1}
}
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberSymbolKind: Interface,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemOnLine6 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberSymbolKind: Class,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "IBar",
                        targetSymbolKind: Interface,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemOnLine4 = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberSymbolKind: memberKind,
                memberName: name,
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target3",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemOnLine8 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberSymbolKind: memberKind,
                memberName: name,
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Implementing))
                );

            await VerifyInSingleDocumentAsync(
                string.Format(markupTemplate, interfaceDeclaration, classDeclaration),
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine6,
                itemOnLine4,
                itemOnLine8).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("public abstract void {|target4:Foo|}();", "public override void {|target3:Foo|}() { }", "Foo", Method)]
        [InlineData("public abstract int {|target4:Property|} { get; set; }", "public override int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("public abstract int {|target4:Property|} { get; }", "public override int |target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("public abstract int {|target4:Property|} { set; }", "public override int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("public abstract event EventHandler {target4:|e|};", "public override event EventHandler {target3:|e|}", "e", Event)]
        [InlineData("public virtual void {|target4:Foo|}();", "public override void {|target3:Foo|}() { }", "Foo", Method)]
        [InlineData("public virtual int {|target4:Property|} { get; set; }", "public override int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("public virtual int {|target4:Property|} { get; }", "public override int |target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("public virtual int {|target4:Property|} { set; }", "public override int {|target3:Property|} { get; set; }", "Property", Property)]
        [InlineData("public virtual event EventHandler {target4:|e|};", "public override event EventHandler {target3:|e|}", "e", Event)]
        public async Task TestAbstractClass(string interfaceDeclaration, string classDeclaration, string name, string memberKind)
        {
            var markupTemplate = @"using System;
public abstract class {|target2:Bar|}
{
    {0}
}
public class {|target1:Bar2|} : Bar
{
    {1}
}
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberSymbolKind: Class,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar2",
                        targetSymbolKind: Class,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Overriden))
                );

            var itemOnLine6 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberSymbolKind: Class,
                memberName: "Bar2",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Overriding))
                );

            var itemOnLine4 = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberSymbolKind: memberKind,
                memberName: name,
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target3",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemOnLine8 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberSymbolKind: memberKind,
                memberName: name,
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Implementing))
                );

            await VerifyInSingleDocumentAsync(
                string.Format(markupTemplate, interfaceDeclaration, classDeclaration),
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine6,
                itemOnLine4,
                itemOnLine8).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("void {|target4:Foo|}();", "public virtual void {|target5:Foo|}() { }", "public override void {|target6:Foo|}() { }", "Foo", Method)]
        [InlineData("int {|target4:Property|} { get; set; }",
            "public virtual int {|target5:Property|} { get; set; }",
            "public override int {|target6:Property|} { get; set; }",
            "Property", Property)]
        [InlineData("event EventHandler {|target4:e|};",
            "public virtual event EventHandler {|target5:e|};",
            "public override event EventHandler {|target6:e|};",
            "e", Event)]
        public async Task TestAbstractClassAndInterface(
            string interfaceDeclaration,
            string abstractClassDeclaration,
            string derivedClassDeclaration,
            string name,
            string memberKind)
        {
            var markupTemplate = @"using System;
public interface {|target3:IBar|}
{
    {0}
}
public abstract class {|target2:Bar|}
{
    {1}
}
public class {|target1:Bar2|} : Bar
{
    {2}
}
            ";

            var itemOnLine2 = new TestInheritanceMemberItem(
                lineNumber: 2,
                memberSymbolKind: Interface,
                memberName: "IBar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar2",
                        targetSymbolKind: Class,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemOnLine4 = new TestInheritanceMemberItem(
                lineNumber: 4,
                memberSymbolKind: memberKind,
                memberName: name,
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target5",
                        relationship: InheritanceRelationship.Implemented))
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target6",
                        relationship: InheritanceRelationship.Implemented)));

            var itemOnLine6 = new TestInheritanceMemberItem(
                lineNumber: 6,
                memberSymbolKind: Class,
                memberName: "Bar",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar2",
                        targetSymbolKind: Class,
                        locationTag: "target1",
                        relationship: InheritanceRelationship.Implemented))
                    .Add(new TargetInfo(
                        targetSymbolName: "IBar",
                        targetSymbolKind: Interface,
                        locationTag: "target3",
                        relationship: InheritanceRelationship.Implementing))
                );

            var itemOnLine8 = new TestInheritanceMemberItem(
                lineNumber: 8,
                memberSymbolKind: Class,
                memberName: "Bar2",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: memberKind,
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Implemented))
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target6",
                        relationship: InheritanceRelationship.Overriden))
                );

            var itemOnLine10 = new TestInheritanceMemberItem(
                lineNumber: 10,
                memberSymbolKind: Class,
                memberName: "Bar2",
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: "Bar",
                        targetSymbolKind: Class,
                        locationTag: "target2",
                        relationship: InheritanceRelationship.Implemented))
                    .Add(new TargetInfo(
                        targetSymbolName: "IBar",
                        targetSymbolKind: Interface,
                        locationTag: "target3",
                        relationship: InheritanceRelationship.Implemented))
                );

            var itemOnLine12 = new TestInheritanceMemberItem(
                lineNumber: 12,
                memberSymbolKind: memberKind,
                memberName: name,
                targets: ImmutableArray<TargetInfo>.Empty
                    .Add(new TargetInfo(
                        targetSymbolName: name,
                        targetSymbolKind: memberKind,
                        locationTag: "target5",
                        relationship: InheritanceRelationship.Overriding))
                    .Add(new TargetInfo(
                        targetSymbolName: "IBar",
                        targetSymbolKind: memberKind,
                        locationTag: "target4",
                        relationship: InheritanceRelationship.Implementing))
                );

            await VerifyInSingleDocumentAsync(
                string.Format(markupTemplate, interfaceDeclaration, abstractClassDeclaration, derivedClassDeclaration),
                LanguageNames.CSharp,
                itemOnLine2,
                itemOnLine6,
                itemOnLine4,
                itemOnLine8,
                itemOnLine10,
                itemOnLine12).ConfigureAwait(false);
        }

        #endregion
    }
}
