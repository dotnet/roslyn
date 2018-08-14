// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
