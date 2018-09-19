// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Tainted data sink information for a type.
    /// </summary>
    /// <remarks>It's bad if tainted data reaches a sink.</remarks>
    internal sealed class SinkInfo
    {
        public SinkInfo(
            string fullTypeName, 
            bool isInterface, 
            bool isAnyStringParameterInConstructorASink, 
            ImmutableHashSet<string> sinkProperties, 
            ImmutableDictionary<string, ImmutableHashSet<string>> sinkMethodParameters)
        {
            this.FullTypeName = fullTypeName;
            this.IsInterface = isInterface;
            this.IsAnyStringParameterInConstructorASink = isAnyStringParameterInConstructorASink;
            this.SinkProperties = sinkProperties;
            this.SinkMethodParameters = sinkMethodParameters;
        }

        /// <summary>
        /// Full name of the type that can lead to sinks.
        /// </summary>
        public string FullTypeName { get; private set; }

        /// <summary>
        /// Indicates this type is an interface.
        /// </summary>
        public bool IsInterface { get; private set; }

        /// <summary>
        /// Indicates that any string parameter in the constructor is a sink.
        /// </summary>
        public bool IsAnyStringParameterInConstructorASink { get; private set; }

        /// <summary>
        /// Set of properties on the type that are sinks.
        /// </summary>
        public ImmutableHashSet<string> SinkProperties { get; private set; }
        
        /// <summary>
        /// Mapping of method name to parameter names that are sinks.
        /// </summary>
        public ImmutableDictionary<string, ImmutableHashSet<string>> SinkMethodParameters { get; private set; }
    }
}
