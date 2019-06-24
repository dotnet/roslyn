// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Info for tainted data sources, which generate tainted data.
    /// </summary>
    internal class SourceInfo : ITaintedDataInfo, IEquatable<SourceInfo>
    {
        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="fullTypeName">Full type name of the...type (namespace + type).</param>
        /// 
        /// <param name="taintedProperties">Properties that generate tainted data.</param>
        /// <param name="taintedMethods">Methods that generate tainted data.</param>
        public SourceInfo(string fullTypeName, bool isInterface, ImmutableHashSet<string> taintedProperties, ImmutableHashSet<string> taintedMethods, bool taintArray)
        {
            FullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
            IsInterface = isInterface;
            TaintedProperties = taintedProperties ?? throw new ArgumentNullException(nameof(taintedProperties));
            TaintedMethods = taintedMethods ?? throw new ArgumentNullException(nameof(taintedMethods));
            TaintArray = taintArray;
        }

        /// <summary>
        /// Full type name of the...type (namespace + type).
        /// </summary>
        public string FullTypeName { get; }

        /// <summary>
        /// Indicates this type is an interface.
        /// </summary>
        public bool IsInterface { get; }

        /// <summary>
        /// Properties that generate tainted data.
        /// </summary>
        public ImmutableHashSet<string> TaintedProperties { get; }

        /// <summary>
        /// Methods that generate tainted data.
        /// </summary>
        public ImmutableHashSet<string> TaintedMethods { get; }

        /// <summary>
        /// Indicates array of this type generates tainted data.
        /// </summary>
        public bool TaintArray { get; }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.TaintArray.GetHashCode(),
                HashUtilities.Combine(this.TaintedProperties,
                HashUtilities.Combine(this.TaintedMethods,
                HashUtilities.Combine(this.IsInterface.GetHashCode(),
                    StringComparer.Ordinal.GetHashCode(this.FullTypeName)))));
        }

        public override bool Equals(object obj)
        {
            return obj is SourceInfo other ? this.Equals(other) : false;
        }

        public bool Equals(SourceInfo other)
        {
            return other != null
                && this.FullTypeName == other.FullTypeName
                && this.IsInterface == other.IsInterface
                && this.TaintedProperties == other.TaintedProperties
                && this.TaintedMethods == other.TaintedMethods
                && this.TaintArray == other.TaintArray;
        }
    }
}
