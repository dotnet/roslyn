// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchService : IWorkspaceService
    {
        /// <summary>
        /// Checks the given source for a package with the specified name.
        /// </summary>
        Task<PackageInfo> FindPackageAsync(
            PackageSource source, string packageName, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for packages that contain a type with the provided name and arity.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// They can use or ignore the arity depending on their capabilities. 
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<ImmutableArray<PackageWithTypeInfo>> FindPackagesWithTypeAsync(
            PackageSource source, string name, int arity, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for packages that contain an assembly with the provided name.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<ImmutableArray<PackageWithAssemblyInfo>> FindPackagesWithAssemblyAsync(
            PackageSource source, string assemblyName, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for reference assemblies that contain a type with the provided name and arity.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// They can use or ignore the arity depending on their capabilities. 
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<ImmutableArray<ReferenceAssemblyWithTypeInfo>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ISymbolSearchService)), Shared]
    internal class DefaultSymbolSearchService : ISymbolSearchService
    {
        public Task<PackageInfo> FindPackageAsync(PackageSource source, string packageName, CancellationToken cancellationToken)
            => SpecializedTasks.Default<PackageInfo>();

        public Task<ImmutableArray<PackageWithTypeInfo>> FindPackagesWithTypeAsync(
            PackageSource source, string name, int arity, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<PackageWithTypeInfo>();
        }

        public Task<ImmutableArray<PackageWithAssemblyInfo>> FindPackagesWithAssemblyAsync(
            PackageSource source, string assemblyName, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<PackageWithAssemblyInfo>();
        }

        public Task<ImmutableArray<ReferenceAssemblyWithTypeInfo>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<ReferenceAssemblyWithTypeInfo>();
        }
    }
}