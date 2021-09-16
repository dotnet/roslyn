// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if NET_ANALYZERS || TEXT_ANALYZERS || MICROSOFT_CODEANALYSIS_ANALYZERS

namespace Microsoft.CodeAnalysis
{
#pragma warning disable CA1008 // Enums should have zero value
#pragma warning disable CA1027 // Mark enums with FlagsAttribute
    internal enum RuleLevel
#pragma warning restore CA1027 // Mark enums with FlagsAttribute
#pragma warning restore CA1008 // Enums should have zero value
    {
        /// <summary>
        /// Correctness rule which should have <b>no false positives</b>, and is extremely likely to be fixed by users.
        /// This rule will be <b>enabled in CI and IDE live analysis</b> by default with severity <see cref="DiagnosticSeverity.Warning"/>.
        /// </summary>
        BuildWarning = 1,

        /// <summary>
        /// Correctness rule which should have <b>no false positives</b>, and is extremely likely to be fixed by users.
        /// This rule is a candidate to be turned into a <see cref="BuildWarning"/>.
        /// Until then, this rule will be an <see cref="IdeSuggestion"/>
        /// </summary>
        BuildWarningCandidate = IdeSuggestion,

        /// <summary>
        /// Rule which should have <b>no false positives</b>, and is a valuable IDE live analysis suggestion for opportunistic improvement, but not something to be enforced in CI.
        /// This rule will be <b>enabled by default as an IDE-only suggestion</b> with severity <see cref="DiagnosticSeverity.Info"/> which will be shown in "Messages" tab in Error list.
        /// </summary>
        IdeSuggestion = 2,

        /// <summary>
        /// Rule which <b>may have some false positives</b> and hence would be noisy to be enabled by default as a suggestion or a warning in IDE live analysis or CI.
        /// This rule will be enabled by default with <see cref="DiagnosticSeverity.Hidden"/> severity, so it will be <b>effectively disabled in both IDE live analysis and CI</b>.
        /// However, hidden severity ensures that this rule can be <i>enabled using the category based bulk configuration</i> (TODO: add documentation link on category based bulk configuration).
        /// </summary>
        IdeHidden_BulkConfigurable = 3,

        /// <summary>
        /// <b>Disabled by default rule.</b>
        /// Users would need to explicitly enable this rule with an ID-based severity configuration entry for the rule ID.
        /// This rule <i>cannot be enabled using the category based bulk configuration</i> (TODO: add documentation link on category based bulk configuration).
        /// </summary>
        Disabled = 4,

        /// <summary>
        /// <b>Disabled by default rule</b>, which is a candidate for <b>deprecation</b>.
        /// </summary>
        CandidateForRemoval = 5,
    }
}

#endif