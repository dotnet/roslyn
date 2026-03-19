// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class BindingDiagnosticBag : BindingDiagnosticBag<AssemblySymbol>
    {
        private static readonly ObjectPool<BindingDiagnosticBag> s_poolWithBoth = new ObjectPool<BindingDiagnosticBag>(() => new BindingDiagnosticBag(s_poolWithBoth!, new DiagnosticBag(), new HashSet<AssemblySymbol>()));
        private static readonly ObjectPool<BindingDiagnosticBag> s_poolWithDiagnosticsOnly = new ObjectPool<BindingDiagnosticBag>(() => new BindingDiagnosticBag(s_poolWithDiagnosticsOnly!, new DiagnosticBag(), dependenciesBag: null));
        private static readonly ObjectPool<BindingDiagnosticBag> s_poolWithDependenciesOnly = new ObjectPool<BindingDiagnosticBag>(() => new BindingDiagnosticBag(s_poolWithDependenciesOnly!, diagnosticBag: null, new HashSet<AssemblySymbol>()));
        private static readonly ObjectPool<BindingDiagnosticBag> s_poolWithConcurrent = new ObjectPool<BindingDiagnosticBag>(() => new BindingDiagnosticBag(s_poolWithConcurrent!, new DiagnosticBag(), new Roslyn.Utilities.ConcurrentSet<AssemblySymbol>()));

        public static readonly BindingDiagnosticBag Discarded = new BindingDiagnosticBag(null, null);

        private readonly ObjectPool<BindingDiagnosticBag>? _pool;

        private BindingDiagnosticBag(DiagnosticBag? diagnosticBag, ICollection<AssemblySymbol>? dependenciesBag)
            : base(diagnosticBag, dependenciesBag)
        {
        }

        private BindingDiagnosticBag(ObjectPool<BindingDiagnosticBag> pool, DiagnosticBag? diagnosticBag, ICollection<AssemblySymbol>? dependenciesBag)
            : base(diagnosticBag, dependenciesBag)
        {
            _pool = pool;
        }

        internal static BindingDiagnosticBag GetInstance()
        {
            return s_poolWithBoth.Allocate();
        }

        internal static BindingDiagnosticBag GetInstance(bool withDiagnostics, bool withDependencies)
        {
            if (withDiagnostics)
            {
                if (withDependencies)
                {
                    return GetInstance();
                }

                return s_poolWithDiagnosticsOnly.Allocate();
            }
            else if (withDependencies)
            {
                return s_poolWithDependenciesOnly.Allocate();
            }
            else
            {
                return Discarded;
            }
        }

        internal static BindingDiagnosticBag GetInstance(BindingDiagnosticBag template)
        {
            return GetInstance(template.AccumulatesDiagnostics, template.AccumulatesDependencies);
        }

        /// <summary>
        /// Get an instance suitable for concurrent additions to both underlying bags.
        /// </summary>
        internal static BindingDiagnosticBag GetConcurrentInstance()
        {
            return s_poolWithConcurrent.Allocate();
        }

        internal override void Free()
        {
            if (_pool is { } pool)
            {
                Clear();
                pool.Free(this);
            }
            else
            {
                base.Free();
            }
        }

        internal void AddDependencies(Symbol? symbol)
        {
            if (symbol is object && DependenciesBag is object)
            {
                AddDependencies(symbol.GetUseSiteInfo());
            }
        }

        internal bool ReportUseSite(Symbol? symbol, SyntaxNode node)
        {
            return ReportUseSite(symbol, static node => node.Location, node);
        }

        internal bool ReportUseSite(Symbol? symbol, SyntaxToken token)
        {
            return ReportUseSite(symbol, static token => token.GetLocation(), token);
        }

        internal bool ReportUseSite(Symbol? symbol, Location location)
            => ReportUseSite(symbol, static location => location, location);

        internal bool ReportUseSite<TData>(Symbol? symbol, Func<TData, Location> getLocation, TData data)
        {
            if (symbol is object)
            {
                return Add(symbol.GetUseSiteInfo(), getLocation, data);
            }

            return false;
        }

        internal void AddAssembliesUsedByNamespaceReference(NamespaceSymbol ns)
        {
            if (DependenciesBag is null)
            {
                return;
            }

            addAssembliesUsedByNamespaceReferenceImpl(ns);

            void addAssembliesUsedByNamespaceReferenceImpl(NamespaceSymbol ns)
            {
                // Treat all assemblies contributing to this namespace symbol as used
                if (ns.Extent.Kind == NamespaceKind.Compilation)
                {
                    foreach (var constituent in ns.ConstituentNamespaces)
                    {
                        addAssembliesUsedByNamespaceReferenceImpl(constituent);
                    }
                }
                else
                {
                    AssemblySymbol? containingAssembly = ns.ContainingAssembly;

                    if (containingAssembly?.IsMissing == false)
                    {
                        DependenciesBag.Add(containingAssembly);
                    }
                }
            }
        }

        protected override bool ReportUseSiteDiagnostic(DiagnosticInfo diagnosticInfo, DiagnosticBag diagnosticBag, Location location)
        {
            return Symbol.ReportUseSiteDiagnostic(diagnosticInfo, diagnosticBag, location);
        }

        internal CSDiagnosticInfo Add(ErrorCode code, Location location)
        {
            var info = new CSDiagnosticInfo(code);
            Add(info, location);
            return info;
        }

        internal CSDiagnosticInfo Add(ErrorCode code, SyntaxNode syntax, params object[] args)
            => Add(code, syntax.Location, args);

        internal CSDiagnosticInfo Add(ErrorCode code, SyntaxToken syntax, params object[] args)
            => Add(code, syntax.GetLocation(), args);

        internal CSDiagnosticInfo Add(ErrorCode code, Location location, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args);
            Add(info, location);
            return info;
        }

        internal CSDiagnosticInfo Add(ErrorCode code, Location location, ImmutableArray<Symbol> symbols, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args, symbols, ImmutableArray<Location>.Empty);
            Add(info, location);
            return info;
        }

        internal void Add(DiagnosticInfo? info, Location location)
        {
            if (info is object)
            {
                DiagnosticBag?.Add(info, location);
            }
        }
    }
}
