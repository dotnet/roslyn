// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        internal Func<int, CSharpSyntaxContext, IEnumerable<RecommendedKeyword>> RecommendKeywords;

        public void VerifyWorker(string markup, bool absent, CSharpParseOptions options = null)
        {
            string code;
            int position;
            MarkupTestFile.GetPosition(markup, out code, out position);

            VerifyAtPosition(code, position, absent, options: options);
            VerifyInFrontOfComment(code, position, absent, options: options);
            VerifyAtEndOfFile(code, position, absent, options: options);
            VerifyAtPosition_KeywordPartiallyWritten(code, position, absent, options: options);
            VerifyInFrontOfComment_KeywordPartiallyWritten(code, position, absent, options: options);
            VerifyAtEndOfFile_KeywordPartiallyWritten(code, position, absent, options: options);
        }

        private void VerifyInFrontOfComment(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options)
        {
            text = text.Substring(0, position) + insertText + "/**/" + text.Substring(position);

            position += insertText.Length;

            CheckResult(text, position, absent, options);
        }

        private void CheckResult(string text, int position, bool absent, CSharpParseOptions options)
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
            CheckResult(absent, position, context, semanticModel);
        }

        private void CheckResult(bool absent, int position, CSharpSyntaxContext context, SemanticModel semanticModel)
        {
            if (absent)
            {
                if (RecommendKeywords != null)
                {
                    var keywords = RecommendKeywords(position, context);
                    Assert.True(keywords == null || !keywords.Any(), "Keywords must be null or empty.");
                }
            }
            else
            {
                if (RecommendKeywords == null)
                {
                    Assert.False(true, "No recommender for: " + keywordText);
                }
                else
                {
                    var result = RecommendKeywords(position, context).Select(k => k.Keyword);
                    Assert.NotNull(result);
                    Assert.Equal(keywordText, result.Single());
                }
            }
        }

        private void VerifyInFrontOfComment(string text, int cursorPosition, bool absent, CSharpParseOptions options)
        {
            VerifyInFrontOfComment(text, cursorPosition, absent, string.Empty, options: options);
        }

        private void VerifyInFrontOfComment_KeywordPartiallyWritten(string text, int position, bool absent, CSharpParseOptions options)
        {
            VerifyInFrontOfComment(text, position, absent, keywordText.Substring(0, 1), options: options);
        }

        private void VerifyAtPosition(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options)
        {
            text = text.Substring(0, position) + insertText + text.Substring(position);

            position += insertText.Length;

            CheckResult(text, position, absent, options);
        }

        private void VerifyAtPosition(string text, int position, bool absent, CSharpParseOptions options)
        {
            VerifyAtPosition(text, position, absent, string.Empty, options: options);
        }

        private void VerifyAtPosition_KeywordPartiallyWritten(string text, int position, bool absent, CSharpParseOptions options)
        {
            VerifyAtPosition(text, position, absent, keywordText.Substring(0, 1), options: options);
        }

        private void VerifyAtEndOfFile(
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

            CheckResult(text, position, absent, options);
        }

        private void VerifyAtEndOfFile(string text, int position, bool absent, CSharpParseOptions options)
        {
            VerifyAtEndOfFile(text, position, absent, string.Empty, options: options);
        }

        private void VerifyAtEndOfFile_KeywordPartiallyWritten(string text, int position, bool absent, CSharpParseOptions options)
        {
            VerifyAtEndOfFile(text, position, absent, keywordText.Substring(0, 1), options: options);
        }

        protected void VerifyKeyword(string text, CSharpParseOptions options = null, CSharpParseOptions scriptOptions = null)
        {
            // run the verification in both context(normal and script)
            VerifyWorker(text, absent: false, options: options);
            VerifyWorker(text, absent: false, options: scriptOptions ?? Options.Script);
        }

        protected void VerifyKeyword(SourceCodeKind kind, string text)
        {
            switch (kind)
            {
                case SourceCodeKind.Regular:
                    VerifyWorker(text, absent: false);
                    break;

                case SourceCodeKind.Script:
                    VerifyWorker(text, absent: false, options: Options.Script);
                    break;
            }
        }

        protected void VerifyAbsence(string text, CSharpParseOptions options = null, CSharpParseOptions scriptOptions = null)
        {
            // run the verification in both context(normal and script)
            VerifyWorker(text, absent: true, options: options);
            VerifyWorker(text, absent: true, options: scriptOptions ?? Options.Script);
        }

        protected void VerifyAbsence(SourceCodeKind kind, string text)
        {
            switch (kind)
            {
                case SourceCodeKind.Regular:
                    VerifyWorker(text, absent: true);
                    break;
                case SourceCodeKind.Script:
                    VerifyWorker(text, absent: true, options: Options.Script);
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
