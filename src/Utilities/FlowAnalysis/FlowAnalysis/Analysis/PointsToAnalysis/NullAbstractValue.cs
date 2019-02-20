// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Abstract null value for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="PointsToAnalysis"/>.
    /// </summary>
    public enum NullAbstractValue
    {
        Invalid,
        Undefined,
        Null,
        NotNull,
        MaybeNull
    }
}
