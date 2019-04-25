// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.LiveShare
{
    public class DiagnosticsHandlerTests : LiveShareRequestHandlerTestsBase
    {
        [Fact]
        public async Task TestDiagnosticsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|diagnostic:int|} i = 1;
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var diagnosticLocation = ranges["diagnostic"].First();

            var results = await TestHandleAsync<TextDocumentParams, Diagnostic[]>(solution, CreateTestDocumentParams(diagnosticLocation.Uri));
            int i = 1;
            //AssertCollectionsEqual(new ClassificationSpan[] { CreateClassificationSpan("keyword", classifyLocation.Range) }, results, AssertClassificationsEqual);
        }

        private static void AssertClassificationsEqual(ClassificationSpan expected, ClassificationSpan actual)
        {
            Assert.Equal(expected.Classification, actual.Classification);
            Assert.Equal(expected.Range, actual.Range);
        }

        private static TextDocumentParams CreateTestDocumentParams(Uri uri)
            => new TextDocumentParams()
            {
                TextDocument = CreateTextDocumentIdentifier(uri)
            };

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
