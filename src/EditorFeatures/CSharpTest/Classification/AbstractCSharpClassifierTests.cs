// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public abstract class AbstractCSharpClassifierTests : AbstractClassifierTests
    {
        internal abstract Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan textSpan, CSharpParseOptions options);

        protected string GetText(Tuple<string, string> tuple)
        {
            return "(" + tuple.Item1 + ", " + tuple.Item2 + ")";
        }

        internal string GetText(ClassifiedSpan tuple)
        {
            return "(" + tuple.TextSpan + ", " + tuple.ClassificationType + ")";
        }

        protected async Task TestAsync(string code,
           string allCode,
           Tuple<string, string>[] expected,
           CSharpParseOptions options = null)
        {
            var start = allCode.IndexOf(code, StringComparison.Ordinal);
            var length = code.Length;
            var span = new TextSpan(start, length);

            var actual = (await GetClassificationSpansAsync(allCode, span, options: options)).ToList();

            actual.Sort((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start);

            var actualFormatted = actual.Select(a => new FormattedClassification(a.ClassificationType, allCode.Substring(a.TextSpan.Start, a.TextSpan.Length)));
            var expectedFormatted = expected.Select(e => new FormattedClassification(e.Item2, e.Item1));
            AssertEx.Equal(expectedFormatted, actualFormatted);
        }

        private class FormattedClassification
        {
            private readonly string _classification;
            private readonly string _text;

            public FormattedClassification(string classification, string text)
            {
                _classification = classification;
                _text = text;
            }

            public override bool Equals(object obj)
            {
                if (obj is FormattedClassification other)
                {
                    return this._classification == other._classification && this._text == other._text;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return _classification.GetHashCode() ^ _text.GetHashCode();
            }

            public override string ToString()
            {
                switch(_classification)
                {
                    case "punctuation":
                        switch (_text)
                        {
                            case "(":
                                return "Punctation.OpenParen";
                            case ")":
                                return "Punctation.CloseParen";
                            case ";":
                                return "Punctation.Semicolon";
                            case ":":
                                return "Punctuation.Colon";
                            case ",":
                                return "Punctuation.Comma";
                        }
                        goto default;

                    case "operator":
                        switch(_text)
                        {
                            case "=":
                                return "Operators.Equals";
                        }
                        goto default;

                    default:
                        return $"{char.ToUpperInvariant(_classification[0])}{_classification.Substring(1)}(\"{_text}\")";
                }
            }
        }

        protected Tuple<string, string>[] Classifications(params Tuple<string, string>[] expected)
        {
            return expected;
        }

        protected Task TestAsync(
            string code,
            string allCode,
            params Tuple<string, string>[] expected)
        {
            return TestAsync(code, allCode, expected, null);
        }

        protected Task TestAsync(
            string code,
            string allCode,
            CSharpParseOptions options,
            params Tuple<string, string>[] expected)
        {
            return TestAsync(code, allCode, expected, options);
        }

        protected async Task TestAsync(
            string code,
            params Tuple<string, string>[] expected)
        {
            await TestAsync(code, code, expected);
            await TestAsync(code, code, expected, Options.Script);
        }

        protected async Task TestAsync(
            string code,
            CSharpParseOptions options,
            CSharpParseOptions scriptOptions,
            params Tuple<string, string>[] expected)
        {
            await TestAsync(code, code, expected, options);
            await TestAsync(code, code, expected, scriptOptions);
        }

        protected async Task TestInNamespaceAsync(
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "namespace N {\r\n" + code + "\r\n}";
            await TestAsync(code, allCode, expected);
            await TestAsync(code, allCode, expected, Options.Script);
        }

        protected async Task TestInClassAsync(
            string className,
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class " + className + " {\r\n    " +
                code + "\r\n}";
            await TestAsync(code, allCode, expected);
            await TestAsync(code, allCode, expected, Options.Script);
        }

        protected Task TestInClassAsync(
            string code,
            params Tuple<string, string>[] expected)
        {
            return TestInClassAsync("C", code, expected);
        }

        protected async Task TestInMethodAsync(
            string className,
            string methodName,
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class " + className + " {\r\n    void " + methodName + "() {\r\n        " +
                code + "\r\n    \r\n}\r\n}";
            await TestAsync(code, allCode, expected);
            await TestAsync(code, allCode, expected, Options.Script);
        }

        protected async Task TestInMethodAsync(
            string className,
            string methodName,
            string code,
            CSharpParseOptions options,
            CSharpParseOptions scriptOptions,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class " + className + " {\r\n    void " + methodName + "() {\r\n        " +
                code + "\r\n    \r\n}\r\n}";
            await TestAsync(code, allCode, expected, options);
            await TestAsync(code, allCode, expected, scriptOptions);
        }

        protected Task TestInMethodAsync(
            string methodName,
            string code,
            params Tuple<string, string>[] expected)
        {
            return TestInMethodAsync("C", methodName, code, expected);
        }

        protected Task TestInMethodAsync(
            string code,
            params Tuple<string, string>[] expected)
        {
            return TestInMethodAsync("C", "M", code, expected);
        }

        protected Task TestInMethodAsync(
            string code,
            CSharpParseOptions options,
            CSharpParseOptions scriptOptions,
            params Tuple<string, string>[] expected)
        {
            return TestInMethodAsync("C", "M", code, options, scriptOptions, expected);
        }

        protected async Task TestInExpressionAsync(
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class C {\r\n    void M() {\r\n        var q = \r\n        " +
                code + "\r\n    ;\r\n    }\r\n}";
            await TestAsync(code, allCode, expected);
            await TestAsync(code, allCode, expected, Options.Script);
        }
    }
}
