// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [UseExportProvider]
    public abstract class RecommenderTests : TestBase
    {
        protected static readonly CSharpParseOptions CSharp9ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

        protected abstract string KeywordText { get; }
        internal Func<int, CSharpSyntaxContext, Task<ImmutableArray<RecommendedKeyword>>>? RecommendKeywordsAsync;

        internal async Task VerifyWorkerAsync(string markup, bool absent, CSharpParseOptions? options = null, int? matchPriority = null)
        {
            Testing.TestFileMarkupParser.GetPosition(markup, out var code, out var position);
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
            CSharpParseOptions? options,
            int? matchPriority)
        {
            text = text[..position] + insertText + "/**/" + text[position..];

            position += insertText.Length;

            return CheckResultAsync(text, position, absent, options, matchPriority);
        }

        private Task CheckResultAsync(string text, int position, bool absent, CSharpParseOptions? options, int? matchPriority)
        {
            using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("test", "test", LanguageNames.CSharp);
            var document = project.AddDocument("test.cs", text);

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: options);
            var compilation = CSharpCompilation.Create(
                "test",
                syntaxTrees: [tree],
                references: [NetFramework.mscorlib]);

            if (tree.IsInNonUserCode(position, CancellationToken.None) && !absent)
            {
                Assert.False(true, "Wanted keyword, but in non-user code position: " + KeywordText);
            }

            var semanticModel = compilation.GetSemanticModel(tree);
            var context = CSharpSyntaxContext.CreateContext(document, semanticModel, position, CancellationToken.None);
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
                    Assert.False(true, "No recommender for: " + KeywordText);
                }
                else
                {
                    var result = (await RecommendKeywordsAsync(position, context)).SingleOrDefault();
                    AssertEx.NotNull(result);
                    Assert.Equal(KeywordText, result!.Keyword);
                    if (matchPriority != null)
                    {
                        Assert.Equal(matchPriority.Value, result.MatchPriority);
                    }
                }
            }
        }

        private Task VerifyInFrontOfCommentAsync(string text, int cursorPosition, bool absent, CSharpParseOptions? options, int? matchPriority)
            => VerifyInFrontOfCommentAsync(text, cursorPosition, absent, string.Empty, options: options, matchPriority: matchPriority);

        private Task VerifyInFrontOfComment_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions? options, int? matchPriority)
            => VerifyInFrontOfCommentAsync(text, position, absent, KeywordText[..1], options: options, matchPriority: matchPriority);

        private Task VerifyAtPositionAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions? options,
            int? matchPriority)
        {
            text = text[..position] + insertText + text[position..];

            position += insertText.Length;

            return CheckResultAsync(text, position, absent, options, matchPriority);
        }

        private Task VerifyAtPositionAsync(string text, int position, bool absent, CSharpParseOptions? options, int? matchPriority)
            => VerifyAtPositionAsync(text, position, absent, string.Empty, options: options, matchPriority: matchPriority);

        private Task VerifyAtPosition_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions? options, int? matchPriority)
            => VerifyAtPositionAsync(text, position, absent, KeywordText[..1], options: options, matchPriority: matchPriority);

        private async Task VerifyAtEndOfFileAsync(
            string text,
            int position,
            bool absent,
            string insertText,
            CSharpParseOptions? options,
            int? matchPriority)
        {
            // only do this if the placeholder was at the end of the text.
            if (text.Length != position)
            {
                return;
            }

            text = text[..position] + insertText;

            position += insertText.Length;

            await CheckResultAsync(text, position, absent, options, matchPriority);
        }

        private Task VerifyAtEndOfFileAsync(string text, int position, bool absent, CSharpParseOptions? options, int? matchPriority)
            => VerifyAtEndOfFileAsync(text, position, absent, string.Empty, options: options, matchPriority: matchPriority);

        private Task VerifyAtEndOfFile_KeywordPartiallyWrittenAsync(string text, int position, bool absent, CSharpParseOptions? options, int? matchPriority)
            => VerifyAtEndOfFileAsync(text, position, absent, KeywordText[..1], options: options, matchPriority: matchPriority);

        internal async Task VerifyKeywordAsync(
            [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string text,
            CSharpParseOptions? options = null,
            CSharpParseOptions? scriptOptions = null)
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

        protected async Task VerifyAbsenceAsync(
            [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string text,
            CSharpParseOptions? options = null,
            CSharpParseOptions? scriptOptions = null)
        {
            // run the verification in both context(normal and script)
            await VerifyWorkerAsync(text, absent: true, options: options);
            await VerifyWorkerAsync(text, absent: true, options: scriptOptions ?? Options.Script);
        }

        protected async Task VerifyAbsenceAsync(
            SourceCodeKind kind,
            [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string text)
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
            builder.Append('}');

            return builder.ToString();
        }
    }
}
