// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public abstract class RecommenderTests : TestBase
    {
        protected string keywordText;
        internal Func<int, CSharpSyntaxContext, Task<IEnumerable<RecommendedKeyword>>> RecommendKeywordsAsync;

        public async Task VerifyWorkerAsync(string markup, bool absent, CSharpParseOptions options = null)
        {
            string code;
            int position;
            MarkupTestFile.GetPosition(markup, out code, out position);

            await VerifyAtPositionAsync(code, position, absent, options: options);
            await VerifyInFrontOfCommentAsync(code, position, absent, options: options);
            await VerifyAtEndOfFileAsync(code, position, absent, options: options);
            await VerifyAtPosition_KeywordPartiallyWrittenAsync(code, position, absent, options: options);
            await VerifyInFrontOfComment_KeywordPartiallyWrittenAsync(code, position, absent, options: options);
            await VerifyAtEndOfFile_KeywordPartiallyWrittenAsync(code, position, absent, options: options);
        }

        private Task VerifyInFrontOfCommentAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options)
        {
            text = text.Substring(0, position) + insertText + "/**/" + text.Substring(position);

            position += insertText.Length;

            return CheckResultAsync(text, position, absent, options);
        }

        private Task CheckResultAsync(string text, int position, bool absent, CSharpParseOptions options)
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
            return CheckResultAsync(absent, position, context, semanticModel);
        }

        private async Task CheckResultAsync(bool absent, int position, CSharpSyntaxContext context, SemanticModel semanticModel)
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
                    var result = (await RecommendKeywordsAsync(position, context)).Select(k => k.Keyword);
                    Assert.NotNull(result);
                    Assert.Equal(keywordText, result.Single());
                }
            }
        }

        private Task VerifyInFrontOfCommentAsync(string text, int cursorPosition, bool absent, CSharpParseOptions options)
        {
            return VerifyInFrontOfCommentAsync(text, cursorPosition, absent, string.Empty, options: options);
        }

        private Task VerifyInFrontOfComment_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions options)
        {
            return VerifyInFrontOfCommentAsync(text, position, absent, keywordText.Substring(0, 1), options: options);
        }

        private Task VerifyAtPositionAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options)
        {
            text = text.Substring(0, position) + insertText + text.Substring(position);

            position += insertText.Length;

            return CheckResultAsync(text, position, absent, options);
        }

        private Task VerifyAtPositionAsync(string text, int position, bool absent, CSharpParseOptions options)
        {
            return VerifyAtPositionAsync(text, position, absent, string.Empty, options: options);
        }

        private Task VerifyAtPosition_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions options)
        {
            return VerifyAtPositionAsync(text, position, absent, keywordText.Substring(0, 1), options: options);
        }

        private async Task VerifyAtEndOfFileAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options)
        {
            // only do this if the placeholder was at the end of the text.
            if (text.Length != position)
            {
                return;
            }

            text = text.Substring(startIndex: 0, length: position) + insertText;

            position += insertText.Length;

            await CheckResultAsync(text, position, absent, options);
        }

        private Task VerifyAtEndOfFileAsync(string text, int position, bool absent, CSharpParseOptions options)
        {
            return VerifyAtEndOfFileAsync(text, position, absent, string.Empty, options: options);
        }

        private Task VerifyAtEndOfFile_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions options)
        {
            return VerifyAtEndOfFileAsync(text, position, absent, keywordText.Substring(0, 1), options: options);
        }

        protected async Task VerifyKeywordAsync(string text, CSharpParseOptions options = null, CSharpParseOptions scriptOptions = null)
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
