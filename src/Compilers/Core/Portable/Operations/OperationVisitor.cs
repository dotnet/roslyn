// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    // Only remaining visitors in this class have nonstandard naming before the generator.
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
        public virtual void VisitUnaryOperator(IUnaryOperation operation) => DefaultVisit(operation);
        public virtual void VisitBinaryOperator(IBinaryOperation operation) => DefaultVisit(operation);
        public virtual void VisitTupleBinaryOperator(ITupleBinaryOperation operation) => DefaultVisit(operation);
        internal virtual void VisitRecursivePattern(IRecursivePatternOperation operation) => DefaultVisit(operation);
        internal virtual void VisitPropertySubpattern(IPropertySubpatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitMethodBodyOperation(IMethodBodyOperation operation) => DefaultVisit(operation);
        public virtual void VisitConstructorBodyOperation(IConstructorBodyOperation operation) => DefaultVisit(operation);
        public virtual void VisitDiscardOperation(IDiscardOperation operation) => DefaultVisit(operation);
        public virtual void VisitRangeOperation(IRangeOperation operation) => DefaultVisit(operation);
    }

    // Only remaining visitors in this class have nonstandard naming before the generator.
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
        public virtual TResult VisitUnaryOperator(IUnaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitBinaryOperator(IBinaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTupleBinaryOperator(ITupleBinaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitRecursivePattern(IRecursivePatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitPropertySubpattern(IPropertySubpatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitMethodBodyOperation(IMethodBodyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConstructorBodyOperation(IConstructorBodyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDiscardOperation(IDiscardOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitRangeOperation(IRangeOperation operation, TArgument argument) => DefaultVisit(operation, argument);
    }
}
