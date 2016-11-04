// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal abstract partial class AbstractAddSpecificPackageCodeFixProvider : CodeFixProvider
    {
        private readonly IPackageInstallerService _packageInstallerService;
        private readonly ISymbolSearchService _symbolSearchService;

        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AbstractAddSpecificPackageCodeFixProvider(
            IPackageInstallerService packageInstallerService = null,
            ISymbolSearchService symbolSearchService = null)
        {
            _packageInstallerService = packageInstallerService;
            _symbolSearchService = symbolSearchService;
        }

        protected abstract string GetPackageName(string diagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var packageName = GetPackageName(context.Diagnostics[0].Id);
            if (packageName != null)
            {
                var addPackageCodeActions = await GetAddPackagesCodeActionsAsync(
                    context, packageName).ConfigureAwait(false);
                context.RegisterFixes(addPackageCodeActions, context.Diagnostics);
            }
        }

        private async Task<ImmutableArray<CodeAction>> GetAddPackagesCodeActionsAsync(
            CodeFixContext context, string packageName)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var workspaceServices = document.Project.Solution.Workspace.Services;

            var symbolSearchService = _symbolSearchService ?? workspaceServices.GetService<ISymbolSearchService>();
            var installerService = _packageInstallerService ?? workspaceServices.GetService<IPackageInstallerService>();

            var language = document.Project.Language;

            var options = workspaceServices.Workspace.Options;
            var searchNugetPackages = options.GetOption(
                SymbolSearchOptions.SuggestForTypesInNuGetPackages, language);

            var codeActions = ArrayBuilder<CodeAction>.GetInstance();
            if (symbolSearchService != null &&
                installerService != null &&
                searchNugetPackages &&
                installerService.IsEnabled)
            {
                foreach (var packageSource in installerService.PackageSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var packageInfo = await symbolSearchService.FindPackageAsync(
                        packageSource, packageName, cancellationToken).ConfigureAwait(false);

                    if (packageInfo != null)
                    {
                        codeActions.Add(new InstallPackageParentCodeAction(
                            installerService, packageInfo, document));
                    }
                }
            }

            return codeActions.ToImmutableAndFree();
        }
    }
}