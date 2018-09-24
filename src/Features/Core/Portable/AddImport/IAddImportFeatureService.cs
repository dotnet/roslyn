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
        Task<ImmutableArray<AddImportFixData>> GetFixesAsync(
            Document document, TextSpan span, string diagnosticId, bool placeSystemNamespaceFirst,
            ISymbolSearchService symbolSearchService, bool searchReferenceAssemblies,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);

        Task<ImmutableArray<(Diagnostic Diagnostic, ImmutableArray<AddImportFixData> Fixes)>> GetFixesForDiagnosticsAsync(
            Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics,
            ISymbolSearchService symbolSearchService, bool searchReferenceAssemblies,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);

        ImmutableArray<CodeAction> GetCodeActionsForFixes(Document document, ImmutableArray<AddImportFixData> fixes, IPackageInstallerService installerService, bool limitResults);
    }
}
