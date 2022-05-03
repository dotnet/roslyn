// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    [DataContract]
    internal readonly record struct AddImportOptions(
        [property: DataMember(Order = 0)] SymbolSearchOptions SearchOptions,
        [property: DataMember(Order = 1)] bool HideAdvancedMembers,
        [property: DataMember(Order = 2)] AddImportPlacementOptions Placement);

    internal interface IAddImportFeatureService : ILanguageService
    {
        /// <summary>
        /// Gets data for how to fix a particular <see cref="Diagnostic" /> id within the specified Document.
        /// Useful when you do not have an instance of the diagnostic, such as when invoked as a remote service.
        /// </summary>
        Task<ImmutableArray<AddImportFixData>> GetFixesAsync(
            Document document, TextSpan span, string diagnosticId, int maxResults,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);

        /// <summary>
        /// Gets data for how to fix a set of <see cref="Diagnostic" />s within the specified Document.
        /// The fix data can be used to create code actions that apply the fixes.
        /// </summary>
        Task<ImmutableArray<(Diagnostic Diagnostic, ImmutableArray<AddImportFixData> Fixes)>> GetFixesForDiagnosticsAsync(
            Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, int maxResultsPerDiagnostic,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);

        /// <summary>
        /// Gets code actions that, when applied, will fix the missing imports for the document using
        /// the information from the provided fixes.
        /// </summary>
        ImmutableArray<CodeAction> GetCodeActionsForFixes(
            Document document, ImmutableArray<AddImportFixData> fixes,
            IPackageInstallerService? installerService, int maxResults);

        /// <summary>
        /// Gets data for how to fix a particular <see cref="Diagnostic" /> id within the specified Document.
        /// Similar to <see cref="GetFixesAsync(Document, TextSpan, string, int, ISymbolSearchService, AddImportOptions, ImmutableArray{PackageSource}, CancellationToken)"/> 
        /// except it only returns fix data when there is a single using fix for a given span
        /// </summary>
        Task<ImmutableArray<AddImportFixData>> GetUniqueFixesAsync(
            Document document, TextSpan span, ImmutableArray<string> diagnosticIds,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);
    }
}
