// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch;

/// <summary>
/// A service which enables searching for packages matching certain criteria.
/// It works against a <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
/// 
/// This implementation also spawns a task which will attempt to keep that database up to
/// date by downloading patches on a daily basis.
/// </summary>
internal sealed partial class SymbolSearchUpdateEngine : ISymbolSearchUpdateEngine
{
    private readonly ConcurrentDictionary<string, IAddReferenceDatabaseWrapper> _sourceToDatabase = [];

    /// <summary>
    /// Don't call directly. Use <see cref="SymbolSearchUpdateEngineFactory"/> instead.
    /// </summary>
    public SymbolSearchUpdateEngine(IFileDownloaderFactory fileDownloaderFactory)
        : this(fileDownloaderFactory,
               new DelayService(),
               new IOService(),
               new PatchService(),
               new DatabaseFactoryService(),
               // Report all exceptions we encounter, but don't crash on them. Propagate expected cancellation.
               static (e, ct) => FatalError.ReportAndCatchUnlessCanceled(e, ct))
    {
    }

    /// <summary>
    /// For testing purposes only.
    /// </summary>
    internal SymbolSearchUpdateEngine(
        IFileDownloaderFactory fileDownloaderFactory,
        IDelayService delayService,
        IIOService ioService,
        IPatchService patchService,
        IDatabaseFactoryService databaseFactoryService,
        Func<Exception, CancellationToken, bool> reportAndSwallowExceptionUnlessCanceled)
    {
        _delayService = delayService;
        _ioService = ioService;
        _fileDownloaderFactory = fileDownloaderFactory;
        _patchService = patchService;
        _databaseFactoryService = databaseFactoryService;
        _reportAndSwallowExceptionUnlessCanceled = reportAndSwallowExceptionUnlessCanceled;
    }

    public void Dispose()
    {
        // Nothing to do for the core symbol search engine.
    }

    public ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
        string source, string name, int arity, CancellationToken cancellationToken)
    {
        if (!_sourceToDatabase.TryGetValue(source, out var databaseWrapper))
        {
            // Don't have a database to search.  
            return ValueTaskFactory.FromResult(ImmutableArray<PackageWithTypeResult>.Empty);
        }

        var database = databaseWrapper.Database;
        if (name == "var")
        {
            // never find anything named 'var'.
            return ValueTaskFactory.FromResult(ImmutableArray<PackageWithTypeResult>.Empty);
        }

        var query = new MemberQuery(name, isFullSuffix: true, isFullNamespace: false);
        var symbols = new PartialArray<Symbol>(100);

        var result = ArrayBuilder<PackageWithTypeResult>.GetInstance();
        if (query.TryFindMembers(database, ref symbols))
        {
            var types = FilterToViableTypes(symbols);

            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ignore any reference assembly results.
                if (type.PackageName.ToString() != MicrosoftAssemblyReferencesName)
                {
                    result.Add(CreateResult(database, type));
                }
            }
        }

        return ValueTaskFactory.FromResult(result.ToImmutableAndFree());
    }

    public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
        string source, string assemblyName, CancellationToken cancellationToken)
    {
        if (!_sourceToDatabase.TryGetValue(source, out var databaseWrapper))
        {
            // Don't have a database to search.  
            return ValueTaskFactory.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);
        }

        var result = ArrayBuilder<PackageWithAssemblyResult>.GetInstance();

        var database = databaseWrapper.Database;
        var index = database.Index;
        var stringStore = database.StringStore;
        if (stringStore.TryFindString(assemblyName, out var range) &&
            index.TryGetMatchesInRange(range, out var matches, out var startIndex, out var count))
        {
            for (var i = startIndex; i < (startIndex + count); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var symbol = new Symbol(database, matches[i]);
                if (symbol.Type == SymbolType.Assembly)
                {
                    // Ignore any reference assembly results.
                    if (symbol.PackageName.ToString() != MicrosoftAssemblyReferencesName)
                    {
                        var version = database.GetPackageVersion(symbol.Index).ToString();

                        result.Add(new PackageWithAssemblyResult(
                            symbol.PackageName.ToString(),
                            GetRank(symbol),
                            string.IsNullOrWhiteSpace(version) ? null : version));
                    }
                }
            }
        }

        return ValueTaskFactory.FromResult(result.ToImmutableAndFree());
    }

    public ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
        string name, int arity, CancellationToken cancellationToken)
    {
        // Our reference assembly data is stored in the nuget.org DB.
        if (!_sourceToDatabase.TryGetValue(PackageSourceHelper.NugetOrgSourceName, out var databaseWrapper))
        {
            // Don't have a database to search.  
            return ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty);
        }

        var database = databaseWrapper.Database;
        if (name == "var")
        {
            // never find anything named 'var'.
            return ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty);
        }

        var query = new MemberQuery(name, isFullSuffix: true, isFullNamespace: false);
        var symbols = new PartialArray<Symbol>(100);

        var results = ArrayBuilder<ReferenceAssemblyWithTypeResult>.GetInstance();
        if (query.TryFindMembers(database, ref symbols))
        {
            var types = FilterToViableTypes(symbols);

            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only look at reference assembly results.
                if (type.PackageName.ToString() == MicrosoftAssemblyReferencesName)
                {
                    var nameParts = ArrayBuilder<string>.GetInstance();
                    GetFullName(nameParts, type.FullName.Parent);
                    var result = new ReferenceAssemblyWithTypeResult(
                        type.AssemblyName.ToString(), type.Name.ToString(),
                        containingNamespaceNames: nameParts.ToImmutableAndFree());
                    results.Add(result);
                }
            }
        }

        return ValueTaskFactory.FromResult(results.ToImmutableAndFree());
    }

    private static List<Symbol> FilterToViableTypes(PartialArray<Symbol> symbols)
    {
        // Don't return nested types.  Currently their value does not seem worth
        // it given all the extra stuff we'd have to plumb through.  Namely 
        // going down the "using static" code path and whatnot.
        return new List<Symbol>(
            from symbol in symbols
            where IsType(symbol) && !IsType(symbol.Parent())
            select symbol);
    }

    private static PackageWithTypeResult CreateResult(AddReferenceDatabase database, Symbol type)
    {
        var nameParts = ArrayBuilder<string>.GetInstance();
        GetFullName(nameParts, type.FullName.Parent);

        var packageName = type.PackageName.ToString();

        var version = database.GetPackageVersion(type.Index).ToString();

        return new PackageWithTypeResult(
            packageName: packageName,
            rank: GetRank(type),
            typeName: type.Name.ToString(),
            version: string.IsNullOrWhiteSpace(version) ? null : version,
            containingNamespaceNames: nameParts.ToImmutableAndFree());
    }

    private static int GetRank(Symbol symbol)
    {
        if (!TryGetRankingSymbol(symbol, out var rankingSymbol) ||
            !int.TryParse(rankingSymbol.Name.ToString(), out var rank))
        {
            return 0;
        }

        return rank;
    }

    private static bool TryGetRankingSymbol(Symbol symbol, out Symbol rankingSymbol)
    {
        for (var current = symbol; current.IsValid; current = current.Parent())
        {
            if (current.Type is SymbolType.Package or SymbolType.Version)
            {
                return TryGetRankingSymbolForPackage(current, out rankingSymbol);
            }
        }

        rankingSymbol = default;
        return false;
    }

    private static bool TryGetRankingSymbolForPackage(Symbol package, out Symbol rankingSymbol)
    {
        for (var child = package.FirstChild(); child.IsValid; child = child.NextSibling())
        {
            if (child.Type == SymbolType.PopularityRank)
            {
                rankingSymbol = child;
                return true;
            }
        }

        rankingSymbol = default;
        return false;
    }

    private static bool IsType(Symbol symbol)
        => symbol.Type.IsType();

    private static void GetFullName(ArrayBuilder<string> nameParts, Path8 path)
    {
        if (!path.IsEmpty)
        {
            GetFullName(nameParts, path.Parent);
            nameParts.Add(path.Name.ToString());
        }
    }
}
