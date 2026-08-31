// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Classification;

[UseExportProvider]
public sealed class ClassifiedSpansAndHighlightSpanFactoryTests
{
    [Fact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/8354")]
    public async Task ClassifyAsync_ReferenceOnBlankLine()
    {
        using var workspace = TestWorkspace.CreateCSharp("\r\nclass C;");
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

        var result = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
            new DocumentSpan(document, new TextSpan(0, 0)),
            classifiedSpans: null,
            ClassificationOptions.Default,
            CancellationToken.None);

        Assert.Empty(result.ClassifiedSpans);
        Assert.Equal(default, result.HighlightSpan);
    }
}
