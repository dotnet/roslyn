// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

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
        private static readonly BoundedCacheWithFactory<Compilation, TaintedDataConfig> s_ConfigsByCompilation = new();

        /// <summary>
        /// Caches the results for <see cref="GetSourceInfos(SinkKind)"/>.
        /// </summary>
        private static ImmutableDictionary<SinkKind, ImmutableHashSet<SourceInfo>> s_sinkKindToSourceInfo
            = ImmutableDictionary.Create<SinkKind, ImmutableHashSet<SourceInfo>>();

        /// <summary>
        /// Caches the results for <see cref="GetSanitizerInfos(SinkKind)"/>.
        /// </summary>
        private static ImmutableDictionary<SinkKind, ImmutableHashSet<SanitizerInfo>> s_sinkKindToSanitizerInfo
            = ImmutableDictionary.Create<SinkKind, ImmutableHashSet<SanitizerInfo>>();

        /// <summary>
        /// Caches the results for <see cref="HasTaintArraySource(SinkKind)"/>.
        /// </summary>
        private static ImmutableDictionary<SinkKind, bool> s_sinkKindHasTaintArraySource
            = ImmutableDictionary.Create<SinkKind, bool>();

        /// <summary>
        /// <see cref="WellKnownTypeProvider"/> for this instance's <see cref="Compilation"/>.
        /// </summary>
        private WellKnownTypeProvider WellKnownTypeProvider { get; }

#pragma warning disable CA1721 // Property names should not match get methods

        /// <summary>
        /// Mapping of sink kind to source symbol map.
        /// </summary>
        private ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SourceInfo>>> SourceSymbolMap { get; }

        /// <summary>
        /// Mapping of sink kind to sanitizer symbol map.
        /// </summary>
        private ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SanitizerInfo>>> SanitizerSymbolMap { get; }

        /// <summary>
        /// Mapping of sink kind to sink symbol map.
        /// </summary>
        private ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SinkInfo>>> SinkSymbolMap { get; }

#pragma warning restore CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets a cached <see cref="TaintedDataConfig"/> for <paramref name="compilation"/>.
        /// </summary>
        /// <param name="compilation">Whatever is being compiled.</param>
        /// <returns>The TaintedDataConfig.</returns>
        public static TaintedDataConfig GetOrCreate(Compilation compilation)
            => s_ConfigsByCompilation.GetOrCreateValue(compilation, Create);

        private TaintedDataConfig(
            WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SourceInfo>>> sourceSymbolMap,
            ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SanitizerInfo>>> sanitizerSymbolMap,
            ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SinkInfo>>> sinkSymbolMap)
        {
            WellKnownTypeProvider = wellKnownTypeProvider;
            SourceSymbolMap = sourceSymbolMap;
            SanitizerSymbolMap = sanitizerSymbolMap;
            SinkSymbolMap = sinkSymbolMap;
        }

        private static TaintedDataConfig Create(Compilation compilation)
        {
            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            using var _1 =
                PooledDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SourceInfo>>>.GetInstance(out var sourceSymbolMapBuilder);
            using var _2 =
                PooledDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SanitizerInfo>>>.GetInstance(out var sanitizerSymbolMapBuilder);
            using var _3 =
                PooledDictionary<SinkKind, Lazy<TaintedDataSymbolMap<SinkInfo>>>.GetInstance(out var sinkSymbolMapBuilder);

            // For tainted data rules with the same set of sources, we'll reuse the same TaintedDataSymbolMap<SourceInfo> instance.
            // Same for sanitizers.
            using var _4 =
                PooledDictionary<ImmutableHashSet<SourceInfo>, Lazy<TaintedDataSymbolMap<SourceInfo>>>.GetInstance(out var sourcesToSymbolMap);
            using var _5 =
                PooledDictionary<ImmutableHashSet<SanitizerInfo>, Lazy<TaintedDataSymbolMap<SanitizerInfo>>>.GetInstance(out var sanitizersToSymbolMap);

            // Build a mapping of (sourceSet, sanitizerSet) -> (sinkKinds, sinkSet), so we'll reuse the same TaintedDataSymbolMap<SinkInfo> instance.
            using var _6 =
                PooledDictionary<(ImmutableHashSet<SourceInfo> SourceInfos, ImmutableHashSet<SanitizerInfo> SanitizerInfos), (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SinkInfo>.Builder SinkInfos)>.GetInstance(out var sourceSanitizersToSinks);

            // Using LazyThreadSafetyMode.ExecutionAndPublication to avoid instantiating multiple times.
            foreach (var sinkKind in Enum.GetValues<SinkKind>())
            {
                ImmutableHashSet<SourceInfo> sources = GetSourceInfos(sinkKind);
                if (!sourcesToSymbolMap.TryGetValue(sources, out Lazy<TaintedDataSymbolMap<SourceInfo>> lazySourceSymbolMap))
                {
                    lazySourceSymbolMap = new Lazy<TaintedDataSymbolMap<SourceInfo>>(
                        () => { return new TaintedDataSymbolMap<SourceInfo>(wellKnownTypeProvider, sources); },
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    sourcesToSymbolMap.Add(sources, lazySourceSymbolMap);
                }

                sourceSymbolMapBuilder.Add(sinkKind, lazySourceSymbolMap);

                ImmutableHashSet<SanitizerInfo> sanitizers = GetSanitizerInfos(sinkKind);
                if (!sanitizersToSymbolMap.TryGetValue(sanitizers, out Lazy<TaintedDataSymbolMap<SanitizerInfo>> lazySanitizerSymbolMap))
                {
                    lazySanitizerSymbolMap = new Lazy<TaintedDataSymbolMap<SanitizerInfo>>(
                        () => { return new TaintedDataSymbolMap<SanitizerInfo>(wellKnownTypeProvider, sanitizers); },
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    sanitizersToSymbolMap.Add(sanitizers, lazySanitizerSymbolMap);
                }

                sanitizerSymbolMapBuilder.Add(sinkKind, lazySanitizerSymbolMap);

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
                    () => { return new TaintedDataSymbolMap<SinkInfo>(wellKnownTypeProvider, sinks); },
                    LazyThreadSafetyMode.ExecutionAndPublication);
                foreach (SinkKind sinkKind in kvp.Value.SinkKinds)
                {
                    sinkSymbolMapBuilder.Add(sinkKind, lazySinkSymbolMap);
                }
            }

            return new TaintedDataConfig(
                wellKnownTypeProvider,
                sourceSymbolMapBuilder.ToImmutableDictionary(),
                sanitizerSymbolMapBuilder.ToImmutableDictionary(),
                sinkSymbolMapBuilder.ToImmutableDictionary());
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

        public static bool HasTaintArraySource(SinkKind sinkKind)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref s_sinkKindHasTaintArraySource,
                sinkKind,
                static sinkKind => GetSourceInfos(sinkKind).Any(static o => o.TaintConstantArray));
        }

        private TaintedDataSymbolMap<T> GetFromMap<T>(SinkKind sinkKind, ImmutableDictionary<SinkKind, Lazy<TaintedDataSymbolMap<T>>> map)
            where T : ITaintedDataInfo
        {
            if (map.TryGetValue(sinkKind, out var lazySourceSymbolMap))
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
            if (s_sinkKindToSourceInfo.TryGetValue(sinkKind, out var sourceInfo))
            {
                return sourceInfo;
            }

            switch (sinkKind)
            {
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
                    // All of these use WebInputSources.SourceInfos.AddRange(StringTranferSources.SourceInfos), which is
                    // the same set as SinkKind.Sql. Delegate the call to SinkKind.Sql to avoid computing separate hash
                    // sets for identical cases.
                    sourceInfo = GetSourceInfos(SinkKind.Sql);
                    break;

                case SinkKind.Sql:
                    sourceInfo = WebInputSources.SourceInfos.AddRange(StringTranferSources.SourceInfos);
                    break;

                case SinkKind.InformationDisclosure:
                    sourceInfo = InformationDisclosureSources.SourceInfos.AddRange(StringTranferSources.SourceInfos);
                    break;

                case SinkKind.ZipSlip:
                    sourceInfo = ZipSlipSources.SourceInfos.AddRange(StringTranferSources.SourceInfos);
                    break;

                case SinkKind.HardcodedEncryptionKey:
                    sourceInfo = HardcodedSymmetricAlgorithmKeysSources.SourceInfos.AddRange(StringTranferSources.SourceInfos);
                    break;

                case SinkKind.HardcodedCertificate:
                    sourceInfo = HardcodedCertificateSources.SourceInfos.AddRange(HardcodedBytesSources.SourceInfos).AddRange(StringTranferSources.SourceInfos);
                    break;

                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SourceInfo>.Empty;
            }

            return ImmutableInterlocked.GetOrAdd(ref s_sinkKindToSourceInfo, sinkKind, sourceInfo);
        }

        private static ImmutableHashSet<SanitizerInfo> GetSanitizerInfos(SinkKind sinkKind)
        {
            if (s_sinkKindToSanitizerInfo.TryGetValue(sinkKind, out var sanitizerInfo))
            {
                return sanitizerInfo;
            }

            switch (sinkKind)
            {
                case SinkKind.XPath:
                    // All of these use PrimitiveTypeConverterSanitizers.SanitizerInfos.AddRange(AnySanitizers.SanitizerInfos),
                    // which is the same set as SinkKind.Sql. Delegate the call to SinkKind.Sql to avoid computing
                    // separate hash sets for identical cases.
                    sanitizerInfo = GetSanitizerInfos(SinkKind.Sql);
                    break;

                case SinkKind.Sql:
                    sanitizerInfo = PrimitiveTypeConverterSanitizers.SanitizerInfos.AddRange(AnySanitizers.SanitizerInfos);
                    break;

                case SinkKind.Xss:
                    sanitizerInfo = XssSanitizers.SanitizerInfos.AddRange(PrimitiveTypeConverterSanitizers.SanitizerInfos).AddRange(AnySanitizers.SanitizerInfos);
                    break;

                case SinkKind.Ldap:
                    sanitizerInfo = LdapSanitizers.SanitizerInfos.AddRange(AnySanitizers.SanitizerInfos);
                    break;

                case SinkKind.Xml:
                    sanitizerInfo = XmlSanitizers.SanitizerInfos.AddRange(PrimitiveTypeConverterSanitizers.SanitizerInfos).AddRange(AnySanitizers.SanitizerInfos);
                    break;

                case SinkKind.Dll:
                case SinkKind.InformationDisclosure:
                case SinkKind.FilePathInjection:
                case SinkKind.ProcessCommand:
                case SinkKind.Regex:
                case SinkKind.Redirect:
                case SinkKind.Xaml:
                case SinkKind.HardcodedEncryptionKey:
                case SinkKind.HardcodedCertificate:
                    sanitizerInfo = AnySanitizers.SanitizerInfos;
                    break;

                case SinkKind.ZipSlip:
                    sanitizerInfo = ZipSlipSanitizers.SanitizerInfos.AddRange(AnySanitizers.SanitizerInfos);
                    break;

                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SanitizerInfo>.Empty;
            }

            return ImmutableInterlocked.GetOrAdd(ref s_sinkKindToSanitizerInfo, sinkKind, sanitizerInfo);
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

                case SinkKind.HardcodedCertificate:
                    return HardcodedCertificateSinks.SinkInfos;

                default:
                    Debug.Fail($"Unhandled SinkKind {sinkKind}");
                    return ImmutableHashSet<SinkInfo>.Empty;
            }
        }
    }
}
