// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
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

        /// <summary>
        /// Add-using gets special privileges as being the most used code-action, along with being a core
        /// 'smart tag' feature in VS prior to us even having 'light bulbs'.  We want them to be computed
        /// first, ahead of everything else, and the main results should show up at the top of the list.
        /// </summary>
        private protected override CodeActionRequestPriority ComputeRequestPriority()
            => CodeActionRequestPriority.High;

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

            var searchNuGetPackages = solution.Options.GetOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, document.Project.Language);

            var options = new AddImportOptions(
                context.Options.SearchReferenceAssemblies,
                context.Options.HideAdvancedMembers);

            var symbolSearchService = options.SearchReferenceAssemblies || searchNuGetPackages
                ? _symbolSearchService ?? solution.Workspace.Services.GetService<ISymbolSearchService>()
                : null;

            var installerService = GetPackageInstallerService(document);
            var packageSources = searchNuGetPackages && symbolSearchService != null && installerService?.IsEnabled(document.Project.Id) == true
                ? installerService.TryGetPackageSources()
                : ImmutableArray<PackageSource>.Empty;

            var fixesForDiagnostic = await addImportService.GetFixesForDiagnosticsAsync(
                document, span, diagnostics, MaxResults, symbolSearchService, options, packageSources, cancellationToken).ConfigureAwait(false);

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
