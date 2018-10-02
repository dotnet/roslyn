// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Info for tainted data sources.
    /// </summary>
    internal class SourceInfo
    {
        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="fullTypeName">Full type name of the...type (namespace + type).</param>
        /// <param name="taintedProperties">Properties that generate tainted data.</param>
        /// <param name="taintedMethods">Methods that generate tainted data.</param>
        public SourceInfo(string fullTypeName, ImmutableHashSet<string> taintedProperties, ImmutableHashSet<string> taintedMethods)
        {
            FullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
            TaintedProperties = taintedProperties ?? throw new ArgumentNullException(nameof(taintedProperties));
            TaintedMethods = taintedMethods ?? throw new ArgumentNullException(nameof(taintedMethods));
        }

        /// <summary>
        /// Full type name of the...type (namespace + type).
        /// </summary>
        public string FullTypeName { get; }

        /// <summary>
        /// Properties that generate tainted data.
        /// </summary>
        public ImmutableHashSet<string> TaintedProperties { get; }

        /// <summary>
        /// Methods that generate tainted data.
        /// </summary>
        public ImmutableHashSet<string> TaintedMethods { get; }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.TaintedProperties,
                HashUtilities.Combine(this.TaintedMethods,
                    this.FullTypeName.GetHashCode()));
        }

        public override bool Equals(object obj)
        {
            SourceInfo other = obj as SourceInfo;
            return other != null ? this.Equals(other) : false;
        }

        public bool Equals(SourceInfo other)
        {
            return this.FullTypeName == other.FullTypeName
                && this.TaintedProperties == other.TaintedProperties
                && this.TaintedMethods == other.TaintedMethods;
        }
    }
}
