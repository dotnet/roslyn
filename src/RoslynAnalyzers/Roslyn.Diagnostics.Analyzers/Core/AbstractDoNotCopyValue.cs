// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    /// <summary>
    /// RS0042: <inheritdoc cref="DoNotCopyValueTitle"/>
    /// </summary>
    public abstract class AbstractDoNotCopyValue : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = CreateLocalizableResourceString(nameof(DoNotCopyValueTitle));

        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor UnsupportedUseRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueUnsupportedUseMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueUnsupportedUseDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor AvoidNullableWrapperRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueAvoidNullableWrapperMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueAvoidNullableWrapperDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor NoAssignValueFromReferenceRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueNoAssignValueFromReferenceMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueNoAssignValueFromReferenceDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor NoReturnValueFromReferenceRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueNoReturnValueFromReferenceMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueNoReturnValueFromReferenceDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor NoBoxingRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueNoBoxingMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueNoBoxingDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor NoUnboxingRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueNoUnboxingMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueNoUnboxingDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor NoFieldOfCopyableTypeRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueNoFieldOfCopyableTypeMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueNoFieldOfCopyableTypeDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor NoAutoPropertyRule = new(
            RoslynDiagnosticIds.DoNotCopyValueRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotCopyValueNoAutoPropertyMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotCopyValueNoAutoPropertyDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule, UnsupportedUseRule, NoBoxingRule, NoUnboxingRule, NoFieldOfCopyableTypeRule, NoAutoPropertyRule);

        protected abstract NonCopyableWalker CreateWalker(OperationBlockAnalysisContext context, NonCopyableTypesCache cache);

        protected abstract NonCopyableSymbolWalker CreateSymbolWalker(SymbolAnalysisContext context, NonCopyableTypesCache cache);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var cache = new NonCopyableTypesCache(context.Compilation);
                context.RegisterOperationBlockAction(context => AnalyzeOperationBlock(context, cache));
                context.RegisterSymbolAction(
                    context => AnalyzeSymbol(context, cache),
                    //SymbolKind.Alias,
                    //SymbolKind.ArrayType,
                    //SymbolKind.Assembly,
                    //SymbolKind.Discard,
                    //SymbolKind.DynamicType,
                    //SymbolKind.ErrorType,
                    SymbolKind.Event,
                    SymbolKind.Field,
                    //SymbolKind.FunctionPointerType,
                    //SymbolKind.Label,
                    //SymbolKind.Local,
                    SymbolKind.Method,
                    SymbolKind.NamedType,
                    SymbolKind.Namespace,
                    //SymbolKind.NetModule,
                    SymbolKind.Parameter,
                    //SymbolKind.PointerType,
                    //SymbolKind.Preprocessing,
                    SymbolKind.Property
                    //SymbolKind.RangeVariable,
                    //SymbolKind.TypeParameter
                    );
            });
        }

        private void AnalyzeOperationBlock(OperationBlockAnalysisContext context, NonCopyableTypesCache cache)
        {
            var walker = CreateWalker(context, cache);
            foreach (var operation in context.OperationBlocks)
            {
                walker.Visit(operation);
            }
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context, NonCopyableTypesCache cache)
        {
            var walker = CreateSymbolWalker(context, cache);
            walker.Visit(context.Symbol);
        }

        private static VisitReleaser<T> TryAddForVisit<T>(HashSet<T> set, T? value, out bool added)
            where T : class
        {
            if (value is null)
            {
                added = false;
                return default;
            }

            added = set.Add(value);
            if (!added)
                return default;

            return new VisitReleaser<T>(set, value);
        }

        [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This type is never compared.")]
        private readonly struct VisitReleaser<T> : IDisposable
            where T : class
        {
            private readonly HashSet<T> _set;
            private readonly T _value;

            public VisitReleaser(HashSet<T> set, T value)
            {
                _set = set;
                _value = value;
            }

            public void Dispose()
            {
                _set?.Remove(_value);
            }
        }

        protected abstract class NonCopyableSymbolWalker : SymbolVisitor
        {
            private readonly SymbolAnalysisContext _context;
            //private readonly HashSet<ISymbol> _handledSymbols = new();

            protected NonCopyableSymbolWalker(SymbolAnalysisContext context, NonCopyableTypesCache cache)
            {
                _context = context;
                Cache = cache;
            }

            protected NonCopyableTypesCache Cache { get; }

            public sealed override void Visit(ISymbol? symbol)
            {
                base.Visit(symbol);
            }

            public override void DefaultVisit(ISymbol symbol)
            {
                base.DefaultVisit(symbol);
            }

            public override void VisitAlias(IAliasSymbol symbol)
            {
                base.VisitAlias(symbol);
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                base.VisitArrayType(symbol);
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                base.VisitAssembly(symbol);
            }

            public override void VisitDiscard(IDiscardSymbol symbol)
            {
                base.VisitDiscard(symbol);
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                base.VisitDynamicType(symbol);
            }

            public override void VisitEvent(IEventSymbol symbol)
            {
                base.VisitEvent(symbol);
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                // Fields of copyable value types must be copyable. Copying a value type makes a shallow copy of the
                // fields, which implicitly copies any value type fields.
                if (Cache.IsNonCopyableType(symbol.Type)
                    && !Cache.IsNonCopyableType(symbol.ContainingType)
                    && symbol.ContainingType.IsValueType)
                {
                    _context.ReportDiagnostic(symbol.CreateDiagnostic(NoFieldOfCopyableTypeRule, symbol.Type, symbol));
                }

                base.VisitField(symbol);
            }

            public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
            {
                base.VisitFunctionPointerType(symbol);
            }

            public override void VisitLabel(ILabelSymbol symbol)
            {
                base.VisitLabel(symbol);
            }

            public override void VisitLocal(ILocalSymbol symbol)
            {
                base.VisitLocal(symbol);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                base.VisitMethod(symbol);
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
                base.VisitModule(symbol);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                base.VisitNamedType(symbol);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                base.VisitNamespace(symbol);
            }

            public override void VisitParameter(IParameterSymbol symbol)
            {
                base.VisitParameter(symbol);
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                base.VisitPointerType(symbol);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                // Auto-properties cannot have non-copyable types. The getter always returns the backing field by value,
                // which requires making a copy.
                if (symbol.IsAutoProperty()
                    && Cache.IsNonCopyableType(symbol.Type))
                {
                    _context.ReportDiagnostic(symbol.CreateDiagnostic(NoAutoPropertyRule, symbol.Type, symbol));
                }

                base.VisitProperty(symbol);
            }

            public override void VisitRangeVariable(IRangeVariableSymbol symbol)
            {
                base.VisitRangeVariable(symbol);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                base.VisitTypeParameter(symbol);
            }
        }

        protected abstract class NonCopyableWalker : OperationWalker
        {
            private readonly OperationBlockAnalysisContext _context;
            private readonly HashSet<IOperation> _handledOperations = [];

            protected NonCopyableWalker(OperationBlockAnalysisContext context, NonCopyableTypesCache cache)
            {
                _context = context;
                Cache = cache;
            }

            protected NonCopyableTypesCache Cache { get; }

            protected abstract bool CheckForEachGetEnumerator(IForEachLoopOperation operation, [DisallowNull] ref IConversionOperation? conversion, [DisallowNull] ref IOperation? instance);

            public override void VisitAddressOf(IAddressOfOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitAddressOf(operation);
            }

            public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.Symbol);
                CheckTypeInUnsupportedContext(operation);
                base.VisitAnonymousFunction(operation);
            }

            public override void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitAnonymousObjectCreation(operation);
            }

            public override void VisitArgument(IArgumentOperation operation)
            {
                CheckParameterSymbolInUnsupportedContext(operation, operation.Parameter);
                CheckConversionInUnsupportedContext(operation, operation.InConversion);
                CheckConversionInUnsupportedContext(operation, operation.OutConversion);

                var value = operation.Value;
                var parameterRefKind = operation.Parameter.RefKind;
                var sourceRefKind = Acquire(operation.Value);
                if (!CanAssign(sourceRefKind, parameterRefKind))
                {
                    // Mark the value as not handled
                    value = null;
                }

                using var releaser = TryAddForVisit(_handledOperations, value, out _);

                base.VisitArgument(operation);
            }

            public override void VisitArrayCreation(IArrayCreationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitArrayCreation(operation);
            }

            public override void VisitArrayElementReference(IArrayElementReferenceOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitArrayElementReference(operation);
            }

            public override void VisitArrayInitializer(IArrayInitializerOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitArrayInitializer(operation);
            }

            public override void VisitAwait(IAwaitOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitAwait(operation);
            }

            public override void VisitBinaryOperator(IBinaryOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.OperatorMethod);
                CheckTypeInUnsupportedContext(operation);
                base.VisitBinaryOperator(operation);
            }

            public override void VisitBlock(IBlockOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitBlock(operation);
            }

            public override void VisitBranch(IBranchOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitBranch(operation);
            }

            public override void VisitCatchClause(ICatchClauseOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeSymbolInUnsupportedContext(operation, operation.ExceptionType);
                CheckTypeInUnsupportedContext(operation);
                base.VisitCatchClause(operation);
            }

            public override void VisitCaughtException(ICaughtExceptionOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitCaughtException(operation);
            }

            public override void VisitCoalesce(ICoalesceOperation operation)
            {
                CheckConversionInUnsupportedContext(operation, operation.ValueConversion);
                CheckTypeInUnsupportedContext(operation);
                base.VisitCoalesce(operation);
            }

            public override void VisitCoalesceAssignment(ICoalesceAssignmentOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitCoalesceAssignment(operation);
            }

            public override void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.OperatorMethod);
                CheckConversionInUnsupportedContext(operation, operation.InConversion);
                CheckConversionInUnsupportedContext(operation, operation.OutConversion);
                CheckTypeInUnsupportedContext(operation);
                base.VisitCompoundAssignment(operation);
            }

            public override void VisitConditional(IConditionalOperation operation)
            {
                if (!operation.IsRef && Acquire(operation) != RefKind.None)
                {
                    CheckTypeInUnsupportedContext(operation);
                }

                var currentIsHandled = _handledOperations.Contains(operation);
                var handledWhenTrue = currentIsHandled ? operation.WhenTrue : null;
                var handledWhenFalse = currentIsHandled ? operation.WhenFalse : null;
                using var _1 = TryAddForVisit(_handledOperations, handledWhenTrue, out _);
                using var _2 = TryAddForVisit(_handledOperations, handledWhenFalse, out _);

                base.VisitConditional(operation);
            }

            public override void VisitConditionalAccess(IConditionalAccessOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitConditionalAccess(operation);
            }

            public override void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitConditionalAccessInstance(operation);
            }

            public override void VisitConstantPattern(IConstantPatternOperation operation)
            {
                CheckTypeSymbolInUnsupportedContext(operation, operation.InputType);
                CheckTypeInUnsupportedContext(operation);
                base.VisitConstantPattern(operation);
            }

            public override void VisitConstructorBodyOperation(IConstructorBodyOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitConstructorBodyOperation(operation);
            }

            public override void VisitConversion(IConversionOperation operation)
            {
                if (!IsSupportedConversion())
                {
                    CheckMethodSymbolInUnsupportedContext(operation, operation.OperatorMethod);
                    CheckConversionInUnsupportedContext(operation, operation.Conversion);
                    CheckTypeInUnsupportedContext(operation);
                }

                base.VisitConversion(operation);
                return;

                // Local functions
                bool IsSupportedConversion()
                {
                    if (!operation.Conversion.Exists)
                    {
                        // The compiler will warn or error about this case
                        return true;
                    }

                    if (operation.Conversion.MethodSymbol is object)
                    {
                        // Not yet handled
                        return false;
                    }

                    switch (Acquire(operation.Operand))
                    {
                        case RefKind.None:
                        case RefKind.Ref or RefKind.RefReadOnly or RefKindEx.RefReadOnlyParameter when operation.Conversion.IsIdentity:
                            return true;

                        default:
                            break;
                    }

                    return false;
                }
            }

            public override void VisitDeclarationExpression(IDeclarationExpressionOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDeclarationExpression(operation);
            }

            public override void VisitDeclarationPattern(IDeclarationPatternOperation operation)
            {
                CheckSymbolInUnsupportedContext(operation, operation.DeclaredSymbol);
                CheckTypeSymbolInUnsupportedContext(operation, operation.InputType);
                CheckTypeSymbolInUnsupportedContext(operation, operation.MatchedType);
                CheckTypeInUnsupportedContext(operation);
                base.VisitDeclarationPattern(operation);
            }

            public override void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDeconstructionAssignment(operation);
            }

            public override void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDefaultCaseClause(operation);
            }

            public override void VisitDefaultValue(IDefaultValueOperation operation)
            {
                // default(T) is a valid way to acquire a 'T'. Non-defaultable type analysis is handled separately.
                base.VisitDefaultValue(operation);
            }

            public override void VisitDelegateCreation(IDelegateCreationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDelegateCreation(operation);
            }

            public override void VisitDiscardOperation(IDiscardOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDiscardOperation(operation);
            }

            public override void VisitDiscardPattern(IDiscardPatternOperation operation)
            {
                CheckTypeSymbolInUnsupportedContext(operation, operation.InputType);
                CheckTypeInUnsupportedContext(operation);
                base.VisitDiscardPattern(operation);
            }

            public override void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDynamicIndexerAccess(operation);
            }

            public override void VisitDynamicInvocation(IDynamicInvocationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDynamicInvocation(operation);
            }

            public override void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation)
            {
                foreach (var type in operation.TypeArguments)
                {
                    CheckTypeSymbolInUnsupportedContext(operation, type);
                }

                CheckTypeSymbolInUnsupportedContext(operation, operation.ContainingType);
                CheckTypeInUnsupportedContext(operation);
                base.VisitDynamicMemberReference(operation);
            }

            public override void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitDynamicObjectCreation(operation);
            }

            public override void VisitEmpty(IEmptyOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitEmpty(operation);
            }

            public override void VisitEnd(IEndOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitEnd(operation);
            }

            public override void VisitEventAssignment(IEventAssignmentOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitEventAssignment(operation);
            }

            public override void VisitEventReference(IEventReferenceOperation operation)
            {
                CheckEventSymbolInUnsupportedContext(operation, operation.Event);
                CheckTypeInUnsupportedContext(operation);
                base.VisitEventReference(operation);
            }

            public override void VisitExpressionStatement(IExpressionStatementOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);

                // The result of the top-most operation in an expression statement is not used
                using var releaser = TryAddForVisit(_handledOperations, operation.Operation, out _);

                base.VisitExpressionStatement(operation);
            }

            public override void VisitFieldInitializer(IFieldInitializerOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                foreach (var field in operation.InitializedFields)
                {
                    CheckFieldSymbolInUnsupportedContext(operation, field);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitFieldInitializer(operation);
            }

            public override void VisitFieldReference(IFieldReferenceOperation operation)
            {
                // An instance never needs to be copied to read or write a field value. Even in cases where the instance
                // is read-only, attempts to write to the field would be reported as compiler errors so analyzer
                // diagnostics are not required.
                using var releaser = TryAddForVisit(_handledOperations, operation.Instance, out _);

                CheckFieldSymbolInUnsupportedContext(operation, operation.Field);
                CheckTypeInUnsupportedContext(operation);
                base.VisitFieldReference(operation);
            }

            public override void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.Symbol);
                CheckTypeInUnsupportedContext(operation);
                base.VisitFlowAnonymousFunction(operation);
            }

            public override void VisitFlowCapture(IFlowCaptureOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitFlowCapture(operation);
            }

            public override void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitFlowCaptureReference(operation);
            }

            public override void VisitForEachLoop(IForEachLoopOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                // 'foreach' operations have an identity conversion for the collection property, and then invoke the
                // GetEnumerator method.
                var instance = operation.Collection as IConversionOperation;
                var instance2 = (operation.Collection as IConversionOperation)?.Operand;

                if (instance2 is null)
                {
                    // Didn't match the known pattern
                    instance = null;
                }
                else if (instance?.Conversion is not { IsIdentity: true, MethodSymbol: null })
                {
                    // Not a supported conversion
                    instance = null;
                    instance2 = null;
                }
                else
                {
                    // Treat this as an invocation of the GetEnumerator method.
                    if (!CheckForEachGetEnumerator(operation, ref instance, ref instance2))
                    {
                        // Not supported
                        instance = null;
                        instance2 = null;
                    }
                }

                using var releaser = TryAddForVisit(_handledOperations, instance, out _);
                using var releaser2 = TryAddForVisit(_handledOperations, instance2, out _);

                CheckTypeInUnsupportedContext(operation);
                base.VisitForEachLoop(operation);
            }

            public override void VisitForLoop(IForLoopOperation operation)
            {
                foreach (var local in operation.ConditionLocals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitForLoop(operation);
            }

            public override void VisitForToLoop(IForToLoopOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitForToLoop(operation);
            }

            public override void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.OperatorMethod);
                CheckTypeInUnsupportedContext(operation);
                base.VisitIncrementOrDecrement(operation);
            }

            public override void VisitInstanceReference(IInstanceReferenceOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitInstanceReference(operation);
            }

            public override void VisitInterpolatedString(IInterpolatedStringOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitInterpolatedString(operation);
            }

            public override void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitInterpolatedStringText(operation);
            }

            public override void VisitInterpolation(IInterpolationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitInterpolation(operation);
            }

            public override void VisitInvalid(IInvalidOperation operation)
            {
                // This is already a compiler error. No need to report more diagnostics on it.
                base.VisitInvalid(operation);
            }

            public override void VisitInvocation(IInvocationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);

                var instance = operation.Instance;
                if (instance is object
                    && Cache.IsNonCopyableType(operation.TargetMethod.ReceiverType)
                    && !operation.TargetMethod.IsReadOnly
                    && Acquire(instance) is RefKind.In or RefKindEx.RefReadOnlyParameter)
                {
                    // mark the instance as not checked by this method
                    instance = null;
                }

                using var releaser = TryAddForVisit(_handledOperations, instance, out _);

                // No need to check the method signature further. Parameters will be checked by the IArgumentOperation.
                base.VisitInvocation(operation);
            }

            public override void VisitIsNull(IIsNullOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitIsNull(operation);
            }

            public override void VisitIsPattern(IIsPatternOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitIsPattern(operation);
            }

            public override void VisitIsType(IIsTypeOperation operation)
            {
                CheckTypeSymbolInUnsupportedContext(operation, operation.TypeOperand);
                CheckTypeInUnsupportedContext(operation);
                base.VisitIsType(operation);
            }

            public override void VisitLabeled(ILabeledOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitLabeled(operation);
            }

            public override void VisitLiteral(ILiteralOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitLiteral(operation);
            }

            public override void VisitLocalFunction(ILocalFunctionOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.Symbol);
                CheckTypeInUnsupportedContext(operation);
                base.VisitLocalFunction(operation);
            }

            public override void VisitLocalReference(ILocalReferenceOperation operation)
            {
                CheckLocalSymbolInUnsupportedContext(operation, operation.Local);
                CheckTypeInUnsupportedContext(operation);
                base.VisitLocalReference(operation);
            }

            public override void VisitLock(ILockOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitLock(operation);
            }

            public override void VisitMemberInitializer(IMemberInitializerOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitMemberInitializer(operation);
            }

            public override void VisitMethodBodyOperation(IMethodBodyOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitMethodBodyOperation(operation);
            }

            public override void VisitMethodReference(IMethodReferenceOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.Method);
                CheckTypeInUnsupportedContext(operation);
                base.VisitMethodReference(operation);
            }

            public override void VisitNameOf(INameOfOperation operation)
            {
                // A 'nameof' operation does not copy anything in the value
                return;
            }

            public override void VisitObjectCreation(IObjectCreationOperation operation)
            {
                // 'new T()' is a valid way to acquire a 'T'. Non-defaultable type analysis is handled separately.
                // No need to check the method signature further. Parameters will be checked by the IArgumentOperation.
                // Also mark the initializer (if any) as handled.
                using var releaser = TryAddForVisit(_handledOperations, operation.Initializer, out _);
                base.VisitObjectCreation(operation);
            }

            public override void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitObjectOrCollectionInitializer(operation);
            }

            public override void VisitOmittedArgument(IOmittedArgumentOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitOmittedArgument(operation);
            }

            public override void VisitParameterInitializer(IParameterInitializerOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckParameterSymbolInUnsupportedContext(operation, operation.Parameter);
                CheckTypeInUnsupportedContext(operation);
                base.VisitParameterInitializer(operation);
            }

            public override void VisitParameterReference(IParameterReferenceOperation operation)
            {
                CheckParameterSymbolInUnsupportedContext(operation, operation.Parameter);
                CheckTypeInUnsupportedContext(operation);
                base.VisitParameterReference(operation);
            }

            public override void VisitParenthesized(IParenthesizedOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitParenthesized(operation);
            }

            public override void VisitPatternCaseClause(IPatternCaseClauseOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitPatternCaseClause(operation);
            }

            public override void VisitPropertyInitializer(IPropertyInitializerOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                foreach (var property in operation.InitializedProperties)
                {
                    CheckPropertySymbolInUnsupportedContext(operation, property);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitPropertyInitializer(operation);
            }

            public override void VisitPropertyReference(IPropertyReferenceOperation operation)
            {
                // Allow a property to return a non-copyable type by value
                if (Acquire(operation) != RefKind.None)
                {
                    CheckTypeInUnsupportedContext(operation);
                }

                var instance = operation.Instance;
                if (instance is object
                    && Cache.IsNonCopyableType(operation.Property.ContainingType)
                    && Acquire(instance) is RefKind.In or RefKindEx.RefReadOnlyParameter)
                {
                    if (operation.IsSetMethodInvocation())
                    {
                        if (operation.Property.SetMethod is { IsReadOnly: false })
                        {
                            // mark the instance as not checked by this method
                            instance = null;
                        }
                    }
                    else
                    {
                        if (operation.Property.GetMethod is { IsReadOnly: false })
                        {
                            // mark the instance as not checked by this method
                            instance = null;
                        }
                    }
                }

                using var releaser = TryAddForVisit(_handledOperations, instance, out _);

                // No need to check the method signature further. Parameters will be checked by the IArgumentOperation.
                base.VisitPropertyReference(operation);
            }

            public override void VisitRaiseEvent(IRaiseEventOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitRaiseEvent(operation);
            }

            public override void VisitRangeCaseClause(IRangeCaseClauseOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitRangeCaseClause(operation);
            }

            public override void VisitRangeOperation(IRangeOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.Method);
                CheckTypeInUnsupportedContext(operation);
                base.VisitRangeOperation(operation);
            }

            public override void VisitReDim(IReDimOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitReDim(operation);
            }

            public override void VisitReDimClause(IReDimClauseOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitReDimClause(operation);
            }

            public override void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitRelationalCaseClause(operation);
            }

            public override void VisitReturn(IReturnOperation operation)
            {
                var returnedValue = operation.ReturnedValue;
                if (returnedValue is not null && Acquire(returnedValue) != RefKind.None)
                {
                    if (returnedValue is ILocalReferenceOperation { Local.IsRef: false })
                    {
                        // Returning a by-value local is allowed
                    }
                    else if (returnedValue is IParameterReferenceOperation { Parameter.RefKind: RefKind.None })
                    {
                        // Returning a by-value parameter is allowed
                    }
                    else if (operation.GetRefKind(_context.OwningSymbol) == RefKind.None && Cache.IsNonCopyableType(returnedValue.Type))
                    {
                        _context.ReportDiagnostic(returnedValue.CreateDiagnostic(NoReturnValueFromReferenceRule, returnedValue.Type));
                    }
                }

                using var releaser = TryAddForVisit(_handledOperations, returnedValue, out _);

                CheckTypeInUnsupportedContext(operation);
                base.VisitReturn(operation);
            }

            public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
            {
                var target = operation.Target;
                var value = operation.Value;

                var sourceRefKind = Acquire(value);
                if (Cache.IsNonCopyableType(value.Type)
                    && !operation.IsRef
                    && target.Kind != OperationKind.Discard
                    && sourceRefKind != RefKind.None)
                {
                    _context.ReportDiagnostic(value.CreateDiagnostic(NoAssignValueFromReferenceRule, value.Type));
                }

                using var releaser1 = TryAddForVisit(_handledOperations, target, out _);
                using var releaser2 = TryAddForVisit(_handledOperations, value, out _);

                base.VisitSimpleAssignment(operation);
            }

            public override void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitSingleValueCaseClause(operation);
            }

            public override void VisitSizeOf(ISizeOfOperation operation)
            {
                CheckTypeSymbolInUnsupportedContext(operation, operation.TypeOperand);
                CheckTypeInUnsupportedContext(operation);
                base.VisitSizeOf(operation);
            }

            public override void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation)
            {
                CheckLocalSymbolInUnsupportedContext(operation, operation.Local);
                CheckTypeInUnsupportedContext(operation);
                base.VisitStaticLocalInitializationSemaphore(operation);
            }

            public override void VisitStop(IStopOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitStop(operation);
            }

            public override void VisitSwitch(ISwitchOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitSwitch(operation);
            }

            public override void VisitSwitchCase(ISwitchCaseOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitSwitchCase(operation);
            }

            public override void VisitSwitchExpression(ISwitchExpressionOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitSwitchExpression(operation);
            }

            public override void VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitSwitchExpressionArm(operation);
            }

            public override void VisitThrow(IThrowOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitThrow(operation);
            }

            public override void VisitTranslatedQuery(ITranslatedQueryOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitTranslatedQuery(operation);
            }

            public override void VisitTry(ITryOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitTry(operation);
            }

            public override void VisitTuple(ITupleOperation operation)
            {
                CheckTypeSymbolInUnsupportedContext(operation, operation.NaturalType);
                CheckTypeInUnsupportedContext(operation);
                base.VisitTuple(operation);
            }

            public override void VisitTupleBinaryOperator(ITupleBinaryOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitTupleBinaryOperator(operation);
            }

            public override void VisitTypeOf(ITypeOfOperation operation)
            {
                CheckTypeSymbolInUnsupportedContext(operation, operation.TypeOperand);
                CheckTypeInUnsupportedContext(operation);
                base.VisitTypeOf(operation);
            }

            public override void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitTypeParameterObjectCreation(operation);
            }

            public override void VisitUnaryOperator(IUnaryOperation operation)
            {
                CheckMethodSymbolInUnsupportedContext(operation, operation.OperatorMethod);
                CheckTypeInUnsupportedContext(operation);
                base.VisitUnaryOperator(operation);
            }

            public override void VisitUsing(IUsingOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);

                var resource = operation.Resources;
                if (Acquire(resource) != RefKind.None)
                {
                    // Not yet handled
                    resource = null;
                }

                using var releaser = TryAddForVisit(_handledOperations, resource, out _);
                base.VisitUsing(operation);
            }

            public override void VisitVariableDeclaration(IVariableDeclarationOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);

                var initializer = operation.Initializer;
                if (initializer is not null)
                {
                    var symbol = operation.Declarators.Single().Symbol;
                    var localRefKind = symbol.RefKind;
                    var sourceRefKind = Acquire(operation.Initializer?.Value);
                    if (Cache.IsNonCopyableType(symbol.Type)
                        && !CanAssign(sourceRefKind, localRefKind))
                    {
                        _context.ReportDiagnostic(initializer.CreateDiagnostic(NoAssignValueFromReferenceRule, symbol.Type));
                    }
                }

                using var releaser1 = TryAddForVisit(_handledOperations, initializer, out _);
                using var releaser2 = TryAddForVisit(_handledOperations, initializer?.Value, out _);

                base.VisitVariableDeclaration(operation);
            }

            public override void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation)
            {
                CheckTypeInUnsupportedContext(operation);
                base.VisitVariableDeclarationGroup(operation);
            }

            public override void VisitVariableDeclarator(IVariableDeclaratorOperation operation)
            {
                CheckLocalSymbolInUnsupportedContext(operation, operation.Symbol);

                var initializer = operation.Initializer;
                var localRefKind = operation.Symbol.RefKind;
                var sourceRefKind = Acquire(operation.Initializer?.Value);
                if (initializer is not null
                    && Cache.IsNonCopyableType(operation.Symbol.Type)
                    && !CanAssign(sourceRefKind, localRefKind))
                {
                    _context.ReportDiagnostic(initializer.CreateDiagnostic(NoAssignValueFromReferenceRule, operation.Symbol.Type));
                }

                using var releaser1 = TryAddForVisit(_handledOperations, initializer, out _);
                using var releaser2 = TryAddForVisit(_handledOperations, initializer?.Value, out _);

                base.VisitVariableDeclarator(operation);
            }

            public override void VisitVariableInitializer(IVariableInitializerOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitVariableInitializer(operation);
            }

            public override void VisitWhileLoop(IWhileLoopOperation operation)
            {
                foreach (var local in operation.Locals)
                {
                    CheckLocalSymbolInUnsupportedContext(operation, local);
                }

                CheckTypeInUnsupportedContext(operation);
                base.VisitWhileLoop(operation);
            }

            [Obsolete("ICollectionElementInitializerOperation has been replaced with IInvocationOperation and IDynamicInvocationOperation", error: true)]
            public override void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation)
                => throw new NotSupportedException();

            private static bool CanAssign(RefKind sourceRefKind, RefKind targetRefKind)
            {
                return (sourceRefKind, targetRefKind) switch
                {
                    (RefKind.None, _) => true,
                    (RefKind.Ref, RefKind.Ref or RefKind.RefReadOnly or RefKindEx.RefReadOnlyParameter) => true,
                    (RefKind.RefReadOnly or RefKindEx.RefReadOnlyParameter, RefKind.RefReadOnly or RefKindEx.RefReadOnlyParameter) => true,
                    _ => false,
                };
            }

            protected RefKind Acquire(IOperation? operation)
            {
                if (operation is null)
                    return RefKind.RefReadOnly;

                switch (operation.Kind)
                {
                    case OperationKind.ArrayElementReference:
                        return RefKind.Ref;

                    case OperationKind.Await:
                        return ((IAwaitOperation)operation).Operation switch
                        {
                            { Type: INamedTypeSymbol { OriginalDefinition: var taskType } }
                                when SymbolEqualityComparer.Default.Equals(taskType, Cache.ValueTaskT) || SymbolEqualityComparer.Default.Equals(taskType, Cache.ConfiguredValueTaskAwaitableT)
                                => RefKind.None,
                            var awaited => Acquire(awaited),
                        };

                    case OperationKind.Conditional:
                        var conditional = (IConditionalOperation)operation;
                        return CombineRestrictions(Acquire(conditional.WhenTrue ?? conditional.Condition), Acquire(conditional.WhenFalse));

                    case OperationKind.Conversion:
                        var conversion = (IConversionOperation)operation;
                        return conversion.OperatorMethod switch
                        {
                            null => Acquire(conversion.Operand),
                            { ReturnsByRefReadonly: true } => RefKind.RefReadOnly,
                            { ReturnsByRef: true } => RefKind.Ref,
                            _ => RefKind.None,
                        };

                    case OperationKind.DefaultValue:
                        return RefKind.None;

                    case OperationKind.FieldReference:
                        var field = ((IFieldReferenceOperation)operation).Field;
                        return field.IsReadOnly ? RefKind.RefReadOnly : RefKind.Ref;

                    case OperationKind.InstanceReference:
                        return _context.OwningSymbol.IsReadOnly() ? RefKind.RefReadOnly : RefKind.Ref;

                    case OperationKind.Invocation:
                        return ((IInvocationOperation)operation).TargetMethod switch
                        {
                            { ReturnsByRefReadonly: true } => RefKind.RefReadOnly,
                            { ReturnsByRef: true } => RefKind.Ref,
                            _ => RefKind.None,
                        };

                    case OperationKind.Literal:
                        return RefKind.None;

                    case OperationKind.LocalReference:
                        var local = ((ILocalReferenceOperation)operation).Local;
                        return local.RefKind == RefKind.RefReadOnly ? RefKind.RefReadOnly : RefKind.Ref;

                    case OperationKind.ObjectCreation:
                        return RefKind.None;

                    case OperationKind.ParameterReference:
                        var parameter = ((IParameterReferenceOperation)operation).Parameter;
                        return parameter.RefKind is RefKind.In or RefKindEx.RefReadOnlyParameter ? parameter.RefKind : RefKind.Ref;

                    case OperationKind.Parenthesized:
                        return Acquire(((IParenthesizedOperation)operation).Operand);

                    case OperationKind.PropertyReference:
                        var property = ((IPropertyReferenceOperation)operation).Property;
                        return property switch
                        {
                            { ReturnsByRefReadonly: true } => RefKind.RefReadOnly,
                            { ReturnsByRef: true } => RefKind.Ref,
                            _ => RefKind.None,
                        };

                    case OperationKind.Throw:
                        return RefKind.None;

                    case OperationKind.CollectionExpression:
                        return RefKind.None;

                    default:
                        return RefKind.RefReadOnly;
                }

                // Local functions
                static RefKind CombineRestrictions(RefKind first, RefKind second)
                {
                    return (first, second) switch
                    {
                        (RefKind.RefReadOnly or RefKindEx.RefReadOnlyParameter, _) => RefKind.RefReadOnly,
                        (_, RefKind.RefReadOnly or RefKindEx.RefReadOnlyParameter) => RefKind.RefReadOnly,
                        (RefKind.Out, _) => RefKind.Out,
                        (_, RefKind.Out) => RefKind.Out,
                        (RefKind.None, RefKind.None) => RefKind.None,
                        _ => RefKind.Ref,
                    };
                }
            }

            private void CheckEventSymbolInUnsupportedContext(IOperation operation, IEventSymbol? @event)
            {
                if (@event is null)
                    return;

                CheckTypeSymbolInUnsupportedContext(operation, @event.Type);
                CheckMethodSymbolInUnsupportedContext(operation, @event.AddMethod);
                CheckMethodSymbolInUnsupportedContext(operation, @event.RemoveMethod);
                CheckMethodSymbolInUnsupportedContext(operation, @event.RaiseMethod);
            }

            private void CheckFieldSymbolInUnsupportedContext(IOperation operation, IFieldSymbol? field)
            {
                if (field is null)
                    return;

                CheckTypeSymbolInUnsupportedContext(operation, field.Type);
            }

            private void CheckPropertySymbolInUnsupportedContext(IOperation operation, IPropertySymbol? property)
            {
                if (property is null)
                    return;

                CheckTypeSymbolInUnsupportedContext(operation, property.Type);
                foreach (var parameter in property.Parameters)
                {
                    CheckParameterSymbolInUnsupportedContext(operation, parameter);
                }

                CheckMethodSymbolInUnsupportedContext(operation, property.GetMethod);
                CheckMethodSymbolInUnsupportedContext(operation, property.SetMethod);
            }

            private void CheckSymbolInUnsupportedContext(IOperation operation, ISymbol? symbol)
            {
                if (symbol is null)
                    return;

                switch (symbol.Kind)
                {
                    case SymbolKind.Alias:
                        CheckSymbolInUnsupportedContext(operation, ((IAliasSymbol)symbol).Target);
                        break;

                    case SymbolKind.ArrayType:
                    case SymbolKind.DynamicType:
                    case SymbolKind.ErrorType:
                    case SymbolKind.NamedType:
                    case SymbolKind.PointerType:
                        CheckTypeSymbolInUnsupportedContext(operation, (ITypeSymbol)symbol);
                        break;

                    case SymbolKind.Event:
                        CheckEventSymbolInUnsupportedContext(operation, (IEventSymbol)symbol);
                        break;

                    case SymbolKind.Field:
                        CheckFieldSymbolInUnsupportedContext(operation, (IFieldSymbol)symbol);
                        break;

                    case SymbolKind.Local:
                        CheckLocalSymbolInUnsupportedContext(operation, (ILocalSymbol)symbol);
                        break;

                    case SymbolKind.Method:
                        CheckMethodSymbolInUnsupportedContext(operation, (IMethodSymbol)symbol);
                        break;

                    case SymbolKind.Parameter:
                        CheckParameterSymbolInUnsupportedContext(operation, (IParameterSymbol)symbol);
                        break;

                    case SymbolKind.Property:
                        CheckPropertySymbolInUnsupportedContext(operation, (IPropertySymbol)symbol);
                        break;

                    case SymbolKind.TypeParameter:
                        CheckTypeParameterSymbolInUnsupportedContext(operation, (ITypeParameterSymbol)symbol);
                        break;

                    case SymbolKind.Assembly:
                    case SymbolKind.Discard:
                    case SymbolKind.Label:
                    case SymbolKind.Namespace:
                    case SymbolKind.NetModule:
                    case SymbolKind.Preprocessing:
                    case SymbolKind.RangeVariable:
                        // Nothing to check for these symbols
                        break;

                    default:
                        break;
                }
            }

            private void CheckTypeSymbolInUnsupportedContext(IOperation operation, ITypeSymbol? type)
            {
                if (type is null)
                    return;

                if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    var nullableUnderlyingType = ((INamedTypeSymbol)type).TypeArguments.FirstOrDefault();
                    if (Cache.IsNonCopyableType(nullableUnderlyingType))
                    {
                        _context.ReportDiagnostic(operation.Syntax.CreateDiagnostic(AvoidNullableWrapperRule, type, operation.Kind));
                    }
                }

                _ = operation;
                _ = type;
            }

            private void CheckParameterSymbolInUnsupportedContext(IOperation operation, IParameterSymbol? parameter)
            {
                if (parameter is null)
                    return;

                CheckTypeSymbolInUnsupportedContext(operation, parameter.Type);
            }

            private void CheckTypeParameterSymbolInUnsupportedContext(IOperation operation, ITypeParameterSymbol? typeParameter)
            {
                if (typeParameter is null)
                    return;

                foreach (var constraint in typeParameter.ConstraintTypes)
                {
                    CheckTypeSymbolInUnsupportedContext(operation, constraint);
                }
            }

            private void CheckConversionInUnsupportedContext(IOperation operation, CommonConversion conversion)
            {
                CheckMethodSymbolInUnsupportedContext(operation, conversion.MethodSymbol);
            }

            private void CheckLocalSymbolInUnsupportedContext(IOperation operation, ILocalSymbol? local)
            {
                if (local is null)
                    return;

                CheckTypeSymbolInUnsupportedContext(operation, local.Type);
            }

            protected void CheckMethodSymbolInUnsupportedContext(IOperation operation, IMethodSymbol? symbol)
            {
                if (symbol is null)
                    return;

                CheckTypeSymbolInUnsupportedContext(operation, symbol.ReturnType);

                foreach (var parameter in symbol.Parameters)
                {
                    CheckParameterSymbolInUnsupportedContext(operation, parameter);
                }

                foreach (var typeArguments in symbol.TypeArguments)
                {
                    CheckTypeSymbolInUnsupportedContext(operation, typeArguments);
                }

                foreach (var typeParameter in symbol.TypeParameters)
                {
                    CheckTypeParameterSymbolInUnsupportedContext(operation, typeParameter);
                }
            }

            /// <summary>
            /// Non-copyable types are only allowed in cases explicitly handled by this analyzer. This call handles
            /// unrecognized coding patterns (not known to be safe or unsafe), and reports a generic message if a
            /// non-copyable type is used in this context.
            /// </summary>
            /// <param name="operation">The operation being analyzed.</param>
            private void CheckTypeInUnsupportedContext(IOperation operation)
            {
                if (_handledOperations.Contains(operation))
                    return;

                var node = operation.Syntax;
                var symbol = operation.Type;
                var operationKind = operation.Kind;

                if (symbol is null)
                {
                    // This operation did not have a type.
                    return;
                }

                if (!Cache.IsNonCopyableType(symbol))
                {
                    // Copies of this type are allowed
                    return;
                }

                _context.ReportDiagnostic(node.CreateDiagnostic(UnsupportedUseRule, symbol, operationKind));
            }
        }

        protected sealed class NonCopyableTypesCache
        {
            private readonly ConcurrentDictionary<INamedTypeSymbol, bool> _typesToNonCopyable
                = new();

            public INamedTypeSymbol? ValueTaskT { get; }
            public INamedTypeSymbol? ConfiguredValueTaskAwaitableT { get; }

            public NonCopyableTypesCache(Compilation compilation)
            {
                if (compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingSpinLock) is { } spinLock)
                    _typesToNonCopyable[spinLock] = true;

                if (compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeInteropServicesGCHandle) is { } gcHandle)
                    _typesToNonCopyable[gcHandle] = true;

                ValueTaskT = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksValueTask1);
                ConfiguredValueTaskAwaitableT = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredValueTaskAwaitable1);
            }

            internal bool IsNonCopyableType([NotNullWhen(true)] ITypeSymbol? symbol)
            {
                if (symbol is not { IsValueType: true })
                {
                    return false;
                }

                if (symbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return false;
                }

                if (_typesToNonCopyable.TryGetValue(namedTypeSymbol, out var noncopyable))
                {
                    return noncopyable;
                }

                return IsNonCopyableTypeSlow(namedTypeSymbol);
            }

            private bool IsNonCopyableTypeSlow(INamedTypeSymbol symbol)
            {
                return _typesToNonCopyable.GetOrAdd(
                    symbol,
                    symbol =>
                    {
                        foreach (var attribute in symbol.GetAttributes())
                        {
                            if (attribute.AttributeClass.Name == "NonCopyableAttribute")
                                return true;
                        }

                        return false;
                    });
            }
        }
    }
}
