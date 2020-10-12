﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class SnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(SnippetCompletionProvider);

        protected override TestComposition GetComposition()
            => base.GetComposition().AddParts(typeof(MockSnippetInfoService));

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsInEmptyFile()
            => await VerifyItemExistsAsync(@"$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetDescriptions()
            => await VerifyItemExistsAsync(@"$$", MockSnippetInfoService.SnippetShortcut, MockSnippetInfoService.SnippetTitle + Environment.NewLine + MockSnippetInfoService.SnippetDescription + Environment.NewLine + string.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, MockSnippetInfoService.SnippetShortcut), SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsInNamespace()
            => await VerifyItemExistsAsync(@"namespace NS { $$ }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsInClass()
            => await VerifyItemExistsAsync(@"namespace NS { class C { $$ } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsInMethod()
            => await VerifyItemExistsAsync(@"namespace NS { class C { void M() { $$ } } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsNotInLocalDeclarationIdentifier()
            => await VerifyItemIsAbsentAsync(@"namespace NS { class C { void M() { int $$ } } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsNotInEnum()
            => await VerifyItemIsAbsentAsync(@"namespace NS { enum E { $$ } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SnippetsInExpression()
            => await VerifyItemExistsAsync(@"namespace NS { class C { void M() { bool b = true && $$ } } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(608860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608860")]
        public async Task SnippetsInPreProcessorContextWhenShortcutBeginsWithHash()
            => await VerifyItemExistsAsync(@"#$$", MockSnippetInfoService.PreProcessorSnippetShortcut.Substring(1), sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(608860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608860")]
        public async Task SnippetsNotInPreProcessorContextWhenShortcutDoesNotBeginWithHash()
            => await VerifyItemIsAbsentAsync(@"#$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(770156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770156")]
        public async Task SnippetsNotInPreProcessorContextDirectiveNameAlreadyTyped()
            => await VerifyItemIsAbsentAsync(@"#region $$", MockSnippetInfoService.PreProcessorSnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(839555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/839555")]
        public async Task ShowRegionSnippetWithHashRTyped()
            => await VerifyItemExistsAsync(@"#r$$", MockSnippetInfoService.PreProcessorSnippetShortcut.Substring(1), sourceCodeKind: SourceCodeKind.Regular);

        [WorkItem(968256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowSnippetsFromOtherContext()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    $$
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2""  PreprocessorSymbols=""GOO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            await VerifyItemInLinkedFilesAsync(markup, MockSnippetInfoService.SnippetShortcut, null);
        }

        [WorkItem(1140893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1140893")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitWithEnterObeysOption()
        {
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcu", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcut", sendThroughEnterOption: EnterKeyRule.Always, expected: true);

            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcu", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: false);
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcut", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);

            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcu", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcut", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(6405, "https://github.com/dotnet/roslyn/issues/6405")]
        public async Task SnippetsNotInPreProcessorContextForScriptDirectives()
        {
            await VerifyItemIsAbsentAsync(@"#r f$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync(@"#load f$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync(@"#!$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Script);
        }

        [ExportLanguageService(typeof(ISnippetInfoService), LanguageNames.CSharp, ServiceLayer.Test), Shared, PartNotDiscoverable]
        private class MockSnippetInfoService : ISnippetInfoService
        {
            internal const string SnippetShortcut = nameof(SnippetShortcut);
            internal const string SnippetDescription = nameof(SnippetDescription);
            internal const string SnippetTitle = nameof(SnippetTitle);
            internal const string SnippetPath = nameof(SnippetPath);

            internal const string PreProcessorSnippetShortcut = "#PreProcessorSnippetShortcut";
            internal const string PreProcessorSnippetDescription = nameof(PreProcessorSnippetDescription);
            internal const string PreProcessorSnippetTitle = "#PreProcessorSnippetTitle";
            internal const string PreProcessorSnippetPath = nameof(PreProcessorSnippetPath);

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public MockSnippetInfoService()
            {
            }

            public IEnumerable<SnippetInfo> GetSnippetsIfAvailable()
            {
                return new List<SnippetInfo>
                    {
                        new SnippetInfo(SnippetShortcut, SnippetTitle, SnippetDescription, SnippetPath),
                        new SnippetInfo(PreProcessorSnippetShortcut, PreProcessorSnippetTitle, PreProcessorSnippetDescription, PreProcessorSnippetPath)
                    };
            }

            public bool SnippetShortcutExists_NonBlocking(string shortcut)
            {
                return string.Equals(shortcut, SnippetShortcut, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(shortcut, PreProcessorSnippetShortcut, StringComparison.OrdinalIgnoreCase);
            }

            public bool ShouldFormatSnippet(SnippetInfo snippetInfo)
                => false;
        }
    }
}
