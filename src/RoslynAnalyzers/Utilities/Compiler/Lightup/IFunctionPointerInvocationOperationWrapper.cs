// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Operations;

    //[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not a comparable instance.")]
    //internal readonly struct IFunctionPointerInvocationOperationWrapper : IOperationWrapper
    //{
    //    internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Operations.IFunctionPointerInvocationOperation";
    //    private static readonly Type? WrappedType = OperationWrapperHelper.GetWrappedType(typeof(IFunctionPointerInvocationOperationWrapper));

    //    private static readonly Func<IOperation, ImmutableArray<IArgumentOperation>> ArgumentsAccessor = LightupHelpers.CreateOperationPropertyAccessor<IOperation, ImmutableArray<IArgumentOperation>>(WrappedType, nameof(Arguments), fallbackResult: ImmutableArray<IArgumentOperation>.Empty);
    //    private static readonly Func<IOperation, IOperation> TargetAccessor = LightupHelpers.CreateOperationPropertyAccessor<IOperation, IOperation>(WrappedType, nameof(Target), fallbackResult: null!);

    //    private static readonly Func<IOperation, IMethodSymbol> GetFunctionPointerSignatureAccessor = CreateFunctionPointerSignatureAccessor(WrappedType);

    //    private static Func<IOperation, IMethodSymbol> CreateFunctionPointerSignatureAccessor(Type? wrappedType)
    //    {
    //        if (wrappedType == null)
    //        {
    //            return op => null!;
    //        }

    //        var targetMethod = typeof(OperationExtensions).GetTypeInfo().GetDeclaredMethod("GetFunctionPointerSignature");

    //        if (targetMethod is null)
    //        {
    //            return op => null!;
    //        }

    //        var operation = Expression.Variable(typeof(IOperation));

    //        return Expression.Lambda<Func<IOperation, IMethodSymbol>>(Expression.Call(targetMethod, Expression.Convert(operation, wrappedType)), operation).Compile();
    //    }

    //    private IFunctionPointerInvocationOperationWrapper(IOperation operation)
    //    {
    //        WrappedOperation = operation;
    //    }

    //    public IOperation WrappedOperation { get; }
    //    public ITypeSymbol? Type => WrappedOperation.Type;
    //    public ImmutableArray<IArgumentOperation> Arguments => ArgumentsAccessor(WrappedOperation);
    //    public IOperation Target => TargetAccessor(WrappedOperation);

    //    public IMethodSymbol GetFunctionPointerSignature() => GetFunctionPointerSignatureAccessor(WrappedOperation);

    //    public static IFunctionPointerInvocationOperationWrapper FromOperation(IOperation operation)
    //    {
    //        if (operation == null)
    //        {
    //            return default;
    //        }

    //        if (!IsInstance(operation))
    //        {
    //            throw new InvalidCastException($"Cannot cast '{operation.GetType().FullName}' to '{WrappedTypeName}'");
    //        }

    //        return new IFunctionPointerInvocationOperationWrapper(operation);
    //    }

    //    public static bool IsInstance(IOperation operation)
    //    {
    //        return operation != null && LightupHelpers.CanWrapOperation(operation, WrappedType);
    //    }
    //}
}

#endif
