// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class BindingDiagnosticBag : BindingDiagnosticBag<AssemblySymbol>
    {
        public static readonly BindingDiagnosticBag Discarded = new BindingDiagnosticBag(null, null);

        public BindingDiagnosticBag()
            : this(usePool: false)
        { }

        private BindingDiagnosticBag(bool usePool)
            : base(usePool)
        { }

        public BindingDiagnosticBag(DiagnosticBag? diagnosticBag)
            : base(diagnosticBag, dependenciesBag: null)
        {
        }

        public BindingDiagnosticBag(DiagnosticBag? diagnosticBag, ICollection<AssemblySymbol>? dependenciesBag)
            : base(diagnosticBag, dependenciesBag)
        {
        }

        internal static BindingDiagnosticBag GetInstance()
        {
            return new BindingDiagnosticBag(usePool: true);
        }

        internal static BindingDiagnosticBag GetInstance(bool withDiagnostics, bool withDependencies)
        {
            if (withDiagnostics)
            {
                if (withDependencies)
                {
                    return GetInstance();
                }

                return new BindingDiagnosticBag(DiagnosticBag.GetInstance());
            }
            else if (withDependencies)
            {
                return new BindingDiagnosticBag(diagnosticBag: null, PooledHashSet<AssemblySymbol>.GetInstance());
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

        internal static BindingDiagnosticBag Create(BindingDiagnosticBag template)
        {
            if (template.AccumulatesDiagnostics)
            {
                if (template.AccumulatesDependencies)
                {
                    return new BindingDiagnosticBag();
                }

                return new BindingDiagnosticBag(new DiagnosticBag());
            }
            else if (template.AccumulatesDependencies)
            {
                return new BindingDiagnosticBag(diagnosticBag: null, new HashSet<AssemblySymbol>());
            }
            else
            {
                return Discarded;
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
                        DependenciesBag!.Add(containingAssembly);
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
            => Add(code, syntax.GetLocation()!, args);

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
