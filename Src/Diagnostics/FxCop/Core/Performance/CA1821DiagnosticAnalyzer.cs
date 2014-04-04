// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    public abstract class CA1821DiagnosticAnalyzer : ICodeBlockStartedAnalyzer
    {
        internal const string RuleId = "CA1821";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.RemoveEmptyFinalizers,
                                                                         FxCopRulesResources.RemoveEmptyFinalizers,
                                                                         FxCopDiagnosticCategory.Performance,
                                                                         DiagnosticSeverity.Warning);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public ICodeBlockEndedAnalyzer OnCodeBlockStarted(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            var method = ownerSymbol as IMethodSymbol;
            if (method == null)
            {
                return null;
            }

            if (!IsDestructor(method))
            {
                return null;
            }

            return GetCodeBlockEndedAnalyzer();
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

        protected abstract class AbstractCodeBlockEndedAnalyzer : ICodeBlockEndedAnalyzer
        {
            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public void OnCodeBlockEnded(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                if (IsEmptyFinalizer(codeBlock, semanticModel))
                {
                    addDiagnostic(ownerSymbol.CreateDiagnostic(Rule));
                }
            }

            protected abstract bool IsEmptyFinalizer(SyntaxNode node, SemanticModel model);
        }
    }
}