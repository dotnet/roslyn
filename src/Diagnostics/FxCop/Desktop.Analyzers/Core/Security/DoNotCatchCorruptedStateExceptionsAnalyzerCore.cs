// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace SDL.Analyzer.Common
{
    public abstract class DoNotCatchCorruptedStateExceptionAnalyzerCore
    {
        protected CompilationSecurityTypes TypesOfInterest { get; private set; }

        protected DoNotCatchCorruptedStateExceptionAnalyzerCore(CompilationSecurityTypes compilationTypes)
        {
            TypesOfInterest = compilationTypes;
        }

        protected abstract void CheckNode(SyntaxNode methodNode, SemanticModel model, Action<Diagnostic> reportDiagnostic);

        /// <summary>
        /// Analyze the syntax node.
        /// </summary>
        /// <param name="context">Syntax Context.</param>
        public void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode node = context.Node;
            SemanticModel model = context.SemanticModel;
            IMethodSymbol method = model.GetDeclaredSymbol(node) as IMethodSymbol;
            if (method == null)
            {
                return;
            }

            ImmutableArray<AttributeData> attributes = method.GetAttributes();
            if (attributes.FirstOrDefault(attribute => attribute.AttributeClass == TypesOfInterest.HandleProcessCorruptedStateExceptionsAttribute) != null)
            {
                CheckNode(node, model, context.ReportDiagnostic);
            }
        }

        protected bool IsCatchTypeTooGeneral(ISymbol catchTypeSym)
        {
            return catchTypeSym == null
                    || catchTypeSym == TypesOfInterest.SystemException
                    || catchTypeSym == TypesOfInterest.SystemSystemException
                    || catchTypeSym == TypesOfInterest.SystemObject;
        }
    }
}
