// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Helper for DisposeAnalysis.
    /// </summary>
    internal sealed class DisposeAnalysisHelper
    {
        private static readonly string[] s_disposeOwnershipTransferLikelyTypes =
            [
                "System.IO.Stream",
                "System.IO.TextReader",
                "System.IO.TextWriter",
                "System.Resources.IResourceReader",
            ];
        private static readonly BoundedCacheWithFactory<Compilation, DisposeAnalysisHelper> s_DisposeHelperCache = new();

        private static readonly ImmutableHashSet<OperationKind> s_DisposableCreationKinds = ImmutableHashSet.Create(
            OperationKind.ObjectCreation,
            OperationKind.TypeParameterObjectCreation,
            OperationKind.DynamicObjectCreation,
            OperationKind.Invocation);

        private readonly WellKnownTypeProvider _wellKnownTypeProvider;
        private readonly ImmutableHashSet<INamedTypeSymbol> _disposeOwnershipTransferLikelyTypes;
        private ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>>? _lazyDisposableFieldsMap;
        public INamedTypeSymbol? IDisposable { get; }
        public INamedTypeSymbol? IAsyncDisposable { get; }
        public INamedTypeSymbol? ConfiguredAsyncDisposable { get; }
        public INamedTypeSymbol? Task { get; }
        public INamedTypeSymbol? ValueTask { get; }
        public INamedTypeSymbol? ConfiguredValueTaskAwaitable { get; }
        public INamedTypeSymbol? StringReader { get; }
        public INamedTypeSymbol? MemoryStream { get; }

        private DisposeAnalysisHelper(Compilation compilation)
        {
            _wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);

            IDisposable = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            IAsyncDisposable = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIAsyncDisposable);
            ConfiguredAsyncDisposable = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredAsyncDisposable);
            Task = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);
            ValueTask = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksValueTask);
            ConfiguredValueTaskAwaitable = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredValueTaskAwaitable);
            StringReader = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOStringReader);
            MemoryStream = _wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOMemoryStream);

            _disposeOwnershipTransferLikelyTypes = IDisposable != null ?
                GetDisposeOwnershipTransferLikelyTypes(compilation) :
                ImmutableHashSet<INamedTypeSymbol>.Empty;
        }

        private static ImmutableHashSet<INamedTypeSymbol> GetDisposeOwnershipTransferLikelyTypes(Compilation compilation)
        {
            var builder = PooledHashSet<INamedTypeSymbol>.GetInstance();
            foreach (var typeName in s_disposeOwnershipTransferLikelyTypes)
            {
                INamedTypeSymbol? typeSymbol = compilation.GetOrCreateTypeByMetadataName(typeName);
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

        public static bool TryGetOrCreate(Compilation compilation, [NotNullWhen(returnValue: true)] out DisposeAnalysisHelper? disposeHelper)
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
                => new(compilation);
        }

        public static Func<ITypeSymbol?, bool> GetIsDisposableDelegate(Compilation compilation)
        {
            if (TryGetOrCreate(compilation, out var disposeAnalysisHelper))
            {
                return disposeAnalysisHelper.IsDisposable;
            }

            return _ => false;
        }

        public bool TryGetOrComputeResult(
            ImmutableArray<IOperation> operationBlocks,
            IMethodSymbol containingMethod,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            PointsToAnalysisKind defaultPointsToAnalysisKind,
            bool trackInstanceFields,
            bool trackExceptionPaths,
            [NotNullWhen(returnValue: true)] out DisposeAnalysisResult? disposeAnalysisResult,
            [NotNullWhen(returnValue: true)] out PointsToAnalysisResult? pointsToAnalysisResult,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null,
            bool defaultDisposeOwnershipTransferAtConstructor = false)
        {
            var cfg = operationBlocks.GetControlFlowGraph();
            if (cfg != null && IDisposable != null)
            {
                disposeAnalysisResult = DisposeAnalysis.TryGetOrComputeResult(cfg, containingMethod, _wellKnownTypeProvider,
                    analyzerOptions, rule, _disposeOwnershipTransferLikelyTypes, defaultPointsToAnalysisKind, trackInstanceFields,
                    trackExceptionPaths, out pointsToAnalysisResult, interproceduralAnalysisPredicate: interproceduralAnalysisPredicate,
                    defaultDisposeOwnershipTransferAtConstructor: defaultDisposeOwnershipTransferAtConstructor);
                if (disposeAnalysisResult != null)
                {
                    RoslynDebug.Assert(pointsToAnalysisResult is object);
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
                operation.Parent is IArgumentOperation argument && argument.Parameter?.RefKind == RefKind.Out) &&
               IsDisposable(operation.Type);

        public bool HasAnyDisposableCreationDescendant(ImmutableArray<IOperation> operationBlocks, IMethodSymbol containingMethod)
        {
            return operationBlocks.HasAnyOperationDescendant(IsDisposableCreation) ||
                HasDisposableOwnershipTransferForConstructorParameter(containingMethod);
        }

        public bool IsDisposableTypeNotRequiringToBeDisposed(ITypeSymbol typeSymbol) =>
            // Common case doesn't require dispose. https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.dispose
            typeSymbol.DerivesFrom(Task, baseTypesOnly: true) ||
            // StringReader doesn't need to be disposed: https://learn.microsoft.com/dotnet/api/system.io.stringreader
            SymbolEqualityComparer.Default.Equals(typeSymbol, StringReader) ||
            // MemoryStream doesn't need to be disposed. https://learn.microsoft.com/dotnet/api/system.io.memorystream
            // Subclasses *might* need to be disposed, but that is the less common case,
            // and the common case is a huge source of noisy warnings.
            SymbolEqualityComparer.Default.Equals(typeSymbol, MemoryStream);

        public ImmutableHashSet<IFieldSymbol> GetDisposableFields(INamedTypeSymbol namedType)
        {
            EnsureDisposableFieldsMap();
            RoslynDebug.Assert(_lazyDisposableFieldsMap != null);

            if (_lazyDisposableFieldsMap.TryGetValue(namedType, out ImmutableHashSet<IFieldSymbol> disposableFields))
            {
                return disposableFields;
            }

            if (!namedType.IsDisposable(IDisposable, IAsyncDisposable, ConfiguredAsyncDisposable))
            {
                disposableFields = ImmutableHashSet<IFieldSymbol>.Empty;
            }
            else
            {
                disposableFields = namedType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => IsDisposable(f.Type) && !IsDisposableTypeNotRequiringToBeDisposed(f.Type))
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
            if (location.Creation == null)
            {
                return location.Symbol?.Kind == SymbolKind.Parameter &&
                    HasDisposableOwnershipTransferForConstructorParameter(containingMethod);
            }

            return IsDisposableCreation(location.Creation);
        }

        public bool IsDisposable([NotNullWhen(returnValue: true)] ITypeSymbol? type)
            => type != null && type.IsDisposable(IDisposable, IAsyncDisposable, ConfiguredAsyncDisposable);

        public DisposeMethodKind GetDisposeMethodKind(IMethodSymbol method)
            => method.GetDisposeMethodKind(IDisposable, IAsyncDisposable, ConfiguredAsyncDisposable, Task, ValueTask, ConfiguredValueTaskAwaitable);
    }
}
