// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion
{
    [UseExportProvider]
    public class CompletionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AcquireCompletionService()
        {
            var workspace = new AdhocWorkspace();

            var document = workspace
                .AddProject("TestProject", LanguageNames.CSharp)
                .AddDocument("TestDocument.cs", "");

            var service = CompletionService.GetService(document);
            Assert.NotNull(service);
        }
    }
}
