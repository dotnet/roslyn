// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Debugging;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.Debugging;

public abstract class AbstractDataTipInfoGetterTests
{
    protected abstract EditorTestWorkspace CreateWorkspace(string markup);

    protected Task TestAsync(XElement markup, string? expectedText = null)
        => TestAsync(markup.NormalizedValue(), expectedText);

    protected Task TestNoDataTipAsync(XElement markup)
        => TestNoDataTipAsync(markup.NormalizedValue());

    protected Task TestAsync(string markup, string? expectedText = null)
        => TestSpanGetterAsync(markup, async (workspace, document, position, expectedSpan) =>
        {
            var service = document.GetRequiredLanguageService<ILanguageDebugInfoService>();
            var result = await service.GetDataTipInfoAsync(document, position, includeKind: true, CancellationToken.None);

            Assert.Equal(expectedSpan, result.Span);
            Assert.Equal(expectedText, result.Text);

            var linqExpressionSpans = workspace.DocumentWithCursor.AnnotatedSpans.GetValueOrDefault("LinqExpression").NullToEmpty();

            Assert.Equal(result.Kind == DebugDataTipInfoKind.LinqExpression, linqExpressionSpans.Length == 1);
            if (linqExpressionSpans.Length == 1)
            {
                Assert.Equal(linqExpressionSpans.Single(), result.ExpressionSpan);
            }
        });

    protected Task TestNoDataTipAsync(string markup)
        => TestSpanGetterAsync(markup, async (workspace, document, position, expectedSpan) =>
        {
            var result = await DataTipInfoGetter.GetInfoAsync(document, position, includeKind: true, CancellationToken.None);
            Assert.True(result.IsDefault);
        });

    private async Task TestSpanGetterAsync(string markup, Func<EditorTestWorkspace, Document, int, TextSpan?, Task> continuation)
    {
        using var workspace = CreateWorkspace(markup);
        var testHostDocument = workspace.Documents.Single();
        var position = testHostDocument.CursorPosition!.Value;
        var expectedSpan = testHostDocument.SelectedSpans.Any()
            ? testHostDocument.SelectedSpans.Single()
            : (TextSpan?)null;

        await continuation(
            workspace,
            workspace.CurrentSolution.Projects.First().Documents.First(),
            position,
            expectedSpan);
    }
}
