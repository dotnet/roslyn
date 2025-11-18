// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Test.Utilities
{
    public static class Traits
    {
        public const string DataflowAnalysis = nameof(DataflowAnalysis);
#pragma warning disable CA1034 // Nested types should not be visible - test class matching the pattern used in Roslyn.
#pragma warning disable CA1724 // Type names should not match namespaces - test class matching the pattern used in Roslyn.
        public static class Dataflow
#pragma warning restore CA1724 // Type names should not match namespaces
#pragma warning restore CA1034 // Nested types should not be visible
        {
            public const string CopyAnalysis = nameof(CopyAnalysis);
            public const string NullAnalysis = nameof(NullAnalysis);
            public const string PointsToAnalysis = nameof(PointsToAnalysis);
            public const string ValueContentAnalysis = nameof(ValueContentAnalysis);
            public const string DisposeAnalysis = nameof(DisposeAnalysis);
            public const string ParameterValidationAnalysis = nameof(ParameterValidationAnalysis);
            public const string PropertySetAnalysis = nameof(PropertySetAnalysis);

            public const string PredicateAnalysis = nameof(PredicateAnalysis);

            public const string TaintedDataAnalysis = nameof(TaintedDataAnalysis);
        }
    }
}
