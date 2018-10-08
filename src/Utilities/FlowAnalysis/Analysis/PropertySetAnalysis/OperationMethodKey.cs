using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Operation and Method pair.
    /// </summary>
    /// <remarks>Used as a key to identify invocation operations, which may have different
    /// underlying invoked methods, cuz of delegates or something.</remarks>
    internal sealed partial class OperationMethodKey : IEquatable<OperationMethodKey>
    {
        /// <summary>
        /// A reasonable comparer for sorting.
        /// </summary>
        public static readonly IComparer<OperationMethodKey> Comparer = new ComparerImpl();

        public OperationMethodKey(IOperation operation, IMethodSymbol method)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Method = method ?? throw new ArgumentNullException(nameof(method));

            HashCode = HashUtilities.Combine(this.Operation.GetHashCode(), this.Method.GetHashCode());
        }

        /// <summary>
        /// Operation.
        /// </summary>
        public IOperation Operation { get; }

        /// <summary>
        /// Method.
        /// </summary>
        public IMethodSymbol Method { get; }

        public static int Compare(OperationMethodKey x, OperationMethodKey y)
        {
            int locationCompare = LocationComparer.Instance.Compare(
                x.Operation.Syntax.GetLocation(),
                y.Operation.Syntax.GetLocation());
            if (locationCompare != 0)
            {
                return locationCompare;
            }

            return String.CompareOrdinal(
                x.Method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                y.Method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        private int HashCode { get; }

        public override int GetHashCode()
        {
            return this.HashCode;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as OperationMethodKey);
        }

        public bool Equals(OperationMethodKey other)
        {
            return other != null
                && other.Operation == this.Operation
                && other.Method == this.Method;
        }
    }
}
