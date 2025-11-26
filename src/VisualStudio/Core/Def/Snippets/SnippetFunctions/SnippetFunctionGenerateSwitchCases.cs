// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

internal class SnippetFunctionGenerateSwitchCases : AbstractSnippetFunction
{
    protected readonly string CaseGenerationLocationField;
    protected readonly string SwitchExpressionField;

    public SnippetFunctionGenerateSwitchCases(
        SnippetExpansionClient snippetExpansionClient,
        ITextBuffer subjectBuffer,
        string caseGenerationLocationField,
        string switchExpressionField,
        IThreadingContext threadingContext)
        : base(snippetExpansionClient, subjectBuffer, threadingContext)
    {
        this.CaseGenerationLocationField = caseGenerationLocationField;
        this.SwitchExpressionField = switchExpressionField is ['$', .. var middle, '$'] ? middle : switchExpressionField;
    }

    protected override int FieldChanged(string field, out int requeryFunction)
    {
        requeryFunction = (SwitchExpressionField == field) ? 1 : 0;
        return VSConstants.S_OK;
    }

    protected override async Task<(int ExitCode, string Value, int HasCurrentValue)> GetCurrentValueAsync(CancellationToken cancellationToken)
    {
        var document = GetDocument(cancellationToken);
        if (document is null)
            return (VSConstants.S_OK, string.Empty, HasCurrentValue: 0);

        // If the switch expression is invalid, still show the default case
        var hasCurrentValue = 1;

        var snippetFunctionService = document.Project.GetRequiredLanguageService<SnippetFunctionService>();
        if (!TryGetSpan(SwitchExpressionField, out var switchExpressionSpan) ||
            !TryGetSpan(CaseGenerationLocationField, out var caseGenerationSpan))
        {
            return (VSConstants.S_OK, snippetFunctionService.SwitchDefaultCaseForm, hasCurrentValue);
        }

        var value = await snippetFunctionService.GetSwitchExpansionAsync(
            document, caseGenerationSpan.Value, switchExpressionSpan.Value, cancellationToken).ConfigureAwait(false);
        if (value == null)
            return (VSConstants.S_OK, snippetFunctionService.SwitchDefaultCaseForm, hasCurrentValue);

        return (VSConstants.S_OK, value, hasCurrentValue);
    }

    private bool TryGetSpan(string fieldName, [NotNullWhen(true)] out TextSpan? switchExpressionSpan)
    {
        switchExpressionSpan = null;
        var surfaceBufferFieldSpan = new VsTextSpan[1];
        if (snippetExpansionClient.ExpansionSession?.GetFieldSpan(fieldName, surfaceBufferFieldSpan) != VSConstants.S_OK)
        {
            return false;
        }

        if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out var subjectBufferFieldSpan))
        {
            return false;
        }

        switchExpressionSpan = subjectBufferFieldSpan.Span.ToTextSpan();
        return true;
    }
}
