// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds keys for all options in an <see cref="AnalyzerConfigSet"/>.
    /// </summary>
    public readonly struct AnalyzerConfigOptionKeys
    {
        /// <summary>
        /// Diagnostic Ids whose severities are configured in the corresponding <see cref="AnalyzerConfigSet"/>.
        /// </summary>
        public ImmutableHashSet<string> ConfiguredDiagnosticIds { get; }

        /// <summary>
        /// Keys for the options in the corresponding <see cref="AnalyzerConfigSet"/> and that
        /// do not have any special compiler behavior and are passed to analyzers as-is.
        /// </summary>
        public ImmutableHashSet<string> AnalyzerOptionKeys { get; }

        internal AnalyzerConfigOptionKeys(ImmutableHashSet<string> configuredDiagnosticIds, ImmutableHashSet<string> analyzerOptionKeys)
        {
            ConfiguredDiagnosticIds = UpdateComparerIfNeeded(configuredDiagnosticIds);
            AnalyzerOptionKeys = UpdateComparerIfNeeded(analyzerOptionKeys);
        }

        private static ImmutableHashSet<string> UpdateComparerIfNeeded(ImmutableHashSet<string> keys)
            => keys.KeyComparer == AnalyzerConfig.Section.PropertiesKeyComparer
                ? keys
                : keys.WithComparer(AnalyzerConfig.Section.PropertiesKeyComparer);

        internal AnalyzerConfigOptionKeys WithAdditionalAnalyzerConfigOptionKeys(IEnumerable<string> optionKeys)
        {
            if (optionKeys.IsEmpty())
                return this;

            var newOptionKeys = ImmutableHashSet.CreateBuilder(AnalyzerOptionKeys.KeyComparer);
            newOptionKeys.AddAll(AnalyzerOptionKeys);
            newOptionKeys.AddAll(optionKeys);

            if (newOptionKeys.Count == AnalyzerOptionKeys.Count)
                return this;

            return new AnalyzerConfigOptionKeys(ConfiguredDiagnosticIds, newOptionKeys.ToImmutable());
        }
    }
}
