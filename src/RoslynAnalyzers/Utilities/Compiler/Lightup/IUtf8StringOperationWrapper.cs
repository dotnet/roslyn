// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not a comparable instance.")]
    internal readonly struct IUtf8StringOperationWrapper : IOperationWrapper
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Operations.IUtf8StringOperation";
        private static readonly Type? WrappedType = OperationWrapperHelper.GetWrappedType(typeof(IUtf8StringOperationWrapper));

        private static readonly Func<IOperation, string> ValueAccessor = LightupHelpers.CreateOperationPropertyAccessor<IOperation, string>(WrappedType, nameof(Value), fallbackResult: null!);

        private IUtf8StringOperationWrapper(IOperation operation)
        {
            WrappedOperation = operation;
        }

        public IOperation WrappedOperation { get; }
        public ITypeSymbol? Type => WrappedOperation.Type;
        public string Value => ValueAccessor(WrappedOperation);

        public static IUtf8StringOperationWrapper FromOperation(IOperation operation)
        {
            if (operation == null)
            {
                return default;
            }

            if (!IsInstance(operation))
            {
                throw new InvalidCastException($"Cannot cast '{operation.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new IUtf8StringOperationWrapper(operation);
        }

        public static bool IsInstance(IOperation operation)
        {
            return operation != null && LightupHelpers.CanWrapOperation(operation, WrappedType);
        }
    }
}

#endif
