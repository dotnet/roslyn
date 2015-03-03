// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class ReportDiagnosticAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax, TVariableDeclaratorSyntax> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor InvalidReportDiagnosticRule = new DiagnosticDescriptor(
            DiagnosticIds.InvalidReportDiagnosticRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(InvalidReportDiagnosticRule);
            }
        }

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var compilationEndAnalysisContext = compilation.GetTypeByMetadataName(CompilationEndAnalysisContextFullName);
            if (compilationEndAnalysisContext == null)
            {
                return null;
            }

            var codeBlockEndAnalysisContext = compilation.GetTypeByMetadataName(CodeBlockEndAnalysisContextFullName);
            if (codeBlockEndAnalysisContext == null)
            {
                return null;
            }

            var semanticModelAnalysisContext = compilation.GetTypeByMetadataName(SemanticModelAnalysisContextFullName);
            if (semanticModelAnalysisContext == null)
            {
                return null;
            }

            var symbolAnalysisContext = compilation.GetTypeByMetadataName(SymbolAnalysisContextFullName);
            if (symbolAnalysisContext == null)
            {
                return null;
            }

            var syntaxNodeAnalysisContext = compilation.GetTypeByMetadataName(SyntaxNodeAnalysisContextFullName);
            if (syntaxNodeAnalysisContext == null)
            {
                return null;
            }

            var syntaxTreeAnalysisContext = compilation.GetTypeByMetadataName(SyntaxTreeAnalysisContextFullName);
            if (syntaxTreeAnalysisContext == null)
            {
                return null;
            }

            var diagnosticType = compilation.GetTypeByMetadataName(DiagnosticFullName);
            if (diagnosticType == null)
            {
                return null;
            }

            var diagnosticDescriptorType = compilation.GetTypeByMetadataName(DiagnosticDescriptorFullName);
            if (diagnosticDescriptorType == null)
            {
                return null;
            }

            var contextTypes = ImmutableHashSet.Create(compilationEndAnalysisContext, codeBlockEndAnalysisContext,
                semanticModelAnalysisContext, symbolAnalysisContext, syntaxNodeAnalysisContext, syntaxTreeAnalysisContext);

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
                var descriptorFields = GetSupportedDescriptors(symbolContext.Compilation, (INamedTypeSymbol)symbolContext.Symbol, symbolContext.CancellationToken);
                if (!descriptorFields.IsDefaultOrEmpty)
                {
                    base.AnalyzeDiagnosticAnalyzer(symbolContext);
                }
            }

            private ImmutableArray<IFieldSymbol> GetSupportedDescriptors(Compilation compilation, INamedTypeSymbol analyzer, CancellationToken cancellationToken)
            {
                ImmutableArray<IFieldSymbol> descriptorFields;
                if (_supportedDescriptorFieldsMap.TryGetValue(analyzer, out descriptorFields))
                {
                    return descriptorFields;
                }

                descriptorFields = default(ImmutableArray<IFieldSymbol>);

                var supportedDiagnosticBaseProperty = this.DiagnosticAnalyzer.GetMembers(SupportedDiagnosticsName).FirstOrDefault() as IPropertySymbol;
                if (supportedDiagnosticBaseProperty != null)
                {
                    var supportedDiagnosticsProperty = analyzer.GetMembers()
                        .OfType<IPropertySymbol>()
                        .FirstOrDefault(p => p.OverriddenProperty != null &&
                            p.OverriddenProperty.Equals(supportedDiagnosticBaseProperty));
                    if (supportedDiagnosticsProperty != null && supportedDiagnosticsProperty.GetMethod != null)
                    {
                        var syntaxRef = supportedDiagnosticsProperty.GetMethod.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxRef != null)
                        {
                            var syntax = syntaxRef.GetSyntax(cancellationToken);
                            syntax = GetPropertyGetterBlockSyntax(syntax);
                            if (syntax != null)
                            {
                                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                                descriptorFields = GetReferencedDescriptorFields(syntax, semanticModel);
                            }
                        }
                    }
                }

                return ImmutableInterlocked.GetOrAdd(ref _supportedDescriptorFieldsMap, analyzer, descriptorFields);
            }

            private ImmutableArray<IFieldSymbol> GetReferencedDescriptorFields(SyntaxNode syntax, SemanticModel semanticModel)
            {
                var builder = ImmutableArray.CreateBuilder<IFieldSymbol>();
                foreach (var identifier in syntax.DescendantNodes().OfType<TIdentifierNameSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                    if (symbol != null && symbol.Kind == SymbolKind.Field)
                    {
                        var field = (IFieldSymbol)symbol;
                        var fieldType = field.Type as INamedTypeSymbol;
                        if (fieldType != null && fieldType.GetBaseTypesAndThis().Contains(_diagnosticDescriptorType))
                        {
                            builder.Add((IFieldSymbol)symbol);
                        }
                    }
                }

                return builder.ToImmutable();
            }

            protected override void AnalyzeNode(SymbolAnalysisContext symbolContext, TInvocationExpressionSyntax invocation, SemanticModel semanticModel)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation, symbolContext.CancellationToken).Symbol;
                if (symbol == null ||
                    symbol.Kind != SymbolKind.Method ||
                    !symbol.Name.Equals(ReportDiagnosticName, StringComparison.OrdinalIgnoreCase) ||
                    !_contextTypes.Contains(symbol.ContainingType))
                {
                    return;
                }

                var arguments = GetArgumentExpressions(invocation);
                if (arguments.Count() == 1)
                {
                    var argument = arguments.First();
                    var type = semanticModel.GetTypeInfo(argument, symbolContext.CancellationToken).ConvertedType;
                    if (type != null && type.Equals(_diagnosticType))
                    {
                        var argSymbol = semanticModel.GetSymbolInfo(argument, symbolContext.CancellationToken).Symbol;
                        if (argSymbol != null)
                        {
                            SyntaxNode diagnosticInitializerOpt = null;

                            var local = argSymbol as ILocalSymbol;
                            if (local != null)
                            {
                                var syntaxRef = local.DeclaringSyntaxReferences.FirstOrDefault();
                                if (syntaxRef != null)
                                {
                                    diagnosticInitializerOpt = syntaxRef.GetSyntax(symbolContext.CancellationToken).FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
                                }
                            }
                            else
                            {
                                var method = argSymbol as IMethodSymbol;
                                if (method != null &&
                                    method.ContainingType.Equals(_diagnosticType) &&
                                    method.Name.Equals(nameof(Diagnostic.Create), StringComparison.OrdinalIgnoreCase))
                                {
                                    diagnosticInitializerOpt = argument;
                                }
                            }

                            if (diagnosticInitializerOpt != null)
                            {
                                var descriptorFields = GetReferencedDescriptorFields(diagnosticInitializerOpt, semanticModel);
                                if (descriptorFields.Length == 1 &&
                                    !_supportedDescriptorFieldsMap[(INamedTypeSymbol)symbolContext.Symbol].Contains(descriptorFields[0]))
                                {
                                    var diagnostic = Diagnostic.Create(InvalidReportDiagnosticRule, invocation.GetLocation(), descriptorFields[0].Name);
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
