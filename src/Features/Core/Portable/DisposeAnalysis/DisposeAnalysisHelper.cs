// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.DisposeAnalysis
{
    /// <summary>
    /// Helper for DisposeAnalysis.
    /// </summary>
    internal sealed class DisposeAnalysisHelper
    {
        private static readonly string[] s_disposeOwnershipTransferLikelyTypes = new string[]
            {
                "System.IO.Stream",
                "System.IO.TextReader",
                "System.IO.TextWriter",
                "System.Resources.IResourceReader",
            };
        private static readonly ImmutableHashSet<OperationKind> s_DisposableCreationKinds = ImmutableHashSet.Create(
            OperationKind.ObjectCreation,
            OperationKind.TypeParameterObjectCreation,
            OperationKind.DynamicObjectCreation,
            OperationKind.Invocation);

        private readonly ImmutableHashSet<INamedTypeSymbol> _disposeOwnershipTransferLikelyTypes;
        private ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>> _lazyDisposableFieldsMap;
        public INamedTypeSymbol IDisposableType { get; }
        public INamedTypeSymbol TaskType { get; }

        private DisposeAnalysisHelper(INamedTypeSymbol disposableType, INamedTypeSymbol taskType, ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes)
        {
            IDisposableType = disposableType;
            TaskType = taskType;
            _disposeOwnershipTransferLikelyTypes = disposeOwnershipTransferLikelyTypes;
        }

        public static bool TryCreate(Compilation compilation, out DisposeAnalysisHelper disposeHelper)
        {
            var disposableType = compilation.SystemIDisposableType();
            if (disposableType == null)
            {
                disposeHelper = null;
                return false;
            }

            var taskType = compilation.TaskType();
            var disposeOwnershipTransferLikelyTypes = GetDisposeOwnershipTransferLikelyTypes(compilation);
            disposeHelper = new DisposeAnalysisHelper(disposableType, taskType, disposeOwnershipTransferLikelyTypes);
            return true;
        }

        private static ImmutableHashSet<INamedTypeSymbol> GetDisposeOwnershipTransferLikelyTypes(Compilation compilation)
        {
            var builder = PooledHashSet<INamedTypeSymbol>.GetInstance();
            try
            {
                foreach (var typeName in s_disposeOwnershipTransferLikelyTypes)
                {
                    var typeSymbol = compilation.GetTypeByMetadataName(typeName);
                    if (typeSymbol != null)
                    {
                        builder.Add(typeSymbol);
                    }
                }

                return builder.ToImmutableHashSet();
            }
            finally
            {
                builder.Free();
            }
        }

        private void EnsureDisposableFieldsMap()
        {
            if (_lazyDisposableFieldsMap == null)
            {
                Interlocked.CompareExchange(ref _lazyDisposableFieldsMap, new ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>>(), null);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryGetOrComputeResult(
            OperationBlockAnalysisContext context,
            IMethodSymbol containingMethod,
            DiagnosticDescriptor rule,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool trackInstanceFields,
            out DisposeAnalysisResult disposeAnalysisResult,
            out PointsToAnalysisResult pointsToAnalysisResult,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt = null)
        {
            // Compute the dispose analysis result - skip Attribute blocks (OperationKind.None)
            foreach (var operationBlock in context.OperationBlocks.Where(o => o.Kind != OperationKind.None))
            {
                var cfg = context.GetControlFlowGraph(operationBlock);
                if (cfg != null)
                {
                    var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
                    disposeAnalysisResult = FlowAnalysis.DataFlow.DisposeAnalysis.DisposeAnalysis.TryGetOrComputeResult(cfg, containingMethod, wellKnownTypeProvider,
                        context.Options, rule, _disposeOwnershipTransferLikelyTypes, trackInstanceFields,
                        exceptionPathsAnalysis: false, context.CancellationToken, out pointsToAnalysisResult,
                        interproceduralAnalysisKind,
                        interproceduralAnalysisPredicateOpt: interproceduralAnalysisPredicateOpt,
                        defaultDisposeOwnershipTransferAtConstructor: true,
                        defaultDisposeOwnershipTransferAtMethodCall: true);
                    if (disposeAnalysisResult != null)
                    {
                        return true;
                    }
                }
            }

            disposeAnalysisResult = null;
            pointsToAnalysisResult = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryGetOrComputeResult(
            OperationBlockStartAnalysisContext context,
            IMethodSymbol containingMethod,
            DiagnosticDescriptor rule,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool trackInstanceFields,
            out DisposeAnalysisResult disposeAnalysisResult,
            out PointsToAnalysisResult pointsToAnalysisResult,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt = null)
        {
            // Compute the dispose analysis result - skip Attribute blocks (OperationKind.None)
            foreach (var operationBlock in context.OperationBlocks.Where(o => o.Kind != OperationKind.None))
            {
                var cfg = context.GetControlFlowGraph(operationBlock);
                if (cfg != null)
                {
                    var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
                    disposeAnalysisResult = FlowAnalysis.DataFlow.DisposeAnalysis.DisposeAnalysis.TryGetOrComputeResult(cfg, containingMethod, wellKnownTypeProvider,
                        context.Options, rule, _disposeOwnershipTransferLikelyTypes, trackInstanceFields,
                        exceptionPathsAnalysis: false, context.CancellationToken, out pointsToAnalysisResult,
                        interproceduralAnalysisKind,
                        interproceduralAnalysisPredicateOpt: interproceduralAnalysisPredicateOpt,
                        defaultDisposeOwnershipTransferAtConstructor: true,
                        defaultDisposeOwnershipTransferAtMethodCall: true);
                    if (disposeAnalysisResult != null)
                    {
                        return true;
                    }
                }
            }

            disposeAnalysisResult = null;
            pointsToAnalysisResult = null;
            return false;
        }

        private bool HasDisposableOwnershipTransferForConstructorParameter(IMethodSymbol containingMethod) =>
            containingMethod.MethodKind == MethodKind.Constructor &&
            containingMethod.Parameters.Any(p => _disposeOwnershipTransferLikelyTypes.Contains(p.Type));

        private bool IsDisposableCreation(IOperation operation)
            => (s_DisposableCreationKinds.Contains(operation.Kind) ||
                operation is
        {
            Parent: IArgumentOperation { Parameter: { RefKind: RefKind.Out } } argument
        }) &&
               operation.Type?.IsDisposable(IDisposableType) == true;

        public bool HasAnyDisposableCreationDescendant(ImmutableArray<IOperation> operationBlocks, IMethodSymbol containingMethod)
        {
            return operationBlocks.HasAnyOperationDescendant(IsDisposableCreation) ||
                HasDisposableOwnershipTransferForConstructorParameter(containingMethod);
        }

        public ImmutableHashSet<IFieldSymbol> GetDisposableFields(INamedTypeSymbol namedType)
        {
            EnsureDisposableFieldsMap();
            if (_lazyDisposableFieldsMap.TryGetValue(namedType, out var disposableFields))
            {
                return disposableFields;
            }

            if (!namedType.IsDisposable(IDisposableType))
            {
                disposableFields = ImmutableHashSet<IFieldSymbol>.Empty;
            }
            else
            {
                disposableFields = namedType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.Type.IsDisposable(IDisposableType) && !f.Type.InheritsFromOrEquals(TaskType))
                    .ToImmutableHashSet();
            }

            return _lazyDisposableFieldsMap.GetOrAdd(namedType, disposableFields);
        }

        /// <summary>
        /// Returns true if the given <paramref name="location"/> was created for an allocation in the <paramref name="containingMethod"/>
        /// or represents a location created for a constructor parameter whose type indicates dispose ownership transfer.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsDisposableCreationOrDisposeOwnershipTransfer(AbstractLocation location, IMethodSymbol containingMethod)
        {
            if (location.CreationOpt == null)
            {
                return location.SymbolOpt?.Kind == SymbolKind.Parameter &&
                    HasDisposableOwnershipTransferForConstructorParameter(containingMethod);
            }

            return IsDisposableCreation(location.CreationOpt);
        }

        /// <summary>
        /// Checks if the given method implements <see cref="IDisposable.Dispose"/> or overrides an implementation of <see cref="IDisposable.Dispose"/>.
        /// </summary>
        private bool IsDisposeImplementation(IMethodSymbol method)
        {
            if (method == null)
            {
                return false;
            }

            if (method.IsOverride)
            {
                return IsDisposeImplementation(method.OverriddenMethod);
            }

            // Identify the implementor of IDisposable.Dispose in the given method's containing type and check
            // if it is the given method.
            return method.ReturnsVoid &&
                method.Parameters.Length == 0 &&
                IsImplementationOfInterfaceMethod(method, typeArgument: null, IDisposableType, nameof(IDisposable.Dispose));
        }

        /// <summary>
        /// Returns true if this method is any Dispose method responsible for disposing the disposable fields
        /// of a disposable named type. For example, "void Dispose()", "void Dispose(bool)", "Task DisposeAsync()", etc.
        /// </summary>
        public bool IsAnyDisposeMethod(IMethodSymbol method)
        {
            if (!method.ContainingType.IsDisposable(IDisposableType))
            {
                return false;
            }

            return IsDisposeImplementation(method) ||
                (Equals(method.ContainingType, IDisposableType) && HasDisposeMethodSignature(method)) ||
                HasDisposeBoolMethodSignature(method) ||
                HasDisposeAsyncMethodSignature(method) ||
                HasOverriddenDisposeCoreAsyncMethodSignature(method) ||
                HasDisposeCloseMethodSignature(method);
        }

        /// <summary>
        /// Checks if the given method has the signature "void Dispose()".
        /// </summary>
        private static bool HasDisposeMethodSignature(IMethodSymbol method)
        {
            return method.Name == nameof(IDisposable.Dispose) && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method has the signature "void Dispose(bool)".
        /// </summary>
        public static bool HasDisposeBoolMethodSignature(IMethodSymbol method)
        {
            if (method.Name == nameof(IDisposable.Dispose) && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.Length == 1)
            {
                var parameter = method.Parameters[0];
                return parameter.Type != null &&
                    parameter.Type.SpecialType == SpecialType.System_Boolean &&
                    parameter.RefKind == RefKind.None;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given method has the signature "void Close()".
        /// </summary>
        private static bool HasDisposeCloseMethodSignature(IMethodSymbol method)
        {
            return method.Name == "Close" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method has the signature "Task DisposeAsync()".
        /// </summary>
        private bool HasDisposeAsyncMethodSignature(IMethodSymbol method)
        {
            return method.Name == "DisposeAsync" &&
                method.MethodKind == MethodKind.Ordinary &&
                method.ReturnType.Equals(TaskType) &&
                method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method has the signature "override Task DisposeCoreAsync(bool)".
        /// </summary>
        private bool HasOverriddenDisposeCoreAsyncMethodSignature(IMethodSymbol method)
        {
            return method.Name == "DisposeCoreAsync" &&
                method.MethodKind == MethodKind.Ordinary &&
                method.IsOverride &&
                method.ReturnType.Equals(TaskType) &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }

        /// <summary>
        /// Checks if the given method is an implementation of the given interface method 
        /// Substituted with the given typeargument.
        /// </summary>
        private static bool IsImplementationOfInterfaceMethod(IMethodSymbol method, ITypeSymbol typeArgument, INamedTypeSymbol interfaceType, string interfaceMethodName)
        {
            var constructedInterface = typeArgument != null ? interfaceType?.Construct(typeArgument) : interfaceType;

            return constructedInterface?.GetMembers(interfaceMethodName).Single() is IMethodSymbol interfaceMethod && method.Equals(method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod));
        }
    }
}
