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
        internal static readonly AnalyzerOptions Empty = new AnalyzerOptions(ImmutableArray<AdditionalStream>.Empty);

        /// <summary>
        /// A set of additional non-code streams that can be used by analyzers.
        /// </summary>
        public ImmutableArray<AdditionalStream> AdditionalStreams { get; internal set; }

        /// <summary>
        /// Creates analyzer options to be passed to <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        /// <param name="additionalStreams">A set of additional non-code streams that can be used by analyzers.</param>
        public AnalyzerOptions(ImmutableArray<AdditionalStream> additionalStreams)
        {
            this.AdditionalStreams = additionalStreams.IsDefault ? ImmutableArray<AdditionalStream>.Empty : additionalStreams;
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

            return new AnalyzerOptions(additionalStreams);
        }
    }
}
