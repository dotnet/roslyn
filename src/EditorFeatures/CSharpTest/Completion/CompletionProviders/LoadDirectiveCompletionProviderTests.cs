﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class LoadDirectiveCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public LoadDirectiveCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new LoadDirectiveCompletionProvider();
        }

        protected override bool CompareItems(string actualItem, string expectedItem)
        {
            return actualItem.Equals(expectedItem, StringComparison.OrdinalIgnoreCase);
        }

        protected override Task VerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, 
            int? glyph, int? matchPriority, bool? hasSuggestionItem)
        {
            return BaseVerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem);
        }

        [Fact]
        public async Task IsCommitCharacterTest()
        {
            var commitCharacters = new[] { '"', '\\' };
            await VerifyCommitCharactersAsync("#load \"$$", textTypedSoFar: "", validChars: commitCharacters);
        }

        [Fact]
        public void IsTextualTriggerCharacterTest()
        {
            var validMarkupList = new[]
            {
                "#load \"$$/",
                "#load \"$$\\",
                "#load \"$$,",
                "#load \"$$A",
                "#load \"$$!",
                "#load \"$$(",
            };

            foreach (var markup in validMarkupList)
            {
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
            }
        }
    }
}
