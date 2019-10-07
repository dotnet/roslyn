// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.DisposeAnalysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class DisposeObjectsBeforeLosingScopeDiagnosticAnalyzer
        : AbstractCodeQualityDiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _disposeObjectsBeforeLosingScopeRule;
        private readonly DiagnosticDescriptor _useRecommendedDisposePatternRule;

        public DisposeObjectsBeforeLosingScopeDiagnosticAnalyzer()
           : this(isEnabledByDefault: false)
        {
        }

        // internal for test purposes.
        internal DisposeObjectsBeforeLosingScopeDiagnosticAnalyzer(bool isEnabledByDefault)
            : this(CreateDisposeObjectsBeforeLosingScopeRule(isEnabledByDefault), CreateUseRecommendedDisposePatternRule(isEnabledByDefault))
        {
        }

        public DisposeObjectsBeforeLosingScopeDiagnosticAnalyzer(DiagnosticDescriptor disposeObjectsBeforeLosingScopeRule, DiagnosticDescriptor useRecommendedDisposePatternRule)
            : base(ImmutableArray.Create(disposeObjectsBeforeLosingScopeRule, useRecommendedDisposePatternRule), GeneratedCodeAnalysisFlags.None)
        {
            _disposeObjectsBeforeLosingScopeRule = disposeObjectsBeforeLosingScopeRule;
            _useRecommendedDisposePatternRule = useRecommendedDisposePatternRule;
        }

        private static DiagnosticDescriptor CreateDisposeObjectsBeforeLosingScopeRule(bool isEnabledByDefault)
            => CreateDescriptor(
                IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId,
                title: new LocalizableResourceString(nameof(FeaturesResources.Dispose_objects_before_losing_scope), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                messageFormat: new LocalizableResourceString(nameof(FeaturesResources.Disposable_object_created_by_0_is_never_disposed), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                description: new LocalizableResourceString(nameof(FeaturesResources.UseRecommendedDisposePatternDescription), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                isUnneccessary: false,
                isEnabledByDefault: isEnabledByDefault);

        private static DiagnosticDescriptor CreateUseRecommendedDisposePatternRule(bool isEnabledByDefault)
            => CreateDescriptor(
                IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId,
                title: new LocalizableResourceString(nameof(FeaturesResources.Use_recommended_dispose_pattern), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                messageFormat: new LocalizableResourceString(nameof(FeaturesResources.Use_recommended_dispose_pattern_to_ensure_that_object_created_by_0_is_disposed_on_all_paths_using_statement_declaration_or_try_finally), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                description: new LocalizableResourceString(nameof(FeaturesResources.UseRecommendedDisposePatternDescription), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                isUnneccessary: false,
                isEnabledByDefault: isEnabledByDefault);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (!DisposeAnalysisHelper.TryCreate(compilationContext.Compilation, out var disposeAnalysisHelper))
                {
                    return;
                }

                // Avoid reporting duplicate diagnostics from interprocedural analysis.
                var reportedLocations = new ConcurrentDictionary<Location, bool>();

                compilationContext.RegisterOperationBlockAction(
                    operationBlockContext => AnalyzeOperationBlock(operationBlockContext, disposeAnalysisHelper, reportedLocations));
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AnalyzeOperationBlock(
            OperationBlockAnalysisContext operationBlockContext,
            DisposeAnalysisHelper disposeAnalysisHelper,
            ConcurrentDictionary<Location, bool> reportedLocations)
        {
            // We are only intersted in analyzing method bodies that have at least one disposable object creation.
            if (!(operationBlockContext.OwningSymbol is IMethodSymbol containingMethod) ||
                !disposeAnalysisHelper.HasAnyDisposableCreationDescendant(operationBlockContext.OperationBlocks, containingMethod))
            {
                return;
            }

            PerformFlowAnalysisOnOperationBlock(operationBlockContext, disposeAnalysisHelper, reportedLocations, containingMethod);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PerformFlowAnalysisOnOperationBlock(
            OperationBlockAnalysisContext operationBlockContext,
            DisposeAnalysisHelper disposeAnalysisHelper,
            ConcurrentDictionary<Location, bool> reportedLocations,
            IMethodSymbol containingMethod)
        {
            // We can skip interprocedural analysis for certain invocations.
            var interproceduralAnalysisPredicateOpt = new InterproceduralAnalysisPredicate(
                skipAnalysisForInvokedMethodPredicateOpt: SkipInterproceduralAnalysis,
                skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt: null,
                skipAnalysisForInvokedContextPredicateOpt: null);

            // Compute dispose dataflow analysis result for the operation block.
            if (disposeAnalysisHelper.TryGetOrComputeResult(operationBlockContext, containingMethod,
                _disposeObjectsBeforeLosingScopeRule,
                InterproceduralAnalysisKind.ContextSensitive,
                trackInstanceFields: false,
                out var disposeAnalysisResult, out var pointsToAnalysisResult,
                interproceduralAnalysisPredicateOpt))
            {
                var notDisposedDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();
                var mayBeNotDisposedDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();
                try
                {
                    // Compute diagnostics for undisposed objects at exit block.
                    var exitBlock = disposeAnalysisResult.ControlFlowGraph.ExitBlock();
                    var disposeDataAtExit = disposeAnalysisResult.ExitBlockOutput.Data;
                    ComputeDiagnostics(disposeDataAtExit, notDisposedDiagnostics, mayBeNotDisposedDiagnostics,
                        disposeAnalysisResult, pointsToAnalysisResult);

                    if (disposeAnalysisResult.ControlFlowGraph.OriginalOperation.HasAnyOperationDescendant(o => o.Kind == OperationKind.None))
                    {
                        // Workaround for https://github.com/dotnet/roslyn/issues/32100
                        // Bail out in presence of OperationKind.None - not implemented IOperation.
                        return;
                    }

                    // Report diagnostics preferring *not* disposed diagnostics over may be not disposed diagnostics
                    // and avoiding duplicates.
                    foreach (var diagnostic in notDisposedDiagnostics.Concat(mayBeNotDisposedDiagnostics))
                    {
                        if (reportedLocations.TryAdd(diagnostic.Location, true))
                        {
                            operationBlockContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                finally
                {
                    notDisposedDiagnostics.Free();
                    mayBeNotDisposedDiagnostics.Free();
                }
            }

            return;

            // Local functions.
            bool SkipInterproceduralAnalysis(IMethodSymbol invokedMethod)
            {
                // Skip interprocedural analysis if we are invoking a method and not passing any disposable object as an argument
                // and not receiving a disposable object as a return value.
                // We also check that we are not passing any object type argument which might hold disposable object
                // and also check that we are not passing delegate type argument which can
                // be a lambda or local function that has access to disposable object in current method's scope.

                if (CanBeDisposable(invokedMethod.ReturnType))
                {
                    return false;
                }

                foreach (var p in invokedMethod.Parameters)
                {
                    if (CanBeDisposable(p.Type))
                    {
                        return false;
                    }
                }

                return true;

                bool CanBeDisposable(ITypeSymbol type)
                    => type.SpecialType == SpecialType.System_Object ||
                        type.IsDisposable(disposeAnalysisHelper.IDisposableType) ||
                        type.TypeKind == TypeKind.Delegate;
            }

            void ComputeDiagnostics(
                ImmutableDictionary<AbstractLocation, DisposeAbstractValue> disposeData,
                ArrayBuilder<Diagnostic> notDisposedDiagnostics,
                ArrayBuilder<Diagnostic> mayBeNotDisposedDiagnostics,
                DisposeAnalysisResult disposeAnalysisResult,
                PointsToAnalysisResult pointsToAnalysisResult)
            {
                foreach (var kvp in disposeData)
                {
                    var location = kvp.Key;
                    var disposeValue = kvp.Value;

                    // Ignore non-disposable locations and locations without a Creation operation.
                    if (disposeValue.Kind == DisposeAbstractValueKind.NotDisposable ||
                        location.CreationOpt == null)
                    {
                        continue;
                    }

                    // Check if the disposable creation is definitely not disposed or may be not disposed.
                    var isNotDisposed = disposeValue.Kind == DisposeAbstractValueKind.NotDisposed ||
                        (disposeValue.DisposingOrEscapingOperations.Count > 0 &&
                         disposeValue.DisposingOrEscapingOperations.All(d => d.IsInsideCatchRegion(disposeAnalysisResult.ControlFlowGraph) && !location.CreationOpt.IsInsideCatchRegion(disposeAnalysisResult.ControlFlowGraph)));
                    var isMayBeNotDisposed = !isNotDisposed &&
                        (disposeValue.Kind == DisposeAbstractValueKind.MaybeDisposed || disposeValue.Kind == DisposeAbstractValueKind.NotDisposedOrEscaped);

                    if (isNotDisposed || isMayBeNotDisposed)
                    {
                        var syntax = location.TryGetNodeToReportDiagnostic(pointsToAnalysisResult);
                        if (syntax == null)
                        {
                            continue;
                        }

                        var rule = isNotDisposed ? _disposeObjectsBeforeLosingScopeRule : _useRecommendedDisposePatternRule;

                        // Ensure that we do not include multiple lines for the object creation expression in the diagnostic message.
                        var objectCreationText = syntax.ToString();
                        var indexOfNewLine = objectCreationText.IndexOf(Environment.NewLine);
                        if (indexOfNewLine > 0)
                        {
                            objectCreationText = objectCreationText.Substring(0, indexOfNewLine);
                        }

                        var diagnostic = Diagnostic.Create(
                            rule,
                            syntax.GetLocation(),
                            additionalLocations: null,
                            properties: null,
                            objectCreationText);

                        if (isNotDisposed)
                        {
                            notDisposedDiagnostics.Add(diagnostic);
                        }
                        else
                        {
                            mayBeNotDisposedDiagnostics.Add(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
