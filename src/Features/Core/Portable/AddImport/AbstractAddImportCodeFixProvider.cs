﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider
    {
        private const int MaxResults = 5;

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

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // Currently Fix All is not supported for this provider
            // https://github.com/dotnet/roslyn/issues/34457
            return null;
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

            var installerService = GetPackageInstallerService(document);
            var packageSources = searchNuGetPackages && symbolSearchService != null && installerService?.IsEnabled(document.Project.Id) == true
                ? installerService.TryGetPackageSources()
                : ImmutableArray<PackageSource>.Empty;

            var fixesForDiagnostic = await addImportService.GetFixesForDiagnosticsAsync(
                document, span, diagnostics, MaxResults, symbolSearchService, searchReferenceAssemblies, packageSources, cancellationToken).ConfigureAwait(false);

            foreach (var (diagnostic, fixes) in fixesForDiagnostic)
            {
                // Limit the results returned since this will be displayed to the user
                var codeActions = addImportService.GetCodeActionsForFixes(document, fixes, installerService, MaxResults);
                context.RegisterFixes(codeActions, diagnostic);
            }
        }

        private IPackageInstallerService GetPackageInstallerService(Document document)
            => _packageInstallerService ?? document.Project.Solution.Workspace.Services.GetService<IPackageInstallerService>();
    }
}
