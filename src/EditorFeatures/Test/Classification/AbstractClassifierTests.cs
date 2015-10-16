// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public abstract class AbstractClassifierTests
    {
        protected readonly ClassificationBuilder ClassificationBuilder;

        protected AbstractClassifierTests()
        {
            this.ClassificationBuilder = new ClassificationBuilder();
        }

        public static void Validate(string allCode, Tuple<string, string>[] expected, List<ClassifiedSpan> actual)
        {
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

        protected static string GetText(Tuple<string, string> tuple)
        {
            return "(" + tuple.Item1 + ", " + tuple.Item2 + ")";
        }

        protected static string GetText(ClassifiedSpan tuple)
        {
            return "(" + tuple.TextSpan + ", " + tuple.ClassificationType + ")";
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Struct(string value)
        {
            return ClassificationBuilder.Struct(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Enum(string value)
        {
            return ClassificationBuilder.Enum(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Interface(string value)
        {
            return ClassificationBuilder.Interface(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Class(string value)
        {
            return ClassificationBuilder.Class(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Delegate(string value)
        {
            return ClassificationBuilder.Delegate(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> TypeParameter(string value)
        {
            return ClassificationBuilder.TypeParameter(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> String(string value)
        {
            return ClassificationBuilder.String(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Verbatim(string value)
        {
            return ClassificationBuilder.Verbatim(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Keyword(string value)
        {
            return ClassificationBuilder.Keyword(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> PPKeyword(string value)
        {
            return ClassificationBuilder.PPKeyword(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> PPText(string value)
        {
            return ClassificationBuilder.PPText(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> ExcludedCode(string value)
        {
            return ClassificationBuilder.ExcludedCode(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Identifier(string value)
        {
            return ClassificationBuilder.Identifier(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Inactive(string value)
        {
            return ClassificationBuilder.Inactive(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Comment(string value)
        {
            return ClassificationBuilder.Comment(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Number(string value)
        {
            return ClassificationBuilder.Number(value);
        }

        protected ClassificationBuilder.PunctuationClassificationTypes Punctuation
        {
            get { return ClassificationBuilder.Punctuation; }
        }

        protected ClassificationBuilder.OperatorClassificationTypes Operators
        {
            get { return ClassificationBuilder.Operator; }
        }

        protected ClassificationBuilder.XmlDocClassificationTypes XmlDoc
        {
            get { return ClassificationBuilder.XmlDoc; }
        }
    }
}
