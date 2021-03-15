// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.InheritanceMargin;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.InheritanceMargin
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.InheritanceMargin)]
    public class InheritanceMarginTaggerTests
    {
        private readonly string Overriding = KnownMonikers.Overriding.ToString();
        private readonly string Overriden = KnownMonikers.Overridden.ToString();
        private readonly string Implementing = KnownMonikers.Implementing.ToString();
        private readonly string Implemented = KnownMonikers.Implemented.ToString();
        private readonly string ImplementingOverriden = KnownMonikers.ImplementingOverridden.ToString();
        private readonly string ImplementingAndOverriding = KnownMonikers.ImplementingOverriding.ToString();

        [WpfFact]
        public Task Test1()
        {
            var markup = @"
interface {|target1: IBar|} { }
{|margin, implemented, A target1=IBar: class A : IBar { }|}";
            return VerifyInSameFileAsync(markup, LanguageNames.CSharp);
        }

        private async Task VerifyInSameFileAsync(string markup, string languageName)
        {
            var workspaceFile = $@"
<Workspace>
   <Project Language=""{languageName}"" CommonReferences=""true"">
       <Document>
            {markup}
       </Document>
   </Project>
</Workspace>";

            using var testWorkspace = TestWorkspace.Create(
                workspaceFile,
                composition: EditorTestCompositions.EditorFeaturesWpf);

            var contentType = languageName switch
            {
                LanguageNames.CSharp => ContentTypeNames.CSharpContentType,
                LanguageNames.VisualBasic => ContentTypeNames.VisualBasicContentType,
                _ => throw ExceptionUtilities.UnexpectedValue(languageName),
            };

            var taggerProvider = testWorkspace.GetService<IViewTaggerProvider>(contentType, nameof(InheritanceChainMarginTaggerProvider));
            var testAccessor = ((InheritanceChainMarginTaggerProvider)taggerProvider).GetTestAccessor();
            var testHostDocument = testWorkspace.Documents.Single();
            var document = testWorkspace.CurrentSolution.GetRequiredDocument(testHostDocument.Id);

            var context = new TaggerContext<InheritanceMarginTag>(document, testHostDocument.GetTextView().TextSnapshot);
            await testAccessor.ProduceTagsAsync(context);
            var tagSpans = context.tagSpans.ToImmutableArray();
            await VerifyTagAsync(document, testHostDocument.AnnotatedSpans, tagSpans);
        }

        private static async Task VerifyTagAsync(
            Document document,
            IDictionary<string, ImmutableArray<TextSpan>> selectedSpan,
            ImmutableArray<ITagSpan<InheritanceMarginTag>> tagSpans)
        {
            var sourceText = await document.GetTextAsync().ConfigureAwait(false);
            var allExpectedMargins = selectedSpan
                .Where(kvp => kvp.Key.StartsWith("margin"))
                .ToDictionary(
                    keySelector: kvp => kvp.Key,
                    elementSelector: kvp => kvp.Value[0]);

            var targetToSpans = selectedSpan
                .Where(kvp => kvp.Key.StartsWith("target"))
                .ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.SelectAsArray(span => new DocumentSpan(document, span)));

            var testLineMargins = allExpectedMargins
                .Select(kvp => ParseTestLineMargin(kvp.Key, kvp.Value, sourceText, targetToSpans))
                .OrderBy(testMargin => testMargin.LineNumber)
                .ToImmutableArray();
            var sortedTagSpans = tagSpans
                .OrderBy(tagSpan => tagSpan.Span.Start)
                .ToImmutableArray();
            Assert.Equal(testLineMargins.Length, sortedTagSpans.Length);
            for (var i = 0; i < testLineMargins.Length; i++)
            {
                VerifyTestLineMargin(testLineMargins[i], sortedTagSpans[i]);
            }
        }

        private static void VerifyTestLineMargin(TestLineMargin expectedMargin, ITagSpan<InheritanceMarginTag> actualTaggedSpan)
        {
            var snapshot = actualTaggedSpan.Span.Snapshot;
            var span = actualTaggedSpan.Span;
            var lineOfStart = snapshot.GetLineNumberFromPosition(span.Start);
            var lineOfEnd = snapshot.GetLineNumberFromPosition(span.End);
            // The whole line should be tagged.
            Assert.Equal(expectedMargin.LineNumber, lineOfStart);
            Assert.Equal(expectedMargin.LineNumber, lineOfEnd);

            var tag = actualTaggedSpan.Tag;
            Assert.Equal(expectedMargin.Moniker, tag.Moniker.ToString());
            Assert.Equal(expectedMargin.Members.Length, tag.MembersOnLine.Length);
            for (var i = 0; i < expectedMargin.Members.Length; i++)
            {
                var expectedMember = expectedMargin.Members[i];
                var actualMember = tag.MembersOnLine[i];
                Assert.Equal(expectedMember.MemberName, actualMember.MemberDescription.Text);
                Assert.Equal(expectedMember.Targets.Length, actualMember.TargetItems.Length);
                for (var j = 0; j < expectedMember.Targets.Length; j++)
                {
                    var expectedTarget = expectedMember.Targets[j];
                    var actualTarget = actualMember.TargetItems[j];
                    Assert.Equal(expectedTarget.TargetName, actualTarget.TargetDescription.Text);
                    Assert.Equal(expectedTarget.DocumentSpans, actualTarget.DefinitionItem.SourceSpans);
                }
            }
        }

        private static TestLineMargin ParseTestLineMargin(
            string marginText,
            TextSpan marginSpan,
            SourceText sourceText,
            ImmutableDictionary<string, ImmutableArray<DocumentSpan>> targetIdToDocumentSpans)
        {
            var marginTextGroup = marginText.Split(',')
                .SelectAsArray(text => text.Trim());
            var lineNumber = sourceText.Lines.GetLineFromPosition(marginSpan.Start).LineNumber;

            var moniker = marginTextGroup[1];
            var memberToTargets = marginTextGroup
                    .Skip(1)
                    .ToDictionary(
                        keySelector: text => text.Split(' ').First().Trim(),
                        elementSelector: text => text.Split(' ')
                            .Skip(1)
                            .SelectAsArray(targetAndName => (TargetId: targetAndName.Substring(0, targetAndName.IndexOf("=", StringComparison.Ordinal)), Name: targetAndName.Substring(targetAndName.IndexOf("=", StringComparison.Ordinal) + 1))));
            using var _ = ArrayBuilder<TestMemberTag>.GetInstance(out var builder);
            foreach (var (member, targets) in memberToTargets)
            {
                var testTargetTags = targets
                    .SelectAsArray(target => new TestTargetTag(target.Name, targetIdToDocumentSpans[target.TargetId]));
                var testMemberTag = new TestMemberTag(member, testTargetTags);
                builder.Add(testMemberTag);
            }

            return new TestLineMargin(moniker, lineNumber, builder.ToImmutableArray());
        }

        private class TestLineMargin
        {
            public readonly string Moniker;
            public readonly int LineNumber;
            public readonly ImmutableArray<TestMemberTag> Members;

            public TestLineMargin(string moniker, int lineNumber, ImmutableArray<TestMemberTag> members)
            {
                Moniker = moniker;
                LineNumber = lineNumber;
                Members = members;
            }
        }

        private class TestMemberTag
        {
            public readonly string MemberName;
            public readonly ImmutableArray<TestTargetTag> Targets;

            public TestMemberTag(string memberName, ImmutableArray<TestTargetTag> targets)
            {
                MemberName = memberName;
                Targets = targets;
            }
        }

        private class TestTargetTag
        {
            public readonly string TargetName;
            public readonly ImmutableArray<DocumentSpan> DocumentSpans;

            public TestTargetTag(string targetName, ImmutableArray<DocumentSpan> documentSpans)
            {
                TargetName = targetName;
                DocumentSpans = documentSpans;
            }
        }
    }
}
