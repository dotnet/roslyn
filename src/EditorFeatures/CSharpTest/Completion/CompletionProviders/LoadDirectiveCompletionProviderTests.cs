﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class LoadDirectiveCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(LoadDirectiveCompletionProvider);

        protected override IEqualityComparer<string> GetStringComparer()
            => StringComparer.OrdinalIgnoreCase;

        private protected override Task VerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string inlineDescription = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null)
        {
            return BaseVerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                inlineDescription, matchingFilters, flags);
        }

        [Fact]
        public async Task IsCommitCharacterTest()
        {
            var commitCharacters = new[] { '"', '\\' };
            await VerifyCommitCharactersAsync("#load \"$$", textTypedSoFar: "", validChars: commitCharacters, sourceCodeKind: SourceCodeKind.Script);
        }

        [Theory]
        [InlineData("#load \"$$/")]
        [InlineData("#load \"$$\\")]
        [InlineData("#load \"$$,")]
        [InlineData("#load \"$$A")]
        [InlineData("#load \"$$!")]
        [InlineData("#load \"$$(")]
        public void IsTextualTriggerCharacterTest(string markup)
            => VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true, SourceCodeKind.Script);

        [Theory]
        [InlineData("$$", false)]
        [InlineData("#$$", false)]
        [InlineData("#load$$", false)]
        [InlineData("#loa\"$$", false)]
        [InlineData("#load\"$$", true)]
        [InlineData(" # load \"$$", true)]
        [InlineData(" # load \"$$\"", true)]
        [InlineData(" # load \"\"$$", true)]
        [InlineData("$$ # load \"\"", false)]
        [InlineData(" # load $$\"\"", false)]
        public void ShouldTriggerCompletion(string textWithPositionMarker, bool expectedResult)
        {
            var position = textWithPositionMarker.IndexOf("$$");
            var text = textWithPositionMarker.Replace("$$", "");

            var services = (IMefHostExportProvider)FeaturesTestCompositions.Features.GetHostServices();
            var provider = services.GetExports<CompletionProvider, CompletionProviderMetadata>().Single(p => p.Metadata.Language == LanguageNames.CSharp && p.Metadata.Name == nameof(LoadDirectiveCompletionProvider)).Value;

            Assert.Equal(expectedResult, provider.ShouldTriggerCompletion(SourceText.From(text), position, trigger: default, new TestOptionSet()));
        }
    }
}
