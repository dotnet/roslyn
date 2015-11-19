// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class SnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public SnippetCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new SnippetCompletionProvider(new MockSnippetInfoService());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsInEmptyFile()
        {
            VerifyItemExists(@"$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        public void SnippetDescriptions()
        {
            VerifyItemExists(@"$$", MockSnippetInfoService.SnippetShortcut, MockSnippetInfoService.SnippetTitle + Environment.NewLine + MockSnippetInfoService.SnippetDescription, SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsInNamespace()
        {
            VerifyItemExists(@"namespace NS { $$ }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsInClass()
        {
            VerifyItemExists(@"namespace NS { class C { $$ } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsInMethod()
        {
            VerifyItemExists(@"namespace NS { class C { void M() { $$ } } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsNotInLocalDeclarationIdentifier()
        {
            VerifyItemIsAbsent(@"namespace NS { class C { void M() { int $$ } } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsNotInEnum()
        {
            VerifyItemIsAbsent(@"namespace NS { enum E { $$ } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SnippetsInExpression()
        {
            VerifyItemExists(@"namespace NS { class C { void M() { bool b = true && $$ } } }", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(608860)]
        public void SnippetsInPreProcessorContextWhenShortcutBeginsWithHash()
        {
            VerifyItemExists(@"#$$", MockSnippetInfoService.PreProcessorSnippetShortcut.Substring(1), sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(608860)]
        public void SnippetsNotInPreProcessorContextWhenShortcutDoesNotBeginWithHash()
        {
            VerifyItemIsAbsent(@"#$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(770156)]
        public void SnippetsNotInPreProcessorContextDirectiveNameAlreadyTyped()
        {
            VerifyItemIsAbsent(@"#region $$", MockSnippetInfoService.PreProcessorSnippetShortcut, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(839555)]
        public void ShowRegionSnippetWithHashRTyped()
        {
            VerifyItemExists(@"#r$$", MockSnippetInfoService.PreProcessorSnippetShortcut.Substring(1), sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(968256)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowSnippetsFromOtherContext()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if FOO
    $$
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2""  PreprocessorSymbols=""FOO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            VerifyItemInLinkedFiles(markup, MockSnippetInfoService.SnippetShortcut, null);
        }

        [WorkItem(1140893)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitWithEnterObeysOption()
        {
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcu", sendThroughEnterEnabled: true, expected: false);
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcut", sendThroughEnterEnabled: true, expected: true);

            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcu", sendThroughEnterEnabled: false, expected: false);
            await VerifySendEnterThroughToEnterAsync("$$", "SnippetShortcut", sendThroughEnterEnabled: false, expected: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(6405, "https://github.com/dotnet/roslyn/issues/6405")]
        public void SnippetsNotInPreProcessorContextForScriptDirectives()
        {
            VerifyItemIsAbsent(@"#r f$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemIsAbsent(@"#load f$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemIsAbsent(@"#!$$", MockSnippetInfoService.SnippetShortcut, sourceCodeKind: SourceCodeKind.Script);
        }

        private class MockSnippetInfoService : ISnippetInfoService
        {
            internal const string SnippetShortcut = "SnippetShortcut";
            internal const string SnippetDescription = "SnippetDescription";
            internal const string SnippetTitle = "SnippetTitle";
            internal const string SnippetPath = "SnippetPath";

            internal const string PreProcessorSnippetShortcut = "#PreProcessorSnippetShortcut";
            internal const string PreProcessorSnippetDescription = "PreProcessorSnippetDescription";
            internal const string PreProcessorSnippetTitle = "#PreProcessorSnippetTitle";
            internal const string PreProcessorSnippetPath = "PreProcessorSnippetPath";

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
            {
                return false;
            }
        }
    }
}
