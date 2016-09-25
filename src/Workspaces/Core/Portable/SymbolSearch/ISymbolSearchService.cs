// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchService : IWorkspaceService
    {
        /// <summary>
        /// Searches for packages that contain a type with the provided name and arity.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// They can use or ignore the arity depending on their capabilities. 
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for reference assemblies that contain a type with the provided name and arity.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// They can use or ignore the arity depending on their capabilities. 
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken);
    }

    internal class PackageWithTypeResult
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string PackageName;
        public readonly string TypeName;
        public readonly string Version;

        internal readonly int Rank;

        public PackageWithTypeResult(
            string packageName,
            string typeName,
            string version,
            int rank,
            IReadOnlyList<string> containingNamespaceNames)
        {
            PackageName = packageName;
            TypeName = typeName;
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
            Rank = rank;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }

    internal class ReferenceAssemblyWithTypeResult
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string AssemblyName;
        public readonly string TypeName;

        public ReferenceAssemblyWithTypeResult(
            string assemblyName,
            string typeName,
            IReadOnlyList<string> containingNamespaceNames)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }

    [ExportWorkspaceService(typeof(ISymbolSearchService)), Shared]
    internal class DefaultSymbolSearchService : ISymbolSearchService
    {
        public Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<PackageWithTypeResult>();
        }

        public Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<ReferenceAssemblyWithTypeResult>();
        }
    }
}