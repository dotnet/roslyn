// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method.
    /// </summary>
    public abstract partial class OperationVisitor
    {
        // Make public after review: https://github.com/dotnet/roslyn/issues/21281
        internal virtual void VisitFixed(IFixedOperation operation) =>
            // https://github.com/dotnet/roslyn/issues/21281
            //DefaultVisit(operation);
            VisitNoneOperation(operation);
    }

    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method with an additional argument of the type specified by the
    /// <typeparamref name="TArgument"/> parameter and produces a value of the type specified by
    /// the <typeparamref name="TResult"/> parameter.
    /// </summary>
    /// <typeparam name="TArgument">
    /// The type of the additional argument passed to this visitor's Visit method.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of the return value of this visitor's Visit method.
    /// </typeparam>
    public abstract partial class OperationVisitor<TArgument, TResult>
    {
        // Make public after review: https://github.com/dotnet/roslyn/issues/21281
        internal virtual TResult VisitFixed(IFixedOperation operation, TArgument argument) =>
            // https://github.com/dotnet/roslyn/issues/21281
            //return DefaultVisit(operation, argument);
            VisitNoneOperation(operation, argument);
    }
}
