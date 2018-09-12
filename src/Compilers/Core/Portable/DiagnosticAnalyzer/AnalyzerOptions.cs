// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Options passed to <see cref="DiagnosticAnalyzer"/>.
    /// </summary>
    public class AnalyzerOptions
    {
        internal static readonly AnalyzerOptions Empty = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);

        /// <summary>
        /// A set of additional non-code text files that can be used by analyzers.
        /// </summary>
        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

        /// <summary>
        /// A set of options keyed to <see cref="SyntaxTree"/> or <see cref="AdditionalText"/>.
        /// </summary>
        public AnalyzerConfigOptionsProvider AnalyzerConfigOptionsProvider { get; }

        /// <summary>
        /// Creates analyzer options to be passed to <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        /// <param name="additionalFiles">A set of additional non-code text files that can be used by analyzers.</param>
        /// <param name="optionsProvider">A set of per-tree options that can be used by analyzers.</param>
        public AnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles, AnalyzerConfigOptionsProvider optionsProvider)
        {
            if (optionsProvider is null)
            {
                throw new ArgumentNullException(nameof(optionsProvider));
            }

            AdditionalFiles = additionalFiles.NullToEmpty();
            AnalyzerConfigOptionsProvider = optionsProvider;
        }

        /// <summary>
        /// Creates analyzer options to be passed to <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        /// <param name="additionalFiles">A set of additional non-code text files that can be used by analyzers.</param>
        public AnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles)
            : this(additionalFiles, CompilerAnalyzerConfigOptionsProvider.Empty)
        { }

        /// <summary>
        /// Returns analyzer options with the given <paramref name="additionalFiles"/>.
        /// </summary>
        public AnalyzerOptions WithAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles)
        {
            if (this.AdditionalFiles == additionalFiles)
            {
                return this;
            }

            return new AnalyzerOptions(additionalFiles);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as AnalyzerOptions;
            return other != null &&
                (this.AdditionalFiles == other.AdditionalFiles ||
                this.AdditionalFiles.SequenceEqual(other.AdditionalFiles, ReferenceEquals));
        }

        public override int GetHashCode()
        {
            return Hash.CombineValues(this.AdditionalFiles);
        }
    }
}
