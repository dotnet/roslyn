// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching
{
    [UseExportProvider]
    public abstract class AbstractBraceMatcherTests
    {
        protected abstract TestWorkspace CreateWorkspaceFromCode(string code, ParseOptions options);

        protected async Task TestAsync(string markup, string expectedCode, ParseOptions options = null)
        {
            using (var workspace = CreateWorkspaceFromCode(markup, options))
            {
                var position = workspace.Documents.Single().CursorPosition.Value;
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var braceMatcher = workspace.GetService<IBraceMatchingService>();

                var foundSpan = await braceMatcher.FindMatchingSpanAsync(document, position, CancellationToken.None);
                MarkupTestFile.GetSpans(expectedCode, out var parsedExpectedCode, out ImmutableArray<TextSpan> expectedSpans);

                if (expectedSpans.Any())
                {
                    Assert.Equal(expectedSpans.Single(), foundSpan.Value);
                }
                else
                {
                    Assert.False(foundSpan.HasValue);
                }
            }
        }
    }
}
