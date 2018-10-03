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
        public SinkInfo(string fullTypeName, bool isInterface, bool isAnyStringParameterInConstructorASink, ImmutableHashSet<string> sinkProperties, ImmutableDictionary<string, ImmutableHashSet<string>> sinkMethodParameters)
        {
            FullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
            IsInterface = isInterface;
            IsAnyStringParameterInConstructorASink = isAnyStringParameterInConstructorASink;
            SinkProperties = sinkProperties ?? throw new ArgumentNullException(nameof(sinkProperties));
            SinkMethodParameters = sinkMethodParameters ?? throw new ArgumentNullException(nameof(sinkMethodParameters));
        }

        /// <summary>
        /// Full name of the type that can lead to sinks.
        /// </summary>
        public string FullTypeName { get; }

        /// <summary>
        /// Indicates this type is an interface.
        /// </summary>
        public bool IsInterface { get; }

        /// <summary>
        /// Indicates that any string parameter in the constructor is a sink.
        /// </summary>
        public bool IsAnyStringParameterInConstructorASink { get; }

        /// <summary>
        /// Set of properties on the type that are sinks.
        /// </summary>
        public ImmutableHashSet<string> SinkProperties { get; }
        
        /// <summary>
        /// Mapping of method name to parameter names that are sinks.
        /// </summary>
        public ImmutableDictionary<string, ImmutableHashSet<string>> SinkMethodParameters { get; }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.SinkProperties,
                HashUtilities.Combine(this.SinkMethodParameters,
                HashUtilities.Combine(this.FullTypeName.GetHashCode(),
                HashUtilities.Combine(this.IsInterface.GetHashCode(),
                this.IsAnyStringParameterInConstructorASink.GetHashCode()))));
        }

        public override bool Equals(object obj)
        {
            SinkInfo other = obj as SinkInfo;
            return other != null ? this.Equals(other) : false;
        }

        public bool Equals(SinkInfo other)
        {
            return other != null
                && this.FullTypeName == other.FullTypeName
                && this.IsInterface == other.IsInterface
                && this.IsAnyStringParameterInConstructorASink == other.IsAnyStringParameterInConstructorASink
                && this.SinkProperties == other.SinkProperties
                && this.SinkMethodParameters == other.SinkMethodParameters;
        }
    }
}
