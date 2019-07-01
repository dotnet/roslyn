// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class ReportDiagnosticAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax, TVariableDeclaratorSyntax> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor InvalidReportDiagnosticRule = new DiagnosticDescriptor(
            DiagnosticIds.InvalidReportDiagnosticRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(InvalidReportDiagnosticRule);

        [SuppressMessage("AnalyzerPerformance", "RS1012:Start action has no registered actions.", Justification = "Method returns an analyzer that is registered by the caller.")]
        protected override DiagnosticAnalyzerSymbolAnalyzer GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            Compilation compilation = compilationContext.Compilation;

            INamedTypeSymbol compilationEndAnalysisContext = compilation.GetTypeByMetadataName(CompilationEndAnalysisContextFullName);
            if (compilationEndAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol codeBlockAnalysisContext = compilation.GetTypeByMetadataName(CodeBlockAnalysisContextFullName);
            if (codeBlockAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol operationBlockAnalysisContext = compilation.GetTypeByMetadataName(OperationBlockAnalysisContextFullName);
            if (operationBlockAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol operationAnalysisContext = compilation.GetTypeByMetadataName(OperationAnalysisContextFullName);
            if (operationAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol semanticModelAnalysisContext = compilation.GetTypeByMetadataName(SemanticModelAnalysisContextFullName);
            if (semanticModelAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol symbolAnalysisContext = compilation.GetTypeByMetadataName(SymbolAnalysisContextFullName);
            if (symbolAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol syntaxNodeAnalysisContext = compilation.GetTypeByMetadataName(SyntaxNodeAnalysisContextFullName);
            if (syntaxNodeAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol syntaxTreeAnalysisContext = compilation.GetTypeByMetadataName(SyntaxTreeAnalysisContextFullName);
            if (syntaxTreeAnalysisContext == null)
            {
                return null;
            }

            INamedTypeSymbol diagnosticType = compilation.GetTypeByMetadataName(DiagnosticFullName);
            if (diagnosticType == null)
            {
                return null;
            }

            INamedTypeSymbol diagnosticDescriptorType = compilation.GetTypeByMetadataName(DiagnosticDescriptorFullName);
            if (diagnosticDescriptorType == null)
            {
                return null;
            }

            ImmutableHashSet<INamedTypeSymbol> contextTypes = ImmutableHashSet.Create(compilationEndAnalysisContext, codeBlockAnalysisContext,
                operationBlockAnalysisContext, operationAnalysisContext, semanticModelAnalysisContext, symbolAnalysisContext, syntaxNodeAnalysisContext, syntaxTreeAnalysisContext);

            return GetAnalyzer(contextTypes, diagnosticType, diagnosticDescriptorType, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        protected abstract ReportDiagnosticCompilationAnalyzer GetAnalyzer(ImmutableHashSet<INamedTypeSymbol> contextTypes, INamedTypeSymbol diagnosticType, INamedTypeSymbol diagnosticDescriptorType, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute);

        protected abstract class ReportDiagnosticCompilationAnalyzer : SyntaxNodeWithinAnalyzerTypeCompilationAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax>
        {
            private readonly ImmutableHashSet<INamedTypeSymbol> _contextTypes;
            private readonly INamedTypeSymbol _diagnosticType;
            private readonly INamedTypeSymbol _diagnosticDescriptorType;

            private ImmutableDictionary<INamedTypeSymbol, ImmutableArray<IFieldSymbol>> _supportedDescriptorFieldsMap;

            public ReportDiagnosticCompilationAnalyzer(ImmutableHashSet<INamedTypeSymbol> contextTypes, INamedTypeSymbol diagnosticType, INamedTypeSymbol diagnosticDescriptorType, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                _contextTypes = contextTypes;
                _diagnosticType = diagnosticType;
                _diagnosticDescriptorType = diagnosticDescriptorType;
                _supportedDescriptorFieldsMap = ImmutableDictionary<INamedTypeSymbol, ImmutableArray<IFieldSymbol>>.Empty;
            }

            protected abstract IEnumerable<SyntaxNode> GetArgumentExpressions(TInvocationExpressionSyntax invocation);
            protected virtual SyntaxNode GetPropertyGetterBlockSyntax(SyntaxNode declaringSyntaxRefNode)
            {
                return declaringSyntaxRefNode;
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                ImmutableArray<IFieldSymbol> descriptorFields = GetSupportedDescriptors(symbolContext.Compilation, (INamedTypeSymbol)symbolContext.Symbol, symbolContext.CancellationToken);
                if (!descriptorFields.IsDefaultOrEmpty)
                {
                    base.AnalyzeDiagnosticAnalyzer(symbolContext);
                }
            }

            private ImmutableArray<IFieldSymbol> GetSupportedDescriptors(Compilation compilation, INamedTypeSymbol analyzer, CancellationToken cancellationToken)
            {
                if (_supportedDescriptorFieldsMap.TryGetValue(analyzer, out ImmutableArray<IFieldSymbol> descriptorFields))
                {
                    return descriptorFields;
                }

                descriptorFields = default;

                if (this.DiagnosticAnalyzer.GetMembers(SupportedDiagnosticsName).FirstOrDefault() is IPropertySymbol supportedDiagnosticBaseProperty)
                {
                    IPropertySymbol supportedDiagnosticsProperty = analyzer.GetMembers()
                        .OfType<IPropertySymbol>()
                        .FirstOrDefault(p => p.OverriddenProperty != null &&
                            p.OverriddenProperty.Equals(supportedDiagnosticBaseProperty));
                    if (supportedDiagnosticsProperty != null && supportedDiagnosticsProperty.GetMethod != null)
                    {
                        SyntaxReference syntaxRef = supportedDiagnosticsProperty.GetMethod.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxRef != null)
                        {
                            SyntaxNode syntax = syntaxRef.GetSyntax(cancellationToken);
                            syntax = GetPropertyGetterBlockSyntax(syntax);
                            if (syntax != null)
                            {
                                SemanticModel semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                                descriptorFields = GetReferencedDescriptorFields(syntax, semanticModel);
                            }
                        }
                    }
                }

                return ImmutableInterlocked.GetOrAdd(ref _supportedDescriptorFieldsMap, analyzer, descriptorFields);
            }

            private ImmutableArray<IFieldSymbol> GetReferencedDescriptorFields(SyntaxNode syntax, SemanticModel semanticModel)
            {
                ImmutableArray<IFieldSymbol>.Builder builder = ImmutableArray.CreateBuilder<IFieldSymbol>();
                foreach (TIdentifierNameSyntax identifier in syntax.DescendantNodes().OfType<TIdentifierNameSyntax>())
                {
                    ISymbol symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                    if (symbol != null && symbol.Kind == SymbolKind.Field)
                    {
                        var field = (IFieldSymbol)symbol;
                        if (field.Type is INamedTypeSymbol fieldType && fieldType.GetBaseTypesAndThis().Contains(_diagnosticDescriptorType))
                        {
                            builder.Add((IFieldSymbol)symbol);
                        }
                    }
                }

                return builder.ToImmutable();
            }

            protected override void AnalyzeNode(SymbolAnalysisContext symbolContext, TInvocationExpressionSyntax invocation, SemanticModel semanticModel)
            {
                ISymbol symbol = semanticModel.GetSymbolInfo(invocation, symbolContext.CancellationToken).Symbol;
                if (symbol == null ||
                    symbol.Kind != SymbolKind.Method ||
                    !symbol.Name.Equals(ReportDiagnosticName, StringComparison.OrdinalIgnoreCase) ||
                    !_contextTypes.Contains(symbol.ContainingType))
                {
                    return;
                }

                IEnumerable<SyntaxNode> arguments = GetArgumentExpressions(invocation);
                if (arguments.HasExactly(1))
                {
                    SyntaxNode argument = arguments.First();
                    ITypeSymbol type = semanticModel.GetTypeInfo(argument, symbolContext.CancellationToken).ConvertedType;
                    if (type != null && type.Equals(_diagnosticType))
                    {
                        ISymbol argSymbol = semanticModel.GetSymbolInfo(argument, symbolContext.CancellationToken).Symbol;
                        if (argSymbol != null)
                        {
                            SyntaxNode diagnosticInitializerOpt = null;

                            if (argSymbol is ILocalSymbol local)
                            {
                                SyntaxReference syntaxRef = local.DeclaringSyntaxReferences.FirstOrDefault();
                                if (syntaxRef != null)
                                {
                                    diagnosticInitializerOpt = syntaxRef.GetSyntax(symbolContext.CancellationToken).FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
                                }
                            }
                            else
                            {
                                if (argSymbol is IMethodSymbol method &&
    method.ContainingType.Equals(_diagnosticType) &&
    method.Name.Equals(nameof(Diagnostic.Create), StringComparison.OrdinalIgnoreCase))
                                {
                                    diagnosticInitializerOpt = argument;
                                }
                            }

                            if (diagnosticInitializerOpt != null)
                            {
                                ImmutableArray<IFieldSymbol> descriptorFields = GetReferencedDescriptorFields(diagnosticInitializerOpt, semanticModel);
                                if (descriptorFields.Length == 1 &&
                                    !_supportedDescriptorFieldsMap[(INamedTypeSymbol)symbolContext.Symbol].Contains(descriptorFields[0]))
                                {
                                    Diagnostic diagnostic = Diagnostic.Create(InvalidReportDiagnosticRule, invocation.GetLocation(), descriptorFields[0].Name);
                                    symbolContext.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
