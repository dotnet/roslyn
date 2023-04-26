// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public sealed partial class AnalyzerConfigSet
    {
        /// <summary>
        /// Holds keys for all options in an <see cref="AnalyzerConfigSet"/>.
        /// </summary>
        private sealed class OptionKeys
        {
            /// <summary>
            /// Diagnostic Ids whose severities are configured in the corresponding <see cref="AnalyzerConfigSet"/>.
            /// </summary>
            private readonly ImmutableHashSet<string> _configuredDiagnosticIds;

            /// <summary>
            /// Keys for the options in the corresponding <see cref="AnalyzerConfigSet"/> and that
            /// do not have any special compiler behavior and are passed to analyzers as-is.
            /// </summary>
            private readonly ImmutableHashSet<string> _analyzerOptionKeys;

            public OptionKeys(ImmutableHashSet<string> configuredDiagnosticIds, ImmutableHashSet<string> analyzerOptionKeys)
            {
                Debug.Assert(configuredDiagnosticIds.KeyComparer == AnalyzerConfig.Section.PropertiesKeyComparer);
                Debug.Assert(analyzerOptionKeys.KeyComparer == AnalyzerConfig.Section.PropertiesKeyComparer);

                _configuredDiagnosticIds = configuredDiagnosticIds;
                _analyzerOptionKeys = analyzerOptionKeys;
            }

            public bool HasSeverityConfigurationKey(DiagnosticDescriptor descriptor)
                => _configuredDiagnosticIds.Contains(descriptor.Id) ||
                   AnalyzerOptionsExtensions.HasSeverityBulkConfigurationEntry(_analyzerOptionKeys, descriptor.Category);
        }
    }
}
