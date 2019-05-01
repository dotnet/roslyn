// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal interface ICodeCleanupService : ILanguageService
    {
        Task<Document> CleanupAsync(Document document, EnabledDiagnosticOptions enabledDiagnostics, IProgressTracker progressTracker, CancellationToken cancellationToken);
        EnabledDiagnosticOptions GetAllDiagnostics();
    }
}
