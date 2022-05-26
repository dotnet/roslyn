﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;
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

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported by this code fix
            // https://github.com/dotnet/roslyn/issues/34458
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var assemblyName = GetAssemblyName(context.Diagnostics[0].Id);

            if (assemblyName != null)
            {
                var assemblyNames = new HashSet<string> { assemblyName };
                var addPackageCodeActions = await GetAddPackagesCodeActionsAsync(context, assemblyNames).ConfigureAwait(false);
                context.RegisterFixes(addPackageCodeActions, context.Diagnostics);
            }
        }

        protected abstract string GetAssemblyName(string id);
    }
}
