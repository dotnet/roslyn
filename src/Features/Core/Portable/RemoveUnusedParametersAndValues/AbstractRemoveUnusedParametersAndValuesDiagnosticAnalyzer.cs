// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.ReachingDefinitions;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
{
    using PropertiesMap = ImmutableDictionary<(UnusedValuePreference preference, bool isUnusedLocalAssignment, bool isRemovableAssignment),
                                              ImmutableDictionary<string, string>>;

    /// <summary>
    /// Analyzer to report unused expression values and parameters:
    /// It flags the following cases:
    ///     1. Expression statements that drop computed value, for example, "Computation();".
    ///        These should either be removed (redundant computation) or should be replaced
    ///        with explicit assignment to discard variable OR an unused local variable,
    ///        i.e. "_ = Computation();" or "var unused = Computation();"
    ///     2. Value assignments to locals/parameters that are never used on any control flow path,
    ///        For example, value assigned to 'x' in first statement below is unused and will be flagged:
    ///             x = Computation();
    ///             if (...)
    ///                 x = Computation2();
    ///             else
    ///                 Computation3(out x);
    ///             ... = x;
    ///        Just as for case 1., these should either be removed (redundant computation) or
    ///        should be replaced with explicit assignment to discard variable OR an unused local variable,
    ///        i.e. "_ = Computation();" or "var unused = Computation();"
    ///     3. Redundant parameters that fall into one of the following two categories:
    ///         a. Have no references in the executable code block(s) for it's containing method symbol.
    ///         b. Have one or more references but it's initial value at start of code block is never used.
    ///            For example, if 'x' in the example for case 2. above was a parameter symbol with RefKind.None
    ///            and "x = Computation();" is the first statement in the method body, then it's initial value
    ///            is never used. Such a parameter should be removed and 'x' should be converted into a local.
    ///        We provide additional information in the diagnostic message to clarify the above two categories
    ///        and also detect and mention about potential breaking change if the containing method is a public API.
    ///        Currently, we do not provide any code fix for removing unused parameters as it needs fixing the
    ///        call sites and any automated fix can lead to subtle overload resolution differences,
    ///        though this may change in future.
    /// </summary>
    internal abstract class AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private const string UnusedValuePreferenceKey = nameof(UnusedValuePreferenceKey);
        private const string IsUnusedLocalAssignmentKey = nameof(IsUnusedLocalAssignmentKey);
        private const string IsRemovableAssignmentKey = nameof(IsRemovableAssignmentKey);

        // IDE0056: "Expression value is never used"
        private static readonly DiagnosticDescriptor s_expressionValueIsUnusedRule = CreateDescriptorWithId(
            IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Expression_value_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Expression_value_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: false);

        // IDE0057: "Value assigned to '{0}' is never used"
        private static readonly DiagnosticDescriptor s_valueAssignedIsUnusedRule = CreateDescriptorWithId(
            IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Value_assigned_to_symbol_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Value_assigned_to_0_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        // IDE0058: "Remove unused parameter '{0}'{1}"
        private static readonly DiagnosticDescriptor s_unusedParameterRule = CreateDescriptorWithId(
            IDEDiagnosticIds.UnusedParameterDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Remove_unused_parameter), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Remove_unused_parameter_0_1), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        private static readonly PropertiesMap s_propertiesMap = CreatePropertiesMap();

        protected AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer()
            : base(ImmutableArray.Create(s_expressionValueIsUnusedRule, s_valueAssignedIsUnusedRule, s_unusedParameterRule))
        {
        }

        private static PropertiesMap CreatePropertiesMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<(UnusedValuePreference preference, bool isUnusedLocalAssignment, bool isRemovableAssignment),
                                                            ImmutableDictionary<string, string>>();
            AddEntries(UnusedValuePreference.DiscardVariable);
            AddEntries(UnusedValuePreference.UnusedLocalVariable);
            return builder.ToImmutable();

            void AddEntries(UnusedValuePreference preference)
            {
                AddEntries2(preference, isUnusedLocalAssignment: true);
                AddEntries2(preference, isUnusedLocalAssignment: false);
            }

            void AddEntries2(UnusedValuePreference preference, bool isUnusedLocalAssignment)
            {
                AddEntryCore(preference, isUnusedLocalAssignment, isRemovableAssignment: true);
                AddEntryCore(preference, isUnusedLocalAssignment, isRemovableAssignment: false);
            }

            void AddEntryCore(UnusedValuePreference preference, bool isUnusedLocalAssignment, bool isRemovableAssignment)
            {
                var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string>();

                propertiesBuilder.Add(UnusedValuePreferenceKey, preference.ToString());
                if (isUnusedLocalAssignment)
                {
                    propertiesBuilder.Add(IsUnusedLocalAssignmentKey, string.Empty);
                }
                if (isRemovableAssignment)
                {
                    propertiesBuilder.Add(IsRemovableAssignmentKey, string.Empty);
                }

                builder.Add((preference, isUnusedLocalAssignment, isRemovableAssignment), propertiesBuilder.ToImmutable());
            }
        }

        protected abstract Location GetDefinitionLocationToFade(IOperation unusedDefinition);
        protected abstract bool SupportsDiscard(SyntaxTree tree);
        protected abstract Option<CodeStyleOption<UnusedValuePreference>> UnusedValueExpressionStatementOption { get; }
        protected abstract Option<CodeStyleOption<UnusedValuePreference>> UnusedValueAssignmentOption { get; }

        public override bool OpenFileOnly(Workspace workspace) => false;

        // Our analysis is limited to unused expressions in a code block, hence is unaffected by changes outside the code block.
        // Hence, we can support incremental span based method body analysis.
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(
                compilationContext => SymbolStartAnalyzer.CreateAndRegisterActions(
                    compilationContext, GetDefinitionLocationToFade, SupportsDiscard,
                    UnusedValueExpressionStatementOption, UnusedValueAssignmentOption));

        private sealed class SymbolStartAnalyzer
        {
            private readonly Func<IOperation, Location> _getDefinitionLocationToFade;
            private readonly Func<SyntaxTree, bool> _supportsDiscard;
            private readonly Option<CodeStyleOption<UnusedValuePreference>> _unusedValueExpressionStatementOption;
            private readonly Option<CodeStyleOption<UnusedValuePreference>> _unusedValueAssignmentOption;

            private readonly INamedTypeSymbol _eventArgsType, _interlockedType, _immutableInterlockedType;
            private readonly ImmutableHashSet<INamedTypeSymbol> _attributeSetForMethodsToIgnore;
            private readonly ConcurrentDictionary<IParameterSymbol, bool> _unusedParameters;
            private readonly ConcurrentDictionary<IMethodSymbol, bool> _methodsUsedAsDelegates;

            public SymbolStartAnalyzer(
                Func<IOperation, Location> getDefinitionLocationToFade,
                Func<SyntaxTree, bool> supportsDiscard,
                Option<CodeStyleOption<UnusedValuePreference>> unusedValueExpressionStatementOption,
                Option<CodeStyleOption<UnusedValuePreference>> unusedValueAssignmentOption,
                INamedTypeSymbol eventArgsType,
                INamedTypeSymbol interlockedType,
                INamedTypeSymbol immutableInterlockedType,
                ImmutableHashSet<INamedTypeSymbol> attributeSetForMethodsToIgnore)
            {
                _getDefinitionLocationToFade = getDefinitionLocationToFade;
                _supportsDiscard = supportsDiscard;
                _unusedValueExpressionStatementOption = unusedValueExpressionStatementOption;
                _unusedValueAssignmentOption = unusedValueAssignmentOption;

                _eventArgsType = eventArgsType;
                _interlockedType = interlockedType;
                _immutableInterlockedType = immutableInterlockedType;
                _attributeSetForMethodsToIgnore = attributeSetForMethodsToIgnore;
                _unusedParameters = new ConcurrentDictionary<IParameterSymbol, bool>();
                _methodsUsedAsDelegates = new ConcurrentDictionary<IMethodSymbol, bool>();
            }

            public static void CreateAndRegisterActions(
                CompilationStartAnalysisContext context,
                Func<IOperation, Location> getDefinitionLocationToFade,
                Func<SyntaxTree, bool> supportsDiscard,
                Option<CodeStyleOption<UnusedValuePreference>> unusedValueExpressionStatementOption,
                Option<CodeStyleOption<UnusedValuePreference>> unusedValueAssignmentOption)
            {
                var attributeSetForMethodsToIgnore = ImmutableHashSet.CreateRange(GetAttributesForMethodsToIgnore(context.Compilation));
                var eventsArgType = context.Compilation.EventArgsType();
                var interlockedType = context.Compilation.InterlockedType();
                var immutableInterlockedType = context.Compilation.ImmutableInterlockedType();

                var analyzer = new SymbolStartAnalyzer(getDefinitionLocationToFade, supportsDiscard,
                    unusedValueExpressionStatementOption, unusedValueAssignmentOption,
                    eventsArgType, interlockedType, immutableInterlockedType, attributeSetForMethodsToIgnore);
                context.RegisterSymbolStartAction(analyzer.OnSymbolStart, SymbolKind.NamedType);
            }

            private void OnSymbolStart(SymbolStartAnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(OnOperationBlock);
                context.RegisterSymbolEndAction(OnSymbolEnd);
            }

            private void OnOperationBlock(OperationBlockStartAnalysisContext context)
            {
                context.RegisterOperationAction(OnMethodReference, OperationKind.MethodReference);
                BlockAnalyzer.Analyze(context, this);
            }

            private void OnMethodReference(OperationAnalysisContext context)
            {
                var methodBinding = (IMethodReferenceOperation)context.Operation;
                _methodsUsedAsDelegates.GetOrAdd(methodBinding.Method.OriginalDefinition, true);
            }

            private void OnSymbolEnd(SymbolAnalysisContext context)
            {
                foreach (var parameterAndUsageKvp in _unusedParameters)
                {
                    var parameter = parameterAndUsageKvp.Key;
                    bool hasReference = parameterAndUsageKvp.Value;

                    ReportUnusedParameterDiagnostic(parameter, hasReference, context.ReportDiagnostic, context.Options, context.CancellationToken);
                }
            }

            private void ReportUnusedParameterDiagnostic(
                IParameterSymbol parameter,
                bool hasReference,
                Action<Diagnostic> reportDiagnostic,
                AnalyzerOptions analyzerOptions,
                CancellationToken cancellationToken)
            {
                if (!IsUnusedParameterCandidate(parameter))
                {
                    return;
                }

                var location = parameter.Locations[0];
                var optionSet = analyzerOptions.GetDocumentOptionSetAsync(location.SourceTree, cancellationToken).GetAwaiter().GetResult();
                if (optionSet == null)
                {
                    return;
                }

                var option = optionSet.GetOption(CodeStyleOptions.UnusedParameters, parameter.Language);
                if (!ShouldReportUnusedParameters(parameter.ContainingSymbol, option.Value) ||
                    option.Notification.Severity == ReportDiagnostic.Suppress)
                {
                    return;
                }

                // IDE0058: "Remove unused parameter '{0}'{1}"
                var arg1 = parameter.Name;
                var arg2 = string.Empty;
                if (parameter.ContainingSymbol.IsExternallyVisible() &&
                    !parameter.ContainingSymbol.IsLocalFunction() &&
                    !parameter.ContainingSymbol.IsAnonymousFunction())
                {
                    arg2 += FeaturesResources.if_it_is_not_part_of_a_shipped_public_API;
                }

                if (hasReference)
                {
                    arg2 += FeaturesResources.comma_its_initial_value_is_never_used;
                }

                var diagnostic = DiagnosticHelper.Create(s_unusedParameterRule,
                                                         location,
                                                         option.Notification.Severity,
                                                         additionalLocations: null,
                                                         properties: null,
                                                         arg1,
                                                         arg2);
                reportDiagnostic(diagnostic);
            }

            private static IEnumerable<INamedTypeSymbol> GetAttributesForMethodsToIgnore(Compilation compilation)
            {
                // Ignore conditional methods (One conditional will often call another conditional method as its only use of a parameter)
                var conditionalAttribte = compilation.ConditionalAttribute();
                if (conditionalAttribte != null)
                {
                    yield return conditionalAttribte;
                }

                // Ignore methods with special serialization attributes (All serialization methods need to take 'StreamingContext')
                var onDeserializingAttribute = compilation.OnDeserializingAttribute();
                if (onDeserializingAttribute != null)
                {
                    yield return onDeserializingAttribute;
                }

                var onDeserializedAttribute = compilation.OnDeserializedAttribute();
                if (onDeserializedAttribute != null)
                {
                    yield return onDeserializedAttribute;
                }

                var onSerializingAttribute = compilation.OnSerializingAttribute();
                if (onSerializingAttribute != null)
                {
                    yield return onSerializingAttribute;
                }

                var onSerializedAttribute = compilation.OnSerializedAttribute();
                if (onSerializedAttribute != null)
                {
                    yield return onSerializedAttribute;
                }

                // Don't flag obsolete methods.
                var obsoleteAttribute = compilation.ObsoleteAttribute();
                if (obsoleteAttribute != null)
                {
                    yield return obsoleteAttribute;
                }
            }

            private bool IsUnusedParameterCandidate(IParameterSymbol parameter)
            {
                // Ignore implicitly declared parameters and methods, extern methods, abstract methods,
                // virtual methods, overrides, interface implementations, accessors and lambdas.
                if (parameter.IsImplicitlyDeclared ||
                    parameter.Name == "_" ||
                    !(parameter.ContainingSymbol is IMethodSymbol method) ||
                    method.IsImplicitlyDeclared ||
                    method.IsExtern ||
                    method.IsAbstract ||
                    method.IsVirtual ||
                    method.IsOverride ||
                    !method.ExplicitOrImplicitInterfaceImplementations().IsEmpty ||
                    method.IsAccessor() ||
                    method.IsAnonymousFunction())
                {
                    return false;
                }

                // Ignore event handler methods "Handler(object, MyEventArgs)"
                if (_eventArgsType != null &&
                    method.Parameters.Length == 2 &&
                    method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                    method.Parameters[1].Type.InheritsFromOrEquals(_eventArgsType))
                {
                    return false;
                }

                // Ignore methods with any attributes in 'attributeSetForMethodsToIgnore'.
                if (method.GetAttributes().Any(a => a.AttributeClass != null && _attributeSetForMethodsToIgnore.Contains(a.AttributeClass)))
                {
                    return false;
                }

                // Ignore methods that were used as delegates
                if (_methodsUsedAsDelegates.ContainsKey(method))
                {
                    return false;
                }

                return true;
            }

            private sealed class BlockAnalyzer
            {
                private readonly SymbolStartAnalyzer _symbolStartAnalyzer;
                private readonly Options _options;
                private bool _hasDelegateCreation;
                private bool _hasConversionFromDelegateTypeToNonDelegteType;

                private BlockAnalyzer(SymbolStartAnalyzer symbolStartAnalyzer, Options options)
                {
                    _symbolStartAnalyzer = symbolStartAnalyzer;
                    _options = options;
                }

                public static void Analyze(OperationBlockStartAnalysisContext context, SymbolStartAnalyzer symbolStartAnalyzer)
                {
                    if (HasSyntaxErrors())
                    {
                        return;
                    }

                    // All operation blocks for a symbol belong to the same tree.
                    var firstBlock = context.OperationBlocks[0];
                    var supportsDiscard = symbolStartAnalyzer._supportsDiscard(firstBlock.Syntax.SyntaxTree);
                    var unusedValueExpressionStatementOption = symbolStartAnalyzer._unusedValueExpressionStatementOption;
                    var unusedValueAssignmentOption = symbolStartAnalyzer._unusedValueAssignmentOption;
                    if (!TryGetOptions(firstBlock.Syntax.SyntaxTree, firstBlock.Language,
                                       context.Options, supportsDiscard,
                                       unusedValueExpressionStatementOption, unusedValueAssignmentOption,
                                       context.CancellationToken, out var options))
                    {
                        return;
                    }

                    var blockAnalyzer = new BlockAnalyzer(symbolStartAnalyzer, options);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeExpressionStatement, OperationKind.ExpressionStatement);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeDelegateCreation, OperationKind.DelegateCreation);
                    context.RegisterOperationAction(blockAnalyzer.AnalyzeConversion, OperationKind.Conversion);
                    context.RegisterOperationBlockEndAction(blockAnalyzer.AnalyzeOperationBlockEnd);

                    return;

                    // Local Functions.
                    bool HasSyntaxErrors()
                    {
                        foreach (var operationBlock in context.OperationBlocks)
                        {
                            if (operationBlock.SemanticModel.GetSyntaxDiagnostics(operationBlock.Syntax.Span, context.CancellationToken).HasAnyErrors())
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }

                private void AnalyzeExpressionStatement(OperationAnalysisContext context)
                {
                    if (_options.UnusedValueExpressionStatementPreference == UnusedValuePreference.None)
                    {
                        return;
                    }

                    Debug.Assert(_options.UnusedValueExpressionStatementSeverity != ReportDiagnostic.Suppress);

                    // Bail out cases for report unused expression value:
                    //  1. Null type
                    //  2. Void returning method invocations
                    //  3. Bool returning method invocations
                    //  4. Constant expressions
                    //  5. Assignments, increment/decrement operations
                    //  6. Invalid operation
                    //  7. Expression and expression statement have differing first tokens, indicating explicit discard (for example, VB call statement)
                    //  8. Special cases: Methods beloning to System.Threading.Interlocked and System.Collections.Immutable.ImmutableInterlocked.
                    var expressionStatement = (IExpressionStatementOperation)context.Operation;
                    var value = expressionStatement.Operation;
                    if (value.Type == null ||
                        value.Type.SpecialType == SpecialType.System_Void ||
                        value.Type.SpecialType == SpecialType.System_Boolean ||
                        value.ConstantValue.HasValue ||
                        value is IAssignmentOperation ||
                        value is IIncrementOrDecrementOperation ||
                        value is IInvalidOperation ||
                        value.Syntax.GetFirstToken() != expressionStatement.Syntax.GetFirstToken() ||
                        (value is IInvocationOperation invocation &&
                         (invocation.TargetMethod.ContainingType.OriginalDefinition == _symbolStartAnalyzer._interlockedType ||
                          invocation.TargetMethod.ContainingType.OriginalDefinition == _symbolStartAnalyzer._immutableInterlockedType)))
                    {
                        return;
                    }

                    // IDE0056: "Expression value is never used"
                    var properties = s_propertiesMap[(_options.UnusedValueExpressionStatementPreference, isUnusedLocalAssignment: false, isRemovableAssignment: false)];
                    var diagnostic = DiagnosticHelper.Create(s_expressionValueIsUnusedRule,
                                                             value.Syntax.GetLocation(),
                                                             _options.UnusedValueExpressionStatementSeverity,
                                                             additionalLocations: null,
                                                             properties);
                    context.ReportDiagnostic(diagnostic);
                }

                private void AnalyzeDelegateCreation(OperationAnalysisContext operationAnalysisContext)
                    => _hasDelegateCreation = true;

                private void AnalyzeConversion(OperationAnalysisContext operationAnalysisContext)
                {
                    if (_hasConversionFromDelegateTypeToNonDelegteType)
                    {
                        return;
                    }

                    var conversion = (IConversionOperation)operationAnalysisContext.Operation;
                    if (conversion.Operand.Type.IsDelegateType() &&
                        !conversion.Type.IsDelegateType())
                    {
                        _hasConversionFromDelegateTypeToNonDelegteType = true;
                    }
                }

                private void AnalyzeOperationBlockEnd(OperationBlockAnalysisContext context)
                {
                    var isComputingUnusedParams = _options.IsComputingUnusedParams(context.OwningSymbol);
                    if (_options.UnusedValueExpressionStatementPreference == UnusedValuePreference.None &&
                        !isComputingUnusedParams)
                    {
                        return;
                    }

                    var hasBlockWithAllUsedDefinitions = false;
                    var resultsFromFlowAnalysis = PooledHashSet<DefinitionUsageResult>.GetInstance();

                    try
                    {
                        foreach (var operationBlock in context.OperationBlocks)
                        {
                            if (!ShouldAnalyze(operationBlock))
                            {
                                continue;
                            }

                            // First perform the fast, aggressive, imprecise operation-tree based reaching definitions analysis.
                            // This analysis might flag some "used" definitions as "unused", but will not miss reporting any truly unused definitions.
                            // This initial pass helps us reduce the number of methods for which we perform the slower second pass.
                            // We perform the first fast pass only if there are no delegate creations,
                            // as that requires us to track delegate creation targets, which needs flow analysis.
                            if (!_hasDelegateCreation)
                            {
                                var resultFromOperationBlockAnalysis = ReachingDefinitionsAnalysis.Run(operationBlock, context.OwningSymbol, context.CancellationToken);
                                if (!resultFromOperationBlockAnalysis.HasUnusedDefinitions())
                                {
                                    // Assert that even slow pass (dataflow analysis) would have yielded no unused definitions.
                                    Debug.Assert(!ReachingDefinitionsAnalysis.Run(context.GetControlFlowGraph(operationBlock), context.OwningSymbol, context.CancellationToken)
                                                 .HasUnusedDefinitions());
                                    hasBlockWithAllUsedDefinitions = true;
                                    continue;
                                }
                            }

                            // Now perform the slower, precise, CFG based reaching definitions dataflow analysis to identify the actual unused definitions.
                            var cfg = context.GetControlFlowGraph(operationBlock);
                            var resultFromFlowAnalysis = ReachingDefinitionsAnalysis.Run(cfg, context.OwningSymbol, context.CancellationToken);
                            resultsFromFlowAnalysis.Add(resultFromFlowAnalysis);

                            foreach (var (unusedSymbol, unusedDefinition) in resultFromFlowAnalysis.GetUnusedDefinitions())
                            {
                                if (unusedDefinition == null)
                                {
                                    // Null definition indicates initial definition for parameter from method declaration.
                                    // Report unused local function parameters (which are specific to this operation block) here.
                                    // We process parameters for the context's owning symbol after analyzing all operation blocks for this symbol
                                    // as we need to verify parameter usages across all operation blocks before flagging it as unused.
                                    // For example, a constructor with both a constructor initializer and body will have two different operation blocks.
                                    var unusedParameter = (IParameterSymbol)unusedSymbol;
                                    if (isComputingUnusedParams &&
                                        unusedParameter.ContainingSymbol.IsLocalFunction())
                                    {
                                        var hasReference = resultFromFlowAnalysis.SymbolsRead.Contains(unusedParameter);
                                        _symbolStartAnalyzer.ReportUnusedParameterDiagnostic(unusedParameter, hasReference, context.ReportDiagnostic, context.Options, context.CancellationToken);
                                    }

                                    continue;
                                }

                                if (ShouldReportUnusedValueDiagnostic(unusedSymbol, unusedDefinition, resultFromFlowAnalysis, out var properties))
                                {
                                    // IDE0057: "Value assigned to '{0}' is never used"
                                    var diagnostic = DiagnosticHelper.Create(s_valueAssignedIsUnusedRule,
                                                                             _symbolStartAnalyzer._getDefinitionLocationToFade(unusedDefinition),
                                                                             _options.UnusedValueAssignmentSeverity,
                                                                             additionalLocations: null,
                                                                             properties,
                                                                             unusedSymbol.Name);
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }

                        // Process parameters for the context's OwningSymbol that are unused across all operation blocks.
                        if (isComputingUnusedParams &&
                            !hasBlockWithAllUsedDefinitions &&
                            resultsFromFlowAnalysis.Count > 0 &&
                            context.OwningSymbol is IMethodSymbol method)
                        {
                            foreach (var parameter in method.Parameters)
                            {
                                bool isUsed = false;
                                bool isSymbolRead = false;
                                var isRefOrOutParam = parameter.IsRefOrOut();

                                foreach (var resultFromFlowAnalysis in resultsFromFlowAnalysis)
                                {
                                    if (resultFromFlowAnalysis.IsInitialParameterValueUsed(parameter))
                                    {
                                        isUsed = true;
                                        break;
                                    }

                                    isSymbolRead |= resultFromFlowAnalysis.SymbolsRead.Contains(parameter);

                                    // Ref/Out parameters are considered used if they any reads or writes (note that we always have 1 definition for input value).
                                    if (isRefOrOutParam &&
                                        (isSymbolRead ||
                                        resultFromFlowAnalysis.GetDefinitionCount(parameter) > 1))
                                    {
                                        isUsed = true;
                                        break;
                                    }
                                }

                                if (!isUsed)
                                {
                                    _symbolStartAnalyzer._unusedParameters.GetOrAdd(parameter, isSymbolRead);
                                }
                            }
                        }
                    }
                    finally
                    {
                        resultsFromFlowAnalysis.Free();
                    }

                    return;

                    // Local functions.
                    bool ShouldAnalyze(IOperation operationBlock)
                    {
                        switch (operationBlock.Kind)
                        {
                            case OperationKind.None:
                            case OperationKind.ParameterInitializer:
                                // Skip blocks from attributes (which have OperationKind.None) and parameter initializers.
                                return false;
                        }

                        // We currently do not support points-to analysis, so we cannot accurately 
                        // track delegate invocations for all cases.
                        // We attempt to do our best effort delegate invocation analysis as follows:

                        //  1. If we have no delegate creation operation, our current analysis works fine,
                        //     return true.
                        if (!_hasDelegateCreation)
                        {
                            return true;
                        }

                        //  2. Bail out if we have a conversion from a delegate type to a non-delegate type.
                        //     We can analyze this correctly when we do points-to-analysis.
                        if (_hasConversionFromDelegateTypeToNonDelegteType)
                        {
                            return false;
                        }

                        //  3. Bail out for method returning delegates or ref/out parameters of delegate type.
                        //     We can analyze this correctly when we do points-to-analysis.
                        if (context.OwningSymbol is IMethodSymbol method &&
                            (method.ReturnType.IsDelegateType() ||
                             method.Parameters.Any(p => p.IsRefOrOut() && p.Type.IsDelegateType())))
                        {
                            return false;
                        }

                        //  4. Otherwise, we execute analysis by walking the reaching definitions chain to attempt to
                        //     find the target method being invoked.
                        //     This works for most common and simple cases where a local is assigned a lambda and invoked later.
                        //     If we are unable to find a target, we will conservatively mark all current definitions as read.
                        return true;
                    }

                    bool ShouldReportUnusedValueDiagnostic(
                        ISymbol unusedSymbol,
                        IOperation unusedDefinition,
                        DefinitionUsageResult resultFromFlowAnalysis,
                        out ImmutableDictionary<string, string> properties)
                    {
                        properties = null;
                        if (_options.UnusedValueAssignmentPreference == UnusedValuePreference.None)
                        {
                            return false;
                        }

                        Debug.Assert(_options.UnusedValueAssignmentSeverity != ReportDiagnostic.Suppress);

                        var isUnusedLocalAssignment = unusedSymbol is ILocalSymbol localSymbol &&
                                                      !resultFromFlowAnalysis.SymbolsRead.Contains(localSymbol);
                        var isRemovableAssignment = IsRemovableAssignment(unusedDefinition);

                        if (isUnusedLocalAssignment &&
                            !isRemovableAssignment &&
                            _options.UnusedValueAssignmentPreference == UnusedValuePreference.UnusedLocalVariable)
                        {
                            // Meets current user preference, skip reporting diagnostic.
                            return false;
                        }

                        properties = s_propertiesMap[(_options.UnusedValueAssignmentPreference, isUnusedLocalAssignment, isRemovableAssignment)];
                        return true;
                    }

                    bool IsRemovableAssignment(IOperation unusedDefinition)
                    {
                        if (unusedDefinition.Parent is IAssignmentOperation assignment &&
                            assignment.Target == unusedDefinition)
                        {
                            if (assignment.Value.ConstantValue.HasValue)
                            {
                                return true;
                            }

                            switch (assignment.Value.Kind)
                            {
                                case OperationKind.ParameterReference:
                                case OperationKind.LocalReference:
                                    return true;

                                case OperationKind.FieldReference:
                                    var fieldReference = (IFieldReferenceOperation)assignment.Value;
                                    return fieldReference.Instance == null || fieldReference.Instance.Kind == OperationKind.InstanceReference;
                            }
                        }
                        else if (unusedDefinition.Parent is IIncrementOrDecrementOperation)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        private static bool TryGetOptions(
            SyntaxTree syntaxTree,
            string language,
            AnalyzerOptions analyzerOptions,
            bool supportsDiscard,
            Option<CodeStyleOption<UnusedValuePreference>> unusedValueExpressionStatementOption,
            Option<CodeStyleOption<UnusedValuePreference>> unusedValueAssignmentOption,
            CancellationToken cancellationToken,
            out Options options)
        {
            options = null;
            var optionSet = analyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return false;
            }

            var unusedParametersPreference = optionSet.GetOption(CodeStyleOptions.UnusedParameters, language).Value;
            var (unusedValueExpressionStatementPreference, unusedValueExpressionStatementSeverity) = GetPreferenceAndSeverity(unusedValueExpressionStatementOption);
            var (unusedValueAssignmentPreference, unusedValueAssignmentSeverity) = GetPreferenceAndSeverity(unusedValueAssignmentOption);
            if (unusedParametersPreference == UnusedParametersPreference.None &&
                unusedValueExpressionStatementPreference == UnusedValuePreference.None &&
                unusedValueAssignmentPreference == UnusedValuePreference.None)
            {
                return false;
            }

            options = new Options(unusedValueExpressionStatementPreference, unusedValueExpressionStatementSeverity,
                unusedValueAssignmentPreference, unusedValueAssignmentSeverity, unusedParametersPreference);
            return true;

            // Local functions.
            (UnusedValuePreference preference, ReportDiagnostic severity) GetPreferenceAndSeverity(
                Option<CodeStyleOption<UnusedValuePreference>> codeStyleOption)
            {
                var option = optionSet.GetOption(codeStyleOption);
                var preference = option?.Value ?? UnusedValuePreference.None;
                if (preference == UnusedValuePreference.None ||
                    option.Notification.Severity == ReportDiagnostic.Suppress)
                {
                    return (UnusedValuePreference.None, ReportDiagnostic.Suppress);
                }

                // If language or language version does not support discard, fall back to prefer unused local variable.
                if (!supportsDiscard && preference == UnusedValuePreference.DiscardVariable)
                {
                    preference = UnusedValuePreference.UnusedLocalVariable;
                }

                return (preference, option.Notification.Severity);
            }
        }

        private sealed class Options
        {
            private readonly UnusedParametersPreference _unusedParametersPreference;
            public Options(
                UnusedValuePreference unusedValueExpressionStatementPreference,
                ReportDiagnostic unusedValueExpressionStatementSeverity,
                UnusedValuePreference unusedValueAssignmentPreference,
                ReportDiagnostic unusedValueAssignmentSeverity,
                UnusedParametersPreference unusedParametersPreference)
            {
                Debug.Assert(unusedValueExpressionStatementPreference != UnusedValuePreference.None ||
                             unusedValueAssignmentPreference != UnusedValuePreference.None ||
                             unusedParametersPreference != UnusedParametersPreference.None);

                UnusedValueExpressionStatementPreference = unusedValueExpressionStatementPreference;
                UnusedValueExpressionStatementSeverity = unusedValueExpressionStatementSeverity;
                UnusedValueAssignmentPreference = unusedValueAssignmentPreference;
                UnusedValueAssignmentSeverity = unusedValueAssignmentSeverity;
                _unusedParametersPreference = unusedParametersPreference;
            }

            public UnusedValuePreference UnusedValueExpressionStatementPreference { get; }
            public ReportDiagnostic UnusedValueExpressionStatementSeverity { get; }
            public UnusedValuePreference UnusedValueAssignmentPreference { get; }
            public ReportDiagnostic UnusedValueAssignmentSeverity { get; }
            public bool IsComputingUnusedParams(ISymbol symbol)
                => ShouldReportUnusedParameters(symbol, _unusedParametersPreference);
        }

        public static bool ShouldReportUnusedParameters(ISymbol symbol, UnusedParametersPreference unusedParametersPreference)
        {
            switch (unusedParametersPreference)
            {
                case UnusedParametersPreference.None:
                    return false;
                case UnusedParametersPreference.AllMethods:
                    return true;
                case UnusedParametersPreference.PrivateMethods:
                    return symbol.DeclaredAccessibility == Accessibility.Private;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public static UnusedValuePreference GetUnusedValuePreference(Diagnostic diagnostic)
        {
            if (diagnostic.Properties != null &&
                diagnostic.Properties.TryGetValue(UnusedValuePreferenceKey, out var preference))
            {
                switch (preference)
                {
                    case nameof(UnusedValuePreference.DiscardVariable):
                        return UnusedValuePreference.DiscardVariable;

                    case nameof(UnusedValuePreference.UnusedLocalVariable):
                        return UnusedValuePreference.UnusedLocalVariable;
                }
            }

            return UnusedValuePreference.None;
        }

        public static bool GetIsUnusedLocalDiagnostic(Diagnostic diagnostic)
        {
            Debug.Assert(GetUnusedValuePreference(diagnostic) != UnusedValuePreference.None);
            return diagnostic.Properties.ContainsKey(IsUnusedLocalAssignmentKey);
        }

        public static bool GetIsRemovableAssignmentDiagnostic(Diagnostic diagnostic)
        {
            Debug.Assert(GetUnusedValuePreference(diagnostic) != UnusedValuePreference.None);
            return diagnostic.Properties.ContainsKey(IsRemovableAssignmentKey);
        }
    }
}
