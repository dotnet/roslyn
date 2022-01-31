// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Operations;

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not a comparable instance.")]
    internal readonly struct INegatedPatternOperationWrapper : IOperationWrapper
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Operations.INegatedPatternOperation";
        private static readonly Type? WrappedType = OperationWrapperHelper.GetWrappedType(typeof(INegatedPatternOperationWrapper));

        private static readonly Func<IOperation, IPatternOperation> PatternAccessor = LightupHelpers.CreateOperationPropertyAccessor<IOperation, IPatternOperation>(WrappedType, nameof(Pattern), fallbackResult: null!);

        private INegatedPatternOperationWrapper(IOperation operation)
        {
            WrappedOperation = operation;
        }

        public IOperation WrappedOperation { get; }
        public ITypeSymbol? Type => this.WrappedOperation.Type;
        public IPatternOperation Pattern => PatternAccessor(WrappedOperation);

        public static INegatedPatternOperationWrapper FromOperation(IOperation operation)
        {
            if (operation == null)
            {
                return default;
            }

            if (!IsInstance(operation))
            {
                throw new InvalidCastException($"Cannot cast '{operation.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new INegatedPatternOperationWrapper(operation);
        }

        public static bool IsInstance(IOperation operation)
        {
            return operation != null && LightupHelpers.CanWrapOperation(operation, WrappedType);
        }
    }
}

#endif
