// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching
{
    public abstract class AbstractBraceMatcherTests
    {
        private Document GetDocument(TestWorkspace workspace)
        {
            return workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
        }

        protected abstract Task<TestWorkspace> CreateWorkspaceFromCodeAsync(string code);

        protected async Task TestAsync(string markup, string expectedCode)
        {
            using (var workspace = await CreateWorkspaceFromCodeAsync(markup))
            {
                var position = workspace.Documents.Single().CursorPosition.Value;
                var document = GetDocument(workspace);
                var braceMatcher = workspace.GetService<IBraceMatchingService>();

                var foundSpan = braceMatcher.FindMatchingSpanAsync(document, position, CancellationToken.None).Result;

                string parsedExpectedCode;
                IList<TextSpan> expectedSpans;
                MarkupTestFile.GetSpans(expectedCode, out parsedExpectedCode, out expectedSpans);

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
