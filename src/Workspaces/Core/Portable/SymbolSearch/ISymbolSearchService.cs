// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch;

/// <param name="Name">The roslyn simple name of the type to search for. For example <see cref="ImmutableArray{T}"/>
/// would have the name <c>ImmutableArray</c></param>
/// <param name="Arity">The arity of the type.  For example <see cref="ImmutableArray{T}"/> would have arity
/// <c>1</c></param>.
[DataContract]
internal readonly record struct TypeQuery(
    [property: DataMember(Order = 0)] string Name,
    [property: DataMember(Order = 1)] int Arity)
{
    public static readonly TypeQuery Default = default;

    public bool IsDefault => string.IsNullOrEmpty(Name);
}

/// <param name="Names">The names comprising the namespace being searched for.  For example <c>["System", "Collections",
/// "Immutable"]</c>.</param>
[DataContract]
internal readonly record struct NamespaceQuery(
    [property: DataMember(Order = 0)] ImmutableArray<string> Names)
{
    public static readonly NamespaceQuery Default = default;

    public static implicit operator NamespaceQuery(ImmutableArray<string> names)
        => new(names);

    public bool IsDefault => Names.IsDefaultOrEmpty;
}

internal interface ISymbolSearchService : IWorkspaceService
{
    /// <summary>
    /// Searches for packages that contain a type with the provided name and arity. Note: Implementations are free to
    /// return the results they feel best for the given data.  Specifically, they can do exact or fuzzy matching on the
    /// name. They can use or ignore the arity depending on their capabilities. 
    /// 
    /// Implementations should return results in order from best to worst (from their perspective).
    /// </summary>
    ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(
        string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Searches for packages that contain an assembly with the provided name. Note: Implementations are free to return
    /// the results they feel best for the given data.  Specifically, they can do exact or fuzzy matching on the name.
    /// 
    /// Implementations should return results in order from best to worst (from their perspective).
    /// </summary>
    ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
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
    ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(
        TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken);
}

[DataContract]
internal abstract class AbstractPackageResult(string packageName, int rank)
{
    [DataMember(Order = 0)]
    public readonly string PackageName = packageName;

    [DataMember(Order = 1)]
    public readonly int Rank = rank;
}

[DataContract]
internal sealed class PackageResult(
    string packageName,
    int rank,
    string typeName,
    string? version,
    ImmutableArray<string> containingNamespaceNames) : AbstractPackageResult(packageName, rank)
{
    [DataMember(Order = 2)]
    public readonly string TypeName = typeName;

    [DataMember(Order = 3)]
    public readonly string? Version = version;

    [DataMember(Order = 4)]
    public readonly ImmutableArray<string> ContainingNamespaceNames = containingNamespaceNames;
}

[DataContract]
internal sealed class PackageWithAssemblyResult(
    string packageName,
    int rank,
    string version) : AbstractPackageResult(packageName, rank), IEquatable<PackageWithAssemblyResult?>, IComparable<PackageWithAssemblyResult?>
{
    [DataMember(Order = 2)]
    public readonly string? Version = version;

    public override int GetHashCode()
        => PackageName.GetHashCode();

    public override bool Equals(object? obj)
        => Equals(obj as PackageWithAssemblyResult);

    public bool Equals(PackageWithAssemblyResult? other)
        => PackageName.Equals(other?.PackageName);

    public int CompareTo(PackageWithAssemblyResult? other)
    {
        if (other is null)
            return 1;

        return ComparerWithState.CompareTo(this, other, s_comparers);
    }

    private static readonly ImmutableArray<Func<PackageWithAssemblyResult, IComparable>> s_comparers =
        [p => p.Rank, p => p.PackageName];
}

[DataContract]
internal sealed class ReferenceAssemblyResult(
    string assemblyName,
    string typeName,
    ImmutableArray<string> containingNamespaceNames)
{
    [DataMember(Order = 0)]
    public readonly string AssemblyName = assemblyName;

    [DataMember(Order = 1)]
    public readonly string TypeName = typeName;

    [DataMember(Order = 2)]
    public readonly ImmutableArray<string> ContainingNamespaceNames = containingNamespaceNames;
}

[ExportWorkspaceService(typeof(ISymbolSearchService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultSymbolSearchService() : ISymbolSearchService
{
    public ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
        => ValueTaskFactory.FromResult(ImmutableArray<PackageResult>.Empty);

    public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        => ValueTaskFactory.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);

    public ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
        => ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyResult>.Empty);
}
