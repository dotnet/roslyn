// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class AnalyzerOptions
    {
        /// <summary>
        /// A set of additional non-code streams that can be used by analyzers.
        /// </summary>
        public ImmutableArray<AdditionalStream> AdditionalStreams { get; internal set; }

        /// <summary>
        /// A set of global options for analyzers.
        /// </summary>
        public ImmutableDictionary<string, string> GlobalOptions { get; internal set; }

        public AnalyzerOptions(IEnumerable<AdditionalStream> additionalStreams, IDictionary<string, string> globalOptions)
        {
            this.AdditionalStreams = additionalStreams.ToImmutableArray();
            this.GlobalOptions = globalOptions.ToImmutableDictionary();
        }
    }
}
