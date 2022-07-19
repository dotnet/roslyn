// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

#pragma warning disable IDE0060 // Remove unused parameter - TODO: Will follow-up and remove language parameters.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract partial class AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// unique <see cref="IPerLanguageValuedOption"/> code style option.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="option">
        /// Per-language option that can be used to configure the given <paramref name="diagnosticId"/>.
        /// <see langword="null"/>, if there is no such unique option.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// <see langword="null"/> if the message is identical to the title.
        /// </param>
        /// <param name="isUnnecessary"><see langword="true"/> if the diagnostic is reported on unnecessary code; otherwise, <see langword="false"/>.</param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            IPerLanguageValuedOption? option,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, enforceOnBuild, title, messageFormat, isUnnecessary, configurable)
        {
            AddDiagnosticIdToOptionMapping(diagnosticId, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// unique <see cref="ISingleValuedOption"/> code style option for the given language.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="option">
        /// Language specific option that can be used to configure the given <paramref name="diagnosticId"/>.
        /// <see langword="null"/>, if there is no such unique option.
        /// </param>
        /// <param name="language">Language for the given language-specific <paramref name="option"/>.</param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// <see langword="null"/> if the message is identical to the title.
        /// </param>
        /// <param name="isUnnecessary"><see langword="true"/> if the diagnostic is reported on unnecessary code; otherwise, <see langword="false"/>.</param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            ISingleValuedOption? option,
            string language,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, enforceOnBuild, title, messageFormat, isUnnecessary, configurable)
        {
            AddDiagnosticIdToOptionMapping(diagnosticId, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// two or more <see cref="IPerLanguageValuedOption "/> code style options.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="options">
        /// Set of two or more per-language options that can be used to configure the diagnostic severity of the given diagnosticId.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// Null if the message is identical to the title.
        /// </param>
        /// <param name="isUnnecessary"><see langword="true"/> if the diagnostic is reported on unnecessary code; otherwise, <see langword="false"/>.</param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            ImmutableHashSet<IPerLanguageValuedOption> options,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, enforceOnBuild, title, messageFormat, isUnnecessary, configurable)
        {
            RoslynDebug.Assert(options != null);
            Debug.Assert(options.Count > 1);
            AddDiagnosticIdToOptionMapping(diagnosticId, options);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// two or more <see cref="ISingleValuedOption"/> code style options for the given language.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="options">
        /// Set of two or more language-specific options that can be used to configure the diagnostic severity of the given diagnosticId.
        /// </param>
        /// <param name="language">Language for the given language-specific <paramref name="options"/>.</param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// Null if the message is identical to the title.
        /// </param>
        /// <param name="isUnnecessary"><see langword="true"/> if the diagnostic is reported on unnecessary code; otherwise, <see langword="false"/>.</param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            ImmutableHashSet<ISingleValuedOption> options,
            string language,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, enforceOnBuild, title, messageFormat, isUnnecessary, configurable)
        {
            RoslynDebug.Assert(options != null);
            Debug.Assert(options.Count > 1);
            AddDiagnosticIdToOptionMapping(diagnosticId, options);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with per-language options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, IPerLanguageValuedOption> supportedDiagnosticsWithOptions)
            : this(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var (descriptor, option) in supportedDiagnosticsWithOptions)
                AddDiagnosticIdToOptionMapping(descriptor.Id, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with language-specific options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            ImmutableDictionary<DiagnosticDescriptor, ISingleValuedOption> supportedDiagnosticsWithOptions,
            string language)
            : this(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var (descriptor, option) in supportedDiagnosticsWithOptions)
                AddDiagnosticIdToOptionMapping(descriptor.Id, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with a mix of language-specific and per-language options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            ImmutableDictionary<DiagnosticDescriptor, ISingleValuedOption> supportedDiagnosticsWithLangaugeSpecificOptions,
            ImmutableDictionary<DiagnosticDescriptor, IPerLanguageValuedOption> supportedDiagnosticsWithPerLanguageOptions,
            string language)
            : this(supportedDiagnosticsWithLangaugeSpecificOptions.Keys.Concat(supportedDiagnosticsWithPerLanguageOptions.Keys).ToImmutableArray())
        {
            foreach (var (descriptor, option) in supportedDiagnosticsWithLangaugeSpecificOptions)
                AddDiagnosticIdToOptionMapping(descriptor.Id, option);

            foreach (var (descriptor, option) in supportedDiagnosticsWithPerLanguageOptions)
                AddDiagnosticIdToOptionMapping(descriptor.Id, option);
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, IPerLanguageValuedOption? option)
        {
            if (option != null)
            {
                AddDiagnosticIdToOptionMapping(diagnosticId, ImmutableHashSet.Create(option));
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ISingleValuedOption? option)
        {
            if (option != null)
            {
                AddDiagnosticIdToOptionMapping(diagnosticId, ImmutableHashSet.Create(option));
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ImmutableHashSet<IPerLanguageValuedOption> options)
            => IDEDiagnosticIdToOptionMappingHelper.AddOptionMapping(diagnosticId, options);

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ImmutableHashSet<ISingleValuedOption> options)
            => IDEDiagnosticIdToOptionMappingHelper.AddOptionMapping(diagnosticId, options);

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public virtual bool OpenFileOnly(SimplifierOptions? options)
            => false;
    }
}
