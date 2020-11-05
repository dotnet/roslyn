﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract partial class AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// unique <see cref="IPerLanguageOption"/> code style option.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
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
            IPerLanguageOption? option,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, title, messageFormat, isUnnecessary, configurable)
        {
            AddDiagnosticIdToOptionMapping(diagnosticId, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// unique <see cref="ILanguageSpecificOption"/> code style option for the given language.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
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
            ILanguageSpecificOption? option,
            string language,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, title, messageFormat, isUnnecessary, configurable)
        {
            AddDiagnosticIdToOptionMapping(diagnosticId, option, language);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// two or more <see cref="IPerLanguageOption"/> code style options.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
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
            ImmutableHashSet<IPerLanguageOption> options,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, title, messageFormat, isUnnecessary, configurable)
        {
            RoslynDebug.Assert(options != null);
            Debug.Assert(options.Count > 1);
            AddDiagnosticIdToOptionMapping(diagnosticId, options);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// two or more <see cref="ILanguageSpecificOption"/> code style options for the given language.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
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
            ImmutableHashSet<ILanguageSpecificOption> options,
            string language,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, title, messageFormat, isUnnecessary, configurable)
        {
            RoslynDebug.Assert(options != null);
            Debug.Assert(options.Count > 1);
            AddDiagnosticIdToOptionMapping(diagnosticId, options, language);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with per-language options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, IPerLanguageOption> supportedDiagnosticsWithOptions)
            : this(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var kvp in supportedDiagnosticsWithOptions)
            {
                AddDiagnosticIdToOptionMapping(kvp.Key.Id, kvp.Value);
            }
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with language-specific options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            ImmutableDictionary<DiagnosticDescriptor, ILanguageSpecificOption> supportedDiagnosticsWithOptions,
            string language)
            : this(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var kvp in supportedDiagnosticsWithOptions)
            {
                AddDiagnosticIdToOptionMapping(kvp.Key.Id, kvp.Value, language);
            }
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with a mix of language-specific and per-language options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            ImmutableDictionary<DiagnosticDescriptor, ILanguageSpecificOption> supportedDiagnosticsWithLangaugeSpecificOptions,
            ImmutableDictionary<DiagnosticDescriptor, IPerLanguageOption> supportedDiagnosticsWithPerLanguageOptions,
            string language)
            : this(supportedDiagnosticsWithLangaugeSpecificOptions.Keys.Concat(supportedDiagnosticsWithPerLanguageOptions.Keys).ToImmutableArray())
        {
            foreach (var kvp in supportedDiagnosticsWithLangaugeSpecificOptions)
            {
                AddDiagnosticIdToOptionMapping(kvp.Key.Id, kvp.Value, language);
            }

            foreach (var kvp in supportedDiagnosticsWithPerLanguageOptions)
            {
                AddDiagnosticIdToOptionMapping(kvp.Key.Id, kvp.Value);
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, IPerLanguageOption? option)
        {
            if (option != null)
            {
                AddDiagnosticIdToOptionMapping(diagnosticId, ImmutableHashSet.Create(option));
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ILanguageSpecificOption? option, string language)
        {
            if (option != null)
            {
                AddDiagnosticIdToOptionMapping(diagnosticId, ImmutableHashSet.Create(option), language);
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ImmutableHashSet<IPerLanguageOption> options)
            => IDEDiagnosticIdToOptionMappingHelper.AddOptionMapping(diagnosticId, options);

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ImmutableHashSet<ILanguageSpecificOption> options, string language)
            => IDEDiagnosticIdToOptionMappingHelper.AddOptionMapping(diagnosticId, options, language);

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public virtual bool OpenFileOnly(OptionSet options)
            => false;
    }
}
