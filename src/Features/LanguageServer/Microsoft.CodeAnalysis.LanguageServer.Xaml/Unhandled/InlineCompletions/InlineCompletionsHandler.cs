// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Supports built in legacy snippets for razor scenarios.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(InlineCompletionsHandler)), Shared]
[XamlMethod(VSInternalMethods.TextDocumentInlineCompletionName)]
internal class InlineCompletionsHandler : ILspServiceDocumentRequestHandler<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList?>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlineCompletionsHandler()
    {
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalInlineCompletionRequest request)
    {
        return request.TextDocument;
    }

    public Task<VSInternalInlineCompletionList?> HandleRequestAsync(VSInternalInlineCompletionRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<VSInternalInlineCompletionList?>(null);
    }
}
