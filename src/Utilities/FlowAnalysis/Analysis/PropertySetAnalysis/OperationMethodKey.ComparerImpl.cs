using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    partial class OperationMethodKey
    {
        private class ComparerImpl : IComparer<OperationMethodKey>
        {
            public int Compare(OperationMethodKey x, OperationMethodKey y)
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
        }
    }
}
