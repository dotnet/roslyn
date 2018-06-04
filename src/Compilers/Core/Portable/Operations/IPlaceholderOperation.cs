// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal enum PlaceholderKind
    {
        Unspecified = 0,
        SwitchOperationExpression = 1,
        ForToLoopBinaryOperatorLeftOperand = 2,
        ForToLoopBinaryOperatorRightOperand = 3,
        AggregationGroup = 4,
    }

    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal interface IPlaceholderOperation : IOperation // https://github.com/dotnet/roslyn/issues/21294
    {
        PlaceholderKind PlaceholderKind { get; }
    }
}

