// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

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
        /// Per-language option that can be used to configure the given <paramref name="diagnosticId"/>.
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
            IPerLanguageValuedOption? option,
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
        /// unique <see cref="ISingleValuedOption"/> code style option for the given language.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="option">
        /// Language specific option that can be used to configure the given <paramref name="diagnosticId"/>.
        /// <see langword="null"/>, if there is no such unique option.
        /// </param>
        /// <param name="fadingOption">
        /// Per-language fading option that can be used to configure if the diagnostic should be faded in the IDE or not.
        /// <see langword="null"/>, if there is no such unique fading option.
        /// </param>
        /// <param name="language">Language for the given language-specific <paramref name="option"/>.</param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// <see langword="null"/> if the message is identical to the title.
        /// </param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            ISingleValuedOption? option,
            PerLanguageOption2<bool>? fadingOption,
            string language,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool configurable = true)
            : base(diagnosticId, enforceOnBuild, option, language, title, messageFormat, isUnnecessary: true, configurable)
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
            ImmutableHashSet<IPerLanguageValuedOption> options,
            PerLanguageOption2<bool>? fadingOption,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool configurable = true)
            : base(diagnosticId, enforceOnBuild, options, title, messageFormat, isUnnecessary: true, configurable)
        {
            AddDiagnosticIdToFadingOptionMapping(diagnosticId, fadingOption);
        }

        private static void AddDiagnosticIdToFadingOptionMapping(string diagnosticId, PerLanguageOption2<bool>? fadingOption)
        {
            if (fadingOption != null)
            {
                IDEDiagnosticIdToOptionMappingHelper.AddFadingOptionMapping(diagnosticId, fadingOption);
            }
        }
    }
}
