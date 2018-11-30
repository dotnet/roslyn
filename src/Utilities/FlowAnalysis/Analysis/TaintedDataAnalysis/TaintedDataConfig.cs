// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Manages tainted data sources, sanitizers, and sinks for all the 
    /// different tainted data analysis rules.
    /// </summary>
    /// <remarks>
    /// This is centralized, so that rules that use the same set of sources
    /// and sanitizers but have different sinks, can reuse equivalent <see
    /// cref="TaintedDataAnalysisContext"/>s, and thus reuse the same dataflow
    /// analysis result, so DFA doesn't have be invoked multiple times.
    /// </remarks>
    internal class TaintedDataConfig
    {
        private static ConditionalWeakTable<Compilation, TaintedDataConfig> s_ConfigsByCompilation = new ConditionalWeakTable<Compilation, TaintedDataConfig>();

        /// <summary>
        /// <see cref="WellKnownTypeProvider"/> for this instance's <see cref="Compilation"/>.
        /// </summary>
        private WellKnownTypeProvider WellKnownTypeProvider { get; }

        /// <summary>
        /// Mapping of sink kind to source symbol map.
        /// </summary>
        private Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<SourceInfo>>> SourceSymbolMap { get; }

        /// <summary>
        /// Mapping of sink kind to sanitizer symbol map.
        /// </summary>
        private Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<SanitizerInfo>>> SanitizerSymbolMap { get; }

        /// <summary>
        /// Mapping of sink kind to sink symbol map.
        /// </summary>
        private Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<SinkInfo>>> SinkSymbolMap { get; }

        /// <summary>
        /// Gets a cached <see cref="TaintedDataConfig"/> for <paramref name="compilation"/>.
        /// </summary>
        /// <param name="compilation">Whatev is being compiled.</param>
        /// <returns>The TaintedDataConfig.</returns>
        public static TaintedDataConfig GetOrCreate(Compilation compilation)
        {
            return s_ConfigsByCompilation.GetValue(compilation, CreateValueCallback);
        }

        private static TaintedDataConfig CreateValueCallback(Compilation key)
        {
            return new TaintedDataConfig(key);
        }

        private TaintedDataConfig()
        {
        }

        private TaintedDataConfig(Compilation compilation)
        {
            this.WellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            this.SourceSymbolMap = new Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<SourceInfo>>>();
            this.SanitizerSymbolMap = new Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<SanitizerInfo>>>();
            this.SinkSymbolMap = new Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<SinkInfo>>>();

            // For tainted data rules with the same set of sources, we'll reuse the same TaintedDataSymbolMap<SourceInfo> instance.
            // Same for sanitizers.
            Dictionary<ImmutableHashSet<SourceInfo>, Lazy<TaintedDataSymbolMap<SourceInfo>>> sourcesToSymbolMap =
                new Dictionary<ImmutableHashSet<SourceInfo>, Lazy<TaintedDataSymbolMap<SourceInfo>>>();
            Dictionary<ImmutableHashSet<SanitizerInfo>, Lazy<TaintedDataSymbolMap<SanitizerInfo>>> sanitizersToSymbolMap =
                new Dictionary<ImmutableHashSet<SanitizerInfo>, Lazy<TaintedDataSymbolMap<SanitizerInfo>>>();

            // Build a mapping of (sourceSet, sanitizerSet) -> sinkSet, we'll reuse same TaintedDataSymbolMap<SinkInfo> instance.
            Dictionary<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), ImmutableHashSet<SinkInfo>.Builder> sourceSanitizersToSinks =
                new Dictionary<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), ImmutableHashSet<SinkInfo>.Builder>();

            // Using LazyThreadSafetyMode.ExecutionAndPublication to avoid instantiating multiple times.
            foreach (SinkKind sinkKind in Enum.GetValues(typeof(SinkKind)))
            {
                ImmutableHashSet<SourceInfo> sources = GetSourceInfos(sinkKind);
                if (!sourcesToSymbolMap.TryGetValue(sources, out Lazy<TaintedDataSymbolMap<SourceInfo>> lazySourceSymbolMap))
                {
                    lazySourceSymbolMap = new Lazy<TaintedDataSymbolMap<SourceInfo>>(
                        () => { return new TaintedDataSymbolMap<SourceInfo>(this.WellKnownTypeProvider, sources); },
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    sourcesToSymbolMap.Add(sources, lazySourceSymbolMap);
                }

                this.SourceSymbolMap.Add(sinkKind, lazySourceSymbolMap);

                ImmutableHashSet<SanitizerInfo> sanitizers = GetSanitizerInfos(sinkKind);
                if (!sanitizersToSymbolMap.TryGetValue(sanitizers, out Lazy<TaintedDataSymbolMap<SanitizerInfo>> lazySanitizerSymbolMap))
                {
                    lazySanitizerSymbolMap = new Lazy<TaintedDataSymbolMap<SanitizerInfo>>(
                        () => { return new TaintedDataSymbolMap<SanitizerInfo>(this.WellKnownTypeProvider, sanitizers); },
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    sanitizersToSymbolMap.Add(sanitizers, lazySanitizerSymbolMap);
                }

                this.SanitizerSymbolMap.Add(sinkKind, lazySanitizerSymbolMap);

                ImmutableHashSet<SinkInfo> sinks = GetSinkInfos(sinkKind);
                if (!sourceSanitizersToSinks.TryGetValue((sources, sanitizers), out ImmutableHashSet<SinkInfo>.Builder sinksBuilder))
                {
                    sinksBuilder = ImmutableHashSet.CreateBuilder<SinkInfo>();
                    sourceSanitizersToSinks.Add((sources, sanitizers), sinksBuilder);
                }

                sinksBuilder.UnionWith(sinks);
            }

            foreach (KeyValuePair<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), ImmutableHashSet<SinkInfo>.Builder> kvp in sourceSanitizersToSinks)
            {
                ImmutableHashSet<SinkInfo> sinks = kvp.Value.ToImmutable();
                Lazy<TaintedDataSymbolMap<SinkInfo>> lazySinkSymbolMap = new Lazy<TaintedDataSymbolMap<SinkInfo>>(
                    () => { return new TaintedDataSymbolMap<SinkInfo>(this.WellKnownTypeProvider, sinks); },
                    LazyThreadSafetyMode.ExecutionAndPublication);
                foreach (SinkKind sinkKind in sinks.Select(s => s.SinkKind).Distinct())
                {
                    this.SinkSymbolMap.Add(sinkKind, lazySinkSymbolMap);
                }
            }
        }

        public TaintedDataSymbolMap<SourceInfo> GetSourceSymbolMap(SinkKind sinkKind)
        {
            return this.GetFromMap<SourceInfo>(sinkKind, this.SourceSymbolMap);
        }

        public TaintedDataSymbolMap<SanitizerInfo> GetSanitizerSymbolMap(SinkKind sinkKind)
        {
            return this.GetFromMap<SanitizerInfo>(sinkKind, this.SanitizerSymbolMap);
        }

        public TaintedDataSymbolMap<SinkInfo> GetSinkSymbolMap(SinkKind sinkKind)
        {
            return this.GetFromMap<SinkInfo>(sinkKind, this.SinkSymbolMap);
        }

        private TaintedDataSymbolMap<T> GetFromMap<T>(SinkKind sinkKind, Dictionary<SinkKind, Lazy<TaintedDataSymbolMap<T>>> map)
            where T : ITaintedDataInfo
        {
            if (map.TryGetValue(sinkKind, out Lazy<TaintedDataSymbolMap<T>> lazySourceSymbolMap))
            {
                return lazySourceSymbolMap.Value;
            }
            else
            {
                Debug.Fail($"SinkKind {sinkKind} entry missing from {typeof(T).Name} map");
                return new TaintedDataSymbolMap<T>(this.WellKnownTypeProvider, Enumerable.Empty<T>());
            }
        }

        private static ImmutableHashSet<SourceInfo> GetSourceInfos(SinkKind sinkKind)
        {
            switch (sinkKind)
            {
                case SinkKind.Sql:
                    return WebInputSources.SourceInfos;
                    
                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SourceInfo>.Empty;
            }
        }

        private static ImmutableHashSet<SanitizerInfo> GetSanitizerInfos(SinkKind sinkKind)
        {
            switch (sinkKind)
            {
                case SinkKind.Sql:
                    return PrimitiveTypeConverterSanitizers.SanitizerInfos;

                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SanitizerInfo>.Empty;
            }
        }

        private static ImmutableHashSet<SinkInfo> GetSinkInfos(SinkKind sinkKind)
        {
            switch (sinkKind)
            {
                case SinkKind.Sql:
                    return SqlSinks.SinkInfos;

                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SinkInfo>.Empty;
            }
        }
    }
}
