// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of rules as specified in a ruleset file.
    /// </summary>
    public class RuleSet
    {
        private readonly string _filePath;
        /// <summary>
        /// The file path of the ruleset file.
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }

        private readonly ReportDiagnostic _generalDiagnosticOption;
        /// <summary>
        /// The global option specified by the IncludeAll tag.
        /// </summary>
        public ReportDiagnostic GeneralDiagnosticOption
        {
            get { return _generalDiagnosticOption; }
        }

        private readonly ImmutableDictionary<string, ReportDiagnostic> _specificDiagnosticOptions;
        /// <summary>
        /// Individual rule ids and their associated actions.
        /// </summary>
        public ImmutableDictionary<string, ReportDiagnostic> SpecificDiagnosticOptions
        {
            get { return _specificDiagnosticOptions; }
        }

        private readonly ImmutableArray<RuleSetInclude> _includes;
        /// <summary>
        /// List of rulesets included by this ruleset.
        /// </summary>
        public ImmutableArray<RuleSetInclude> Includes
        {
            get { return _includes; }
        }

        /// <summary>
        /// Create a RuleSet.
        /// </summary>
        public RuleSet(string filePath, ReportDiagnostic generalOption, ImmutableDictionary<string, ReportDiagnostic> specificOptions, ImmutableArray<RuleSetInclude> includes)
        {
            _filePath = filePath;
            _generalDiagnosticOption = generalOption;
            _specificDiagnosticOptions = specificOptions == null ? ImmutableDictionary<string, ReportDiagnostic>.Empty : specificOptions;
            _includes = includes.NullToEmpty();
        }

        /// <summary>
        /// Create a RuleSet with a global effective action applied on it.
        /// </summary>
        public RuleSet WithEffectiveAction(ReportDiagnostic action)
        {
            if (!_includes.IsEmpty)
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
                case ReportDiagnostic.Hidden:
                    var generalOption = _generalDiagnosticOption == ReportDiagnostic.Default ? ReportDiagnostic.Default : action;
                    var specificOptions = _specificDiagnosticOptions.ToBuilder();
                    foreach (var item in _specificDiagnosticOptions)
                    {
                        if (item.Value != ReportDiagnostic.Suppress && item.Value != ReportDiagnostic.Default)
                        {
                            specificOptions[item.Key] = action;
                        }
                    }
                    return new RuleSet(FilePath, generalOption, specificOptions.ToImmutable(), _includes);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the effective ruleset after resolving all the included rulesets.
        /// </summary>
        private RuleSet GetEffectiveRuleSet(HashSet<string> includedRulesetPaths)
        {
            var effectiveGeneralOption = _generalDiagnosticOption;
            var effectiveSpecificOptions = new Dictionary<string, ReportDiagnostic>();

            // If we don't have any include then there's nothing to resolve.
            if (_includes.IsEmpty)
            {
                return this;
            }

            foreach (var ruleSetInclude in _includes)
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

                // If the ruleset has already been included then just ignore it.
                if (includedRulesetPaths.Contains(ruleSet.FilePath.ToLowerInvariant()))
                {
                    continue;
                }

                includedRulesetPaths.Add(ruleSet.FilePath.ToLowerInvariant());

                // Recursively get the effective ruleset of the included file, in case they in turn
                // contain includes.
                var effectiveRuleset = ruleSet.GetEffectiveRuleSet(includedRulesetPaths);

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
                    if (effectiveSpecificOptions.TryGetValue(item.Key, out var value))
                    {
                        if (IsStricterThan(item.Value, value))
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
            foreach (var item in _specificDiagnosticOptions)
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

            return new RuleSet(_filePath, effectiveGeneralOption, effectiveSpecificOptions.ToImmutableDictionary(), ImmutableArray<RuleSetInclude>.Empty);
        }

        /// <summary>
        /// Get all the files involved in resolving this ruleset.
        /// </summary>
        private ImmutableArray<string> GetEffectiveIncludes()
        {
            var arrayBuilder = ImmutableArray.CreateBuilder<string>();

            GetEffectiveIncludesCore(arrayBuilder);

            return arrayBuilder.ToImmutable();
        }

        private void GetEffectiveIncludesCore(ImmutableArray<string>.Builder arrayBuilder)
        {
            arrayBuilder.Add(this.FilePath);

            foreach (var ruleSetInclude in _includes)
            {
                var ruleSet = ruleSetInclude.LoadRuleSet(this);

                // If we couldn't load the ruleset file, then there's nothing to do.
                if (ruleSet == null)
                {
                    continue;
                }

                // If this file has already been included don't recurse into it.
                if (!arrayBuilder.Contains(ruleSet.FilePath, StringComparer.OrdinalIgnoreCase))
                {
                    ruleSet.GetEffectiveIncludesCore(arrayBuilder);
                }
            }
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
                    return action1 == ReportDiagnostic.Warn || action1 == ReportDiagnostic.Error || action1 == ReportDiagnostic.Info || action1 == ReportDiagnostic.Hidden;
                case ReportDiagnostic.Hidden:
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
        /// Load the ruleset from the specified file. This ruleset will contain
        /// all the rules resolved from the includes specified in the ruleset file
        /// as well. See also: <seealso cref="GetEffectiveIncludesFromFile(string)" />.
        /// </summary>
        /// <returns>
        /// A ruleset that contains resolved rules or null if there were errors.
        /// </returns>
        public static RuleSet LoadEffectiveRuleSetFromFile(string filePath)
        {
            var ruleSet = RuleSetProcessor.LoadFromFile(filePath);
            if (ruleSet != null)
            {
                return ruleSet.GetEffectiveRuleSet(new HashSet<string>());
            }

            return null;
        }

        /// <summary>
        /// Get the paths to all files contributing rules to the ruleset from the specified file.
        /// See also: <seealso cref="LoadEffectiveRuleSetFromFile(string)" />.
        /// </summary>
        /// <returns>
        /// The full paths to included files, or an empty array if there were errors.
        /// </returns>
        public static ImmutableArray<string> GetEffectiveIncludesFromFile(string filePath)
        {
            var ruleSet = RuleSetProcessor.LoadFromFile(filePath);
            if (ruleSet != null)
            {
                return ruleSet.GetEffectiveIncludes();
            }

            return ImmutableArray<string>.Empty;
        }

        /// <summary>
        /// Parses the ruleset file at the given <paramref name="rulesetFileFullPath"/> and returns the following diagnostic options from the parsed file:
        /// 1) A map of <paramref name="specificDiagnosticOptions"/> from rule ID to <see cref="ReportDiagnostic"/> option.
        /// 2) A global <see cref="ReportDiagnostic"/> option for all rules in the ruleset file.
        /// </summary>
        public static ReportDiagnostic GetDiagnosticOptionsFromRulesetFile(string rulesetFileFullPath, out Dictionary<string, ReportDiagnostic> specificDiagnosticOptions)
        {
            return GetDiagnosticOptionsFromRulesetFile(rulesetFileFullPath, out specificDiagnosticOptions, null, null);
        }

        internal static ReportDiagnostic GetDiagnosticOptionsFromRulesetFile(string rulesetFileFullPath, out Dictionary<string, ReportDiagnostic> diagnosticOptions, IList<Diagnostic> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            diagnosticOptions = new Dictionary<string, ReportDiagnostic>();
            if (rulesetFileFullPath == null)
            {
                return ReportDiagnostic.Default;
            }

            return GetDiagnosticOptionsFromRulesetFile(diagnosticOptions, rulesetFileFullPath, diagnosticsOpt, messageProviderOpt);
        }

        private static ReportDiagnostic GetDiagnosticOptionsFromRulesetFile(Dictionary<string, ReportDiagnostic> diagnosticOptions, string resolvedPath, IList<Diagnostic> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            Debug.Assert(resolvedPath != null);

            var generalDiagnosticOption = ReportDiagnostic.Default;
            try
            {
                var ruleSet = RuleSet.LoadEffectiveRuleSetFromFile(resolvedPath);
                generalDiagnosticOption = ruleSet.GeneralDiagnosticOption;
                foreach (var rule in ruleSet.SpecificDiagnosticOptions)
                {
                    diagnosticOptions.Add(rule.Key, rule.Value);
                }
            }
            catch (InvalidRuleSetException e)
            {
                if (diagnosticsOpt != null && messageProviderOpt != null)
                {
                    diagnosticsOpt.Add(Diagnostic.Create(messageProviderOpt, messageProviderOpt.ERR_CantReadRulesetFile, resolvedPath, e.Message));
                }
            }
            catch (IOException e)
            {
                if (e is FileNotFoundException || e.GetType().Name == "DirectoryNotFoundException")
                {
                    if (diagnosticsOpt != null && messageProviderOpt != null)
                    {
                        diagnosticsOpt.Add(Diagnostic.Create(messageProviderOpt, messageProviderOpt.ERR_CantReadRulesetFile, resolvedPath, new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.FileNotFound))));
                    }
                }
                else
                {
                    if (diagnosticsOpt != null && messageProviderOpt != null)
                    {
                        diagnosticsOpt.Add(Diagnostic.Create(messageProviderOpt, messageProviderOpt.ERR_CantReadRulesetFile, resolvedPath, e.Message));
                    }
                }
            }

            return generalDiagnosticOption;
        }
    }
}
