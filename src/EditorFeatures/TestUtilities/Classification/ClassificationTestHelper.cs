// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public static class ClassificationTestHelper
    {
        private static string GetText(Tuple<string, string> tuple)
        {
            return "(" + tuple.Item1 + ", " + tuple.Item2 + ")";
        }

        private static string GetText(ClassifiedSpan tuple)
        {
            return "(" + tuple.TextSpan + ", " + tuple.ClassificationType + ")";
        }

        internal static void Verify(
            string expectedText,
            IEnumerable<Tuple<string, string>> expectedClassifications,
            string actualText,
            IEnumerable<ClassifiedSpan> actualClassifications)
        {
            Assert.Equal(expectedText, actualText);

            if (expectedClassifications != null)
            {
                var expectedClassificationList = expectedClassifications.ToList();
                var actualClassificationList = actualClassifications.ToList();

                actualClassificationList.Sort((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start);

                var max = Math.Max(expectedClassificationList.Count, actualClassificationList.Count);
                for (int i = 0; i < max; i++)
                {
                    if (i >= expectedClassificationList.Count)
                    {
                        AssertEx.Fail("Unexpected actual classification: {0}", GetText(actualClassificationList[i]));
                    }
                    else if (i >= actualClassificationList.Count)
                    {
                        AssertEx.Fail("Missing classification for: {0}", GetText(expectedClassificationList[i]));
                    }

                    var actual = actualClassificationList[i];
                    var expected = expectedClassificationList[i];

                    var text = actualText.Substring(actual.TextSpan.Start, actual.TextSpan.Length);
                    Assert.Equal(expected.Item1, text);
                    Assert.Equal(expected.Item2, actual.ClassificationType);
                }
            }
        }

        internal static void Verify(
            string expectedText,
            IEnumerable<Tuple<string, string>> expectedClassifications,
            IList<TaggedText> actualContent)
        {
            Verify(
                expectedText,
                expectedClassifications,
                actualText: actualContent.GetFullText(),
                actualClassifications: actualContent.ToClassifiedSpans());
        }
    }
}
