// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Tasks;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DocumentTaskDiagnosticSourceProvider([Import] IGlobalOptionService globalOptions) : IDiagnosticSourceProvider
{
    public bool IsDocument => true;
    public string Name => PullDiagnosticCategories.Task;

    public bool IsEnabled(ClientCapabilities capabilities) => capabilities.HasVisualStudioLspCapability();

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        return new([new TaskListDiagnosticSource(context.GetRequiredDocument(), globalOptions)]);
    }
}

