using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    class ConstructorMapper
    {
        public Func<IMethodSymbol, IReadOnlyList<ValueContentAbstractValue>, PropertySetAbstractValueKind[]> { get; }
        public PropertySetAbstractValueKind[] PropertyAbstractValues { get; }
    }
}
