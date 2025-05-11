// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.Text;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

internal sealed class SnippetFunctionSimpleTypeName : AbstractSnippetFunction
{
    private readonly string _fieldName;
    private readonly string _fullyQualifiedName;

    public SnippetFunctionSimpleTypeName(
        SnippetExpansionClient snippetExpansionClient,
        ITextBuffer subjectBuffer,
        string fieldName,
        string fullyQualifiedName,
        IThreadingContext threadingContext)
        : base(snippetExpansionClient, subjectBuffer, threadingContext)
    {
        _fieldName = fieldName;
        _fullyQualifiedName = fullyQualifiedName;
    }

    protected override async Task<(int ExitCode, string Value, int HasDefaultValue)> GetDefaultValueAsync(CancellationToken cancellationToken)
    {
        var value = _fullyQualifiedName;
        var hasDefaultValue = 1;
        var document = GetDocument(cancellationToken);
        if (document is null)
            return (VSConstants.E_FAIL, value, hasDefaultValue);

        if (!TryGetFieldSpan(out var fieldSpan))
            return (VSConstants.E_FAIL, value, hasDefaultValue);

        var simplifierOptions = await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

        var simplifiedTypeName = await SnippetFunctionService.GetSimplifiedTypeNameAsync(document, fieldSpan.Value, _fullyQualifiedName, simplifierOptions, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(simplifiedTypeName))
            return (VSConstants.E_FAIL, value, hasDefaultValue);

        return (VSConstants.S_OK, simplifiedTypeName!, hasDefaultValue);
    }

    private bool TryGetFieldSpan([NotNullWhen(true)] out TextSpan? fieldSpan)
    {
        fieldSpan = null;
        var surfaceBufferFieldSpan = new VsTextSpan[1];
        if (snippetExpansionClient.ExpansionSession == null)
        {
            return false;
        }

        if (snippetExpansionClient.ExpansionSession.GetFieldSpan(_fieldName, surfaceBufferFieldSpan) != VSConstants.S_OK)
        {
            return false;
        }

        if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out var subjectBufferFieldSpan))
        {
            return false;
        }

        fieldSpan = new TextSpan(subjectBufferFieldSpan.Start, subjectBufferFieldSpan.Length);
        return true;
    }
}
