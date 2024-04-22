// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class XamlDiagnosticSourceProvider([Import(AllowDefault = true)] IXamlDiagnosticSource? xamlDiagnosticSource) : IDiagnosticSourceProvider
{
    bool IDiagnosticSourceProvider.IsDocument => true;

    string IDiagnosticSourceProvider.Name => "XamlDiagnosticSource";

    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (xamlDiagnosticSource != null && context.TextDocument is { } document &&
            document.Project.GetAdditionalDocument(document.Id) != null)
        {
            return new([new XamlDiagnosticSource(xamlDiagnosticSource, document)]);
        }

        return new([]);
    }
}
