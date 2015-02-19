// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class RegisterActionAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax, TLanguageKindEnum> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TLanguageKindEnum : struct
    {
        private static LocalizableString s_localizableTitleMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableMessageMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableDescriptionMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor MissingKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
            s_localizableTitleMissingKindArgument,
            s_localizableMessageMissingKindArgument,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescriptionMissingKindArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static LocalizableString s_localizableTitleUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableMessageUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor UnsupportedSymbolKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.UnsupportedSymbolKindArgumentRuleId,
            s_localizableTitleUnsupportedSymbolKindArgument,
            s_localizableMessageUnsupportedSymbolKindArgument,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static LocalizableString s_localizableTitleInvalidSyntaxKindTypeArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableMessageInvalidSyntaxKindTypeArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableDescriptionInvalidSyntaxKindTypeArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), nameof(TLanguageKindEnumName));

        public static DiagnosticDescriptor InvalidSyntaxKindTypeArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.InvalidSyntaxKindTypeArgumentRuleId,
            s_localizableTitleInvalidSyntaxKindTypeArgument,
            s_localizableMessageInvalidSyntaxKindTypeArgument,
            "AnalyzerCorrectness",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescriptionInvalidSyntaxKindTypeArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(MissingKindArgumentRule, UnsupportedSymbolKindArgumentRule, InvalidSyntaxKindTypeArgumentRule);
            }
        }

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var analysisContext = compilation.GetTypeByMetadataName(AnalysisContextFullName);
            if (analysisContext == null)
            {
                return null;
            }

            var compilationStartAnalysisContext = compilation.GetTypeByMetadataName(CompilationStartAnalysisContextFullName);
            if (compilationStartAnalysisContext == null)
            {
                return null;
            }

            var codeBlockStartAnalysisContext = compilation.GetTypeByMetadataName(CodeBlockStartAnalysisContextFullName);
            if (codeBlockStartAnalysisContext == null)
            {
                return null;
            }

            var symbolKind = compilation.GetTypeByMetadataName(SymbolKindFullName);
            if (symbolKind == null)
            {
                return null;
            }

            return GetAnalyzer(compilation, analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        protected abstract RegisterActionCompilationAnalyzer GetAnalyzer(Compilation compilation, INamedTypeSymbol analysisContext, INamedTypeSymbol compilationStartAnalysisContext, INamedTypeSymbol codeBlockStartAnalysisContext, INamedTypeSymbol symbolKind, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute);

        protected abstract class RegisterActionCompilationAnalyzer : SyntaxNodeWithinAnalyzerTypeCompilationAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax>
        {
            private readonly INamedTypeSymbol _analysisContext;
            private readonly INamedTypeSymbol _compilationStartAnalysisContext;
            private readonly INamedTypeSymbol _codeBlockStartAnalysisContext;
            private readonly INamedTypeSymbol _symbolKind;

            private static readonly ImmutableHashSet<string> s_supportedSymbolKinds =
                ImmutableHashSet.Create(
                    nameof(SymbolKind.Event),
                    nameof(SymbolKind.Field),
                    nameof(SymbolKind.Method),
                    nameof(SymbolKind.NamedType),
                    nameof(SymbolKind.Namespace),
                    nameof(SymbolKind.Property));

            protected RegisterActionCompilationAnalyzer(
                INamedTypeSymbol analysisContext,
                INamedTypeSymbol compilationStartAnalysisContext,
                INamedTypeSymbol codeBlockStartAnalysisContext,
                INamedTypeSymbol symbolKind,
                INamedTypeSymbol diagnosticAnalyzer,
                INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                _analysisContext = analysisContext;
                _compilationStartAnalysisContext = compilationStartAnalysisContext;
                _codeBlockStartAnalysisContext = codeBlockStartAnalysisContext;
                _symbolKind = symbolKind;
            }

            protected abstract IEnumerable<SyntaxNode> GetArgumentExpressions(TInvocationExpressionSyntax invocation);
            protected abstract SyntaxNode GetInvocationExpression(TInvocationExpressionSyntax invocation);
            protected abstract bool IsSyntaxKind(ITypeSymbol type);

            private static bool IsRegisterAction(string expectedName, IMethodSymbol method, params INamedTypeSymbol[] allowedContainginTypes)
            {
                if (method.Name.Equals(expectedName))
                {
                    foreach (var containingType in allowedContainginTypes)
                    {
                        if (method.ContainingType.Equals(containingType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            protected override void AnalyzeNode(SymbolAnalysisContext symbolContext, TInvocationExpressionSyntax invocation, SemanticModel semanticModel)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation, symbolContext.CancellationToken).Symbol;
                if (symbol == null || symbol.Kind != SymbolKind.Method || !symbol.Name.StartsWith("Register", StringComparison.Ordinal))
                {
                    return;
                }

                var method = (IMethodSymbol)symbol;
                var isRegisterSymbolAction = IsRegisterAction(RegisterSymbolActionName, method, _analysisContext, _compilationStartAnalysisContext);
                var isRegisterSyntaxNodeAction = IsRegisterAction(RegisterSyntaxNodeActionName, method, _analysisContext, _compilationStartAnalysisContext, _codeBlockStartAnalysisContext);
                var isRegisterCodeBlockStartAction = IsRegisterAction(RegisterCodeBlockStartActionName, method, _analysisContext, _compilationStartAnalysisContext);

                if (isRegisterSymbolAction || isRegisterSyntaxNodeAction)
                {
                    if (method.Parameters.Length == 2 && method.Parameters[1].IsParams)
                    {
                        var arguments = GetArgumentExpressions(invocation);
                        if (arguments != null)
                        {
                            var argumentCount = arguments.Count();
                            if (argumentCount >= 1)
                            {
                                var type = semanticModel.GetTypeInfo(arguments.First(), symbolContext.CancellationToken).ConvertedType;
                                if (type == null || type.Name.Equals(nameof(Action)))
                                {
                                    if (argumentCount == 1)
                                    {
                                        string arg1, arg2;
                                        if (isRegisterSymbolAction)
                                        {
                                            arg1 = nameof(SymbolKind);
                                            arg2 = "symbol";
                                        }
                                        else
                                        {
                                            arg1 = "SyntaxKind";
                                            arg2 = "syntax";
                                        }

                                        var invocationExpression = GetInvocationExpression(invocation);
                                        var diagnostic = Diagnostic.Create(MissingKindArgumentRule, invocationExpression.GetLocation(), arg1, arg2);
                                        symbolContext.ReportDiagnostic(diagnostic);
                                    }
                                    else if (isRegisterSymbolAction)
                                    {
                                        foreach (var argument in arguments.Skip(1))
                                        {
                                            symbol = semanticModel.GetSymbolInfo(argument, symbolContext.CancellationToken).Symbol;
                                            if (symbol != null &&
                                                symbol.Kind == SymbolKind.Field &&
                                                _symbolKind.Equals(symbol.ContainingType) &&
                                                !s_supportedSymbolKinds.Contains(symbol.Name))
                                            {
                                                var diagnostic = Diagnostic.Create(UnsupportedSymbolKindArgumentRule, argument.GetLocation(), symbol.Name);
                                                symbolContext.ReportDiagnostic(diagnostic);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (isRegisterSyntaxNodeAction || isRegisterCodeBlockStartAction)
                {
                    Debug.Assert(method.TypeParameters.Length > 0);

                    ITypeSymbol typeArgument = null;
                    if (method.TypeParameters.Length == 1)
                    {
                        if (method.TypeParameters[0].Name == TLanguageKindEnumName)
                        {
                            typeArgument = method.TypeArguments[0];
                        }
                    }
                    else
                    {
                        var typeParam = method.TypeParameters.SingleOrDefault(t => t.Name == TLanguageKindEnumName);
                        if (typeParam != null)
                        {
                            var index = method.TypeParameters.IndexOf(typeParam);
                            typeArgument = method.TypeArguments[index];
                        }
                    }

                    if (typeArgument != null &&
                        typeArgument.TypeKind != TypeKind.TypeParameter &&
                        typeArgument.TypeKind != TypeKind.Error &&
                        !IsSyntaxKind(typeArgument))
                    {
                        var location = typeArgument.Locations[0];
                        if (!location.IsInSource)
                        {
                            var invocationExpression = GetInvocationExpression(invocation);
                            location = invocationExpression.GetLocation();
                        }

                        var diagnostic = Diagnostic.Create(InvalidSyntaxKindTypeArgumentRule, location, typeArgument.Name, TLanguageKindEnumName, method.Name);
                        symbolContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
