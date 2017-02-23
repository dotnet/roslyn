// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal abstract partial class AbstractAddSpecificPackageCodeFixProvider : AbstractAddPackageCodeFixProvider
    {
        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AbstractAddSpecificPackageCodeFixProvider(
            IPackageInstallerService packageInstallerService = null,
            ISymbolSearchService symbolSearchService = null)
            : base(packageInstallerService, symbolSearchService)
        {
        }

        protected override bool IncludePrerelease => true;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var assemblyName = GetAssemblyName(context.Diagnostics[0].Id);

            // context.Document.Project.WithCompilationOptions

            if (assemblyName != null)
            {
                var assemblyNames = new HashSet<string> { assemblyName };
                var addPackageCodeActions = await GetAddPackagesCodeActionsAsync(context, assemblyNames).ConfigureAwait(false);
                context.RegisterFixes(addPackageCodeActions, context.Diagnostics);
            }
        }

        protected abstract string GetAssemblyName(string id);

        class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution) 
                : base(title, createChangedSolution)
            {
            }
        }
    }
}