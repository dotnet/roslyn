// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for packages that contain an assembly with the provided name.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
            string source, string assemblyName, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for reference assemblies that contain a type with the provided name and arity.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// They can use or ignore the arity depending on their capabilities. 
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken);
    }

    internal abstract class PackageResult
    {
        public readonly string PackageName;
        internal readonly int Rank;

        protected PackageResult(string packageName, int rank)
        {
            PackageName = packageName;
            Rank = rank;
        }
    }

    internal class PackageWithTypeResult : PackageResult
    {
        public readonly IList<string> ContainingNamespaceNames;
        public readonly string TypeName;
        public readonly string Version;

        public PackageWithTypeResult(
            string packageName,
            string typeName,
            string version,
            int rank,
            IList<string> containingNamespaceNames)
            : base(packageName, rank)
        {
            TypeName = typeName;
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }

    internal class PackageWithAssemblyResult : PackageResult, IEquatable<PackageWithAssemblyResult>, IComparable<PackageWithAssemblyResult>
    {
        public readonly string Version;

        public PackageWithAssemblyResult(
            string packageName,
            string version,
            int rank)
            : base(packageName, rank)
        {
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
        }

        public override int GetHashCode()
            => PackageName.GetHashCode();

        public override bool Equals(object obj)
            => Equals((PackageWithAssemblyResult)obj);

        public bool Equals(PackageWithAssemblyResult other)
            => PackageName.Equals(other.PackageName);

        public int CompareTo(PackageWithAssemblyResult other)
        {
            var diff = Rank - other.Rank;
            if (diff != 0)
            {
                return -diff;
            }

            return PackageName.CompareTo(other.PackageName);
        }
    }

    internal class ReferenceAssemblyWithTypeResult
    {
        public readonly IList<string> ContainingNamespaceNames;
        public readonly string AssemblyName;
        public readonly string TypeName;

        public ReferenceAssemblyWithTypeResult(
            string assemblyName,
            string typeName,
            IList<string> containingNamespaceNames)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }

    [ExportWorkspaceService(typeof(ISymbolSearchService)), Shared]
    internal class DefaultSymbolSearchService : ISymbolSearchService
    {
        [ImportingConstructor]
        public DefaultSymbolSearchService()
        {
        }

        public Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyList<PackageWithTypeResult>();
        }

        public Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
            string source, string assemblyName, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyList<PackageWithAssemblyResult>();
        }

        public Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyList<ReferenceAssemblyWithTypeResult>();
        }
    }
}
