// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of rules as specified in a rulset file.
    /// </summary>
    internal class RuleSet
    {
        private readonly string filePath;
        /// <summary>
        /// The file path of the ruleset file.
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
        }

        private readonly ReportDiagnostic generalDiagnosticOption;
        /// <summary>
        /// The global option specified by the IncludeAll tag.
        /// </summary>
        public ReportDiagnostic GeneralDiagnosticOption
        {
            get { return generalDiagnosticOption; }
        }

        private readonly ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions;
        /// <summary>
        /// Individual ruleids and their associated actions.
        /// </summary>
        public ImmutableDictionary<string, ReportDiagnostic> SpecificDiagnosticOptions
        {
            get { return specificDiagnosticOptions; }
        }

        private readonly ImmutableArray<RuleSetInclude> includes;
        /// <summary>
        /// List of rulesets included by this ruleset.
        /// </summary>
        public ImmutableArray<RuleSetInclude> Includes
        {
            get { return includes; }
        }

        /// <summary>
        /// Create a RuleSet.
        /// </summary>
        public RuleSet(string filePath, ReportDiagnostic generalOption, IDictionary<string, ReportDiagnostic> specificOptions, IEnumerable<RuleSetInclude> includes)
        {
            this.filePath = filePath;
            this.generalDiagnosticOption = generalOption;
            this.specificDiagnosticOptions = specificOptions == null ? ImmutableDictionary<string, ReportDiagnostic>.Empty : specificOptions.ToImmutableDictionary();
            this.includes = includes == null ? ImmutableArray<RuleSetInclude>.Empty : includes.ToImmutableArray();
        }

        /// <summary>
        /// Create a RuleSet with a global effective action applied on it.
        /// </summary>
        public RuleSet WithEffectiveAction(ReportDiagnostic action)
        {
            if (!includes.IsEmpty)
            {
                throw new ArgumentException("Effective action cannot be applied to rulesets with Includes");
            }

            switch (action)
            {
                case ReportDiagnostic.Default:
                    return this;
                case ReportDiagnostic.Suppress:
                    return null;
                case ReportDiagnostic.Error:
                case ReportDiagnostic.Warn:
                case ReportDiagnostic.Info:
                    var generalOption = generalDiagnosticOption == ReportDiagnostic.Default ? ReportDiagnostic.Default : action;
                    var specificOptions = specificDiagnosticOptions.ToBuilder();
                    foreach (var item in specificDiagnosticOptions)
                    {
                        if (item.Value != ReportDiagnostic.Suppress && item.Value != ReportDiagnostic.Default)
                        {
                            specificOptions[item.Key] = action;
                        }
                    }
                    return new RuleSet(FilePath, generalOption, specificOptions.ToImmutable(), includes);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the effective ruleset after resolving all the included rulesets.
        /// </summary>
        public RuleSet GetEffectiveRuleSet()
        {
            var effectiveGeneralOption = generalDiagnosticOption;
            var effectiveSpecificOptions = new Dictionary<string, ReportDiagnostic>();

            // If we don't have any include then there's nothing to resolve.
            if (includes.IsEmpty)
            {
                return this;
            }

            foreach (var ruleSetInclude in includes)
            {
                // If the include has been suppressed then there's nothing to do.
                if (ruleSetInclude.Action == ReportDiagnostic.Suppress)
                {
                    continue;
                }

                var ruleSet = ruleSetInclude.LoadRuleSet(this);

                // If we couldn't load the ruleset file, then there's nothing to do.
                if (ruleSet == null)
                {
                    continue;
                }

                // Recursively get the effective ruleset of the included file, in case they in turn
                // contain includes.
                var effectiveRuleset = ruleSet.GetEffectiveRuleSet();

                // Apply the includeAction on this ruleset.
                effectiveRuleset = effectiveRuleset.WithEffectiveAction(ruleSetInclude.Action);

                // If the included ruleset's global option is stricter, then make that the effective option.
                if (IsStricterThan(effectiveRuleset.GeneralDiagnosticOption, effectiveGeneralOption))
                {
                    effectiveGeneralOption = effectiveRuleset.GeneralDiagnosticOption;
                }

                // Copy every rule in the ruleset and change the action if there's a stricter one.
                foreach (var item in effectiveRuleset.SpecificDiagnosticOptions)
                {
                    if (effectiveSpecificOptions.ContainsKey(item.Key))
                    {
                        if (IsStricterThan(item.Value, effectiveSpecificOptions[item.Key]))
                        {
                            effectiveSpecificOptions[item.Key] = item.Value;
                        }
                    }
                    else
                    {
                        effectiveSpecificOptions.Add(item.Key, item.Value);
                    }
                }
            }

            // Finally, copy all the rules in the current ruleset. This overrides the actions
            // of any included ruleset - therefore, no strictness check.
            foreach (var item in specificDiagnosticOptions)
            {
                if (effectiveSpecificOptions.ContainsKey(item.Key))
                {
                    effectiveSpecificOptions[item.Key] = item.Value;
                }
                else
                {
                    effectiveSpecificOptions.Add(item.Key, item.Value);
                }
            }

            return new RuleSet(filePath, effectiveGeneralOption, effectiveSpecificOptions.ToImmutableDictionary(), null);
        }

        /// <summary>
        /// Returns true if the action1 is stricter than action2.
        /// </summary>
        private static bool IsStricterThan(ReportDiagnostic action1, ReportDiagnostic action2)
        {
            switch (action2)
            {
                case ReportDiagnostic.Suppress:
                    return true;
                case ReportDiagnostic.Default:
                    return action1 == ReportDiagnostic.Warn || action1 == ReportDiagnostic.Error || action1 == ReportDiagnostic.Info;
                case ReportDiagnostic.Info:
                    return action1 == ReportDiagnostic.Warn || action1 == ReportDiagnostic.Error;
                case ReportDiagnostic.Warn:
                    return action1 == ReportDiagnostic.Error;
                case ReportDiagnostic.Error:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Load the rulset from the specified file. This ruleset will contain
        /// all the rules resolved from the includes specified in the ruleset file
        /// as well.
        /// </summary>
        /// <returns>
        /// A ruleset that contains resolved rules or null if there were errors.
        /// </returns>
        public static RuleSet LoadEffectiveRuleSetFromFile(string filePath)
        {
            var ruleSet = RuleSetProcessor.LoadFromFile(filePath);
            if (ruleSet != null)
            {
                return ruleSet.GetEffectiveRuleSet();
            }

            return null;
        }
    }
}
