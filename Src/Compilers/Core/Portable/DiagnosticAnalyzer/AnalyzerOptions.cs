// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Options passed to <see cref="DiagnosticAnalyzer"/>.
    /// </summary>
    public class AnalyzerOptions
    {
        internal static readonly AnalyzerOptions Empty = new AnalyzerOptions(ImmutableArray<AdditionalStream>.Empty, ImmutableDictionary<string, string>.Empty);

        /// <summary>
        /// A set of additional non-code streams that can be used by analyzers.
        /// </summary>
        public ImmutableArray<AdditionalStream> AdditionalStreams { get; internal set; }

        /// <summary>
        /// A set of global options for analyzers.
        /// </summary>
        public ImmutableDictionary<string, string> GlobalOptions { get; internal set; }

        /// <summary>
        /// CultureInfo to be used for localizing diagnostics.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Creates analyzer options to be passed to <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        /// <param name="additionalStreams">A set of additional non-code streams that can be used by analyzers.</param>
        /// <param name="globalOptions">A set of global options for analyzers.</param>
        /// <param name="culture">Optional CultureInfo to be used for localizing diagnostics.</param>
        public AnalyzerOptions(ImmutableArray<AdditionalStream> additionalStreams, ImmutableDictionary<string, string> globalOptions, CultureInfo culture = null)
        {
            this.AdditionalStreams = additionalStreams.IsDefault ? ImmutableArray<AdditionalStream>.Empty : additionalStreams;
            this.GlobalOptions = globalOptions ?? ImmutableDictionary<string, string>.Empty;
            this.Culture = culture ?? CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Returns analyzer options with the given additionalStreams.
        /// </summary>
        public AnalyzerOptions WithAdditionalStreams(ImmutableArray<AdditionalStream> additionalStreams)
        {
            if (this.AdditionalStreams == additionalStreams)
            {
                return this;
            }

            return new AnalyzerOptions(additionalStreams, this.GlobalOptions, this.Culture);
        }

        /// <summary>
        /// Returns analyzer options with the given globalOptions.
        /// </summary>
        public AnalyzerOptions WithGlobalOptions(ImmutableDictionary<string, string> globalOptions)
        {
            if (this.GlobalOptions == globalOptions)
            {
                return this;
            }

            return new AnalyzerOptions(this.AdditionalStreams, globalOptions, this.Culture);
        }

        /// <summary>
        /// Returns analyzer options with the given culture.
        /// </summary>
        public AnalyzerOptions WithCulture(CultureInfo culture)
        {
            if (this.Culture == culture)
            {
                return this;
            }

            return new AnalyzerOptions(this.AdditionalStreams, this.GlobalOptions, culture);
        }
    }
}
