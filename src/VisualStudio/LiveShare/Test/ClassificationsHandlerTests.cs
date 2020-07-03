// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class ClassificationsHandlerTests : AbstractLiveShareRequestHandlerTests
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
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var classifyLocation = locations["classify"].First();

            var results = await TestHandleAsync<ClassificationParams, object[]>(workspace.CurrentSolution, CreateClassificationParams(classifyLocation), RoslynMethods.ClassificationsName);
            AssertJsonEquals(new ClassificationSpan[] { CreateClassificationSpan("keyword", classifyLocation.Range) }, results);
        }

        [Fact]
        public async Task TestClassificationsAsync_WithMappedPath()
        {
            var markup =
@"class A
{
    void M()
    {
        {|classify:var|} i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var classifyLocation = locations["classify"].First();

            var guestUri = new Uri("vsls://guestUri");

            var results = await TestHandleAsync<ClassificationParams, object[]>(workspace.CurrentSolution,
                CreateClassificationParams(classifyLocation, guestUri), RoslynMethods.ClassificationsName, ConversionFunction);
            AssertJsonEquals(new ClassificationSpan[] { CreateClassificationSpan("keyword", classifyLocation.Range) }, results);

            Uri ConversionFunction(Uri uri) => uri == guestUri ? classifyLocation.Uri : null;
        }

        private static ClassificationSpan CreateClassificationSpan(string classification, LanguageServer.Protocol.Range range)
            => new ClassificationSpan()
            {
                Classification = classification,
                Range = range
            };

        private static ClassificationParams CreateClassificationParams(Location location)
            => CreateClassificationParams(location, location.Uri);

        private static ClassificationParams CreateClassificationParams(Location location, Uri originalUri)
            => new ClassificationParams()
            {
                Range = location.Range,
                TextDocument = CreateTextDocumentIdentifier(originalUri)
            };
    }
}
