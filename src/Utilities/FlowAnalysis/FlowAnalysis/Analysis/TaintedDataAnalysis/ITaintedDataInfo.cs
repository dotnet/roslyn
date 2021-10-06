// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
