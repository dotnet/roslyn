// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Desktop.Analyzers.Common;

namespace Desktop.Analyzers
{
    public abstract class DoNotCatchCorruptedStateExceptionsAnalyzer<TLanguageKindEnum, TCatchClauseSyntax, TThrowStatementSyntax> : DiagnosticAnalyzer  
        where TLanguageKindEnum : struct
        where TCatchClauseSyntax : SyntaxNode
        where TThrowStatementSyntax : SyntaxNode
    {
        internal const string RuleId = "CA2153";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(DesktopAnalyzersResources.DoNotCatchCorruptedStateExceptions), DesktopAnalyzersResources.ResourceManager, typeof(DesktopAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(DesktopAnalyzersResources.DoNotCatchCorruptedStateExceptionsMessage), DesktopAnalyzersResources.ResourceManager, typeof(DesktopAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(DesktopAnalyzersResources.DoNotCatchCorruptedStateExceptionsDescription), DesktopAnalyzersResources.ResourceManager, typeof(DesktopAnalyzersResources));
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessage,
                                                                             DiagnosticCategory.Security,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: true,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: null,
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        protected abstract Analyzer GetAnalyzer(CompilationSecurityTypes compilationTypes, ISymbol owningSymbol, SyntaxNode codeBlock);

        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedDiagnostics = ImmutableArray.Create(Rule);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return s_supportedDiagnostics;
            }
        }

        /// <summary>
        /// Initialize the analyzer.
        /// </summary>
        /// <param name="analysisContext">Analyzer Context.</param>
        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    var compilationTypes = new CompilationSecurityTypes(context.Compilation);
                    if (compilationTypes.HandleProcessCorruptedStateExceptionsAttribute != null)
                    {
                        context.RegisterCodeBlockStartAction<TLanguageKindEnum>(
                        codeBlockStartContext =>
                        {
                            ISymbol owningSymbol = codeBlockStartContext.OwningSymbol;
                            if (owningSymbol.Kind == SymbolKind.Method)
                            {
                                var method = (IMethodSymbol)owningSymbol;

                                ImmutableArray<AttributeData> attributes = method.GetAttributes();
                                if (attributes.FirstOrDefault(attribute => attribute.AttributeClass == compilationTypes.HandleProcessCorruptedStateExceptionsAttribute) != null)
                                {
                                    Analyzer analyzer = GetAnalyzer(compilationTypes, owningSymbol, codeBlockStartContext.CodeBlock);
                                    codeBlockStartContext.RegisterSyntaxNodeAction(analyzer.AnalyzeCatchClause, analyzer.CatchClauseKind);
                                    codeBlockStartContext.RegisterSyntaxNodeAction(analyzer.AnalyzeThrowStatement, analyzer.ThrowStatementKind);
                                    codeBlockStartContext.RegisterCodeBlockEndAction(analyzer.AnalyzeCodeBlockEnd);
                                }
                            }
                        }); 
                    }
                });
        }

        protected abstract class Analyzer
        {
            private readonly ISymbol _owningSymbol;
            private readonly SyntaxNode _codeBlock;
            private readonly Dictionary<TCatchClauseSyntax, ISymbol> _catchAllCatchClauses;

            public abstract TLanguageKindEnum CatchClauseKind { get; }
            public abstract TLanguageKindEnum ThrowStatementKind { get; }
            protected CompilationSecurityTypes TypesOfInterest { get; private set; }
            protected abstract ISymbol GetExceptionTypeSymbolFromCatchClause(TCatchClauseSyntax catchNode, SemanticModel model);
            protected abstract bool IsThrowStatementWithNoArgument(TThrowStatementSyntax throwNode);
            protected abstract bool IsCatchClause(SyntaxNode node);
            protected abstract bool IslambdaExpression(SyntaxNode node);

            protected Analyzer(CompilationSecurityTypes compilationTypes, ISymbol owningSymbol, SyntaxNode codeBlock)
            {
                _owningSymbol = owningSymbol;
                _codeBlock = codeBlock;
                _catchAllCatchClauses = new Dictionary<TCatchClauseSyntax, ISymbol>();
                TypesOfInterest = compilationTypes;
            }

            public void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
            {
                var catchNode = (TCatchClauseSyntax)context.Node;
                ISymbol exceptionTypeSymbol = GetExceptionTypeSymbolFromCatchClause(catchNode, context.SemanticModel);

                if (IsCatchTypeTooGeneral(exceptionTypeSymbol))
                {
                    var parentNode = catchNode.Parent;
                    while (parentNode != _codeBlock)
                    {
                        // for now there doesn't seem to have any way to annotate lambdas with attributes
                        if (IslambdaExpression(parentNode))
                        {
                            return;
                        }
                        parentNode = parentNode.Parent;
                    }
                    _catchAllCatchClauses[catchNode] = exceptionTypeSymbol;
                }
            }

            public void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
            {
                var throwNode = (TThrowStatementSyntax)context.Node;

                // throwNode is a throw statement with no argument, which is not allowed outside of a catch clause
                if (IsThrowStatementWithNoArgument(throwNode))
                {
                    var enlcosingCatchClause = (TCatchClauseSyntax)throwNode.Ancestors().First(IsCatchClause);
                    _catchAllCatchClauses.Remove(enlcosingCatchClause);
                }
            }

            public void AnalyzeCodeBlockEnd(CodeBlockAnalysisContext context)
            {
                foreach (var pair in _catchAllCatchClauses)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rule,
                            pair.Key.GetLocation(),
                            _owningSymbol.ToDisplayString(),
                            pair.Value.ToDisplayString()));
                }
            }

            private bool IsCatchTypeTooGeneral(ISymbol catchTypeSym)
            {
                return catchTypeSym == null
                        || catchTypeSym == TypesOfInterest.SystemException
                        || catchTypeSym == TypesOfInterest.SystemSystemException
                        || catchTypeSym == TypesOfInterest.SystemObject;
            }
        }
    }
}
