// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    // PROTOTYPE(UsedAssemblyReferences): Consider if it makes sense to move this type into its own file
    internal static class BindingDiagnosticBagExtensions
    {
        internal static CSDiagnosticInfo Add(this CodeAnalysis.BindingDiagnosticBag diagnostics, ErrorCode code, Location location)
        {
            var info = new CSDiagnosticInfo(code);
            diagnostics.DiagnosticBag?.Add(new CSDiagnostic(info, location));
            return info;
        }

        internal static CSDiagnosticInfo Add(this CodeAnalysis.BindingDiagnosticBag diagnostics, ErrorCode code, Location location, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args);
            diagnostics.DiagnosticBag?.Add(new CSDiagnostic(info, location));
            return info;
        }

        internal static CSDiagnosticInfo Add(this CodeAnalysis.BindingDiagnosticBag diagnostics, ErrorCode code, Location location, ImmutableArray<Symbol> symbols, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args, symbols, ImmutableArray<Location>.Empty);
            diagnostics.DiagnosticBag?.Add(new CSDiagnostic(info, location));
            return info;
        }

        internal static void Add(this CodeAnalysis.BindingDiagnosticBag diagnostics, DiagnosticInfo? info, Location location)
        {
            if (info is object)
            {
                diagnostics.DiagnosticBag?.Add(info, location);
            }
        }

        internal static void AddForSymbol(this ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, Symbol? symbol, bool addDiagnostics = true)
        {
            if (symbol is null || useSiteInfo.IsDiscarded)
            {
                return;
            }

            var info = symbol.GetUseSiteInfo();

            if (addDiagnostics)
            {
                useSiteInfo.AddDiagnostics(info);
            }

            useSiteInfo.AddDependencies(info);
        }
    }

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

        internal void AddDependencies(Symbol? symbol)
        {
            if (symbol is object && DependenciesBag is object)
            {
                AddDependencies(symbol.GetUseSiteInfo());
            }
        }

        internal bool ReportUseSite(Symbol? symbol, SyntaxNode node)
        {
            return ReportUseSite(symbol, node.Location);
        }

        internal bool ReportUseSite(Symbol? symbol, SyntaxToken token)
        {
            return ReportUseSite(symbol, token.GetLocation());
        }

        internal bool ReportUseSite(Symbol? symbol, Location location)
        {
            if (symbol is object)
            {
                return Add(symbol.GetUseSiteInfo(), location);
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
    }
}
