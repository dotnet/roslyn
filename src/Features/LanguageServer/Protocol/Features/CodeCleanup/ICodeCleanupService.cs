// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal interface ICodeCleanupService : ILanguageService
    {
        Task<Document> CleanupAsync(Document document, EnabledDiagnosticOptions enabledDiagnostics, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);
        EnabledDiagnosticOptions GetAllDiagnostics();
    }
}
