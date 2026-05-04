// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Represents an abstract index into a location.
    /// It is used by an <see cref="AnalysisEntity"/> for operations such as an <see cref="Operations.IArrayElementReferenceOperation"/>, index access <see cref="Operations.IPropertyReferenceOperation"/>, etc.
    /// </summary>
    public abstract partial class AbstractIndex : CacheBasedEquatable<AbstractIndex>
    {
        public static AbstractIndex Create(int index) => new ConstantValueIndex(index);
        public static AbstractIndex Create(AnalysisEntity analysisEntity) => new AnalysisEntityBasedIndex(analysisEntity);
        public static AbstractIndex Create(IOperation operation) => new OperationBasedIndex(operation);

        internal bool IsConstant() => this is ConstantValueIndex;
    }
}
