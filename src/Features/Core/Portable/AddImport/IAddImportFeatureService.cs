// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal interface IAddImportFeatureService : ILanguageService
    {
        /// <summary>
        /// Gets data for how to fix a particular <see cref="Diagnostic" /> id within the specified Document.
        /// Useful when you do not have an instance of the diagnostic, such as when invoked as a remote service.
        /// </summary>
        Task<ImmutableArray<AddImportFixData>> GetFixesAsync(
            Document document, TextSpan span, string diagnosticId, int maxResults, bool placeSystemNamespaceFirst,
            ISymbolSearchService symbolSearchService, bool searchReferenceAssemblies,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);

        /// <summary>
        /// Gets data for how to fix a set of <see cref="Diagnostic" />s within the specified Document.
        /// The fix data can be used to create code actions that apply the fixes.
        /// </summary>
        Task<ImmutableArray<(Diagnostic Diagnostic, ImmutableArray<AddImportFixData> Fixes)>> GetFixesForDiagnosticsAsync(
            Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, int maxResultsPerDiagnostic,
            ISymbolSearchService symbolSearchService, bool searchReferenceAssemblies,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);

        /// <summary>
        /// Gets code actions that, when applied, will fix the missing imports for the document using
        /// the information from the provided fixes.
        /// </summary>
        ImmutableArray<CodeAction> GetCodeActionsForFixes(
            Document document, ImmutableArray<AddImportFixData> fixes,
            IPackageInstallerService installerService, int maxResults);
    }
}
