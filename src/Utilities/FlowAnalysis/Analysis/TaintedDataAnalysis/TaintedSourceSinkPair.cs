// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Microsoft.CodeAnalysis.Operations;

    /// <summary>
    /// Represents tainted data from a source ending up in a sink.
    /// </summary>
    internal class TaintedSourceSinkPair
    {
        /// <summary>
        /// The source of the tainted data.
        /// </summary>
        public IInvocationOperation Source { get; private set; }

        /// <summary>
        /// Where the tainted data can potentially end up.
        /// </summary>
        public IInvocationOperation Sink { get; private set; }
    }
}
