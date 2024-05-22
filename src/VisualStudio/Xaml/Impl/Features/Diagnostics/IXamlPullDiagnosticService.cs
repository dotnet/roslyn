// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics
{
    internal interface IXamlPullDiagnosticService : ILanguageService
    {
        /// <summary>
        /// Get diagnostic report for the given TextDocument.
        /// </summary>
        /// <param name="document">The TextDocument to get diagnostic report from. Should not be null.</param>
        /// <param name="previousResultId">Previous ResultId we get from the Pull Diagnostic request. This can null when we don't see a corresponding previousResultId for this document from the request.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>A XamlDiagnosticReport which will be used as the response to the Pull Diagnostic request.</returns>
        Task<XamlDiagnosticReport> GetDiagnosticReportAsync(TextDocument document, string? previousResultId, CancellationToken cancellationToken);
    }
}
