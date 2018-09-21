// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddImport
{
#pragma warning disable RS1016 // Code fix providers should provide FixAll support. https://github.com/dotnet/roslyn/issues/23528
    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider
#pragma warning restore RS1016 // Code fix providers should provide FixAll support.
    {
        private readonly IPackageInstallerService _packageInstallerService;
        private readonly ISymbolSearchService _symbolSearchService;

        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary>
        protected AbstractAddImportCodeFixProvider(
            IPackageInstallerService packageInstallerService = null,
            ISymbolSearchService symbolSearchService = null)
        {
            _packageInstallerService = packageInstallerService;
            _symbolSearchService = symbolSearchService;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;
            var diagnostics = context.Diagnostics;

            var addImportService = document.GetLanguageService<IAddImportFeatureService>();

            var solution = document.Project.Solution;
            var options = solution.Options;

            var searchReferenceAssemblies = options.GetOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, document.Project.Language);
            var searchNuGetPackages = options.GetOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, document.Project.Language);

            var symbolSearchService = searchReferenceAssemblies || searchNuGetPackages
                ? _symbolSearchService ?? solution.Workspace.Services.GetService<ISymbolSearchService>()
                : null;

            var packageSources = symbolSearchService != null && searchNuGetPackages
                ? GetPackageSources(document)
                : ImmutableArray<PackageSource>.Empty;

            var fixesForDiagnostic = await addImportService
                .GetFixesForDiagnosticsAsync(document, span, diagnostics, symbolSearchService, searchReferenceAssemblies, packageSources, cancellationToken).ConfigureAwait(false);

            foreach (var (diagnostic, fixes) in fixesForDiagnostic)
            {
                var codeActions = addImportService.GetCodeActionsForFixes(document, fixes, GetPackageInstallerService(document));
                context.RegisterFixes(codeActions, diagnostic);
            }
        }

        private IPackageInstallerService GetPackageInstallerService(Document document)
            => _packageInstallerService ?? document.Project.Solution.Workspace.Services.GetService<IPackageInstallerService>();

        private ImmutableArray<PackageSource> GetPackageSources(Document document)
            => GetPackageInstallerService(document)?.PackageSources ?? ImmutableArray<PackageSource>.Empty;
    }
}
