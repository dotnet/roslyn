// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public abstract class AbstractClassifierTests
    {
        protected AbstractClassifierTests() { }

        protected abstract Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string text, TextSpan span, ParseOptions parseOptions);

        protected abstract string WrapInClass(string className, string code);
        protected abstract string WrapInExpression(string code);
        protected abstract string WrapInMethod(string className, string methodName, string code);
        protected abstract string WrapInNamespace(string code);

        protected abstract Task DefaultTestAsync(string code, string allCode, FormattedClassification[] expected);

        protected async Task TestAsync(
           string code,
           string allCode,
           ParseOptions parseOptions,
           params FormattedClassification[] expected)
        {
            var start = allCode.IndexOf(code, StringComparison.Ordinal);
            var length = code.Length;
            var span = new TextSpan(start, length);
            var actual = await GetClassificationSpansAsync(allCode, span, parseOptions);

            actual = actual.Sort((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start);

            var actualFormatted = actual.Select(a => new FormattedClassification(allCode.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType));
            AssertEx.Equal(expected, actualFormatted);
        }

        protected async Task TestAsync(
           string code,
           string allCode,
           ParseOptions[] parseOptionsSet,
           params FormattedClassification[] expected)
        {
            foreach (var parseOptions in parseOptionsSet)
            {
                await TestAsync(code, allCode, parseOptions, expected);
            }
        }

        protected async Task TestAsync(
           string code,
           ParseOptions[] parseOptionsSet,
           params FormattedClassification[] expected)
        {
            foreach (var parseOptions in parseOptionsSet)
            {
                await TestAsync(code, code, parseOptions, expected);
            }
        }

        protected async Task TestAsync(
            string code,
            params FormattedClassification[] expected)
        {
            await DefaultTestAsync(code, code, expected);
        }

        protected async Task TestAsync(
            string code,
            string allCode,
            params FormattedClassification[] expected)
        {
            await DefaultTestAsync(code, allCode, expected);
        }

        protected async Task TestInClassAsync(
            string className,
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInClass(className, code);

            await TestAsync(code, allCode, parseOptionsSet, expected);
        }

        protected async Task TestInClassAsync(
            string className,
            string code,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInClass(className, code);

            await DefaultTestAsync(code, allCode, expected);
        }

        protected Task TestInClassAsync(
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            return TestInClassAsync("C", code, parseOptionsSet, expected);
        }

        protected Task TestInClassAsync(
            string code,
            params FormattedClassification[] expected)
        {
            return TestInClassAsync("C", code, expected);
        }

        protected async Task TestInExpressionAsync(
            string code,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInExpression(code);

            await DefaultTestAsync(code, allCode, expected);
        }

        protected async Task TestInExpressionAsync(
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInExpression(code);

            await TestAsync(code, allCode, parseOptionsSet, expected);
        }

        protected async Task TestInMethodAsync(
            string className,
            string methodName,
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInMethod(className, methodName, code);

            await TestAsync(code, allCode, parseOptionsSet, expected);
        }

        protected async Task TestInMethodAsync(
            string className,
            string methodName,
            string code,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInMethod(className, methodName, code);

            await DefaultTestAsync(code, allCode, expected);
        }

        protected Task TestInMethodAsync(
            string methodName,
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", methodName, code, parseOptionsSet, expected);
        }

        protected Task TestInMethodAsync(
            string methodName,
            string code,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", methodName, code, expected);
        }

        protected Task TestInMethodAsync(
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", "M", code, parseOptionsSet, expected);
        }

        protected Task TestInMethodAsync(
            string code,
            params FormattedClassification[] expected)
        {
            return TestInMethodAsync("C", "M", code, expected);
        }

        protected async Task TestInNamespaceAsync(
            string code,
            ParseOptions[] parseOptionsSet,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInNamespace(code);

            await TestAsync(code, allCode, parseOptionsSet, expected);
        }

        protected async Task TestInNamespaceAsync(
            string code,
            params FormattedClassification[] expected)
        {
            var allCode = WrapInNamespace(code);

            await DefaultTestAsync(code, allCode, expected);
        }

        [DebuggerStepThrough]
        protected static FormattedClassification[] Classifications(params FormattedClassification[] expected) => expected;

        [DebuggerStepThrough]
        protected static ParseOptions[] ParseOptions(params ParseOptions[] options) => options;
    }
}
