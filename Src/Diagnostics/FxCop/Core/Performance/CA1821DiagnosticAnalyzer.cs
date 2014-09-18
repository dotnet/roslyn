// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    public abstract class CA1821DiagnosticAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1821";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.RemoveEmptyFinalizers,
                                                                         FxCopRulesResources.RemoveEmptyFinalizers,
                                                                         FxCopDiagnosticCategory.Performance,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: FxCopRulesResources.RemoveEmptyFinalizersDescription,
                                                                         helpLink: "http://msdn.microsoft.com/library/bb264476.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(CA1821DiagnosticAnalyzerRule.Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCodeBlockStartAction<TSyntaxKind>(
                (context) =>
        {
                    var method = context.OwningSymbol as IMethodSymbol;
            if (method == null)
            {
                        return;
            }

            if (!IsDestructor(method))
            {
                        return;
            }

                    context.RegisterCodeBlockEndAction(GetCodeBlockEndedAnalyzer().AnalyzeCodeBlock);
                });
        }

        protected abstract AbstractCodeBlockEndedAnalyzer GetCodeBlockEndedAnalyzer();

        private static bool IsDestructor(IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.Destructor)
            {
                return true; // for C#
            }

            if (method.Name != "Finalize" || method.Parameters.Length != 0 || !method.ReturnsVoid)
            {
                return false;
            }

            var overridden = method.OverriddenMethod;
            if (overridden == null)
            {
                return false;
            }

            for (var o = overridden.OverriddenMethod; o != null; o = o.OverriddenMethod)
            {
                overridden = o;
            }

            return overridden.ContainingType.SpecialType == SpecialType.System_Object; // it is object.Finalize
        }

        protected abstract class AbstractCodeBlockEndedAnalyzer
        {
            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(CA1821DiagnosticAnalyzerRule.Rule);
                }
            }

            public void AnalyzeCodeBlock(CodeBlockEndAnalysisContext context)
            {
                if (IsEmptyFinalizer(context.CodeBlock, context.SemanticModel))
                {
                    context.ReportDiagnostic(context.OwningSymbol.CreateDiagnostic(CA1821DiagnosticAnalyzerRule.Rule));
                }
            }

            protected abstract bool IsEmptyFinalizer(SyntaxNode node, SemanticModel model);
        }
    }
}