// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class ReferenceDirectiveCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ReferenceDirectiveCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new ReferenceDirectiveCompletionProvider();
        }

        protected override IEqualityComparer<string> GetStringComparer()
        {
            return StringComparer.OrdinalIgnoreCase;
        }

        private protected override Task VerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null)
        {
            return BaseVerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters);
        }

        [Fact]
        public async Task IsCommitCharacterTest()
        {
            var commitCharacters = PathUtilities.IsUnixLikePlatform ? new[] { '"', '/' } : new[] { '"', '\\', '/', ',' };
            await VerifyCommitCharactersAsync("#r \"$$", textTypedSoFar: "", validChars: commitCharacters);
        }

        [Fact]
        public void IsTextualTriggerCharacterTest()
        {
            var validMarkupList = new[]
            {
                "#r \"$$/",
                "#r \"$$\\",
                "#r \"$$,",
                "#r \"$$A",
                "#r \"$$!",
                "#r \"$$(",
            };

            foreach (var markup in validMarkupList)
            {
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            }
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task SendEnterThroughToEditorTest()
        {
            await VerifySendEnterThroughToEnterAsync("#r \"System$$", "System", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
            await VerifySendEnterThroughToEnterAsync("#r \"System$$", "System", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: false);
            await VerifySendEnterThroughToEnterAsync("#r \"System$$", "System", sendThroughEnterOption: EnterKeyRule.Always, expected: false); // note: GAC completion helper uses its own EnterKeyRule
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task GacReference()
        {
            await VerifyItemExistsAsync("#r \"$$", "System.Windows.Forms", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task GacReferenceFullyQualified()
        {
            await VerifyItemExistsAsync(
                "#r \"System.Windows.Forms,$$",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public async Task FileSystemReference()
        {
            var systemDir = Path.GetFullPath(Environment.SystemDirectory);
            var windowsDir = Directory.GetParent(systemDir);
            var windowsDirPath = windowsDir.FullName;
            var windowsRoot = Directory.GetDirectoryRoot(systemDir);

            // we need to get the exact casing from the file system:
            var normalizedWindowsPath = Directory.GetDirectories(windowsRoot, windowsDir.Name).Single();
            var windowsFolderName = Path.GetFileName(normalizedWindowsPath);

            var code = "#r \"" + windowsRoot + "$$";
            await VerifyItemExistsAsync(code, windowsFolderName, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }
    }
}
