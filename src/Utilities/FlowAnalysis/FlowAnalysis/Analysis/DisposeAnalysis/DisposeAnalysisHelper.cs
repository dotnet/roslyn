// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities
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
        private static readonly BoundedCacheWithFactory<Compilation, DisposeAnalysisHelper> s_DisposeHelperCache =
            new BoundedCacheWithFactory<Compilation, DisposeAnalysisHelper>();

        private static readonly ImmutableHashSet<OperationKind> s_DisposableCreationKinds = ImmutableHashSet.Create(
            OperationKind.ObjectCreation,
            OperationKind.TypeParameterObjectCreation,
            OperationKind.DynamicObjectCreation,
            OperationKind.Invocation);

        private readonly WellKnownTypeProvider _wellKnownTypeProvider;
        private readonly ImmutableHashSet<INamedTypeSymbol> _disposeOwnershipTransferLikelyTypes;
        private ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>> _lazyDisposableFieldsMap;
        public INamedTypeSymbol IDisposable => _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
        public INamedTypeSymbol Task => _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);

        private DisposeAnalysisHelper(Compilation compilation)
        {
            _wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            if (IDisposable != null)
            {
                _disposeOwnershipTransferLikelyTypes = GetDisposeOwnershipTransferLikelyTypes(compilation);
            }
        }

        private static ImmutableHashSet<INamedTypeSymbol> GetDisposeOwnershipTransferLikelyTypes(Compilation compilation)
        {
            var builder = PooledHashSet<INamedTypeSymbol>.GetInstance();
            foreach (var typeName in s_disposeOwnershipTransferLikelyTypes)
            {
                INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(typeName);
                if (typeSymbol != null)
                {
                    builder.Add(typeSymbol);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private void EnsureDisposableFieldsMap()
        {
            if (_lazyDisposableFieldsMap == null)
            {
                Interlocked.CompareExchange(ref _lazyDisposableFieldsMap, new ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>>(), null);
            }
        }

        public static bool TryGetOrCreate(Compilation compilation, out DisposeAnalysisHelper disposeHelper)
        {
            disposeHelper = s_DisposeHelperCache.GetOrCreateValue(compilation, CreateDisposeAnalysisHelper);
            if (disposeHelper.IDisposable == null)
            {
                disposeHelper = null;
                return false;
            }

            return true;

            // Local functions
            static DisposeAnalysisHelper CreateDisposeAnalysisHelper(Compilation compilation)
                => new DisposeAnalysisHelper(compilation);
        }

        public bool TryGetOrComputeResult(
            ImmutableArray<IOperation> operationBlocks,
            IMethodSymbol containingMethod,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            bool trackInstanceFields,
            bool trackExceptionPaths,
            CancellationToken cancellationToken,
            out DisposeAnalysisResult disposeAnalysisResult,
            out PointsToAnalysisResult pointsToAnalysisResult,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt = null,
            bool defaultDisposeOwnershipTransferAtConstructor = false)
        {
            var cfg = operationBlocks.GetControlFlowGraph();
            if (cfg != null)
            {
                disposeAnalysisResult = DisposeAnalysis.TryGetOrComputeResult(cfg, containingMethod, _wellKnownTypeProvider,
                    analyzerOptions, rule, _disposeOwnershipTransferLikelyTypes, trackInstanceFields,
                    trackExceptionPaths, cancellationToken, out pointsToAnalysisResult,
                    interproceduralAnalysisPredicateOpt: interproceduralAnalysisPredicateOpt,
                    defaultDisposeOwnershipTransferAtConstructor: defaultDisposeOwnershipTransferAtConstructor);
                if (disposeAnalysisResult != null)
                {
                    return true;
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
                operation.Parent is IArgumentOperation argument && argument.Parameter.RefKind == RefKind.Out) &&
               operation.Type?.IsDisposable(IDisposable) == true;

        public bool HasAnyDisposableCreationDescendant(ImmutableArray<IOperation> operationBlocks, IMethodSymbol containingMethod)
        {
            return operationBlocks.HasAnyOperationDescendant(IsDisposableCreation) ||
                HasDisposableOwnershipTransferForConstructorParameter(containingMethod);
        }

        public ImmutableHashSet<IFieldSymbol> GetDisposableFields(INamedTypeSymbol namedType)
        {
            EnsureDisposableFieldsMap();
            if (_lazyDisposableFieldsMap.TryGetValue(namedType, out ImmutableHashSet<IFieldSymbol> disposableFields))
            {
                return disposableFields;
            }

            if (!namedType.IsDisposable(IDisposable))
            {
                disposableFields = ImmutableHashSet<IFieldSymbol>.Empty;
            }
            else
            {
                disposableFields = namedType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.Type.IsDisposable(IDisposable) && !f.Type.DerivesFrom(_wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask)))
                    .ToImmutableHashSet();
            }

            return _lazyDisposableFieldsMap.GetOrAdd(namedType, disposableFields);
        }

        /// <summary>
        /// Returns true if the given <paramref name="location"/> was created for an allocation in the <paramref name="containingMethod"/>
        /// or represents a location created for a constructor parameter whose type indicates dispose ownership transfer.
        /// </summary>
        public bool IsDisposableCreationOrDisposeOwnershipTransfer(AbstractLocation location, IMethodSymbol containingMethod)
        {
            if (location.CreationOpt == null)
            {
                return location.SymbolOpt?.Kind == SymbolKind.Parameter &&
                    HasDisposableOwnershipTransferForConstructorParameter(containingMethod);
            }

            return IsDisposableCreation(location.CreationOpt);
        }
    }
}
