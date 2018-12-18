// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
{
    internal abstract partial class AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private sealed partial class SymbolStartAnalyzer
        {
            private sealed partial class BlockAnalyzer
            {
                private readonly SymbolStartAnalyzer _symbolStartAnalyzer;
                private readonly Options _options;

                /// <summary>
                /// Indicates if the operation block has an <see cref="IDelegateCreationOperation"/> or an <see cref="IAnonymousFunctionOperation"/>.
                /// We use this value in <see cref="ShouldAnalyze(IOperation, ISymbol)"/> to determine whether to bail from analysis or not.
                /// </summary>
                private bool _hasDelegateCreationOrAnonymousFunction;

                /// <summary>
                /// Indicates if the operation block has an <see cref="IArgumentOperation"/> with a delegate type argument.
                /// We use this value in <see cref="ShouldAnalyze(IOperation, ISymbol)"/> to determine whether to bail from analysis or not.
                /// </summary>
                private bool _hasDelegateTypeArgument;

                /// <summary>
                /// Indicates if a delegate instance escaped this operation block, via an assignment to a field or a property symbol.
                /// that can be accessed outside this executable code block.
                /// We use this value in <see cref="ShouldAnalyze(IOperation, ISymbol)"/> to determine whether to bail from analysis or not.
                /// </summary>
                private bool _delegateAssignedToFieldOrProperty;

                /// <summary>
                /// Indicates if the operation block has an <see cref="IConversionOperation"/> with a delegate type or an anonymous function
                /// as it's source and a non-delegate type as it's target.
                /// We use this value in <see cref="ShouldAnalyze(IOperation, ISymbol)"/> to determine whether to bail from analysis or not.
                /// </summary>
                private bool _hasConversionFromDelegateTypeOrAnonymousFunctionToNonDelegateType;

                /// <summary>
                /// Parameters which have at least one read/write reference.
                /// </summary>
                private readonly ConcurrentDictionary<IParameterSymbol, bool> _referencedParameters;

                private BlockAnalyzer(SymbolStartAnalyzer symbolStartAnalyzer, Options options)
                {
                    _symbolStartAnalyzer = symbolStartAnalyzer;
                    _options = options;
                    _referencedParameters = new ConcurrentDictionary<IParameterSymbol, bool>();
                }

                public static void Analyze(OperationBlockStartAnalysisContext context, SymbolStartAnalyzer symbolStartAnalyzer)
                {
                    if (HasSyntaxErrors() || context.OperationBlocks.IsEmpty)
                    {
                        return;
                    }

                    // Bail out in presence of conditional directives
                    // This is a workaround for https://github.com/dotnet/roslyn/issues/31820
                    // Issue https://github.com/dotnet/roslyn/issues/31821 tracks
                    // reverting this workaround.
                    if (HasConditionalDirectives())
                    {
                        return;
                    }

                    // All operation blocks for a symbol belong to the same tree.
                    var firstBlock = context.OperationBlocks[0];
                    if (!symbolStartAnalyzer._compilationAnalyzer.TryGetOptions(firstBlock.Syntax.SyntaxTree,
                                                                                firstBlock.Language,
                                                                                context.Options,
                                                                                context.CancellationToken,
                                                                                out var options))
                    {
                        return;
                    }

                    var blockAnalyzer = new BlockAnalyzer(symbolStartAnalyzer, options);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeExpressionStatement, OperationKind.ExpressionStatement);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeDelegateCreationOrAnonymousFunction, OperationKind.DelegateCreation, OperationKind.AnonymousFunction);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeArgument, OperationKind.Argument);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeConversion, OperationKind.Conversion);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeFieldOrPropertyReference, OperationKind.FieldReference, OperationKind.PropertyReference);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeParameterReference, OperationKind.ParameterReference);
                    context.RegisterOperationBlockEndAction(blockAnalyzer.AnalyzeOperationBlockEnd);

                    return;

                    // Local Functions.
                    bool HasSyntaxErrors()
                    {
                        foreach (var operationBlock in context.OperationBlocks)
                        {
                            if (operationBlock.Syntax.GetDiagnostics().ToImmutableArrayOrEmpty().HasAnyErrors())
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    bool HasConditionalDirectives()
                    {
                        foreach (var operationBlock in context.OperationBlocks)
                        {
                            if (operationBlock.Syntax.DescendantNodes(descendIntoTrivia: true)
                                                     .Any(n => symbolStartAnalyzer._compilationAnalyzer.IsIfConditionalDirective(n)))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }

                private void AnalyzeExpressionStatement(OperationAnalysisContext context)
                {
                    if (_options.UnusedValueExpressionStatementSeverity == ReportDiagnostic.Suppress)
                    {
                        return;
                    }

                    var expressionStatement = (IExpressionStatementOperation)context.Operation;
                    var value = expressionStatement.Operation;

                    // Bail out cases for report unused expression value:

                    //  1. Null type and void returning method invocations: no value being dropped here.
                    if (value.Type == null ||
                        value.Type.SpecialType == SpecialType.System_Void)
                    {
                        return;
                    }

                    //  2. Bail out for semantic error (invalid operation) cases.
                    //     Also bail out for constant expressions in expression statement syntax, say as "1;",
                    //     which do not seem to have an invalid operation in the operation tree.
                    if (value is IInvalidOperation ||
                        value.ConstantValue.HasValue)
                    {
                        return;
                    }

                    //  3. Assignments, increment/decrement operations: value is actually being assigned.
                    if (value is IAssignmentOperation ||
                        value is IIncrementOrDecrementOperation)
                    {
                        return;
                    }

                    //  4. Bail out if there is language specific syntax to indicate an explicit discard.
                    //     For example, VB call statement is used to explicitly ignore the value returned by
                    //     an invocation by prefixing the invocation with keyword "Call".
                    if (_symbolStartAnalyzer._compilationAnalyzer.IsCallStatement(expressionStatement))
                    {
                        return;
                    }

                    var properties = s_propertiesMap[(_options.UnusedValueExpressionStatementPreference, isUnusedLocalAssignment: false, isRemovableAssignment: false)];
                    var diagnostic = DiagnosticHelper.Create(s_expressionValueIsUnusedRule,
                                                             value.Syntax.GetLocation(),
                                                             _options.UnusedValueExpressionStatementSeverity,
                                                             additionalLocations: null,
                                                             properties);
                    context.ReportDiagnostic(diagnostic);
                }

                private void AnalyzeDelegateCreationOrAnonymousFunction(OperationAnalysisContext operationAnalysisContext)
                    => _hasDelegateCreationOrAnonymousFunction = true;

                private void AnalyzeArgument(OperationAnalysisContext operationAnalysisContext)
                {
                    var argument = (IArgumentOperation)operationAnalysisContext.Operation;
                    if (!_hasDelegateTypeArgument &&
                        argument.Value.Type.IsDelegateType())
                    {
                        _hasDelegateTypeArgument = true;
                    }
                }

                private void AnalyzeConversion(OperationAnalysisContext operationAnalysisContext)
                {
                    var conversion = (IConversionOperation)operationAnalysisContext.Operation;
                    if (!_hasConversionFromDelegateTypeOrAnonymousFunctionToNonDelegateType &&
                        (conversion.Operand.Type.IsDelegateType() || conversion.Operand.Kind == OperationKind.AnonymousFunction) &&
                        !conversion.Type.IsDelegateType())
                    {
                        _hasConversionFromDelegateTypeOrAnonymousFunctionToNonDelegateType = true;
                    }
                }

                private void AnalyzeFieldOrPropertyReference(OperationAnalysisContext operationAnalysisContextContext)
                {
                    var fieldOrPropertyReference = operationAnalysisContextContext.Operation;
                    if (!_delegateAssignedToFieldOrProperty &&
                        fieldOrPropertyReference.Type.IsDelegateType() &&
                        fieldOrPropertyReference.Parent is ISimpleAssignmentOperation simpleAssignment &&
                        simpleAssignment.Target.Equals(fieldOrPropertyReference))
                    {
                        _delegateAssignedToFieldOrProperty = true;
                    }
                }

                private void AnalyzeParameterReference(OperationAnalysisContext operationAnalysisContextContext)
                {
                    var parameter = ((IParameterReferenceOperation)operationAnalysisContextContext.Operation).Parameter;
                    _referencedParameters.GetOrAdd(parameter, true);
                }

                /// <summary>
                /// Method invoked in <see cref="AnalyzeOperationBlockEnd(OperationBlockAnalysisContext)"/>
                /// for each operation block to determine if we should analyze the operation block or bail out.
                /// </summary>
                private bool ShouldAnalyze(IOperation operationBlock, ISymbol owningSymbol)
                {
                    switch (operationBlock.Kind)
                    {
                        case OperationKind.None:
                        case OperationKind.ParameterInitializer:
                            // Skip blocks from attributes (which have OperationKind.None) and parameter initializers.
                            // We don't have any unused values in such operation blocks.
                            return false;
                    }

                    // We currently do not support points-to analysis, which is needed to accurately track locations of
                    // allocated objects and their aliasing, which enables us to determine if two symbols reference the
                    // same object instance at a given program point and also enables us to track the set of runtime objects
                    // that a variable can point to.
                    // Hence, we cannot accurately track the exact set of delegates that a symbol with delegate type
                    // can point to for all control flow cases.
                    // We attempt to do our best effort delegate invocation analysis as follows:

                    //  1. If we have no delegate creations or lambdas, our current analysis works fine,
                    //     return true.
                    if (!_hasDelegateCreationOrAnonymousFunction)
                    {
                        return true;
                    }

                    //  2. Bail out if we have a delegate escape via an assigment to a field/property reference.
                    //     This indicates the delegate targets (such as lambda/local functions) have been captured
                    //     and can be invoked from a separate method, and these invocations can read values written
                    //     to any local/parameter in the current method. We cannot reliably flag any write to a 
                    //     local/parameter as unused for such cases.
                    if (_delegateAssignedToFieldOrProperty)
                    {
                        return false;
                    }

                    //  3. Bail out if we have a conversion from a delegate or an anonymous function to a non-delegate type.
                    //     We can analyze this correctly when we do points-to-analysis.
                    if (_hasConversionFromDelegateTypeOrAnonymousFunctionToNonDelegateType)
                    {
                        return false;
                    }

                    //  4. Bail out if we pass a delegate type as an argument to a method.
                    //     We can analyze this correctly when we do points-to-analysis.
                    if (_hasDelegateTypeArgument)
                    {
                        return false;
                    }

                    //  5. Bail out for method returning delegates or ref/out parameters of delegate type.
                    //     We can analyze this correctly when we do points-to-analysis.
                    if (owningSymbol is IMethodSymbol method &&
                        (method.ReturnType.IsDelegateType() ||
                         method.Parameters.Any(p => p.IsRefOrOut() && p.Type.IsDelegateType())))
                    {
                        return false;
                    }

                    //  6. Otherwise, we execute analysis by walking the reaching symbol write chain to attempt to
                    //     find the target method being invoked.
                    //     This works for most common and simple cases where a local is assigned a lambda and invoked later.
                    //     If we are unable to find a target, we will conservatively mark all current symbol writes as read.
                    return true;
                }

                private void AnalyzeOperationBlockEnd(OperationBlockAnalysisContext context)
                {
                    // Bail out if we are neither computing unused parameters nor unused value assignments.
                    var isComputingUnusedParams = _options.IsComputingUnusedParams(context.OwningSymbol);
                    if (_options.UnusedValueAssignmentSeverity == ReportDiagnostic.Suppress &&
                        !isComputingUnusedParams)
                    {
                        return;
                    }

                    // We perform analysis to compute unused parameters and value assignments in two passes.
                    // Unused value assignments can be identified by analyzing each operation block independently in the first pass.
                    // However, to identify unused parameters we need to first analyze all operation blocks and then iterate
                    // through the parameters to identify unused ones

                    // Builder to store the symbol read/write usage result for each operation block computed during the first pass.
                    // These are later used to compute unused parameters in second pass.
                    var symbolUsageResultsBuilder = PooledHashSet<SymbolUsageResult>.GetInstance();

                    try
                    {
                        // Flag indicating if we found an operation block where all symbol writes were used. 
                        bool hasBlockWithAllUsedWrites;

                        AnalyzeUnusedValueAssignments(context, isComputingUnusedParams, symbolUsageResultsBuilder, out hasBlockWithAllUsedWrites);

                        AnalyzeUnusedParameters(context, isComputingUnusedParams, symbolUsageResultsBuilder, hasBlockWithAllUsedWrites);
                    }
                    finally
                    {
                        symbolUsageResultsBuilder.Free();
                    }
                }

                private void AnalyzeUnusedValueAssignments(
                    OperationBlockAnalysisContext context,
                    bool isComputingUnusedParams,
                    PooledHashSet<SymbolUsageResult> symbolUsageResultsBuilder,
                    out bool hasBlockWithAllUsedSymbolWrites)
                {
                    hasBlockWithAllUsedSymbolWrites = false;

                    foreach (var operationBlock in context.OperationBlocks)
                    {
                        if (!ShouldAnalyze(operationBlock, context.OwningSymbol))
                        {
                            continue;
                        }

                        // First perform the fast, aggressive, imprecise operation-tree based analysis.
                        // This analysis might flag some "used" symbol writes as "unused", but will not miss reporting any truly unused symbol writes.
                        // This initial pass helps us reduce the number of methods for which we perform the slower second pass.
                        // We perform the first fast pass only if there are no delegate creations/lambda methods.
                        // This is due to the fact that tracking which local/parameter points to which delegate creation target
                        // at any given program point needs needs flow analysis (second pass).
                        if (!_hasDelegateCreationOrAnonymousFunction)
                        {
                            var resultFromOperationBlockAnalysis = SymbolUsageAnalysis.Run(operationBlock, context.OwningSymbol, context.CancellationToken);
                            if (!resultFromOperationBlockAnalysis.HasUnreadSymbolWrites())
                            {
                                // Assert that even slow pass (dataflow analysis) would have yielded no unused symbol writes.
                                Debug.Assert(!SymbolUsageAnalysis.Run(context.GetControlFlowGraph(operationBlock), context.OwningSymbol, context.CancellationToken)
                                             .HasUnreadSymbolWrites());

                                hasBlockWithAllUsedSymbolWrites = true;
                                continue;
                            }
                        }

                        // Now perform the slower, precise, CFG based dataflow analysis to identify the actual unused symbol writes.
                        var controlFlowGraph = context.GetControlFlowGraph(operationBlock);
                        var symbolUsageResult = SymbolUsageAnalysis.Run(controlFlowGraph, context.OwningSymbol, context.CancellationToken);
                        symbolUsageResultsBuilder.Add(symbolUsageResult);

                        foreach (var (symbol, unreadWriteOperation) in symbolUsageResult.GetUnreadSymbolWrites())
                        {
                            if (unreadWriteOperation == null)
                            {
                                // Null operation is used for initial write for the parameter from method declaration.
                                // So, the initial value of the parameter is never read in this operation block.
                                // However, we do not report this as an unused parameter here as a different operation block
                                // might be reading the initial parameter value.
                                // For example, a constructor with both a constructor initializer and body will have two different operation blocks
                                // and a parameter must be unused across both these blocks to be marked unused.

                                // However, we do report unused parameters for local function here.
                                // Local function parameters are completely scoped to this operation block, and should be reported per-operation block.
                                var unusedParameter = (IParameterSymbol)symbol;
                                if (isComputingUnusedParams &&
                                    unusedParameter.ContainingSymbol.IsLocalFunction())
                                {
                                    var hasReference = symbolUsageResult.SymbolsRead.Contains(unusedParameter);
                                    _symbolStartAnalyzer.ReportUnusedParameterDiagnostic(unusedParameter, hasReference, context.ReportDiagnostic, context.Options, context.CancellationToken);
                                }

                                continue;
                            }

                            if (ShouldReportUnusedValueDiagnostic(symbol, unreadWriteOperation, symbolUsageResult, out var properties))
                            {
                                var diagnostic = DiagnosticHelper.Create(s_valueAssignedIsUnusedRule,
                                                                         _symbolStartAnalyzer._compilationAnalyzer.GetDefinitionLocationToFade(unreadWriteOperation),
                                                                         _options.UnusedValueAssignmentSeverity,
                                                                         additionalLocations: null,
                                                                         properties,
                                                                         symbol.Name);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }

                    return;

                    // Local functions.
                    bool ShouldReportUnusedValueDiagnostic(
                        ISymbol symbol,
                        IOperation unreadWriteOperation,
                        SymbolUsageResult resultFromFlowAnalysis,
                        out ImmutableDictionary<string, string> properties)
                    {
                        properties = null;

                        // Bail out in following cases:
                        //   1. End user has configured the diagnostic to be suppressed.
                        //   2. Symbol has error type, hence the diagnostic could be noised
                        //   3. Static local symbols. Assignment to static locals
                        //      is not unnecessary as the assigned value can be used on the next invocation.
                        if (_options.UnusedValueAssignmentSeverity == ReportDiagnostic.Suppress ||
                            symbol.GetSymbolType().IsErrorType() ||
                            symbol.IsStatic && symbol.Kind == SymbolKind.Local)
                        {
                            return false;
                        }

                        // Flag to indicate if the symbol has no reads.
                        var isUnusedLocalAssignment = symbol is ILocalSymbol localSymbol &&
                                                      !resultFromFlowAnalysis.SymbolsRead.Contains(localSymbol);

                        var isRemovableAssignment = IsRemovableAssignmentWithoutSideEffects(unreadWriteOperation);

                        if (isUnusedLocalAssignment &&
                            !isRemovableAssignment &&
                            _options.UnusedValueAssignmentPreference == UnusedValuePreference.UnusedLocalVariable)
                        {
                            // Meets current user preference of using unused local symbols for storing computation result.
                            // Skip reporting diagnostic.
                            return false;
                        }

                        properties = s_propertiesMap[(_options.UnusedValueAssignmentPreference, isUnusedLocalAssignment, isRemovableAssignment)];
                        return true;
                    }

                    // Indicates if the given unused symbol write is a removable assignment.
                    // This is true if the expression for the assigned value has no side effects.
                    bool IsRemovableAssignmentWithoutSideEffects(IOperation unusedSymbolWriteOperation)
                    {
                        if (unusedSymbolWriteOperation.Parent is IAssignmentOperation assignment &&
                            assignment.Target == unusedSymbolWriteOperation)
                        {
                            return IsRemovableAssignmentValueWithoutSideEffects(assignment.Value);
                        }
                        else if (unusedSymbolWriteOperation.Parent is IIncrementOrDecrementOperation)
                        {
                            // As the new value assigned to the incremented/decremented variable is unused,
                            // it is safe to remove the entire increment/decrement operation,
                            // as it cannot have side effects on anything but the variable.
                            return true;
                        }

                        // Assume all other operations can have side effects, and cannot be removed.
                        return false;
                    }

                    bool IsRemovableAssignmentValueWithoutSideEffects(IOperation assignmentValue)
                    {
                        if (assignmentValue.ConstantValue.HasValue)
                        {
                            // Constant expressions have no side effects.
                            return true;
                        }

                        switch (assignmentValue.Kind)
                        {
                            case OperationKind.ParameterReference:
                            case OperationKind.LocalReference:
                                // Parameter/local references have no side effects and can be removed.
                                return true;

                            case OperationKind.FieldReference:
                                // Field references with null instance (static fields) or 'this' or 'Me' instance can
                                // have no side effects and can be removed.
                                var fieldReference = (IFieldReferenceOperation)assignmentValue;
                                return fieldReference.Instance == null || fieldReference.Instance.Kind == OperationKind.InstanceReference;

                            case OperationKind.DefaultValue:
                                // Default value expressions have no side-effects.
                                return true;

                            case OperationKind.Conversion:
                                // Conversions can theoretically have side-effects as the conversion can throw exception(s).
                                // However, for all practical purposes, we can assume that a non-user defined conversion whose operand
                                // has no side effects can be safely removed.
                                var conversion = (IConversionOperation)assignmentValue;
                                return conversion.OperatorMethod == null &&
                                    IsRemovableAssignmentValueWithoutSideEffects(conversion.Operand);
                        }

                        // Assume all other operations can have side effects, and cannot be removed.
                        return false;
                    }
                }

                private void AnalyzeUnusedParameters(
                    OperationBlockAnalysisContext context,
                    bool isComputingUnusedParams,
                    PooledHashSet<SymbolUsageResult> symbolUsageResultsBuilder,
                    bool hasBlockWithAllUsedSymbolWrites)
                {
                    // Process parameters for the context's OwningSymbol that are unused across all operation blocks.

                    // Bail out cases:
                    //  1. Skip analysis if we are not computing unused parameters based on user's option preference.
                    if (!isComputingUnusedParams)
                    {
                        return;
                    }

                    // 2. Report unused parameters only for method symbols.
                    if (!(context.OwningSymbol is IMethodSymbol method))
                    {
                        return;
                    }

                    // Mark all unreferenced parameters as unused parameters with no read reference.
                    // We do so prior to bail out cases 3. and 4. below
                    // so that we flag unreferenced parameters even when we bail out from flow analysis.
                    foreach (var parameter in method.Parameters)
                    {
                        if (!_referencedParameters.ContainsKey(parameter))
                        {
                            // Unused parameter without a reference.
                            _symbolStartAnalyzer._unusedParameters[parameter] = false;
                        }
                    }

                    // 3. Bail out if we found a single operation block where all symbol writes were used.
                    if (hasBlockWithAllUsedSymbolWrites)
                    {
                        return;
                    }

                    // 4. Bail out if symbolUsageResultsBuilder is empty, indicating we skipped analysis for all operation blocks.
                    if (symbolUsageResultsBuilder.Count == 0)
                    {
                        return;
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        bool isUsed = false;
                        bool isSymbolRead = false;
                        var isRefOrOutParam = parameter.IsRefOrOut();

                        // Iterate through symbol usage results for each operation block.
                        foreach (var symbolUsageResult in symbolUsageResultsBuilder)
                        {
                            if (symbolUsageResult.IsInitialParameterValueUsed(parameter))
                            {
                                // Parameter is used in this block.
                                isUsed = true;
                                break;
                            }

                            isSymbolRead |= symbolUsageResult.SymbolsRead.Contains(parameter);

                            // Ref/Out parameters are considered used if they have any reads or writes
                            // Note that we always have one write for the parameter input value from the caller.
                            if (isRefOrOutParam &&
                                (isSymbolRead ||
                                symbolUsageResult.GetSymbolWriteCount(parameter) > 1))
                            {
                                isUsed = true;
                                break;
                            }
                        }

                        if (!isUsed)
                        {
                            _symbolStartAnalyzer._unusedParameters[parameter] = isSymbolRead;
                        }
                    }
                }
            }
        }
    }
}
