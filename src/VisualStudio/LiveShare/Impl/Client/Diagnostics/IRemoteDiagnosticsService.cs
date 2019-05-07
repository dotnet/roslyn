//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    /// <summary>
    /// A service to get diagnostics for a given document from the remote machine.
    /// </summary>
    internal interface IRemoteDiagnosticsService : ILanguageService
    {
        /// <summary>
        /// Given a document get the diagnostics in that document.
        /// </summary>
        Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken);
    }
}
