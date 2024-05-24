// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

internal abstract partial class AbstractSnippetFunction : IVsExpansionFunction
{
    private readonly ITextBuffer _subjectBuffer;
    private readonly IThreadingContext _threadingContext;

    protected readonly SnippetExpansionClient snippetExpansionClient;

    public AbstractSnippetFunction(SnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, IThreadingContext threadingContext)
    {
        this.snippetExpansionClient = snippetExpansionClient;
        _subjectBuffer = subjectBuffer;
        _threadingContext = threadingContext;
    }

    protected Document? GetDocument(CancellationToken cancellationToken)
        => _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges()?.WithFrozenPartialSemantics(cancellationToken);

    private int GetDefaultValue(CancellationToken cancellationToken, out string value, out int hasDefaultValue)
    {
        (var exitCode, value, hasDefaultValue) = _threadingContext.JoinableTaskFactory.Run(() => GetDefaultValueAsync(cancellationToken));
        return exitCode;
    }

    protected virtual Task<(int ExitCode, string Value, int HasDefaultValue)> GetDefaultValueAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((ExitCode: VSConstants.S_OK, Value: string.Empty, HasDefaultValue: 0));
    }

    private int GetCurrentValue(CancellationToken cancellationToken, out string value, out int hasCurrentValue)
    {
        (var exitCode, value, hasCurrentValue) = _threadingContext.JoinableTaskFactory.Run(() => GetCurrentValueAsync(cancellationToken));
        return exitCode;
    }

    protected virtual Task<(int ExitCode, string Value, int HasCurrentValue)> GetCurrentValueAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((ExitCode: VSConstants.S_OK, Value: string.Empty, HasDefaultValue: 0));
    }

    protected virtual int FieldChanged(string field, out int requeryFunction)
    {
        requeryFunction = 0;
        return VSConstants.S_OK;
    }
}
