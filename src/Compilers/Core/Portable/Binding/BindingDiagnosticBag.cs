// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal class BindingDiagnosticBag
    {
        public readonly DiagnosticBag? DiagnosticBag;

        public BindingDiagnosticBag(DiagnosticBag? diagnosticBag)
        {
            DiagnosticBag = diagnosticBag;
        }

        internal void AddRange<T>(ImmutableArray<T> diagnostics) where T : Diagnostic
        {
            DiagnosticBag?.AddRange(diagnostics);
        }

        internal void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            DiagnosticBag?.AddRange(diagnostics);
        }

        internal bool HasAnyResolvedErrors()
        {
            Debug.Assert(DiagnosticBag is object);
            return DiagnosticBag?.HasAnyResolvedErrors() == true;
        }

        internal bool HasAnyErrors()
        {
            Debug.Assert(DiagnosticBag is object);
            return DiagnosticBag?.HasAnyErrors() == true;
        }

        internal void Add(Diagnostic diag)
        {
            DiagnosticBag?.Add(diag);
        }

        internal static bool HasErrors(HashSet<DiagnosticInfo>? diagnostics)
        {
            if (!diagnostics.IsNullOrEmpty())
            {
                foreach (var info in diagnostics)
                {
                    if (info.Severity == DiagnosticSeverity.Error)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // PROTOTYPE(UsedAssemblyReferences): Remove conversions once VB side is implemented.
        public static implicit operator DiagnosticBag?(BindingDiagnosticBag from)
        {
            return from.DiagnosticBag;
        }

        public static implicit operator BindingDiagnosticBag(DiagnosticBag? from)
        {
            return new BindingDiagnosticBag(from);
        }
    }

    internal abstract class BindingDiagnosticBag<TAssemblySymbol> : BindingDiagnosticBag
        where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        public readonly ICollection<TAssemblySymbol>? DependenciesBag;

        public BindingDiagnosticBag(DiagnosticBag? diagnosticBag, ICollection<TAssemblySymbol>? dependenciesBag)
            : base(diagnosticBag)
        {
            DependenciesBag = dependenciesBag;
        }

        public BindingDiagnosticBag(bool usePool)
            : this(usePool ? DiagnosticBag.GetInstance() : new DiagnosticBag(), usePool ? PooledHashSet<TAssemblySymbol>.GetInstance() : new HashSet<TAssemblySymbol>())
        { }

        internal void Free()
        {
            DiagnosticBag?.Free();
            ((PooledHashSet<TAssemblySymbol>?)DependenciesBag)?.Free();
        }

        internal ImmutableBindingDiagnostic<TAssemblySymbol> ToReadOnly()
        {
            return new ImmutableBindingDiagnostic<TAssemblySymbol>(DiagnosticBag?.ToReadOnly() ?? default, DependenciesBag?.ToImmutableArray() ?? default);
        }

        internal ImmutableBindingDiagnostic<TAssemblySymbol> ToReadOnlyAndFree()
        {
            var result = ToReadOnly();
            Free();
            return result;
        }

        internal void AddRangeAndFree(BindingDiagnosticBag<TAssemblySymbol> other)
        {
            AddRange(other);
            other.Free();
        }

        // PROTOTYPE(UsedAssemblyReferences): Remove conversions once VB side is implemented.

        [Obsolete("Implicit conversion from BindingDiagnosticBag<TAssemblySymbol> to DiagnosticBag is not supported.", error: true)]
        public static implicit operator DiagnosticBag(BindingDiagnosticBag<TAssemblySymbol>? from)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Implicit conversion from DiagnosticBag to BindingDiagnosticBag<TAssemblySymbol> is not supported.", error: true)]
        public static implicit operator BindingDiagnosticBag<TAssemblySymbol>(DiagnosticBag? from)
        {
            throw new NotSupportedException();
        }

        internal void Clear()
        {
            DiagnosticBag?.Clear();
            DependenciesBag?.Clear();
        }

        internal void AddRange(ImmutableBindingDiagnostic<TAssemblySymbol> other)
        {
            AddRange(other.Diagnostics);
            AddDependencies(other.Dependencies);
        }

        internal void AddRange(BindingDiagnosticBag<TAssemblySymbol> other)
        {
            AddRange(other.DiagnosticBag);
            AddDependencies(other.DependenciesBag);
        }

        internal void AddRange(DiagnosticBag? bag)
        {
            if (bag is object)
            {
                DiagnosticBag?.AddRange(bag);
            }
        }

        internal void AddDependency(TAssemblySymbol? dependency)
        {
            if (dependency is object && DependenciesBag is object)
            {
                DependenciesBag.Add(dependency);
            }
        }

        internal void AddDependencies(ICollection<TAssemblySymbol>? dependencies)
        {
            if (dependencies?.Count > 0 && DependenciesBag is object)
            {
                foreach (var candidate in dependencies)
                {
                    DependenciesBag.Add(candidate);
                }
            }
        }

        internal void AddDependencies(BindingDiagnosticBag<TAssemblySymbol> dependencies)
        {
            AddDependencies(dependencies.DependenciesBag);
        }

        internal void AddDependencies(UseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            if (DependenciesBag is object)
            {
                AddDependency(useSiteInfo.PrimaryDependency);
                AddDependencies(useSiteInfo.SecondaryDependencies);
            }
        }

        internal bool Add(SyntaxNode node, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            return Add(node.Location, useSiteInfo);
        }

        internal bool Add(Location location, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            if (!useSiteInfo.Diagnostics.IsNullOrEmpty())
            {
                if (DiagnosticBag is DiagnosticBag diagnosticBag)
                {
                    bool haveError = false;
                    foreach (var diagnosticInfo in useSiteInfo.Diagnostics)
                    {
                        if (ReportUseSiteDiagnostic(diagnosticInfo, diagnosticBag, location))
                        {
                            haveError = true;
                        }
                    }

                    if (haveError)
                    {
                        return true;
                    }
                }
                else if (CodeAnalysis.BindingDiagnosticBag.HasErrors(useSiteInfo.Diagnostics))
                {
                    return true;
                }
            }

            AddDependencies(useSiteInfo.Dependencies);
            return false;
        }

        protected abstract bool ReportUseSiteDiagnostic(DiagnosticInfo diagnosticInfo, DiagnosticBag diagnosticBag, Location location);

        internal bool Add(UseSiteInfo<TAssemblySymbol> useSiteInfo, SyntaxNode node)
        {
            return Add(useSiteInfo, node.Location);
        }

        internal bool Add(UseSiteInfo<TAssemblySymbol> info, Location location)
        {
            if (ReportUseSiteDiagnostic(info.DiagnosticInfo, location))
            {
                return true;
            }

            AddDependencies(info);
            return false;
        }

        internal bool ReportUseSiteDiagnostic(DiagnosticInfo? info, Location location)
        {
            if (info is null)
            {
                return false;
            }

            if (DiagnosticBag is object)
            {
                return ReportUseSiteDiagnostic(info, DiagnosticBag, location);
            }

            return info.Severity == DiagnosticSeverity.Error;
        }
    }

    internal readonly struct ImmutableBindingDiagnostic<TAssemblySymbol> where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        public readonly ImmutableArray<Diagnostic> Diagnostics;
        public readonly ImmutableArray<TAssemblySymbol> Dependencies;

        public static ImmutableBindingDiagnostic<TAssemblySymbol> Empty => new ImmutableBindingDiagnostic<TAssemblySymbol>(default, default);

        public ImmutableBindingDiagnostic(ImmutableArray<Diagnostic> diagnostics, ImmutableArray<TAssemblySymbol> dependencies)
        {
            Diagnostics = diagnostics.NullToEmpty<Diagnostic>();
            Dependencies = dependencies.NullToEmpty();
        }

        public ImmutableBindingDiagnostic<TAssemblySymbol> NullToEmpty() => new ImmutableBindingDiagnostic<TAssemblySymbol>(Diagnostics, Dependencies);

        public static bool operator ==(ImmutableBindingDiagnostic<TAssemblySymbol> first, ImmutableBindingDiagnostic<TAssemblySymbol> second)
        {
            return first.Diagnostics == second.Diagnostics && first.Dependencies == second.Dependencies;
        }

        public static bool operator !=(ImmutableBindingDiagnostic<TAssemblySymbol> first, ImmutableBindingDiagnostic<TAssemblySymbol> second)
        {
            return !(first == second);
        }

        public override bool Equals(object obj)
        {
            return (obj as ImmutableBindingDiagnostic<TAssemblySymbol>?)?.Equals(this) ?? false;
        }

        public bool Equals(ImmutableBindingDiagnostic<TAssemblySymbol> other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Diagnostics.GetHashCode();
        }
    }
}
