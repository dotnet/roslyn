// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.LiveShare
{
    public class ClassificationsHandlerTests : LiveShareRequestHandlerTestsBase
    {
        [Fact]
        public async Task TestClassificationsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|classify:var|} i = 1;
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var classifyLocation = ranges["classify"].First();

            var results = await TestHandleAsync<ClassificationParams, ClassificationSpan[]>(solution, CreateClassificationParams(classifyLocation));
            AssertCollectionsEqual(new ClassificationSpan[] { CreateClassificationSpan("keyword", classifyLocation.Range) }, results, AssertClassificationsEqual);
        }

        private static void AssertClassificationsEqual(ClassificationSpan expected, ClassificationSpan actual)
        {
            Assert.Equal(expected.Classification, actual.Classification);
            Assert.Equal(expected.Range, actual.Range);
        }

        private static ClassificationSpan CreateClassificationSpan(string classification, Range range)
            => new ClassificationSpan()
            {
                Classification = classification,
                Range = range
            };

        private static ClassificationParams CreateClassificationParams(Location location)
            => new ClassificationParams()
            {
                Range = location.Range,
                TextDocument = CreateTextDocumentIdentifier(location.Uri)
            };
    }
}
