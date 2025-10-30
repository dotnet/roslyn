// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Correctness rule which prevents the compiler from producing a well-defined output binary, and must have
        /// <b>no false positives</b>. Violations of this rule must be fixed by users before any other testing work can
        /// continue. This rule will be <b>enabled in CI and IDE live analysis</b> by default with severity
        /// <see cref="DiagnosticSeverity.Error"/>.
        /// </summary>
        /// <remarks>
        /// Since analyzers cannot directly influence output binaries, this value is typically only valid in the
        /// implementation of source generators. Rare exceptions may occur at the request of a director in coordination
        /// with the core compiler team.
        /// </remarks>
        BuildError = 1,

        /// <summary>
        /// Correctness rule which should have <b>no false positives</b>, and is extremely likely to be fixed by users.
        /// This rule will be <b>enabled in CI and IDE live analysis</b> by default with severity <see cref="DiagnosticSeverity.Warning"/>.
        /// </summary>
        BuildWarning = 2,

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
#pragma warning disable CA1069 // Enums values should not be duplicated - BuildWarningCandidate is used to mark a separate bucket of rules that could be promoted.
        IdeSuggestion = 3,
#pragma warning restore CA1069 // Enums values should not be duplicated

        /// <summary>
        /// Rule which <b>may have some false positives</b> and hence would be noisy to be enabled by default as a suggestion or a warning in IDE live analysis or CI.
        /// This rule will be enabled by default with <see cref="DiagnosticSeverity.Hidden"/> severity, so it will be <b>effectively disabled in both IDE live analysis and CI</b>.
        /// However, hidden severity ensures that this rule can be <i>enabled using the category based bulk configuration</i> (TODO: add documentation link on category based bulk configuration).
        /// </summary>
        IdeHidden_BulkConfigurable = 4,

        /// <summary>
        /// <b>Disabled by default rule.</b>
        /// Users would need to explicitly enable this rule with an ID-based severity configuration entry for the rule ID.
        /// This rule <i>cannot be enabled using the category based bulk configuration</i> (TODO: add documentation link on category based bulk configuration).
        /// </summary>
        Disabled = 5,

        /// <summary>
        /// <b>Disabled by default rule</b>, which is a candidate for <b>deprecation</b>.
        /// </summary>
        CandidateForRemoval = 6,

    }
}

#endif
