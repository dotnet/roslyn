// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    public abstract class CA1821DiagnosticAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(CA1821DiagnosticAnalyzerRule.Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCodeBlockStartAction<TLanguageKindEnum>(
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

            public void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
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
