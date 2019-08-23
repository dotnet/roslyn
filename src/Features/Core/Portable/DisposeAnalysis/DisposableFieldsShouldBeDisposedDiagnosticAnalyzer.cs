// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.DisposeAnalysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class DisposableFieldsShouldBeDisposedDiagnosticAnalyzer
        : AbstractBuiltInCodeQualityDiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_disposableFieldsShouldBeDisposedRule = CreateDescriptor(
            IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId,
            title: new LocalizableResourceString(nameof(FeaturesResources.Disposable_fields_should_be_disposed), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            messageFormat: new LocalizableResourceString(nameof(FeaturesResources.Disposable_field_0_is_never_disposed), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            description: new LocalizableResourceString(nameof(FeaturesResources.DisposableFieldsShouldBeDisposedDescription), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: false);

        public DisposableFieldsShouldBeDisposedDiagnosticAnalyzer()
            : base(ImmutableArray.Create(s_disposableFieldsShouldBeDisposedRule), GeneratedCodeAnalysisFlags.Analyze)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (!DisposeAnalysisHelper.TryCreate(compilationContext.Compilation, out var disposeAnalysisHelper))
                {
                    return;
                }

                // Register a symbol start action to analyze all named types.
                compilationContext.RegisterSymbolStartAction(
                    symbolStartContext => SymbolAnalyzer.OnSymbolStart(symbolStartContext, disposeAnalysisHelper),
                    SymbolKind.NamedType);
            });
        }

        private sealed class SymbolAnalyzer
        {
            private readonly ImmutableHashSet<IFieldSymbol> _disposableFields;
            private readonly ConcurrentDictionary<IFieldSymbol, /*disposed*/bool> _fieldDisposeValueMap;
            private readonly DisposeAnalysisHelper _disposeAnalysisHelper;
            private bool _hasErrors;
            private bool _hasDisposeMethod;

            public SymbolAnalyzer(ImmutableHashSet<IFieldSymbol> disposableFields, DisposeAnalysisHelper disposeAnalysisHelper)
            {
                Debug.Assert(!disposableFields.IsEmpty);

                _disposableFields = disposableFields;
                _disposeAnalysisHelper = disposeAnalysisHelper;
                _fieldDisposeValueMap = new ConcurrentDictionary<IFieldSymbol, bool>();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void OnSymbolStart(SymbolStartAnalysisContext symbolStartContext, DisposeAnalysisHelper disposeAnalysisHelper)
            {
                // We only want to analyze types which are disposable (implement System.IDisposable directly or indirectly)
                // and have at least one disposable field.
                var namedType = (INamedTypeSymbol)symbolStartContext.Symbol;
                if (!namedType.IsDisposable(disposeAnalysisHelper.IDisposableType))
                {
                    return;
                }

                var disposableFields = disposeAnalysisHelper.GetDisposableFields(namedType);
                if (disposableFields.IsEmpty)
                {
                    return;
                }

                var analyzer = new SymbolAnalyzer(disposableFields, disposeAnalysisHelper);

                // Register an operation block action to analyze disposable assignments and dispose invocations for fields.
                symbolStartContext.RegisterOperationBlockStartAction(analyzer.OnOperationBlockStart);

                // Register symbol end action for containing type to report non-disposed fields.
                // We report fields that have disposable type (implement System.IDisposable directly or indirectly)
                // and were assigned a disposable object within this containing type, but were not disposed in
                // containing type's Dispose method.
                symbolStartContext.RegisterSymbolEndAction(analyzer.OnSymbolEnd);
            }

            private void AddOrUpdateFieldDisposedValue(IFieldSymbol field, bool disposed)
            {
                Debug.Assert(_disposableFields.Contains(field));
                Debug.Assert(!field.IsStatic);
                Debug.Assert(field.Type.IsDisposable(_disposeAnalysisHelper.IDisposableType));

                // Update the dispose value for the field.
                // Update value factory delegate ensures that fields for which we have
                // already seen dispose invocations, i.e. currentValue = true, continue to be marked as disposed.
                _fieldDisposeValueMap.AddOrUpdate(field,
                    addValue: disposed,
                    updateValueFactory: (f, currentValue) => currentValue || disposed);
            }

            private void OnSymbolEnd(SymbolAnalysisContext symbolEndContext)
            {
                if (_hasErrors || !_hasDisposeMethod)
                {
                    return;
                }

                foreach (var kvp in _fieldDisposeValueMap)
                {
                    var field = kvp.Key;
                    var disposed = kvp.Value;
                    if (!disposed)
                    {
                        // Disposable field '{0}' is never disposed
                        var diagnostic = Diagnostic.Create(s_disposableFieldsShouldBeDisposedRule, field.Locations[0], field.Name);
                        symbolEndContext.ReportDiagnostic(diagnostic);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void OnOperationBlockStart(OperationBlockStartAnalysisContext operationBlockStartContext)
            {
                if (_hasErrors)
                {
                    return;
                }

                operationBlockStartContext.RegisterOperationAction(_ => _hasErrors = true, OperationKind.Invalid);

                switch (operationBlockStartContext.OwningSymbol)
                {
                    case IFieldSymbol _:
                        // Field initializer.
                        if (operationBlockStartContext.OperationBlocks.Length == 1 &&
                            operationBlockStartContext.OperationBlocks[0] is IFieldInitializerOperation fieldInitializer)
                        {
                            foreach (var field in fieldInitializer.InitializedFields)
                            {
                                if (!field.IsStatic && _disposableFields.Contains(field))
                                {
                                    // Instance field initialized with a disposable object is considered a candidate.
                                    AddOrUpdateFieldDisposedValue(field, disposed: false);
                                }
                            }
                        }

                        break;

                    case IMethodSymbol containingMethod:
                        // Method body.
                        OnMethodOperationBlockStart(operationBlockStartContext, containingMethod);
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void OnMethodOperationBlockStart(OperationBlockStartAnalysisContext operationBlockStartContext, IMethodSymbol containingMethod)
            {
                // Shared PointsTo dataflow analysis result for all the callbacks to AnalyzeFieldReference
                // for this method's executable code.
                PointsToAnalysisResult lazyPointsToAnalysisResult = null;

                // If we have any potential disposable object creation descendant within the operation blocks,
                // register an operation action to analyze field references where field might be assigned a disposable object.
                if (_disposeAnalysisHelper.HasAnyDisposableCreationDescendant(operationBlockStartContext.OperationBlocks, containingMethod))
                {
                    operationBlockStartContext.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
                }

                // If this is a Dispose method, then analyze dispose invocations for fields within this method.
                if (_disposeAnalysisHelper.IsAnyDisposeMethod(containingMethod))
                {
                    AnalyzeDisposeMethod();
                }

                return;

                // Local function
                void AnalyzeFieldReference(OperationAnalysisContext operationContext)
                {
                    var fieldReference = (IFieldReferenceOperation)operationContext.Operation;
                    var field = fieldReference.Field;

                    // Check if this is a Disposable field that is not currently being tracked.
                    if (_fieldDisposeValueMap.ContainsKey(field) ||
                        !_disposableFields.Contains(field) ||
                        _hasErrors)
                    {
                        return;
                    }

                    // Only track instance fields on the current instance.
                    if (field.IsStatic || fieldReference.Instance?.Kind != OperationKind.InstanceReference)
                    {
                        return;
                    }

                    // We have a field reference for a disposable field.
                    // Check if it is being assigned a locally created disposable object.
                    // PERF: Do not perform interprocedural analysis for this detection.
                    if (fieldReference.Parent is ISimpleAssignmentOperation simpleAssignmentOperation &&
                        simpleAssignmentOperation.Target == fieldReference)
                    {
                        if (lazyPointsToAnalysisResult == null)
                        {
                            if (_disposeAnalysisHelper.TryGetOrComputeResult(
                                operationBlockStartContext, containingMethod,
                                s_disposableFieldsShouldBeDisposedRule,
                                InterproceduralAnalysisKind.None,
                                trackInstanceFields: false,
                                out _, out var pointsToAnalysisResult) &&
                                pointsToAnalysisResult != null)
                            {
                                Interlocked.CompareExchange(ref lazyPointsToAnalysisResult, pointsToAnalysisResult, null);
                            }
                            else
                            {
                                _hasErrors = true;
                                return;
                            }
                        }

                        var assignedPointsToValue = lazyPointsToAnalysisResult[simpleAssignmentOperation.Value.Kind, simpleAssignmentOperation.Value.Syntax];
                        foreach (var location in assignedPointsToValue.Locations)
                        {
                            if (_disposeAnalysisHelper.IsDisposableCreationOrDisposeOwnershipTransfer(location, containingMethod))
                            {
                                AddOrUpdateFieldDisposedValue(field, disposed: false);
                                break;
                            }
                        }
                    }
                }

                void AnalyzeDisposeMethod()
                {
                    _hasDisposeMethod = true;

                    if (_hasErrors)
                    {
                        return;
                    }

                    // Perform dataflow analysis to compute dispose value of disposable fields at the end of dispose method.
                    if (_disposeAnalysisHelper.TryGetOrComputeResult(operationBlockStartContext, containingMethod,
                        s_disposableFieldsShouldBeDisposedRule,
                        InterproceduralAnalysisKind.ContextSensitive,
                        trackInstanceFields: true,
                        disposeAnalysisResult: out var disposeAnalysisResult,
                        pointsToAnalysisResult: out var pointsToAnalysisResult))
                    {
                        var exitBlock = disposeAnalysisResult.ControlFlowGraph.GetExit();
                        foreach (var fieldWithPointsToValue in disposeAnalysisResult.TrackedInstanceFieldPointsToMap)
                        {
                            var field = fieldWithPointsToValue.Key;
                            var pointsToValue = fieldWithPointsToValue.Value;

                            if (!_disposableFields.Contains(field))
                            {
                                continue;
                            }

                            var disposeDataAtExit = disposeAnalysisResult.ExitBlockOutput.Data;
                            var disposed = false;
                            foreach (var location in pointsToValue.Locations)
                            {
                                if (disposeDataAtExit.TryGetValue(location, out var disposeValue))
                                {
                                    switch (disposeValue.Kind)
                                    {
                                        // For MaybeDisposed, conservatively mark the field as disposed as we don't support path sensitive analysis.
                                        case DisposeAbstractValueKind.MaybeDisposed:
                                        case DisposeAbstractValueKind.Unknown:
                                        case DisposeAbstractValueKind.Escaped:
                                        case DisposeAbstractValueKind.Disposed:
                                            disposed = true;
                                            AddOrUpdateFieldDisposedValue(field, disposed);
                                            break;
                                    }
                                }

                                if (disposed)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        _hasErrors = true;
                    }
                }
            }
        }
    }
}
