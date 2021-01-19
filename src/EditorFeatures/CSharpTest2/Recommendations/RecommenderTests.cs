// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
        protected static readonly CSharpParseOptions CSharp9ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

        protected string keywordText;
        internal Func<int, CSharpSyntaxContext, ImmutableArray<RecommendedKeyword>> RecommendKeywords;

        internal void VerifyWorker(string markup, bool absent, CSharpParseOptions options = null, int? matchPriority = null)
        {
            MarkupTestFile.GetPosition(markup, out var code, out int position);
            VerifyAtPosition(code, position, absent, options: options, matchPriority: matchPriority);
            VerifyInFrontOfComment(code, position, absent, options: options, matchPriority: matchPriority);
            VerifyAtEndOfFile(code, position, absent, options: options, matchPriority: matchPriority);
            VerifyAtPosition_KeywordPartiallyWritten(code, position, absent, options: options, matchPriority: matchPriority);
            VerifyInFrontOfComment_KeywordPartiallyWritten(code, position, absent, options: options, matchPriority: matchPriority);
            VerifyAtEndOfFile_KeywordPartiallyWritten(code, position, absent, options: options, matchPriority: matchPriority);
        }

        private void VerifyInFrontOfComment(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options,
            int? matchPriority)
        {
            text = text.Substring(0, position) + insertText + "/**/" + text.Substring(position);

            position += insertText.Length;

            CheckResult(text, position, absent, options, matchPriority);
        }

        private void CheckResult(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: options);
            var compilation = CSharpCompilation.Create(
                "test",
                syntaxTrees: new[] { tree },
                references: new[] { TestMetadata.Net451.mscorlib });

            if (tree.IsInNonUserCode(position, CancellationToken.None) && !absent)
            {
                Assert.False(true, "Wanted keyword, but in non-user code position: " + keywordText);
            }

            var semanticModel = compilation.GetSemanticModel(tree);
            var context = CSharpSyntaxContext.CreateContext_Test(semanticModel, position, CancellationToken.None);
            CheckResult(absent, position, context, matchPriority);
        }

        private void CheckResult(bool absent, int position, CSharpSyntaxContext context, int? matchPriority)
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
                    var result = RecommendKeywords(position, context).SingleOrDefault();
                    Assert.True(result != null, "No recommended keywords");
                    Assert.Equal(keywordText, result.Keyword);
                    if (matchPriority != null)
                    {
                        Assert.Equal(matchPriority.Value, result.MatchPriority);
                    }
                }
            }
        }

        private void VerifyInFrontOfComment(string text, int cursorPosition, bool absent, CSharpParseOptions options, int? matchPriority)
            => VerifyInFrontOfComment(text, cursorPosition, absent, string.Empty, options: options, matchPriority: matchPriority);

        private void VerifyInFrontOfComment_KeywordPartiallyWritten(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
            => VerifyInFrontOfComment(text, position, absent, keywordText.Substring(0, 1), options: options, matchPriority: matchPriority);

        private void VerifyAtPosition(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions options,
            int? matchPriority)
        {
            text = text.Substring(0, position) + insertText + text.Substring(position);

            position += insertText.Length;

            CheckResult(text, position, absent, options, matchPriority);
        }

        private void VerifyAtPosition(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
            => VerifyAtPosition(text, position, absent, string.Empty, options: options, matchPriority: matchPriority);

        private void VerifyAtPosition_KeywordPartiallyWritten(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
            => VerifyAtPosition(text, position, absent, keywordText.Substring(0, 1), options: options, matchPriority: matchPriority);

        private void VerifyAtEndOfFile(
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

            CheckResult(text, position, absent, options, matchPriority);
        }

        private void VerifyAtEndOfFile(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
            => VerifyAtEndOfFile(text, position, absent, string.Empty, options: options, matchPriority: matchPriority);

        private void VerifyAtEndOfFile_KeywordPartiallyWritten(string text, int position, bool absent, CSharpParseOptions options, int? matchPriority)
            => VerifyAtEndOfFile(text, position, absent, keywordText.Substring(0, 1), options: options, matchPriority: matchPriority);

        internal void VerifyKeyword(string text, CSharpParseOptions options = null, CSharpParseOptions scriptOptions = null)
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

        protected static string AddInsideMethod(string text, bool isAsync = false, string returnType = "void", bool topLevelStatement = false)
        {
            if (topLevelStatement)
            {
                return returnType switch
                {
                    "void" => text,
                    "int" => text,
                    _ => throw new ArgumentException("Unsupported return type", nameof(returnType)),
                };
            }

            var builder = new StringBuilder();
            if (isAsync && returnType != "void")
            {
                builder.AppendLine("using System.Threading.Tasks;");
            }

            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.Append("  ");

            if (isAsync)
            {
                builder.Append("async ");
                if (returnType == "void")
                {
                    builder.Append("Task");
                }
                else
                {
                    builder.Append($"Task<{returnType}>");
                }
            }
            else
            {
                builder.Append(returnType);
            }

            builder.AppendLine(" F()");
            builder.AppendLine("  {");
            builder.Append("    ").Append(text);
            builder.AppendLine("  }");
            builder.Append("}");

            return builder.ToString();
        }
    }
}
