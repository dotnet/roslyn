// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public static class ClassificationTestHelper
    {
        private static string GetText(FormattedClassification formattedClassification)
            => $"({formattedClassification.Text}, {formattedClassification.ClassificationName})";

        private static string GetText(ClassifiedSpan tuple)
            => $"({tuple.TextSpan}, {tuple.ClassificationType})";

        public static void VerifyTextAndClassifications(
            string expectedText,
            IEnumerable<FormattedClassification> expectedClassifications,
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
                for (var i = 0; i < max; i++)
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
                    Assert.Equal(expected.Text, text);
                    Assert.Equal(expected.ClassificationName, actual.ClassificationType);
                }
            }
        }

        public static void VerifyTextAndClassifications(
            string expectedText,
            IEnumerable<FormattedClassification> expectedClassifications,
            IList<TaggedText> actualContent)
        {
            VerifyTextAndClassifications(
                expectedText,
                expectedClassifications,
                actualText: actualContent.GetFullText(),
                actualClassifications: actualContent.ToClassifiedSpans());
        }
    }
}
