// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

global using BindingDiagnosticBag = Microsoft.CodeAnalysis.BindingDiagnosticBag<Microsoft.CodeAnalysis.CSharp.Symbols.AssemblySymbol>;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class BindingDiagnosticBagFactory
    {
        private static readonly Func<DiagnosticInfo, DiagnosticBag, Location, bool> s_reportUseSiteDiagnostic = Symbol.ReportUseSiteDiagnostic;

        public static BindingDiagnosticBag New()
            => New(usePool: false);

        private static BindingDiagnosticBag New(bool usePool)
            => new BindingDiagnosticBag(usePool, s_reportUseSiteDiagnostic);

        public static BindingDiagnosticBag New(DiagnosticBag? diagnosticBag)
            => new BindingDiagnosticBag(diagnosticBag, dependenciesBag: null, s_reportUseSiteDiagnostic);

        public static BindingDiagnosticBag New(DiagnosticBag? diagnosticBag, ICollection<AssemblySymbol>? dependenciesBag)
            => new BindingDiagnosticBag(diagnosticBag, dependenciesBag, s_reportUseSiteDiagnostic);

        internal static BindingDiagnosticBag GetInstance()
        {
            return BindingDiagnosticBagFactory.New(usePool: true);
        }

        internal static BindingDiagnosticBag GetInstance(bool withDiagnostics, bool withDependencies)
        {
            if (withDiagnostics)
            {
                if (withDependencies)
                {
                    return GetInstance();
                }

                return BindingDiagnosticBagFactory.New(DiagnosticBag.GetInstance());
            }
            else if (withDependencies)
            {
                return BindingDiagnosticBagFactory.New(diagnosticBag: null, PooledHashSet<AssemblySymbol>.GetInstance());
            }
            else
            {
                return BindingDiagnosticBag.Discarded;
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
                    return BindingDiagnosticBagFactory.New();
                }

                return BindingDiagnosticBagFactory.New(new DiagnosticBag());
            }
            else if (template.AccumulatesDependencies)
            {
                return BindingDiagnosticBagFactory.New(diagnosticBag: null, new HashSet<AssemblySymbol>());
            }
            else
            {
                return BindingDiagnosticBag.Discarded;
            }
        }
    }

    internal static class BindingDiagnosticBagExtensions
    {
        internal static void AddDependencies(this BindingDiagnosticBag diagnosticBag, Symbol? symbol)
        {
            if (symbol is object && diagnosticBag.DependenciesBag is object)
            {
                diagnosticBag.AddDependencies(symbol.GetUseSiteInfo());
            }
        }

        internal static bool ReportUseSite(this BindingDiagnosticBag diagnosticBag, Symbol? symbol, SyntaxNode node)
        {
            return ReportUseSite(diagnosticBag, symbol, static node => node.Location, node);
        }

        internal static bool ReportUseSite(this BindingDiagnosticBag diagnosticBag, Symbol? symbol, SyntaxToken token)
        {
            return ReportUseSite(diagnosticBag, symbol, static token => token.GetLocation(), token);
        }

        internal static bool ReportUseSite(this BindingDiagnosticBag diagnosticBag, Symbol? symbol, Location location)
            => ReportUseSite(diagnosticBag, symbol, static location => location, location);

        internal static bool ReportUseSite<TData>(this BindingDiagnosticBag diagnosticBag, Symbol? symbol, Func<TData, Location> getLocation, TData data)
        {
            if (symbol is object)
            {
                return diagnosticBag.Add(symbol.GetUseSiteInfo(), getLocation, data);
            }

            return false;
        }

        internal static void AddAssembliesUsedByNamespaceReference(this BindingDiagnosticBag diagnosticBag, NamespaceSymbol ns)
        {
            if (diagnosticBag.DependenciesBag is null)
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
                        diagnosticBag.DependenciesBag!.Add(containingAssembly);
                    }
                }
            }
        }

        internal static CSDiagnosticInfo Add(this BindingDiagnosticBag diagnosticBag, ErrorCode code, Location location)
        {
            var info = new CSDiagnosticInfo(code);
            Add(diagnosticBag, info, location);
            return info;
        }

        internal static CSDiagnosticInfo Add(this BindingDiagnosticBag diagnosticBag, ErrorCode code, SyntaxNode syntax, params object[] args)
            => Add(diagnosticBag, code, syntax.Location, args);

        internal static CSDiagnosticInfo Add(this BindingDiagnosticBag diagnosticBag, ErrorCode code, SyntaxToken syntax, params object[] args)
            => Add(diagnosticBag, code, syntax.GetLocation()!, args);

        internal static CSDiagnosticInfo Add(this BindingDiagnosticBag diagnosticBag, ErrorCode code, Location location, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args);
            Add(diagnosticBag, info, location);
            return info;
        }

        internal static CSDiagnosticInfo Add(this BindingDiagnosticBag diagnosticBag, ErrorCode code, Location location, ImmutableArray<Symbol> symbols, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args, symbols, ImmutableArray<Location>.Empty);
            Add(diagnosticBag, info, location);
            return info;
        }

        internal static void Add(this BindingDiagnosticBag diagnosticBag, DiagnosticInfo? info, Location location)
        {
            if (info is object)
            {
                diagnosticBag.DiagnosticBag?.Add(info, location);
            }
        }
    }
}
