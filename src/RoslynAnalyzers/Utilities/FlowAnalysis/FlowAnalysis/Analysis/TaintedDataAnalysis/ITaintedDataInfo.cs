// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Interface for a tainted data source, sanitizer, or sink information for one concrete type or interface.
    /// </summary>
    internal interface ITaintedDataInfo
    {
        /// <summary>
        /// Qualified name of the type.
        /// </summary>
        string FullTypeName { get; }

        /// <summary>
        /// Qualified names of the optional dependency types.
        /// </summary>
        ImmutableArray<string> DependencyFullTypeNames { get; }

        /// <summary>
        /// Indicates that the type is an interface, rather than a concrete type.
        /// </summary>
        bool IsInterface { get; }

        /// <summary>
        /// Indicates that <see cref="OperationKind.ParameterReference"/> is required.
        /// </summary>
        bool RequiresParameterReferenceAnalysis { get; }

        /// <summary>
        /// Indicates that this info uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        bool RequiresValueContentAnalysis { get; }
    }
}
