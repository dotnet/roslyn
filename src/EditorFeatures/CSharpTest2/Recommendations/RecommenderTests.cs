// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public abstract class RecommenderTests : TestBase
    {
        protected string keywordText;
        internal Func<int, CSharpSyntaxContext, Task<IEnumerable<RecommendedKeyword>>> RecommendKeywordsAsync;

        internal async Task VerifyWorkerAsync(string markup, bool absent, CSharpParseOptions options = null, int? matchPriority = null)
        {
            MarkupTestFile.GetPosition(markup, out var code, out int position);
            await VerifyAtPositionAsync(code, position, absent, options: options, matchPriority: matchPriority);
            await VerifyInFrontOfCommentAsync(code, position, absent, options: options, matchPriority: matchPriority);
            await VerifyAtEndOfFileAsync(code, position, absent, options: options, matchPriority: matchPriority);
            await VerifyAtPosition_KeywordPartiallyWrittenAsync(code, position, absent, options: options, matchPriority: matchPriority);
            await VerifyInFrontOfComment_KeywordPartiallyWrittenAsync(code, position, absent, options: options, matchPriority: matchPriority);
            await VerifyAtEndOfFile_KeywordPartiallyWrittenAsync(code, position, absent, options: options, matchPriority: matchPriority);
        }

        private Task VerifyInFrontOfCommentAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options,
            int? matchPriority)
        {
            text = text.Substring(0, position) + insertText + "/**/" + text.Substring(position);

            position += insertText.Length;

            return CheckResultAsync(text, position, absent, options, matchPriority);
        }

        private Task CheckResultAsync(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: options);
            var compilation = CSharpCompilation.Create(
                "test",
                syntaxTrees: new[] { tree },
                references: new[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            if (tree.IsInNonUserCode(position, CancellationToken.None) && !absent)
            {
                Assert.False(true, "Wanted keyword, but in non-user code position: " + keywordText);
            }

            var semanticModel = compilation.GetSemanticModel(tree);
            var context = CSharpSyntaxContext.CreateContext_Test(semanticModel, position, CancellationToken.None);
            return CheckResultAsync(absent, position, context, matchPriority);
        }

        private async Task CheckResultAsync(bool absent, int position, CSharpSyntaxContext context, int? matchPriority)
        {
            if (absent)
            {
                if (RecommendKeywordsAsync != null)
                {
                    var keywords = await RecommendKeywordsAsync(position, context);
                    Assert.True(keywords == null || !keywords.Any(), "Keywords must be null or empty.");
                }
            }
            else
            {
                if (RecommendKeywordsAsync == null)
                {
                    Assert.False(true, "No recommender for: " + keywordText);
                }
                else
                {
                    var result = (await RecommendKeywordsAsync(position, context)).SingleOrDefault();
                    Assert.True(result != null, "No recommended keywords");
                    Assert.Equal(keywordText, result.Keyword);
                    if (matchPriority != null)
                    {
                        Assert.Equal(matchPriority.Value, result.MatchPriority);
                    }
                }
            }
        }

        private Task VerifyInFrontOfCommentAsync(string text, int cursorPosition, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            return VerifyInFrontOfCommentAsync(text, cursorPosition, absent, string.Empty, options: options, matchPriority: matchPriority);
        }

        private Task VerifyInFrontOfComment_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            return VerifyInFrontOfCommentAsync(text, position, absent, keywordText.Substring(0, 1), options: options, matchPriority: matchPriority);
        }

        private Task VerifyAtPositionAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options,
            int? matchPriority)
        {
            text = text.Substring(0, position) + insertText + text.Substring(position);

            position += insertText.Length;

            return CheckResultAsync(text, position, absent, options, matchPriority);
        }

        private Task VerifyAtPositionAsync(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            return VerifyAtPositionAsync(text, position, absent, string.Empty, options: options, matchPriority: matchPriority);
        }

        private Task VerifyAtPosition_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            return VerifyAtPositionAsync(text, position, absent, keywordText.Substring(0, 1), options: options, matchPriority: matchPriority);
        }

        private async Task VerifyAtEndOfFileAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options,
            int? matchPriority)
        {
            // only do this if the placeholder was at the end of the text.
            if (text.Length != position)
            {
                return;
            }

            text = text.Substring(startIndex: 0, length: position) + insertText;

            position += insertText.Length;

            await CheckResultAsync(text, position, absent, options, matchPriority);
        }

        private Task VerifyAtEndOfFileAsync(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            return VerifyAtEndOfFileAsync(text, position, absent, string.Empty, options: options, matchPriority: matchPriority);
        }

        private Task VerifyAtEndOfFile_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            return VerifyAtEndOfFileAsync(text, position, absent, keywordText.Substring(0, 1), options: options, matchPriority: matchPriority);
        }

        internal async Task VerifyKeywordAsync(string text, CSharpParseOptions options = null, CSharpParseOptions scriptOptions = null)
        {
            // run the verification in both context(normal and script)
            await VerifyWorkerAsync(text, absent: false, options: options);
            await VerifyWorkerAsync(text, absent: false, options: scriptOptions ?? Options.Script);
        }

        protected async Task VerifyKeywordAsync(SourceCodeKind kind, string text)
        {
            switch (kind)
            {
                case SourceCodeKind.Regular:
                    await VerifyWorkerAsync(text, absent: false);
                    break;

                case SourceCodeKind.Script:
                    await VerifyWorkerAsync(text, absent: false, options: Options.Script);
                    break;
            }
        }

        protected async Task VerifyAbsenceAsync(string text, CSharpParseOptions options = null, CSharpParseOptions scriptOptions = null)
        {
            // run the verification in both context(normal and script)
            await VerifyWorkerAsync(text, absent: true, options: options);
            await VerifyWorkerAsync(text, absent: true, options: scriptOptions ?? Options.Script);
        }

        protected async Task VerifyAbsenceAsync(SourceCodeKind kind, string text)
        {
            switch (kind)
            {
                case SourceCodeKind.Regular:
                    await VerifyWorkerAsync(text, absent: true);
                    break;
                case SourceCodeKind.Script:
                    await VerifyWorkerAsync(text, absent: true, options: Options.Script);
                    break;
            }
        }

        protected string AddInsideMethod(string text)
        {
            return
@"class C
{
  void F()
  {
    " + text +
@"  }
}";
        }
    }
}
