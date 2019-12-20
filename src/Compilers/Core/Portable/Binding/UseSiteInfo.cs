// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct UseSiteInfo<TAssemblySymbol> where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        /// <summary>
        /// Diagnostic info that should be reported at the use site of the symbol, or null if there is none.
        /// </summary>
        public readonly DiagnosticInfo? DiagnosticInfo;

        /// <summary>
        /// When not-null, this is primary dependency of the use-site, usually the assembly defining the used symbol.
        /// Never a core library. Usually it is not included into the <see cref="SecondaryDependencies"/>.
        /// Null if <see cref="DiagnosticInfo"/> is an error.
        /// </summary>
        public readonly TAssemblySymbol? PrimaryDependency;

        /// <summary>
        /// The set of other assemblies the use site will depend upon, excluding assembly for core library.
        /// Empty if <see cref="DiagnosticInfo"/> is an error.
        /// </summary>
        public readonly ImmutableHashSet<TAssemblySymbol>? SecondaryDependencies;

        public UseSiteInfo(TAssemblySymbol? primaryDependency) :
            this(diagnosticInfo: null, primaryDependency, secondaryDependencies: null)
        {
        }

        public UseSiteInfo(ImmutableHashSet<TAssemblySymbol>? secondaryDependencies) :
            this(diagnosticInfo: null, primaryDependency: null, secondaryDependencies)
        {
        }

        public UseSiteInfo(DiagnosticInfo? diagnosticInfo) :
            this(diagnosticInfo, primaryDependency: null, secondaryDependencies: null)
        {
        }

        public UseSiteInfo(DiagnosticInfo? diagnosticInfo, TAssemblySymbol? primaryDependency) :
            this(diagnosticInfo, primaryDependency, secondaryDependencies: null)
        {
        }

        public UseSiteInfo(DiagnosticInfo? diagnosticInfo, TAssemblySymbol? primaryDependency, ImmutableHashSet<TAssemblySymbol>? secondaryDependencies)
        {
            Debug.Assert(diagnosticInfo?.Severity != DiagnosticSeverity.Error || (primaryDependency is null && secondaryDependencies?.IsEmpty != false));
            // PROTOTYPE(UsedAssemblyReferences): Add an assert to verify that a core library is not among the dependencies

            DiagnosticInfo = diagnosticInfo;
            PrimaryDependency = primaryDependency;
            SecondaryDependencies = secondaryDependencies ?? ImmutableHashSet<TAssemblySymbol>.Empty;
        }

        public UseSiteInfo<TAssemblySymbol> AdjustDiagnosticInfo(DiagnosticInfo? diagnosticInfo)
        {
            if ((object?)DiagnosticInfo != diagnosticInfo)
            {
                if (diagnosticInfo?.Severity == DiagnosticSeverity.Error)
                {
                    return new UseSiteInfo<TAssemblySymbol>(diagnosticInfo);
                }

                return new UseSiteInfo<TAssemblySymbol>(diagnosticInfo, PrimaryDependency, SecondaryDependencies);
            }

            return this;
        }
    }

    internal struct CompoundUseSiteInfo<TAssemblySymbol> where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        private bool _haveErrors;
        private readonly bool _discarded;
        public HashSet<DiagnosticInfo>? Diagnostics;
        public HashSet<TAssemblySymbol>? Dependencies;

        public static CompoundUseSiteInfo<TAssemblySymbol> Discarded => new CompoundUseSiteInfo<TAssemblySymbol>(discarded: true);

        private CompoundUseSiteInfo(bool discarded)
        {
            this = default;
            _discarded = discarded;
        }

        public bool IsDiscarded => _discarded;

        public void AddDiagnostics(UseSiteInfo<TAssemblySymbol> info)
        {
            HashSetExtensions.InitializeAndAdd(ref Diagnostics, info.DiagnosticInfo);

            if (info.DiagnosticInfo?.Severity == DiagnosticSeverity.Error)
            {
                _haveErrors = true;
            }
        }

        public void AddDependencies(UseSiteInfo<TAssemblySymbol> info)
        {
            if (!_haveErrors)
            {
                HashSetExtensions.InitializeAndAdd(ref Dependencies, info.PrimaryDependency);

                if (info.SecondaryDependencies?.IsEmpty == false)
                {
                    (Dependencies ??= new HashSet<TAssemblySymbol>()).AddAll(info.SecondaryDependencies);
                }
            }
        }

        public void MergeAndClear(ref CompoundUseSiteInfo<TAssemblySymbol> other)
        {
            if (_discarded)
            {
                other.Diagnostics = null;
                other.Dependencies = null;
                other._haveErrors = false;
                return;
            }

            mergeAndClear(ref Diagnostics, ref other.Diagnostics);

            if (other._haveErrors)
            {
                _haveErrors = true;
                other._haveErrors = false;
            }

            if (!_haveErrors)
            {
                mergeAndClear(ref Dependencies, ref other.Dependencies);
            }
            else
            {
                other.Dependencies = null;
            }

            static void mergeAndClear<T>(ref HashSet<T>? self, ref HashSet<T>? other)
            {
                if (self is null)
                {
                    self = other;
                }
                else if (other is object)
                {
                    self.AddAll(other);
                }

                other = null;
            }
        }

        public void Add(UseSiteInfo<TAssemblySymbol> other)
        {
            if (_discarded)
            {
                return;
            }

            AddDiagnostics(other);
            AddDependencies(other);
        }
    }

    internal struct CachedUseSiteInfo<TAssemblySymbol> where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        /// <summary>
        /// Either 
        /// - null, or
        /// - a <see cref="DiagnosticInfo"/>, or
        /// - dependencies as a <see cref="ImmutableHashSet{TAssemblySymbol}"/>, or
        /// - a <see cref="Boxed"/> tuple of a <see cref="DiagnosticInfo"/> and a <see cref="ImmutableHashSet{TAssemblySymbol}"/>. 
        /// </summary>
        private object? _info;

        private static readonly object Sentinel = new object(); // Indicates unknown state.

        public readonly static CachedUseSiteInfo<TAssemblySymbol> Uninitialized = new CachedUseSiteInfo<TAssemblySymbol>(Sentinel); // Indicates unknown state.

        private CachedUseSiteInfo(object info)
        {
            _info = info;
        }

        public bool IsInitialized => (object?)_info != Sentinel;

        public void Initialize(DiagnosticInfo? diagnosticInfo)
        {
            Debug.Assert(diagnosticInfo is null || diagnosticInfo.Severity == DiagnosticSeverity.Error);
            Initialize(diagnosticInfo, dependencies: ImmutableHashSet<TAssemblySymbol>.Empty);
        }

        public void Initialize(TAssemblySymbol? primaryDependency, UseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            Initialize(useSiteInfo.DiagnosticInfo, GetDependenciesToCache(primaryDependency, useSiteInfo));
        }

        private static ImmutableHashSet<TAssemblySymbol> GetDependenciesToCache(TAssemblySymbol? primaryDependency, UseSiteInfo<TAssemblySymbol> useSiteInfo)
        {
            var secondaryDependencies = useSiteInfo.SecondaryDependencies ?? ImmutableHashSet<TAssemblySymbol>.Empty;
            Debug.Assert(primaryDependency is object || (useSiteInfo.PrimaryDependency is null && secondaryDependencies.IsEmpty));
            Debug.Assert(primaryDependency == useSiteInfo.PrimaryDependency || useSiteInfo.DiagnosticInfo?.Severity == DiagnosticSeverity.Error);
            if (useSiteInfo.PrimaryDependency is object)
            {
                return secondaryDependencies.Remove(useSiteInfo.PrimaryDependency);
            }

            return secondaryDependencies;
        }

        public UseSiteInfo<TAssemblySymbol> ToUseSiteInfo(TAssemblySymbol primaryDependency)
        {
            Expand(_info, out var diagnosticInfo, out var dependencies);

            if (diagnosticInfo?.Severity == DiagnosticSeverity.Error)
            {
                return new UseSiteInfo<TAssemblySymbol>(diagnosticInfo);
            }

            return new UseSiteInfo<TAssemblySymbol>(diagnosticInfo, primaryDependency, dependencies);
        }

        private void Initialize(DiagnosticInfo? diagnosticInfo, ImmutableHashSet<TAssemblySymbol> dependencies)
        {
            _info = Compact(diagnosticInfo, dependencies);
        }

        private static object? Compact(DiagnosticInfo? diagnosticInfo, ImmutableHashSet<TAssemblySymbol> dependencies)
        {
            object? info;

            if (dependencies.IsEmpty)
            {
                info = diagnosticInfo;
            }
            else if (diagnosticInfo is null)
            {
                info = dependencies;
            }
            else
            {
                info = new Boxed(diagnosticInfo, dependencies);
            }

            return info;
        }

        public void InterlockedCompareExchange(TAssemblySymbol? primaryDependency, UseSiteInfo<TAssemblySymbol> value)
        {
            if ((object?)_info == Sentinel)
            {
                object? info = Compact(value.DiagnosticInfo, GetDependenciesToCache(primaryDependency, value));
                Interlocked.CompareExchange(ref _info, info, Sentinel);
            }
        }

        public UseSiteInfo<TAssemblySymbol> InterlockedInitialize(TAssemblySymbol? primaryDependency, UseSiteInfo<TAssemblySymbol> value)
        {
            object? info = Compact(value.DiagnosticInfo, GetDependenciesToCache(primaryDependency, value));
            Debug.Assert(info is object);

            info = Interlocked.CompareExchange(ref _info, info, null);
            if (info == null)
            {
                return value;
            }

            Expand(info, out var diagnosticInfo, out var dependencies);
            return new UseSiteInfo<TAssemblySymbol>(diagnosticInfo, value.PrimaryDependency, dependencies);
        }

        private static void Expand(object? info, out DiagnosticInfo? diagnosticInfo, out ImmutableHashSet<TAssemblySymbol>? dependencies)
        {
            switch (info)
            {
                case null:
                    diagnosticInfo = null;
                    dependencies = null;
                    break;

                case DiagnosticInfo d:
                    diagnosticInfo = d;
                    dependencies = null;
                    break;

                case ImmutableHashSet<TAssemblySymbol> a:
                    diagnosticInfo = null;
                    dependencies = a;
                    break;

                default:
                    var boxed = (Boxed)info;
                    diagnosticInfo = boxed.DiagnosticInfo;
                    dependencies = boxed.Dependencies;
                    break;
            }
        }

        private class Boxed
        {
            /// <summary>
            /// Diagnostic info that should be reported at the use site of the symbol, or null if there is none.
            /// </summary>
            public readonly DiagnosticInfo DiagnosticInfo;

            /// <summary>
            /// The set of assemblies the use site will depend upon, excluding assembly for core library.
            /// Empty or null if <see cref="Boxed.DiagnosticInfo"/> is an error.
            /// </summary>
            public readonly ImmutableHashSet<TAssemblySymbol> Dependencies;

            public Boxed(DiagnosticInfo diagnosticInfo, ImmutableHashSet<TAssemblySymbol> dependencies)
            {
                Debug.Assert(!dependencies.IsEmpty);
                DiagnosticInfo = diagnosticInfo;
                Dependencies = dependencies;
            }
        }
    }
}
