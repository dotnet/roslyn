//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//namespace Analyzer.Utilities.Lightup
//{
//    using System;
//    using System.Collections.Immutable;
//    using System.Diagnostics.CodeAnalysis;
//    using Microsoft.CodeAnalysis;

//    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not a comparable instance.")]
//    internal readonly struct ICollectionExpressionOperationWrapper : IOperationWrapper
//    {
//        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Operations.ICollectionExpressionOperation";
//        private static readonly Type? WrappedType = OperationWrapperHelper.GetWrappedType(typeof(ICollectionExpressionOperationWrapper));

//        private static readonly Func<IOperation, ImmutableArray<IOperation>> ElementsAccessor = LightupHelpers.CreateOperationPropertyAccessor<IOperation, ImmutableArray<IOperation>>(WrappedType, nameof(Elements), fallbackResult: default);

//        private ICollectionExpressionOperationWrapper(IOperation operation)
//        {
//            WrappedOperation = operation;
//        }

//        public IOperation WrappedOperation { get; }
//        public ITypeSymbol? Type => WrappedOperation.Type;
//        public ImmutableArray<IOperation> Elements => ElementsAccessor(WrappedOperation);

//        public static ICollectionExpressionOperationWrapper FromOperation(IOperation operation)
//        {
//            if (operation == null)
//            {
//                return default;
//            }

//            if (!IsInstance(operation))
//            {
//                throw new InvalidCastException($"Cannot cast '{operation.GetType().FullName}' to '{WrappedTypeName}'");
//            }

//            return new ICollectionExpressionOperationWrapper(operation);
//        }

//        public static bool IsInstance(IOperation operation)
//        {
//            return operation != null && LightupHelpers.CanWrapOperation(operation, WrappedType);
//        }
//    }
//}

//#endif
