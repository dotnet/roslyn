// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        [MemberNotNullWhen(true, nameof(DiagnosticBag))]
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

        internal virtual void Free()
        {
            DiagnosticBag?.Free();
            ((PooledHashSet<TAssemblySymbol>?)DependenciesBag)?.Free();
        }

        internal ReadOnlyBindingDiagnostic<TAssemblySymbol> ToReadOnly(bool forceDiagnosticResolution = true)
        {
            return new ReadOnlyBindingDiagnostic<TAssemblySymbol>(DiagnosticBag?.ToReadOnly(forceDiagnosticResolution) ?? default, DependenciesBag?.ToImmutableArray() ?? default);
        }

        internal ReadOnlyBindingDiagnostic<TAssemblySymbol> ToReadOnlyAndFree(bool forceDiagnosticResolution = true)
        {
            var result = ToReadOnly(forceDiagnosticResolution);
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

        internal void AddRange(ReadOnlyBindingDiagnostic<TAssemblySymbol> other, bool allowMismatchInDependencyAccumulation = false)
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
            => Add(useSiteInfo, static node => node.Location, node);

        internal bool AddDiagnostics(SyntaxNode node, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
            => AddDiagnostics(useSiteInfo, static node => node.Location, node);

        internal bool Add(SyntaxToken token, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
            => Add(useSiteInfo, static token => token.GetLocation(), token);

        internal bool Add(Location location, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
            => Add(useSiteInfo, static location => location, location);

        internal bool AddDiagnostics(Location location, CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo)
            => AddDiagnostics(useSiteInfo, static location => location, location);

        internal bool AddDiagnostics<TData>(CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo, Func<TData, Location> getLocation, TData data)
        {
            if (DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                if (!useSiteInfo.Diagnostics.IsNullOrEmpty())
                {
                    bool haveError = false;
                    var location = getLocation(data);
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

        internal bool Add<TData>(CompoundUseSiteInfo<TAssemblySymbol> useSiteInfo, Func<TData, Location> getLocation, TData data)
        {
            Debug.Assert(!useSiteInfo.AccumulatesDependencies || this.AccumulatesDependencies);
            if (AddDiagnostics(useSiteInfo, getLocation, data))
            {
                return true;
            }

            AddDependencies(useSiteInfo);
            return false;
        }

        protected abstract bool ReportUseSiteDiagnostic(DiagnosticInfo diagnosticInfo, DiagnosticBag diagnosticBag, Location location);

        internal bool Add(UseSiteInfo<TAssemblySymbol> useSiteInfo, SyntaxNode node)
            => Add(useSiteInfo, static node => node.Location, node);

        internal bool Add(UseSiteInfo<TAssemblySymbol> useSiteInfo, Location location)
            => Add(useSiteInfo, static location => location, location);

        internal bool Add(UseSiteInfo<TAssemblySymbol> useSiteInfo, SyntaxToken token)
            => Add(useSiteInfo, static token => token.GetLocation(), token);

        internal bool Add<TData>(UseSiteInfo<TAssemblySymbol> info, Func<TData, Location> getLocation, TData data)
        {
            if (ReportUseSiteDiagnostic(info.DiagnosticInfo, getLocation, data))
            {
                return true;
            }

            AddDependencies(info);
            return false;
        }

        internal bool ReportUseSiteDiagnostic(DiagnosticInfo? info, Location location)
            => ReportUseSiteDiagnostic(info, static location => location, location);

        internal bool ReportUseSiteDiagnostic<TData>(DiagnosticInfo? info, Func<TData, Location> getLocation, TData data)
        {
            if (info is null)
            {
                return false;
            }

            if (DiagnosticBag is object)
            {
                return ReportUseSiteDiagnostic(info, DiagnosticBag, getLocation(data));
            }

            return info.Severity == DiagnosticSeverity.Error;
        }
    }

    internal readonly struct ReadOnlyBindingDiagnostic<TAssemblySymbol> where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly ImmutableArray<TAssemblySymbol> _dependencies;

        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics.NullToEmpty();
        public ImmutableArray<TAssemblySymbol> Dependencies => _dependencies.NullToEmpty();

        public static ReadOnlyBindingDiagnostic<TAssemblySymbol> Empty => new ReadOnlyBindingDiagnostic<TAssemblySymbol>(default, default);

        public ReadOnlyBindingDiagnostic(ImmutableArray<Diagnostic> diagnostics, ImmutableArray<TAssemblySymbol> dependencies)
        {
            _diagnostics = diagnostics.NullToEmpty();
            _dependencies = dependencies.NullToEmpty();
        }

        public ReadOnlyBindingDiagnostic<TAssemblySymbol> NullToEmpty() => new ReadOnlyBindingDiagnostic<TAssemblySymbol>(Diagnostics, Dependencies);

        public static bool operator ==(ReadOnlyBindingDiagnostic<TAssemblySymbol> first, ReadOnlyBindingDiagnostic<TAssemblySymbol> second)
        {
            return first.Diagnostics == second.Diagnostics && first.Dependencies == second.Dependencies;
        }

        public static bool operator !=(ReadOnlyBindingDiagnostic<TAssemblySymbol> first, ReadOnlyBindingDiagnostic<TAssemblySymbol> second)
        {
            return !(first == second);
        }

        public override bool Equals(object? obj)
        {
            return (obj as ReadOnlyBindingDiagnostic<TAssemblySymbol>?)?.Equals(this) ?? false;
        }

        public bool Equals(ReadOnlyBindingDiagnostic<TAssemblySymbol> other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Diagnostics.GetHashCode();
        }

        public bool HasAnyErrors() => Diagnostics.HasAnyErrors();

        public bool HasAnyResolvedErrors()
        {
            foreach (var diagnostic in Diagnostics)
            {
                if ((diagnostic as DiagnosticWithInfo)?.HasLazyInfo != true && diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
