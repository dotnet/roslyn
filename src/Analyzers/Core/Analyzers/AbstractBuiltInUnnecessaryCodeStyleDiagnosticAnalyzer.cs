// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Code style analyzer that reports at least one 'unnecessary' code diagnostic.
    /// </summary>
    internal abstract class AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        /// <summary>
        /// Constructor for an unnecessary code style analyzer with a single diagnostic descriptor and
        /// unique <see cref="IPerLanguageValuedOption"/> code style option.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="option">
        /// Code style option that can be used to configure the given <paramref name="diagnosticId"/>.
        /// <see langword="null"/>, if there is no such unique option.
        /// </param>
        /// <param name="fadingOption">
        /// Per-language fading option that can be used to configure if the diagnostic should be faded in the IDE or not.
        /// <see langword="null"/>, if there is no such unique fading option.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// <see langword="null"/> if the message is identical to the title.
        /// </param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            IOption2? option,
            PerLanguageOption2<bool>? fadingOption,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool configurable = true)
            : base(diagnosticId, enforceOnBuild, option, title, messageFormat, isUnnecessary: true, configurable)
        {
            AddDiagnosticIdToFadingOptionMapping(diagnosticId, fadingOption);
        }

        /// <summary>
        /// Constructor for an unnecessary code style analyzer with a single diagnostic descriptor and
        /// two or more <see cref="IPerLanguageValuedOption"/> code style options.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="options">
        /// Set of two or more per-language options that can be used to configure the diagnostic severity of the given diagnosticId.
        /// </param>
        /// <param name="fadingOption">
        /// Per-language fading option that can be used to configure if the diagnostic should be faded in the IDE or not.
        /// <see langword="null"/>, if there is no such unique fading option.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// Null if the message is identical to the title.
        /// </param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            ImmutableHashSet<IOption2> options,
            PerLanguageOption2<bool>? fadingOption,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool configurable = true)
            : base(diagnosticId, enforceOnBuild, options, title, messageFormat, isUnnecessary: true, configurable)
        {
            AddDiagnosticIdToFadingOptionMapping(diagnosticId, fadingOption);
        }

        /// <summary>
        /// Constructor for an unnecessary code style analyzer with multiple descriptors. All unnecessary descriptors will share the same <paramref name="fadingOption"/>
        /// </summary>
        /// <param name="descriptors">Descriptors supported by this analyzer</param>
        /// <param name="fadingOption">The fading option used to control descriptors that are unnecessary.</param>
        protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> descriptors, PerLanguageOption2<bool> fadingOption)
            : base(descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Unnecessary))
                {
                    AddDiagnosticIdToFadingOptionMapping(descriptor.Id, fadingOption);
                }
            }
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with a code style options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, IOption2> supportedDiagnosticsWithOptions, PerLanguageOption2<bool>? fadingOption)
            : base(supportedDiagnosticsWithOptions)
        {
            AddDescriptorsToFadingOptionMapping(supportedDiagnosticsWithOptions.Keys, fadingOption);
        }

        /// <summary>
        /// Constructor for a code style analyzer with multiple diagnostic descriptors with zero or more code style options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, ImmutableHashSet<IOption2>> supportedDiagnosticsWithOptions, PerLanguageOption2<bool>? fadingOption)
            : base(supportedDiagnosticsWithOptions)
        {
            AddDescriptorsToFadingOptionMapping(supportedDiagnosticsWithOptions.Keys, fadingOption);
        }

        private static void AddDiagnosticIdToFadingOptionMapping(string diagnosticId, PerLanguageOption2<bool>? fadingOption)
        {
            if (fadingOption != null)
            {
                IDEDiagnosticIdToOptionMappingHelper.AddFadingOptionMapping(diagnosticId, fadingOption);
            }
        }

        private static void AddDescriptorsToFadingOptionMapping(IEnumerable<DiagnosticDescriptor> descriptors, PerLanguageOption2<bool>? fadingOption)
        {
            if (fadingOption != null)
            {
                foreach (var descriptor in descriptors)
                {
                    if (descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Unnecessary))
                    {
                        IDEDiagnosticIdToOptionMappingHelper.AddFadingOptionMapping(descriptor.Id, fadingOption);
                    }
                }
            }
        }
    }
}
