// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal readonly struct UseSiteInfo
    {
        /// <summary>
        /// When not-null, containing assembly of the symbol is not included into the <see cref="Dependencies"/>
        /// </summary>
        public readonly Symbol? Source;

        /// <summary>
        /// Diagnostic info that should be reported at the use site of the symbol, or null if there is none.
        /// </summary>
        public readonly DiagnosticInfo? DiagnosticInfo;

        /// <summary>
        /// The set of assemblies the use site will depend upon, excluding assembly for core library.
        /// Empty or null if <see cref="UseSiteInfo.DiagnosticInfo"/> is an error.
        /// </summary>
        public readonly ImmutableHashSet<AssemblySymbol>? Dependencies;

        public UseSiteInfo(Symbol source) :
            this(source, diagnosticInfo: null, dependencies: null)
        {
        }

        public UseSiteInfo(DiagnosticInfo? diagnosticInfo) :
            this(source: null, diagnosticInfo, dependencies: null)
        {
        }

        public UseSiteInfo(Symbol? source, DiagnosticInfo? diagnosticInfo) :
            this(source, diagnosticInfo, dependencies: null)
        {
        }

        public UseSiteInfo(Symbol? source, DiagnosticInfo? diagnosticInfo, ImmutableHashSet<AssemblySymbol>? dependencies)
        {
            Source = source;
            DiagnosticInfo = diagnosticInfo;
            Dependencies = dependencies;
        }

        public UseSiteInfo(Builder builder) :
            this(source: null, builder)
        {
        }

        public UseSiteInfo(Symbol? source, Builder builder)
        {
            Source = source;
            DiagnosticInfo = builder.DiagnosticInfo;

            if (DiagnosticInfo?.Severity != DiagnosticSeverity.Error)
            {
                ImmutableHashSet<AssemblySymbol>? dependencies = builder.Dependencies;

                if (dependencies?.IsEmpty == false && source?.ContainingAssembly is object)
                {
                    dependencies = dependencies.Remove(source.ContainingAssembly);
                }

                if (dependencies?.IsEmpty == true)
                {
                    dependencies = null;
                }

                Dependencies = dependencies;
            }
            else
            {
                Dependencies = null;
            }
        }

        //public UseSiteInfo(DiagnosticInfo? diagnosticInfo)
        //{
        //    DiagnosticInfo = diagnosticInfo;
        //    Dependencies = null;
        //}

        //public UseSiteInfo(ImmutableHashSet<AssemblySymbol>? dependencies)
        //{
        //    DiagnosticInfo = null;
        //    Dependencies = dependencies;
        //}

        public struct Builder
        {
            public DiagnosticInfo? DiagnosticInfo;
            public ImmutableHashSet<AssemblySymbol>? Dependencies;

            public static implicit operator Builder(UseSiteInfo info)
            {
                var result = new Builder() { DiagnosticInfo = info.DiagnosticInfo, Dependencies = info.Dependencies };

                if (result.DiagnosticInfo?.Severity != DiagnosticSeverity.Error)
                {
                    AssemblySymbol? containingAssembly = info.Source?.ContainingAssembly;

                    if (containingAssembly is object && containingAssembly.CorLibrary != containingAssembly)
                    {
                        result.Dependencies = (result.Dependencies ?? ImmutableHashSet<AssemblySymbol>.Empty).Add(containingAssembly);
                    }
                    else
                    {
                        //Debug.Assert(info.Dependencies?.IsEmpty != false);
                    }
                }

                return result;
            }
        }
    }

    internal struct CompoundUseSiteInfo
    {
        private bool _haveErrors;
        private readonly bool _discarded;
        public HashSet<DiagnosticInfo>? Diagnostics;
        public HashSet<AssemblySymbol>? Dependencies;

        public static CompoundUseSiteInfo Discarded => new CompoundUseSiteInfo(discarded: true);

        private CompoundUseSiteInfo(bool discarded)
        {
            this = default;
            _discarded = discarded;
        }

        public bool IsDiscarded => _discarded;

        public void AddForSymbol(Symbol? symbol, bool addDiagnostics = true, bool addDependencies = true)
        {
            if (symbol is null || _discarded || !(addDiagnostics || addDependencies))
            {
                return;
            }

            var info = symbol.GetUseSiteInfo();

            if (addDiagnostics)
            {
                AddDiagnostics(info);
            }
            else
            {
                Debug.Assert(info.DiagnosticInfo == null || addDependencies); // It would be strange to drop diagnostics, but record dependencies
            }

            if (addDependencies)
            {
                AddDependencies(symbol, info);
            }
        }

        private void AddDiagnostics(UseSiteInfo info)
        {
            HashSetExtensions.InitializeAndAdd(ref Diagnostics, info.DiagnosticInfo);

            if (info.DiagnosticInfo?.Severity == DiagnosticSeverity.Error)
            {
                _haveErrors = true;
            }
        }

        private void AddDependencies(Symbol? symbol, UseSiteInfo info)
        {
            if (!_haveErrors)
            {
                AssemblySymbol? containingAssembly = symbol?.ContainingAssembly;

                if (containingAssembly is null || containingAssembly.CorLibrary != containingAssembly)
                {
                    HashSetExtensions.InitializeAndAdd(ref Dependencies, containingAssembly);

                    if (info.Dependencies?.IsEmpty == false)
                    {
                        (Dependencies ??= new HashSet<AssemblySymbol>()).AddAll(info.Dependencies);
                    }
                }
                else
                {
                    //Debug.Assert(info.Dependencies?.IsEmpty != false);
                }
            }
        }

        public void MergeAndFree(ref CompoundUseSiteInfo other)
        {
            if (_discarded)
            {
                other.Diagnostics = null;
                other.Dependencies = null;
                other._haveErrors = false;
                return;
            }

            mergeAndFree(ref Diagnostics, ref other.Diagnostics);

            if (other._haveErrors)
            {
                _haveErrors = true;
                other._haveErrors = false;
            }

            if (!_haveErrors)
            {
                mergeAndFree(ref Dependencies, ref other.Dependencies);
            }
            else
            {
                other.Dependencies = null;
            }

            static void mergeAndFree<T>(ref HashSet<T>? self, ref HashSet<T>? other)
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

        public void Add(UseSiteInfo other)
        {
            if (_discarded)
            {
                return;
            }

            AddDiagnostics(other);
            AddDependencies(symbol: null, other);
        }

        public void Add(UseSiteInfo.Builder other)
        {
            new UseSiteInfo(source: null, other);
        }
    }

    internal struct CachedUseSiteInfo
    {
        /// <summary>
        /// Either 
        /// - null, or
        /// - a <see cref="DiagnosticInfo"/>, or
        /// - dependencies as a <see cref="ImmutableHashSet{AssemblySymbol}"/>, or
        /// - a <see cref="Boxed"/> tuple of a <see cref="DiagnosticInfo"/> and a <see cref="ImmutableHashSet{AssemblySymbol}"/>. 
        /// </summary>
        private object? _info;

        public readonly static CachedUseSiteInfo Uninitialized = new CachedUseSiteInfo(CSDiagnosticInfo.EmptyErrorInfo); // Indicates unknown state.

        private CachedUseSiteInfo(object info)
        {
            _info = info;
        }

        public bool IsInitialized => (object?)_info != CSDiagnosticInfo.EmptyErrorInfo;

        public void Initialize(DiagnosticInfo? diagnosticInfo)
        {
            Initialize(diagnosticInfo, dependencies: null);
        }

        public void InitializeForSymbol(Symbol symbol, UseSiteInfo.Builder builder)
        {
            var useSiteInfo = new UseSiteInfo(symbol, builder);
            Initialize(useSiteInfo.DiagnosticInfo, useSiteInfo.Dependencies);
        }

        public UseSiteInfo ToUseSiteInfoForSymbol(Symbol symbol)
        {
            Expand(_info, out var diagnosticInfo, out var dependencies);
            return new UseSiteInfo(symbol, diagnosticInfo, dependencies);
        }

        public void Initialize(DiagnosticInfo? diagnosticInfo, ImmutableHashSet<AssemblySymbol>? dependencies)
        {
            _info = Compact(diagnosticInfo, dependencies);
        }

        private static object? Compact(DiagnosticInfo? diagnosticInfo, ImmutableHashSet<AssemblySymbol>? dependencies)
        {
            object? info;

            Debug.Assert((object?)diagnosticInfo != CSDiagnosticInfo.EmptyErrorInfo);
            if (dependencies?.IsEmpty != false)
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

        public void InterlockedCompareExchange(UseSiteInfo value)
        {
            if ((object?)_info == CSDiagnosticInfo.EmptyErrorInfo)
            {
                object? info = Compact(value.DiagnosticInfo, value.Dependencies);
                Interlocked.CompareExchange(ref _info, info, CSDiagnosticInfo.EmptyErrorInfo);
            }
        }

        public UseSiteInfo InterlockedInitialize(UseSiteInfo value)
        {
            Debug.Assert(value.Source is object);

            object? info = Compact(value.DiagnosticInfo, value.Dependencies);
            Debug.Assert(info is object);

            info = Interlocked.CompareExchange(ref _info, info, null);
            if (info == null)
            {
                return value;
            }

            Expand(info, out var diagnosticInfo, out var dependencies);
            return new UseSiteInfo(value.Source, diagnosticInfo, dependencies);
        }

        private static void Expand(object? info, out DiagnosticInfo? diagnosticInfo, out ImmutableHashSet<AssemblySymbol>? dependencies)
        {
            switch (info)
            {
                case null:
                    diagnosticInfo = null;
                    dependencies = null;
                    break;

                case DiagnosticInfo d:
                    Debug.Assert((object)d != CSDiagnosticInfo.EmptyErrorInfo);
                    diagnosticInfo = d;
                    dependencies = null;
                    break;

                case ImmutableHashSet<AssemblySymbol> a:
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
            public readonly ImmutableHashSet<AssemblySymbol> Dependencies;

            public Boxed(DiagnosticInfo diagnosticInfo, ImmutableHashSet<AssemblySymbol> dependencies)
            {
                Debug.Assert(!dependencies.IsEmpty);
                Debug.Assert((object)diagnosticInfo != CSDiagnosticInfo.EmptyErrorInfo);
                DiagnosticInfo = diagnosticInfo;
                Dependencies = dependencies;
            }
        }
    }
}
