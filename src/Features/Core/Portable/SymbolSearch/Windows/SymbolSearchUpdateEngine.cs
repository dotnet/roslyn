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
using Microsoft.CodeAnalysis.Shared.Collections;
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

    public ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(
        string source, string typeName, int arity, ImmutableArray<string> namespaceNames, CancellationToken cancellationToken)
    {
        if (!_sourceToDatabase.TryGetValue(source, out var databaseWrapper))
        {
            // Don't have a database to search.  
            return ValueTaskFactory.FromResult(ImmutableArray<PackageResult>.Empty);
        }

        var database = databaseWrapper.Database;
        var searchName = namespaceNames.Length > 0 ? namespaceNames.Last() : typeName;
        if (searchName == "var")
        {
            // never find anything named 'var'.
            return ValueTaskFactory.FromResult(ImmutableArray<PackageResult>.Empty);
        }

        var query = new MemberQuery(searchName, isFullSuffix: true, isFullNamespace: false);
        var symbols = new PartialArray<Symbol>(100);

        using var _ = ArrayBuilder<PackageResult>.GetInstance(out var result);
        if (query.TryFindMembers(database, ref symbols))
        {
            foreach (var symbol in FilterToViableSymbols(symbols, namespaceNames))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ignore any reference assembly results.
                if (symbol.PackageName.ToString() != MicrosoftAssemblyReferencesName)
                {
                    var version = database.GetPackageVersion(symbol.Index).ToString();

                    result.Add(new PackageResult(
                        packageName: symbol.PackageName.ToString(),
                        rank: GetRank(symbol),
                        typeName: namespaceNames.Length > 0 ? "" : symbol.Name.ToString(),
                        version: string.IsNullOrWhiteSpace(version) ? null : version,
                        containingNamespaceNames: GetFullName(
                            namespaceNames.Length > 0 ? symbol.FullName : symbol.FullName.Parent)));
                }
            }
        }

        return ValueTaskFactory.FromResult(result.ToImmutableAndClear());
    }

    public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
        string source, string assemblyName, CancellationToken cancellationToken)
    {
        if (!_sourceToDatabase.TryGetValue(source, out var databaseWrapper))
        {
            // Don't have a database to search.  
            return ValueTaskFactory.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);
        }

        using var _ = ArrayBuilder<PackageWithAssemblyResult>.GetInstance(out var result);

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

        return ValueTaskFactory.FromResult(result.ToImmutableAndClear());
    }

    public ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(
        string typeName, int arity, ImmutableArray<string> namespaceNames, CancellationToken cancellationToken)
    {
        // Our reference assembly data is stored in the nuget.org DB.
        if (!_sourceToDatabase.TryGetValue(PackageSourceHelper.NugetOrgSourceName, out var databaseWrapper))
        {
            // Don't have a database to search.  
            return ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyResult>.Empty);
        }

        var database = databaseWrapper.Database;
        var searchName = namespaceNames.Length > 0 ? namespaceNames.Last() : typeName;
        if (searchName == "var")
        {
            // never find anything named 'var'.
            return ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyResult>.Empty);
        }

        var query = new MemberQuery(searchName, isFullSuffix: true, isFullNamespace: false);
        var symbols = new PartialArray<Symbol>(100);

        var results = ArrayBuilder<ReferenceAssemblyResult>.GetInstance();
        if (query.TryFindMembers(database, ref symbols))
        {
            foreach (var symbol in FilterToViableSymbols(symbols, namespaceNames))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only look at reference assembly results.
                if (symbol.PackageName.ToString() == MicrosoftAssemblyReferencesName)
                {
                    results.Add(new ReferenceAssemblyResult(
                        symbol.AssemblyName.ToString(),
                        typeName: namespaceNames.Length > 0 ? "" : symbol.Name.ToString(),
                        containingNamespaceNames: GetFullName(
                            namespaceNames.Length > 0 ? symbol.FullName : symbol.FullName.Parent)));
                }
            }
        }

        return ValueTaskFactory.FromResult(results.ToImmutableAndFree());
    }

    private static IEnumerable<Symbol> FilterToViableSymbols(
        PartialArray<Symbol> symbols, ImmutableArray<string> namespaceNames)
    {
        foreach (var symbol in symbols)
        {
            if (namespaceNames.Length > 0)
            {
                // Searching for a namespace.  Only return a result if the full namespace name matches.
                if (symbol.Type == SymbolType.Namespace &&
                    GetFullName(symbol.FullName).SequenceEqual(namespaceNames))
                {
                    yield return symbol;
                }
            }
            else
            {
                // Don't return nested types.  Currently their value does not seem worth it given all the extra stuff
                // we'd have to plumb through.  Namely going down the "using static" code path and whatnot.
                if (IsType(symbol) && !IsType(symbol.Parent()))
                    yield return symbol;
            }
        }
    }

    private static bool NameMatches(Path8 fullName, ImmutableArray<string> namespaceNames)
    {
        throw new NotImplementedException();
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

    private static ImmutableArray<string> GetFullName(Path8 path)
    {
        using var result = TemporaryArray<string>.Empty;
        for (var current = path; !current.IsEmpty; current = current.Parent)
            result.Add(current.Name.ToString());

        result.ReverseContents();
        return result.ToImmutableAndClear();
    }
}
