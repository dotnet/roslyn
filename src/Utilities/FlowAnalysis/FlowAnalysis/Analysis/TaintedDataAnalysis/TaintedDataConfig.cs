// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.PooledObjects;
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
        /// <summary>
        /// <see cref="WellKnownTypeProvider"/> for this instance's <see cref="Compilation"/>.
        /// </summary>
        private WellKnownTypeProvider WellKnownTypeProvider { get; }

#pragma warning disable CA1721 // Property names should not match get methods

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

#pragma warning restore CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets a cached <see cref="TaintedDataConfig"/> for <paramref name="compilation"/>.
        /// </summary>
        /// <param name="compilation">Whatev is being compiled.</param>
        /// <returns>The TaintedDataConfig.</returns>
        public static TaintedDataConfig GetOrCreate(Compilation compilation)
        {
            return BoundedCompilationCacheWithFactory<TaintedDataConfig>.GetOrCreateValue(compilation, CreateTaintedDataConfig);

            // Local functions.
            static TaintedDataConfig CreateTaintedDataConfig(Compilation compilation)
                => new TaintedDataConfig(compilation);
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
            PooledDictionary<ImmutableHashSet<SourceInfo>, Lazy<TaintedDataSymbolMap<SourceInfo>>> sourcesToSymbolMap =
                PooledDictionary<ImmutableHashSet<SourceInfo>, Lazy<TaintedDataSymbolMap<SourceInfo>>>.GetInstance();
            PooledDictionary<ImmutableHashSet<SanitizerInfo>, Lazy<TaintedDataSymbolMap<SanitizerInfo>>> sanitizersToSymbolMap =
                PooledDictionary<ImmutableHashSet<SanitizerInfo>, Lazy<TaintedDataSymbolMap<SanitizerInfo>>>.GetInstance();

            // Build a mapping of (sourceSet, sanitizerSet) -> (sinkKinds, sinkSet), so we'll reuse the same TaintedDataSymbolMap<SinkInfo> instance.
            PooledDictionary<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SinkInfo>.Builder SinkInfos)> sourceSanitizersToSinks =
                PooledDictionary<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SinkInfo>.Builder SinkInfos)>.GetInstance();
            try
            {
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
                    if (!sourceSanitizersToSinks.TryGetValue((sources, sanitizers), out (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SinkInfo>.Builder SinkInfos) sinksPair))
                    {
                        sinksPair = (ImmutableHashSet.CreateBuilder<SinkKind>(), ImmutableHashSet.CreateBuilder<SinkInfo>());
                        sourceSanitizersToSinks.Add((sources, sanitizers), sinksPair);
                    }

                    sinksPair.SinkKinds.Add(sinkKind);
                    sinksPair.SinkInfos.UnionWith(sinks);
                }

                foreach (KeyValuePair<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SinkInfo>.Builder SinkInfos)> kvp in sourceSanitizersToSinks)
                {
                    ImmutableHashSet<SinkInfo> sinks = kvp.Value.SinkInfos.ToImmutable();
                    Lazy<TaintedDataSymbolMap<SinkInfo>> lazySinkSymbolMap = new Lazy<TaintedDataSymbolMap<SinkInfo>>(
                        () => { return new TaintedDataSymbolMap<SinkInfo>(this.WellKnownTypeProvider, sinks); },
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    foreach (SinkKind sinkKind in kvp.Value.SinkKinds)
                    {
                        this.SinkSymbolMap.Add(sinkKind, lazySinkSymbolMap);
                    }
                }
            }
            finally
            {
                sourcesToSymbolMap.Free();
                sanitizersToSymbolMap.Free();
                sourceSanitizersToSinks.Free();
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

        public bool HasTaintArraySource(SinkKind sinkKind)
        {
            return GetSourceInfos(sinkKind).Any(o => o.TaintConstantArray);
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
                case SinkKind.Dll:
                case SinkKind.FilePathInjection:
                case SinkKind.ProcessCommand:
                case SinkKind.Xss:
                case SinkKind.Regex:
                case SinkKind.Ldap:
                case SinkKind.Redirect:
                case SinkKind.XPath:
                case SinkKind.Xml:
                case SinkKind.Xaml:
                    return WebInputSources.SourceInfos;

                case SinkKind.InformationDisclosure:
                    return InformationDisclosureSources.SourceInfos;

                case SinkKind.ZipSlip:
                    return ZipSlipSources.SourceInfos;

                case SinkKind.HardcodedEncryptionKey:
                    return HardcodedEncryptionKeySources.SourceInfos;

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
                case SinkKind.XPath:
                    return PrimitiveTypeConverterSanitizers.SanitizerInfos;

                case SinkKind.Xss:
                    return XssSanitizers.SanitizerInfos;

                case SinkKind.Ldap:
                    return LdapSanitizers.SanitizerInfos;

                case SinkKind.Xml:
                    return PrimitiveTypeConverterSanitizers.SanitizerInfos.Union(XmlSanitizers.SanitizerInfos);

                case SinkKind.Dll:
                case SinkKind.InformationDisclosure:
                case SinkKind.FilePathInjection:
                case SinkKind.ProcessCommand:
                case SinkKind.Regex:
                case SinkKind.Redirect:
                case SinkKind.Xaml:
                case SinkKind.HardcodedEncryptionKey:
                    return ImmutableHashSet<SanitizerInfo>.Empty;

                case SinkKind.ZipSlip:
                    return ZipSlipSanitizers.SanitizerInfos;

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

                case SinkKind.Dll:
                    return DllSinks.SinkInfos;

                case SinkKind.InformationDisclosure:
                case SinkKind.Xss:
                    return WebOutputSinks.SinkInfos;

                case SinkKind.FilePathInjection:
                    return FilePathInjectionSinks.SinkInfos;

                case SinkKind.ProcessCommand:
                    return ProcessCommandSinks.SinkInfos;

                case SinkKind.Regex:
                    return RegexSinks.SinkInfos;

                case SinkKind.Ldap:
                    return LdapSinks.SinkInfos;

                case SinkKind.Redirect:
                    return RedirectSinks.SinkInfos;

                case SinkKind.XPath:
                    return XPathSinks.SinkInfos;

                case SinkKind.Xml:
                    return XmlSinks.SinkInfos;

                case SinkKind.Xaml:
                    return XamlSinks.SinkInfos;

                case SinkKind.ZipSlip:
                    return ZipSlipSinks.SinkInfos;

                case SinkKind.HardcodedEncryptionKey:
                    return HardcodedEncryptionKeySinks.SinkInfos;

                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SinkInfo>.Empty;
            }
        }
    }
}
