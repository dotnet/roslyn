// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Diagnostics
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
