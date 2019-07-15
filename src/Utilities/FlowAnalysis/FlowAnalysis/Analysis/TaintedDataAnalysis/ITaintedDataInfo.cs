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
        /// Indicates that the type is an interface, rather than a concrete type.
        /// </summary>
        bool IsInterface { get; }

        /// <summary>
        /// Indicates that this info uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        bool RequiresValueContentAnalysis { get; }
    }
}
