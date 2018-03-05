// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
#pragma warning disable RS1016 // Code fix providers should provide FixAll support. https://github.com/dotnet/roslyn/issues/23528
    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider
#pragma warning restore RS1016 // Code fix providers should provide FixAll support.
    {
        private const int MaxResults = 3;

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

            // We might have multiple different diagnostics covering the same span.  Have to
            // process them all as we might produce different fixes for each diagnostic.

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst);

            foreach (var diagnostic in context.Diagnostics)
            {
                var fixes = await addImportService.GetFixesAsync(
                    document, span, diagnostic.Id, placeSystemNamespaceFirst,
                    symbolSearchService, searchReferenceAssemblies, 
                    packageSources, cancellationToken).ConfigureAwait(false);

                var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                foreach (var fix in fixes)
                {
                    var codeAction = TryCreateCodeAction(document, fix);
                    codeActions.AddIfNotNull(codeAction);

                    if (codeActions.Count >= MaxResults)
                    {
                        break;
                    }
                }

                context.RegisterFixes(codeActions, diagnostic);
                codeActions.Free();
            }
        }

        private IPackageInstallerService GetPackageInstallerService(Document document)
            => _packageInstallerService ?? document.Project.Solution.Workspace.Services.GetService<IPackageInstallerService>();

        private ImmutableArray<PackageSource> GetPackageSources(Document document)
            => GetPackageInstallerService(document)?.PackageSources ?? ImmutableArray<PackageSource>.Empty;

        private CodeAction TryCreateCodeAction(Document document, AddImportFixData fixData)
        {
            if (fixData == null)
            {
                return null;
            }

            switch (fixData.Kind)
            {
                case AddImportFixKind.ProjectSymbol:
                    return new ProjectSymbolReferenceCodeAction(document, fixData);

                case AddImportFixKind.MetadataSymbol:
                    return new MetadataSymbolReferenceCodeAction(document, fixData);

                case AddImportFixKind.ReferenceAssemblySymbol:
                    return new AssemblyReferenceCodeAction(document, fixData);

                case AddImportFixKind.PackageSymbol:
                    var packageInstaller = GetPackageInstallerService(document);
                    return !packageInstaller.IsInstalled(document.Project.Solution.Workspace, document.Project.Id, fixData.PackageName)
                        ? new ParentInstallPackageCodeAction(document, fixData, GetPackageInstallerService(document))
                        : null;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
