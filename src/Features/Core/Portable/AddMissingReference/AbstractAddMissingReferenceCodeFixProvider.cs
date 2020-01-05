// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddMissingReference
{
    internal abstract partial class AbstractAddMissingReferenceCodeFixProvider : AbstractAddPackageCodeFixProvider
    {
        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AbstractAddMissingReferenceCodeFixProvider(
            IPackageInstallerService packageInstallerService = null,
            ISymbolSearchService symbolSearchService = null)
            : base(packageInstallerService, symbolSearchService)
        {
        }

        protected override bool IncludePrerelease => false;

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not support for this code fix
            // https://github.com/dotnet/roslyn/issues/34459
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var uniqueIdentities = await GetUniqueIdentitiesAsync(context).ConfigureAwait(false);

            var assemblyNames = uniqueIdentities.Select(i => i.Name).ToSet();
            var addPackageCodeActions = await GetAddPackagesCodeActionsAsync(context, assemblyNames).ConfigureAwait(false);
            var addReferenceCodeActions = await GetAddReferencesCodeActionsAsync(context, uniqueIdentities).ConfigureAwait(false);

            context.RegisterFixes(addPackageCodeActions, context.Diagnostics);
            context.RegisterFixes(addReferenceCodeActions, context.Diagnostics);
        }

        private static async Task<ImmutableArray<CodeAction>> GetAddReferencesCodeActionsAsync(CodeFixContext context, ISet<AssemblyIdentity> uniqueIdentities)
        {
            var result = ArrayBuilder<CodeAction>.GetInstance();
            foreach (var identity in uniqueIdentities)
            {
                var codeAction = await AddMissingReferenceCodeAction.CreateAsync(
                    context.Document.Project, identity, context.CancellationToken).ConfigureAwait(false);
                result.Add(codeAction);
            }

            return result.ToImmutableAndFree();
        }

        private async Task<ISet<AssemblyIdentity>> GetUniqueIdentitiesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var compilation = await context.Document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var uniqueIdentities = new HashSet<AssemblyIdentity>();
            foreach (var diagnostic in context.Diagnostics)
            {
                var assemblyIds = compilation.GetUnreferencedAssemblyIdentities(diagnostic);
                uniqueIdentities.AddRange(assemblyIds);

                var properties = diagnostic.Properties;
                if (properties.TryGetValue(DiagnosticPropertyConstants.UnreferencedAssemblyIdentity, out var displayName) &&
                    AssemblyIdentity.TryParseDisplayName(displayName, out var serializedIdentity))
                {
                    uniqueIdentities.Add(serializedIdentity);
                }
            }

            uniqueIdentities.Remove(compilation.Assembly.Identity);
            return uniqueIdentities;
        }
    }
}
