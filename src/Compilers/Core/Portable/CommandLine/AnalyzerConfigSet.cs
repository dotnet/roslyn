// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.AnalyzerConfig;
using AnalyzerOptions = System.Collections.Immutable.ImmutableDictionary<string, string>;
using TreeOptions = System.Collections.Immutable.ImmutableDictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic>;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of <see cref="AnalyzerConfig"/>, and can compute the effective analyzer options for a given source file. This is used to
    /// collect all the <see cref="AnalyzerConfig"/> files for that would apply to a compilation.
    /// </summary>
    public sealed class AnalyzerConfigSet
    {
        /// <summary>
        /// The list of <see cref="AnalyzerConfig" />s in this set. This list has been sorted per <see cref="AnalyzerConfig.DirectoryLengthComparer"/>.
        /// </summary>
        private readonly ImmutableArray<AnalyzerConfig> _analyzerConfigs;

        /// <summary>
        /// <see cref="SectionNameMatcher"/>s for each section. The entries in the outer array correspond to entries in <see cref="_analyzerConfigs"/>, and each inner array
        /// corresponds to each <see cref="AnalyzerConfig.NamedSections"/>.
        /// </summary>
        private readonly ImmutableArray<ImmutableArray<SectionNameMatcher?>> _analyzerMatchers;

        private readonly static DiagnosticDescriptor InvalidAnalyzerConfigSeverityDescriptor
            = new DiagnosticDescriptor(
                "InvalidSeverityInAnalyzerConfig",
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig_Title,
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig,
                "AnalyzerConfig",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static AnalyzerConfigSet Create<TList>(TList analyzerConfigs) where TList : IReadOnlyCollection<AnalyzerConfig>
        {
            var sortedAnalyzerConfigs = ArrayBuilder<AnalyzerConfig>.GetInstance(analyzerConfigs.Count);
            sortedAnalyzerConfigs.AddRange(analyzerConfigs);
            sortedAnalyzerConfigs.Sort(AnalyzerConfig.DirectoryLengthComparer);

            return new AnalyzerConfigSet(sortedAnalyzerConfigs.ToImmutableAndFree());
        }

        private AnalyzerConfigSet(ImmutableArray<AnalyzerConfig> analyzerConfigs)
        {
            _analyzerConfigs = analyzerConfigs;

            var allMatchers = ArrayBuilder<ImmutableArray<SectionNameMatcher?>>.GetInstance(_analyzerConfigs.Length);

            foreach (var config in _analyzerConfigs)
            {
                // Create an array of regexes with each entry corresponding to the same index
                // in <see cref="EditorConfig.NamedSections"/>.
                var builder = ArrayBuilder<SectionNameMatcher?>.GetInstance(config.NamedSections.Length);
                foreach (var section in config.NamedSections)
                {
                    SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher(section.Name);
                    builder.Add(matcher);
                }

                Debug.Assert(builder.Count == config.NamedSections.Length);

                allMatchers.Add(builder.ToImmutableAndFree());
            }

            Debug.Assert(allMatchers.Count == _analyzerConfigs.Length);

            _analyzerMatchers = allMatchers.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns a <see cref="AnalyzerConfigOptionsResult"/> for a source file. This computes which <see cref="AnalyzerConfig"/> rules applies to this file, and correctly applies
        /// precedence rules if there are multiple rules for the same file.
        /// </summary>
        /// <param name="sourcePath">The path to a file such as a source file or additional file. Must be non-null.</param>
        public AnalyzerConfigOptionsResult GetOptionsForSourcePath(string sourcePath)
        {
            if (sourcePath == null)
            {
                throw new System.ArgumentNullException(nameof(sourcePath));
            }

            var treeOptionsBuilder = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>(
                CaseInsensitiveComparison.Comparer);
            var analyzerOptionsBuilder = ImmutableDictionary.CreateBuilder<string, string>(
                CaseInsensitiveComparison.Comparer);
            var diagnosticBuilder = ArrayBuilder<Diagnostic>.GetInstance();

            var normalizedPath = PathUtilities.NormalizeWithForwardSlash(sourcePath);

            // The editorconfig paths are sorted from shortest to longest, so matches
            // are resolved from most nested to least nested, where last setting wins
            for (int analyzerConfigIndex = 0; analyzerConfigIndex < _analyzerConfigs.Length; analyzerConfigIndex++)
            {
                var config = _analyzerConfigs[analyzerConfigIndex];

                if (normalizedPath.StartsWith(config.NormalizedDirectory, StringComparison.Ordinal))
                {
                    // If this config is a root config, then clear earlier options since they don't apply
                    // to this source file.
                    if (config.IsRoot)
                    {
                        analyzerOptionsBuilder.Clear();
                        treeOptionsBuilder.Clear();
                        diagnosticBuilder.Clear();
                    }

                    int dirLength = config.NormalizedDirectory.Length;
                    // Leave '/' if the normalized directory ends with a '/'. This can happen if
                    // we're in a root directory (e.g. '/' or 'Z:/'). The section matching
                    // always expects that the relative path start with a '/'. 
                    if (config.NormalizedDirectory[dirLength - 1] == '/')
                    {
                        dirLength--;
                    }
                    string relativePath = normalizedPath.Substring(dirLength);

                    ImmutableArray<SectionNameMatcher?> matchers = _analyzerMatchers[analyzerConfigIndex];
                    for (int sectionIndex = 0; sectionIndex < matchers.Length; sectionIndex++)
                    {
                        if (matchers[sectionIndex]?.IsMatch(relativePath) == true)
                        {
                            var section = config.NamedSections[sectionIndex];
                            addOptions(section, treeOptionsBuilder, analyzerOptionsBuilder, diagnosticBuilder, config.PathToFile);
                        }
                    }
                }
            }

            return new AnalyzerConfigOptionsResult(
                treeOptionsBuilder.Count > 0 ? treeOptionsBuilder.ToImmutable() : SyntaxTree.EmptyDiagnosticOptions,
                analyzerOptionsBuilder.Count > 0 ? analyzerOptionsBuilder.ToImmutable() : AnalyzerConfigOptions.EmptyDictionary,
                diagnosticBuilder.ToImmutableAndFree());

            static void addOptions(
                AnalyzerConfig.Section section,
                TreeOptions.Builder treeBuilder,
                AnalyzerOptions.Builder analyzerBuilder,
                ArrayBuilder<Diagnostic> diagnosticBuilder,
                string analyzerConfigPath)
            {
                const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
                const string DiagnosticOptionSuffix = ".severity";

                foreach (var (key, value) in section.Properties)
                {
                    // Keys are lowercased in editorconfig parsing
                    int diagIdLength = -1;
                    if (key.StartsWith(DiagnosticOptionPrefix, StringComparison.Ordinal) &&
                        key.EndsWith(DiagnosticOptionSuffix, StringComparison.Ordinal))
                    {
                        diagIdLength = key.Length - (DiagnosticOptionPrefix.Length + DiagnosticOptionSuffix.Length);
                    }

                    if (diagIdLength >= 0)
                    {
                        var diagId = key.Substring(
                            DiagnosticOptionPrefix.Length,
                            diagIdLength);

                        ReportDiagnostic? severity;
                        var comparer = StringComparer.OrdinalIgnoreCase;
                        if (comparer.Equals(value, "default"))
                        {
                            severity = ReportDiagnostic.Default;
                        }
                        else if (comparer.Equals(value, "error"))
                        {
                            severity = ReportDiagnostic.Error;
                        }
                        else if (comparer.Equals(value, "warning"))
                        {
                            severity = ReportDiagnostic.Warn;
                        }
                        else if (comparer.Equals(value, "suggestion"))
                        {
                            severity = ReportDiagnostic.Info;
                        }
                        else if (comparer.Equals(value, "silent") || comparer.Equals(value, "refactoring"))
                        {
                            severity = ReportDiagnostic.Hidden;
                        }
                        else if (comparer.Equals(value, "none"))
                        {
                            severity = ReportDiagnostic.Suppress;
                        }
                        else
                        {
                            severity = null;
                            diagnosticBuilder.Add(Diagnostic.Create(
                                InvalidAnalyzerConfigSeverityDescriptor,
                                Location.None,
                                diagId,
                                value,
                                analyzerConfigPath));
                        }

                        if (severity.HasValue)
                        {
                            treeBuilder[diagId] = severity.GetValueOrDefault();
                        }
                    }
                    else
                    {
                        analyzerBuilder[key] = value;
                    }
                }
            }
        }
    }
}
