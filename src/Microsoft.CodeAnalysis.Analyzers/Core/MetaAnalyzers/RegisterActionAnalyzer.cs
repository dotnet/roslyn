// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class RegisterActionAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax, TArgumentSyntax, TLanguageKindEnum> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableTitleMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageMissingSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingSymbolKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageMissingSyntaxKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingSyntaxKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageMissingOperationKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingOperationKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescriptionMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor MissingSymbolKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
            s_localizableTitleMissingKindArgument,
            s_localizableMessageMissingSymbolKindArgument,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionMissingKindArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor MissingSyntaxKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
            s_localizableTitleMissingKindArgument,
            s_localizableMessageMissingSyntaxKindArgument,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionMissingKindArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor MissingOperationKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
            s_localizableTitleMissingKindArgument,
            s_localizableMessageMissingOperationKindArgument,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionMissingKindArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableTitleUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor UnsupportedSymbolKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.UnsupportedSymbolKindArgumentRuleId,
            s_localizableTitleUnsupportedSymbolKindArgument,
            s_localizableMessageUnsupportedSymbolKindArgument,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableTitleInvalidSyntaxKindTypeArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageInvalidSyntaxKindTypeArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescriptionInvalidSyntaxKindTypeArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), nameof(TLanguageKindEnumName));

        public static readonly DiagnosticDescriptor InvalidSyntaxKindTypeArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.InvalidSyntaxKindTypeArgumentRuleId,
            s_localizableTitleInvalidSyntaxKindTypeArgument,
            s_localizableMessageInvalidSyntaxKindTypeArgument,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionInvalidSyntaxKindTypeArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableDescriptionStatefulAnalyzerRegisterActionsDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.StatefulAnalyzerRegisterActionsDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), nameof(TLanguageKindEnumName));

        private static readonly LocalizableString s_localizableTitleStartActionWithNoRegisteredActions = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.StartActionWithNoRegisteredActionsTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageStartActionWithNoRegisteredActions = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.StartActionWithNoRegisteredActionsMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor StartActionWithNoRegisteredActionsRule = new DiagnosticDescriptor(
            DiagnosticIds.StartActionWithNoRegisteredActionsRuleId,
            s_localizableTitleStartActionWithNoRegisteredActions,
            s_localizableMessageStartActionWithNoRegisteredActions,
            DiagnosticCategory.MicrosoftCodeAnalysisPerformance,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionStatefulAnalyzerRegisterActionsDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableTitleStartActionWithOnlyEndAction = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.StartActionWithOnlyEndActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageStartActionWithOnlyEndAction = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.StartActionWithOnlyEndActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor StartActionWithOnlyEndActionRule = new DiagnosticDescriptor(
            DiagnosticIds.StartActionWithOnlyEndActionRuleId,
            s_localizableTitleStartActionWithOnlyEndAction,
            s_localizableMessageStartActionWithOnlyEndAction,
            DiagnosticCategory.MicrosoftCodeAnalysisPerformance,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionStatefulAnalyzerRegisterActionsDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            MissingSymbolKindArgumentRule,
            MissingSyntaxKindArgumentRule,
            MissingOperationKindArgumentRule,
            UnsupportedSymbolKindArgumentRule,
            InvalidSyntaxKindTypeArgumentRule,
            StartActionWithNoRegisteredActionsRule,
            StartActionWithOnlyEndActionRule);

        protected override DiagnosticAnalyzerSymbolAnalyzer? GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            Compilation compilation = compilationContext.Compilation;

            INamedTypeSymbol? analysisContext = compilation.GetOrCreateTypeByMetadataName(AnalysisContextFullName);
            if (analysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol? compilationStartAnalysisContext = compilation.GetOrCreateTypeByMetadataName(CompilationStartAnalysisContextFullName);
            if (compilationStartAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol? codeBlockStartAnalysisContext = compilation.GetOrCreateTypeByMetadataName(CodeBlockStartAnalysisContextFullName);
            if (codeBlockStartAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol? operationBlockStartAnalysisContext = compilation.GetOrCreateTypeByMetadataName(OperationBlockStartAnalysisContextFullName);
            if (operationBlockStartAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol? symbolKind = compilation.GetOrCreateTypeByMetadataName(SymbolKindFullName);
            if (symbolKind == null)
            {
                return null;
            }

            compilationContext.RegisterCodeBlockStartAction<TLanguageKindEnum>(codeBlockContext =>
            {
                RegisterActionCodeBlockAnalyzer analyzer = GetCodeBlockAnalyzer(compilation, analysisContext, compilationStartAnalysisContext,
                    codeBlockStartAnalysisContext, operationBlockStartAnalysisContext, symbolKind);

                analyzer.CodeBlockStartAction(codeBlockContext);
            });

            // We don't want to analyze DiagnosticAnalyzer type symbols, just the code blocks.
            return null;
        }

        protected abstract RegisterActionCodeBlockAnalyzer GetCodeBlockAnalyzer(
            Compilation compilation,
            INamedTypeSymbol analysisContext,
            INamedTypeSymbol compilationStartAnalysisContext,
            INamedTypeSymbol codeBlockStartAnalysisContext,
            INamedTypeSymbol operationBlockStartAnalysisContext,
            INamedTypeSymbol symbolKind);

        protected abstract class RegisterActionCodeBlockAnalyzer
        {
            private readonly INamedTypeSymbol _analysisContext;
            private readonly INamedTypeSymbol _compilationStartAnalysisContext;
            private readonly INamedTypeSymbol _codeBlockStartAnalysisContext;
            private readonly INamedTypeSymbol _operationBlockStartAnalysisContext;
            private readonly INamedTypeSymbol _symbolKind;

            private static readonly ImmutableHashSet<string> s_supportedSymbolKinds =
                ImmutableHashSet.Create(
                    nameof(SymbolKind.Event),
                    nameof(SymbolKind.Field),
                    nameof(SymbolKind.Method),
                    nameof(SymbolKind.NamedType),
                    nameof(SymbolKind.Namespace),
                    nameof(SymbolKind.Parameter),
                    nameof(SymbolKind.Property));

#pragma warning disable CA1815 // Override equals and operator equals on value types
            private struct NodeAndSymbol
#pragma warning restore CA1815 // Override equals and operator equals on value types
            {
                public TInvocationExpressionSyntax Invocation { get; set; }
                public IMethodSymbol Method { get; set; }
            }

            /// <summary>
            /// Map from declared analysis context type parameters to invocations of Register methods on them.
            /// </summary>
            private Dictionary<IParameterSymbol, List<NodeAndSymbol>>? _nestedActionsMap;

            /// <summary>
            /// Set of declared start analysis context parameters that need to be analyzed for <see cref="StartActionWithNoRegisteredActionsRule"/> and <see cref="StartActionWithOnlyEndActionRule"/>.
            /// </summary>
            private HashSet<IParameterSymbol>? _declaredStartAnalysisContextParams;

            /// <summary>
            /// Set of declared start analysis context parameters that need to be skipped for <see cref="StartActionWithNoRegisteredActionsRule"/> and <see cref="StartActionWithOnlyEndActionRule"/>.
            /// This is to avoid false positives where context types are passed as arguments to a different invocation, and hence the registration responsibility is not on the current method.
            /// </summary>
            private HashSet<IParameterSymbol>? _startAnalysisContextParamsToSkip;

            protected RegisterActionCodeBlockAnalyzer(
                INamedTypeSymbol analysisContext,
                INamedTypeSymbol compilationStartAnalysisContext,
                INamedTypeSymbol codeBlockStartAnalysisContext,
                INamedTypeSymbol operationBlockStartAnalysisContext,
                INamedTypeSymbol symbolKind)
            {
                _analysisContext = analysisContext;
                _compilationStartAnalysisContext = compilationStartAnalysisContext;
                _codeBlockStartAnalysisContext = codeBlockStartAnalysisContext;
                _operationBlockStartAnalysisContext = operationBlockStartAnalysisContext;
                _symbolKind = symbolKind;

                _nestedActionsMap = null;
                _declaredStartAnalysisContextParams = null;
                _startAnalysisContextParamsToSkip = null;
            }

            protected abstract IEnumerable<SyntaxNode>? GetArgumentExpressions(TInvocationExpressionSyntax invocation);
            protected abstract SyntaxNode GetArgumentExpression(TArgumentSyntax argument);
            protected abstract SyntaxNode GetInvocationExpression(TInvocationExpressionSyntax invocation);
            protected abstract SyntaxNode? GetInvocationReceiver(TInvocationExpressionSyntax invocation);
            protected abstract bool IsSyntaxKind(ITypeSymbol type);
            protected abstract TLanguageKindEnum InvocationExpressionKind { get; }
            protected abstract TLanguageKindEnum ArgumentSyntaxKind { get; }
            protected abstract TLanguageKindEnum ParameterSyntaxKind { get; }

            internal void CodeBlockStartAction(CodeBlockStartAnalysisContext<TLanguageKindEnum> codeBlockContext)
            {
                var method = codeBlockContext.OwningSymbol as IMethodSymbol;
                if (!ShouldAnalyze(method))
                {
                    return;
                }

                foreach (IParameterSymbol param in method.Parameters)
                {
                    AnalyzeParameterDeclaration(param);
                }

                // Analyze all the Register action invocation expressions.
                codeBlockContext.RegisterSyntaxNodeAction(AnalyzeInvocation, InvocationExpressionKind);

                // Analyze all the arguments to invocations.
                codeBlockContext.RegisterSyntaxNodeAction(AnalyzeArgumentSyntax, ArgumentSyntaxKind);

                // Analyze all the lambda parameters in the method body, if any.
                codeBlockContext.RegisterSyntaxNodeAction(AnalyzerParameterSyntax, ParameterSyntaxKind);

                // Report diagnostics based on the final state.
                codeBlockContext.RegisterCodeBlockEndAction(CodeBlockEndAction);
            }

            private bool ShouldAnalyze([NotNullWhen(returnValue: true)] IMethodSymbol? method)
            {
                if (method == null)
                {
                    return false;
                }

                // Only analyze this method if declares a parameter with one of the allowed analysis context types.
                foreach (IParameterSymbol parameter in method.Parameters)
                {
                    if (parameter.Type is INamedTypeSymbol namedType &&
    IsContextType(namedType, _analysisContext, _codeBlockStartAnalysisContext, _operationBlockStartAnalysisContext, _compilationStartAnalysisContext))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsContextType(ITypeSymbol type, params INamedTypeSymbol[] allowedContextTypes)
            {
                INamedTypeSymbol? namedType = (type as INamedTypeSymbol)?.OriginalDefinition;
                if (namedType != null)
                {
                    foreach (INamedTypeSymbol contextType in allowedContextTypes)
                    {
                        if (namedType.Equals(contextType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static bool IsRegisterAction(string expectedName, IMethodSymbol method, params INamedTypeSymbol[] allowedContainingTypes)
            {
                return method.Name.Equals(expectedName, StringComparison.Ordinal) &&
                    IsContextType(method.ContainingType, allowedContainingTypes);
            }

            private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
            {
                var invocation = (TInvocationExpressionSyntax)context.Node;
                SemanticModel semanticModel = context.SemanticModel;

                ISymbol symbol = semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;
                if (symbol == null || symbol.Kind != SymbolKind.Method || !symbol.Name.StartsWith("Register", StringComparison.Ordinal))
                {
                    return;
                }

                var method = (IMethodSymbol)symbol;
                NoteRegisterActionInvocation(method, invocation, semanticModel, context.CancellationToken);

                bool isRegisterSymbolAction = IsRegisterAction(RegisterSymbolActionName, method, _analysisContext, _compilationStartAnalysisContext);
                bool isRegisterSyntaxNodeAction = IsRegisterAction(RegisterSyntaxNodeActionName, method, _analysisContext, _compilationStartAnalysisContext, _codeBlockStartAnalysisContext);
                bool isRegisterCodeBlockStartAction = IsRegisterAction(RegisterCodeBlockStartActionName, method, _analysisContext, _compilationStartAnalysisContext);
                bool isRegisterOperationAction = IsRegisterAction(RegisterOperationActionName, method, _analysisContext, _compilationStartAnalysisContext, _operationBlockStartAnalysisContext);

                if (isRegisterSymbolAction || isRegisterSyntaxNodeAction || isRegisterOperationAction)
                {
                    if (method.Parameters.Length == 2 && method.Parameters[1].IsParams)
                    {
                        IEnumerable<SyntaxNode>? arguments = GetArgumentExpressions(invocation);
                        if (arguments != null)
                        {
                            int argumentCount = arguments.Count();
                            if (argumentCount >= 1)
                            {
                                ITypeSymbol type = semanticModel.GetTypeInfo(arguments.First(), context.CancellationToken).ConvertedType;
                                if (type == null || type.Name.Equals(nameof(Action), StringComparison.Ordinal))
                                {
                                    if (argumentCount == 1)
                                    {
                                        DiagnosticDescriptor rule;
                                        if (isRegisterSymbolAction)
                                        {
                                            rule = MissingSymbolKindArgumentRule;
                                        }
                                        else if (isRegisterOperationAction)
                                        {
                                            rule = MissingOperationKindArgumentRule;
                                        }
                                        else
                                        {
                                            rule = MissingSyntaxKindArgumentRule;
                                        }

                                        SyntaxNode invocationExpression = GetInvocationExpression(invocation);
                                        Diagnostic diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation());
                                        context.ReportDiagnostic(diagnostic);
                                    }
                                    else if (isRegisterSymbolAction)
                                    {
                                        foreach (SyntaxNode argument in arguments.Skip(1))
                                        {
                                            symbol = semanticModel.GetSymbolInfo(argument, context.CancellationToken).Symbol;
                                            if (symbol != null &&
                                                symbol.Kind == SymbolKind.Field &&
                                                _symbolKind.Equals(symbol.ContainingType) &&
                                                !s_supportedSymbolKinds.Contains(symbol.Name))
                                            {
                                                Diagnostic diagnostic = Diagnostic.Create(UnsupportedSymbolKindArgumentRule, argument.GetLocation(), symbol.Name);
                                                context.ReportDiagnostic(diagnostic);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (method.TypeParameters.Length > 0 &&
                    (isRegisterSyntaxNodeAction || isRegisterCodeBlockStartAction))
                {
                    ITypeSymbol? typeArgument = null;
                    if (method.TypeParameters.Length == 1)
                    {
                        if (method.TypeParameters[0].Name == TLanguageKindEnumName)
                        {
                            typeArgument = method.TypeArguments[0];
                        }
                    }
                    else
                    {
                        ITypeParameterSymbol typeParam = method.TypeParameters.FirstOrDefault(t => t.Name == TLanguageKindEnumName);
                        if (typeParam != null)
                        {
                            int index = method.TypeParameters.IndexOf(typeParam);
                            typeArgument = method.TypeArguments[index];
                        }
                    }

                    if (typeArgument != null &&
                        typeArgument.TypeKind != TypeKind.TypeParameter &&
                        typeArgument.TypeKind != TypeKind.Error &&
                        !IsSyntaxKind(typeArgument))
                    {
                        Location location = typeArgument.Locations[0];
                        if (!location.IsInSource)
                        {
                            SyntaxNode invocationExpression = GetInvocationExpression(invocation);
                            location = invocationExpression.GetLocation();
                        }

                        Diagnostic diagnostic = Diagnostic.Create(InvalidSyntaxKindTypeArgumentRule, location, typeArgument.Name, TLanguageKindEnumName, method.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            private void AnalyzeArgumentSyntax(SyntaxNodeAnalysisContext context)
            {
                SyntaxNode argumentExpression = GetArgumentExpression((TArgumentSyntax)context.Node);
                if (argumentExpression != null)
                {
                    if (context.SemanticModel.GetSymbolInfo(argumentExpression, context.CancellationToken).Symbol is IParameterSymbol parameter)
                    {
                        AnalyzeParameterReference(parameter);
                    }
                }
            }

            private void AnalyzerParameterSyntax(SyntaxNodeAnalysisContext context)
            {
                if (context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken) is IParameterSymbol parameter)
                {
                    AnalyzeParameterDeclaration(parameter);
                }
            }

            private void AnalyzeParameterDeclaration(IParameterSymbol parameter)
            {
                if (IsContextType(parameter.Type, _compilationStartAnalysisContext, _codeBlockStartAnalysisContext, _operationBlockStartAnalysisContext))
                {
                    _declaredStartAnalysisContextParams ??= new HashSet<IParameterSymbol>();
                    _declaredStartAnalysisContextParams.Add(parameter);
                }
            }

            private void AnalyzeParameterReference(IParameterSymbol parameter)
            {
                // We skip analysis for context parameters that are passed as arguments to any invocation.
                // This is to avoid false positives, as the registration responsibility is not on the current method.
                if (IsContextType(parameter.Type, _compilationStartAnalysisContext, _codeBlockStartAnalysisContext, _operationBlockStartAnalysisContext))
                {
                    _startAnalysisContextParamsToSkip ??= new HashSet<IParameterSymbol>();
                    _startAnalysisContextParamsToSkip.Add(parameter);
                }
            }

            private void NoteRegisterActionInvocation(IMethodSymbol method, TInvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
            {
                if (method.ContainingType.Equals(_analysisContext))
                {
                    // Not a nested action.
                    return;
                }

                SyntaxNode? receiver = GetInvocationReceiver(invocation);
                if (receiver == null)
                {
                    return;
                }

                // Get the context parameter on which we are registering an action.
                if (!(model.GetSymbolInfo(receiver, cancellationToken).Symbol is IParameterSymbol contextParameter))
                {
                    return;
                }

                // Check if we are bailing out on this context parameter.
                if (_startAnalysisContextParamsToSkip != null && _startAnalysisContextParamsToSkip.Contains(contextParameter))
                {
                    return;
                }

                _nestedActionsMap ??= new Dictionary<IParameterSymbol, List<NodeAndSymbol>>();
                if (!_nestedActionsMap.TryGetValue(contextParameter, out List<NodeAndSymbol> registerInvocations))
                {
                    registerInvocations = new List<NodeAndSymbol>();
                }

                registerInvocations.Add(new NodeAndSymbol { Invocation = invocation, Method = method });
                _nestedActionsMap[contextParameter] = registerInvocations;
            }

            private void CodeBlockEndAction(CodeBlockAnalysisContext codeBlockContext)
            {
                if (_declaredStartAnalysisContextParams == null)
                {
                    // No declared context parameters to analyze.
                    return;
                }

                foreach (IParameterSymbol contextParameter in _declaredStartAnalysisContextParams)
                {
                    // Check if we must bail out on this context parameter.
                    if (_startAnalysisContextParamsToSkip != null && _startAnalysisContextParamsToSkip.Contains(contextParameter))
                    {
                        continue;
                    }

                    var hasEndAction = false;
                    var hasNonEndAction = false;

                    if (_nestedActionsMap != null && _nestedActionsMap.TryGetValue(contextParameter, out List<NodeAndSymbol> registeredActions))
                    {
                        foreach (NodeAndSymbol invocationInfo in registeredActions)
                        {
                            switch (invocationInfo.Method.Name)
                            {
                                case RegisterCompilationEndActionName:
                                case RegisterCodeBlockEndActionName:
                                case RegisterOperationBlockEndActionName:
                                    hasEndAction = true;
                                    break;

                                default:
                                    hasNonEndAction = true;
                                    break;
                            }
                        }
                    }

                    // Report a diagnostic if no non-end actions were registered on start analysis context parameter.
                    if (!hasNonEndAction)
                    {
                        ReportDiagnostic(codeBlockContext, contextParameter, hasEndAction);
                    }
                }
            }

            private void ReportDiagnostic(CodeBlockAnalysisContext codeBlockContext, IParameterSymbol contextParameter, bool hasEndAction)
            {
                Debug.Assert(IsContextType(contextParameter.Type, _codeBlockStartAnalysisContext, _compilationStartAnalysisContext, _operationBlockStartAnalysisContext));
                bool isCompilationStartAction = contextParameter.Type.OriginalDefinition.Equals(_compilationStartAnalysisContext.OriginalDefinition);
                bool isOperationBlockStartAction = !isCompilationStartAction && contextParameter.Type.OriginalDefinition.Equals(_operationBlockStartAnalysisContext.OriginalDefinition);

                Location location = contextParameter.DeclaringSyntaxReferences.First()
                        .GetSyntax(codeBlockContext.CancellationToken).GetLocation();

                string parameterName = contextParameter.Name;
                string endActionName;
                string statelessActionName;
                string parentRegisterMethodName;
                if (isCompilationStartAction)
                {
                    endActionName = "CompilationEndAction";
                    statelessActionName = RegisterCompilationActionName;
                    parentRegisterMethodName = "Initialize";
                }
                else if (isOperationBlockStartAction)
                {
                    endActionName = "OperationBlockEndAction";
                    statelessActionName = RegisterOperationBlockActionName;
                    parentRegisterMethodName = "Initialize, CompilationStartAction";
                }
                else
                {
                    endActionName = "CodeBlockEndAction";
                    statelessActionName = RegisterCodeBlockActionName;
                    parentRegisterMethodName = "Initialize, CompilationStartAction";
                }

                Diagnostic diagnostic;
                if (!hasEndAction)
                {
                    diagnostic = Diagnostic.Create(StartActionWithNoRegisteredActionsRule, location, parameterName, parentRegisterMethodName);
                }
                else
                {
                    diagnostic = Diagnostic.Create(StartActionWithOnlyEndActionRule, location, parameterName, endActionName, statelessActionName, parentRegisterMethodName);
                }

                codeBlockContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
