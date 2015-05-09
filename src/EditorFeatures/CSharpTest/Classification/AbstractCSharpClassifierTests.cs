// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public abstract class AbstractCSharpClassifierTests : AbstractClassifierTests
    {
        internal abstract IEnumerable<ClassifiedSpan> GetClassificationSpans(string code, TextSpan textSpan, CSharpParseOptions options);

        protected string GetText(Tuple<string, string> tuple)
        {
            return "(" + tuple.Item1 + ", " + tuple.Item2 + ")";
        }

        internal string GetText(ClassifiedSpan tuple)
        {
            return "(" + tuple.TextSpan + ", " + tuple.ClassificationType + ")";
        }

        protected void Test(string code,
           string allCode,
           Tuple<string, string>[] expected,
           CSharpParseOptions options = null)
        {
            var start = allCode.IndexOf(code, StringComparison.Ordinal);
            var length = code.Length;
            var span = new TextSpan(start, length);

            var actual = GetClassificationSpans(allCode, span, options: options).ToList();

            actual.Sort((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start);

            var max = Math.Max(expected.Length, actual.Count);
            for (int i = 0; i < max; i++)
            {
                if (i >= expected.Length)
                {
                    AssertEx.Fail("Unexpected actual classification: {0}", GetText(actual[i]));
                }
                else if (i >= actual.Count)
                {
                    AssertEx.Fail("Missing classification for: {0}", GetText(expected[i]));
                }

                var tuple = expected[i];
                var classification = actual[i];

                var text = allCode.Substring(classification.TextSpan.Start, classification.TextSpan.Length);
                Assert.Equal(tuple.Item1, text);
                Assert.Equal(tuple.Item2, classification.ClassificationType);
            }
        }

        protected Tuple<string, string>[] Classifications(params Tuple<string, string>[] expected)
        {
            return expected;
        }

        protected void Test(
            string code,
            string allCode,
            params Tuple<string, string>[] expected)
        {
            Test(code, allCode, expected, null);
        }

        protected void Test(
            string code,
            string allCode,
            CSharpParseOptions options,
            params Tuple<string, string>[] expected)
        {
            Test(code, allCode, expected, options);
        }

        protected void Test(
            string code,
            params Tuple<string, string>[] expected)
        {
            Test(code, code, expected);
            Test(code, code, expected, Options.Script);
        }

        protected void Test(
            string code,
            CSharpParseOptions options,
            CSharpParseOptions scriptOptions,
            params Tuple<string, string>[] expected)
        {
            Test(code, code, expected, options);
            Test(code, code, expected, scriptOptions);
        }

        protected void TestInNamespace(
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "namespace N {\r\n" + code + "\r\n}";
            Test(code, allCode, expected);
            Test(code, allCode, expected, Options.Script);
        }

        protected void TestInClass(
            string className,
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class " + className + " {\r\n    " +
                code + "\r\n}";
            Test(code, allCode, expected);
            Test(code, allCode, expected, Options.Script);
        }

        protected void TestInClass(
            string code,
            params Tuple<string, string>[] expected)
        {
            TestInClass("C", code, expected);
        }

        protected void TestInMethod(
            string className,
            string methodName,
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class " + className + " {\r\n    void " + methodName + "() {\r\n        " +
                code + "\r\n    \r\n}\r\n}";
            Test(code, allCode, expected);
            Test(code, allCode, expected, Options.Script);
        }

        protected void TestInMethod(
            string className,
            string methodName,
            string code,
            CSharpParseOptions options,
            CSharpParseOptions scriptOptions,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class " + className + " {\r\n    void " + methodName + "() {\r\n        " +
                code + "\r\n    \r\n}\r\n}";
            Test(code, allCode, expected, options);
            Test(code, allCode, expected, scriptOptions);
        }

        protected void TestInMethod(
            string methodName,
            string code,
            params Tuple<string, string>[] expected)
        {
            TestInMethod("C", methodName, code, expected);
        }

        protected void TestInMethod(
            string code,
            params Tuple<string, string>[] expected)
        {
            TestInMethod("C", "M", code, expected);
        }

        protected void TestInMethod(
            string code,
            CSharpParseOptions options,
            CSharpParseOptions scriptOptions,
            params Tuple<string, string>[] expected)
        {
            TestInMethod("C", "M", code, options, scriptOptions, expected);
        }

        protected void TestInExpression(
            string code,
            params Tuple<string, string>[] expected)
        {
            var allCode = "class C {\r\n    void M() {\r\n        var q = \r\n        " +
                code + "\r\n    ;\r\n    }\r\n}";
            Test(code, allCode, expected);
            Test(code, allCode, expected, Options.Script);
        }
    }
}
