// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceTests
{
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public class DocumentExtensionsTests
    {
        [Fact]
        public async Task GetSemanticModelForSpanAsync1()
        {
            using (var ws = new AdhocWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                var src = "class C {}";
                var doc = ws.AddDocument(project.Id, "test.cs", SourceText.From(src));

                Assert.NotNull(await doc.GetSemanticModelForSpanAsync(new TextSpan(src.Length, 0), default(CancellationToken)).ConfigureAwait(false));
                Assert.NotNull(await doc.GetSemanticModelForSpanAsync(new TextSpan(src.Length + 1, 0), default(CancellationToken)).ConfigureAwait(false));
            }
        }
    }
}
