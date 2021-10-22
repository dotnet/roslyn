﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This is base class for a bag used to accumulate information while binding is performed.
    /// Including diagnostic messages and dependencies in the form of "used" assemblies. 
    /// </summary>
    internal abstract class BindingDiagnosticBag
    {
        public readonly DiagnosticBag? DiagnosticBag;

        protected BindingDiagnosticBag(DiagnosticBag? diagnosticBag)
        {
            DiagnosticBag = diagnosticBag;
        }

        internal bool AccumulatesDiagnostics => DiagnosticBag is object;

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
    }

    internal abstract class BindingDiagnosticBag<TAssemblySymbol> : BindingDiagnosticBag
        where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        public readonly ICollection<TAssemblySymbol>? DependenciesBag;

        protected BindingDiagnosticBag(DiagnosticBag? diagnosticBag, ICollection<TAssemblySymbol>? dependenciesBag)
            : base(diagnosticBag)
        {
            Debug.Assert(diagnosticBag?.GetType().IsValueType != true);
            DependenciesBag = dependenciesBag;
        }

        protected BindingDiagnosticBag(bool usePool)
            : this(usePool ? DiagnosticBag.GetInstance() : new DiagnosticBag(), usePool ? PooledHashSet<TAssemblySymbol>.GetInstance() : new HashSet<TAssemblySymbol>())
        { }

        internal bool AccumulatesDependencies => DependenciesBag is object;

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

        internal void Clear()
        {
            DiagnosticBag?.Clear();
            DependenciesBag?.Clear();
        }

        internal void AddRange(ImmutableBindingDiagnostic<TAssemblySymbol> other, bool allowMismatchInDependencyAccumulation = false)
        {
            AddRange(other.Diagnostics);
            Debug.Assert(allowMismatchInDependencyAccumulation || other.Dependencies.IsDefaultOrEmpty || this.AccumulatesDependencies || !this.AccumulatesDiagnostics);
            AddDependencies(other.Dependencies);
        }

        internal void AddRange(BindingDiagnosticBag<TAssemblySymbol>? other, bool allowMismatchInDependencyAccumulation = false)
        {
            if (other is object)
            {
                AddRange(other.DiagnosticBag);
                Debug.Assert(allowMismatchInDependencyAccumulation || !other.AccumulatesDependencies || this.AccumulatesDependencies);
                AddDependencies(other.DependenciesBag);
            }
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
            if (!dependencies.IsNullOrEmpty() && DependenciesBag is object)
            {
                foreach (var candidate in dependencies)
                {
                    DependenciesBag.Add(candidate);
                }
            }
        }

        internal void AddDependencies(IReadOnlyCollection<TAssemblySymbol>? dependencies)
        {
            if (!dependencies.IsNullOrEmpty() && DependenciesBag is object)
            {
                foreach (var candidate in dependencies)
                {
                    DependenciesBag.Add(candidate);
                }
            }
        }

        internal void AddDependencies(ImmutableHashSet<TAssemblySymbol>? dependencies)
        {
            if (!dependencies.IsNullOrEmpty() && DependenciesBag is object)
            {
                foreach (var candidate in dependencies)
                {
                    DependenciesBag.Add(candidate);
                }
            }
        }

        internal void AddDependencies(ImmutableArray<TAssemblySymbol> dependencies)
        {
            if (!dependencies.IsDefaultOrEmpty && DependenciesBag is object)
            {
                foreach (var candidate in dependencies)
                {
                    DependenciesBag.Add(candidate);
                }
            }
        }

        internal void AddDependencies(BindingDiagnosticBag<TAssemblySymbol> dependencies, bool allowMismatchInDependencyAccumulation = false)
        {
            Debug.Assert(allowMismatchInDependencyAccumulation || !dependencies.AccumulatesDependencies || this.AccumulatesDependencies);
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

        internal void AddDependencies(CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            Debug.Assert(!useSiteInfo.AccumulatesDependencies || this.AccumulatesDependencies);
            if (DependenciesBag is object)
            {
                AddDependencies(useSiteInfo.Dependencies);
            }
        }

        internal bool Add(SyntaxNode node, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            return Add(node.Location, useSiteInfo);
        }

        internal bool AddDiagnostics(SyntaxNode node, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            return AddDiagnostics(node.Location, useSiteInfo);
        }

        internal bool AddDiagnostics(Location location, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            if (DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                if (!useSiteInfo.Diagnostics.IsNullOrEmpty())
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
            }
            else if (useSiteInfo.AccumulatesDiagnostics && !useSiteInfo.Diagnostics.IsNullOrEmpty())
            {
                foreach (var info in useSiteInfo.Diagnostics)
                {
                    if (info.Severity == DiagnosticSeverity.Error)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool Add(Location location, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            Debug.Assert(!useSiteInfo.AccumulatesDependencies || this.AccumulatesDependencies);
            if (AddDiagnostics(location, useSiteInfo))
            {
                return true;
            }

            AddDependencies(useSiteInfo);
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
        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly ImmutableArray<TAssemblySymbol> _dependencies;

        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics.NullToEmpty();
        public ImmutableArray<TAssemblySymbol> Dependencies => _dependencies.NullToEmpty();

        public static ImmutableBindingDiagnostic<TAssemblySymbol> Empty => new ImmutableBindingDiagnostic<TAssemblySymbol>(default, default);

        public ImmutableBindingDiagnostic(ImmutableArray<Diagnostic> diagnostics, ImmutableArray<TAssemblySymbol> dependencies)
        {
            _diagnostics = diagnostics.NullToEmpty();
            _dependencies = dependencies.NullToEmpty();
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

        public override bool Equals(object? obj)
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
