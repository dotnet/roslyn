// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion
{
    public class CompletionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AcquireCompletionService()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Concat(
                    new[]
                    {
                        typeof(CompletionService).Assembly,
                        typeof(CSharpCompletionService).Assembly
                    }));

            var workspace = new AdhocWorkspace(hostServices);

            var document = workspace
                .AddProject("TestProject", LanguageNames.CSharp)
                .AddDocument("TestDocument.cs", "");

            var service = CompletionService.GetService(document);
            Assert.NotNull(service);
        }
    }
}
