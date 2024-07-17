// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    [UseExportProvider]
    public abstract class AbstractClassifierTests
    {
        protected AbstractClassifierTests() { }

        protected abstract Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string text, ImmutableArray<TextSpan> spans, ParseOptions? parseOptions, TestHost testHost);

        protected abstract string WrapInClass(string className, string code);
        protected abstract string WrapInExpression(string code);
        protected abstract string WrapInMethod(string className, string methodName, string code);
        protected abstract string WrapInNamespace(string code);

        protected abstract Task DefaultTestAsync(string code, string allCode, TestHost testHost, FormattedClassification[] expected);

        protected async Task TestAsync(
           string code,
           string allCode,
           TestHost testHost,
           ParseOptions? parseOptions,
           params FormattedClassification[] expected)
        {
            ImmutableArray<TextSpan> spans;
            if (code != allCode)
            {
                var start = allCode.IndexOf(code, StringComparison.Ordinal);
                var length = code.Length;
                spans = ImmutableArray.Create(new TextSpan(start, length));
            }
            else
            {
                MarkupTestFile.GetSpans(allCode, out var rewrittenCode, out spans);
                if (spans.Any())
                {
                    allCode = rewrittenCode;
                }
                else
                {
                    spans = ImmutableArray.Create(new TextSpan(0, allCode.Length));
                }
            }

            var actual = await GetClassificationSpansAsync(allCode, spans, parseOptions, testHost);

            var actualOrdered = actual.OrderBy((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start);

            var actualFormatted = actualOrdered.Select(a => new FormattedClassification(allCode.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType));
            AssertEx.Equal(expected, actualFormatted);
        }

        private async Task TestAsync(
           string code,
           string allCode,
           TestHost testHost,
           ParseOptions[] parseOptionsSet,
           params FormattedClassification[] expected)
        {
            foreach (var parseOptions in parseOptionsSet)
            {
                await TestAsync(code, allCode, testHost, parseOptions, expected);
            }
        }

        protected async Task TestAsync(
           string code,
           TestHost testHost,
           ParseOptions[] parseOptionsSet,
           params FormattedClassification[] expected)
        {
            await TestAsync(code, code, testHost, parseOptionsSet, expected);
        }

        protected async Task TestAsync(
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            await DefaultTestAsync(code, code, testHost, expected);
        }

        protected async Task TestAsync(
            string code,
            TestHost testHost,
            ParseOptions? parseOptions,
            params FormattedClassification[] expected)
        {
            await TestAsync(code, code, testHost, parseOptions, expected);
        }

        protected async Task TestAsync(
            string code,
            string allCode,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            await DefaultTestAsync(code, allCode, testHost, expected);
        }

        protected async Task TestInClassAsync(
            string className,
            string code,
            TestHost testHost,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInClass(className, code);

            await TestAsync(code, allCode, testHost, parseOptionsSet, expected);
        }

        protected async Task TestInClassAsync(
            string className,
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInClass(className, code);

            await DefaultTestAsync(code, allCode, testHost, expected);
        }

        protected Task TestInClassAsync(
            string code,
            TestHost testHost,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            return TestInClassAsync("C", code, testHost, parseOptionsSet, expected);
        }

        protected Task TestInClassAsync(
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            return TestInClassAsync("C", code, testHost, expected);
        }

        protected async Task TestInExpressionAsync(
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInExpression(code);

            await DefaultTestAsync(code, allCode, testHost, expected);
        }

        protected async Task TestInExpressionAsync(
            string code,
            ParseOptions[] parseOptionsSet,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInExpression(code);

            await TestAsync(code, allCode, testHost, parseOptionsSet, expected);
        }

        protected async Task TestInMethodAsync(
            string className,
            string methodName,
            string code,
            TestHost testHost,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInMethod(className, methodName, code);

            await TestAsync(code, allCode, testHost, parseOptionsSet, expected);
        }

        protected async Task TestInMethodAsync(
            string className,
            string methodName,
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInMethod(className, methodName, code);

            await DefaultTestAsync(code, allCode, testHost, expected);
        }

        protected Task TestInMethodAsync(
            string methodName,
            string code,
            TestHost testHost,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", methodName, code, testHost, parseOptionsSet, expected);
        }

        protected Task TestInMethodAsync(
            string methodName,
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", methodName, code, testHost, expected);
        }

        protected Task TestInMethodAsync(
            string code,
            TestHost testHost,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", "M", code, testHost, parseOptionsSet, expected);
        }

        protected Task TestInMethodAsync(
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", "M", code, testHost, expected);
        }

        protected async Task TestInNamespaceAsync(
            string code,
            TestHost testHost,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInNamespace(code);

            await TestAsync(code, allCode, testHost, parseOptionsSet, expected);
        }

        protected async Task TestInNamespaceAsync(
            string code,
            TestHost testHost,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInNamespace(code);

            await DefaultTestAsync(code, allCode, testHost, expected);
        }

        [DebuggerStepThrough]
        protected static FormattedClassification[] Classifications(params FormattedClassification[] expected) => expected;

        [DebuggerStepThrough]
        protected static ParseOptions[] ParseOptions(params ParseOptions[] options) => options;

        protected static async Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, ImmutableArray<TextSpan> spans)
        {
            var service = document.GetRequiredLanguageService<IClassificationService>();
            var options = ClassificationOptions.Default;

            using var _ = Classifier.GetPooledList(out var result);
            await service.AddSemanticClassificationsAsync(document, spans, options, result, CancellationToken.None);
            await service.AddEmbeddedLanguageClassificationsAsync(document, spans, options, result, CancellationToken.None);
            return result.ToImmutableArray();
        }

        protected static async Task<ImmutableArray<ClassifiedSpan>> GetSyntacticClassificationsAsync(Document document, ImmutableArray<TextSpan> spans)
        {
            var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var service = document.GetRequiredLanguageService<ISyntaxClassificationService>();

            using var _ = Classifier.GetPooledList(out var results);
            service.AddSyntacticClassifications(root, spans, results, CancellationToken.None);
            return results.ToImmutableArray();
        }

        protected static async Task<ImmutableArray<ClassifiedSpan>> GetAllClassificationsAsync(Document document, ImmutableArray<TextSpan> spans)
        {
            var semanticClassifications = await GetSemanticClassificationsAsync(document, spans);
            var syntacticClassifications = await GetSyntacticClassificationsAsync(document, spans);

            var classificationsSpans = new HashSet<TextSpan>();

            // Add all the semantic classifications in.
            var allClassifications = new List<ClassifiedSpan>(semanticClassifications);
            classificationsSpans.AddRange(allClassifications.Select(t => t.TextSpan));

            // Add the syntactic classifications.  But only if they don't conflict with a semantic classification.
            allClassifications.AddRange(
                from t in syntacticClassifications
                where !classificationsSpans.Contains(t.TextSpan)
                select t);

            return allClassifications.ToImmutableArray();
        }
    }
}
