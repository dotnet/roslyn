// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class CapturedVariableRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        internal static BoundNode Rewrite(
            GenerateThisReference getThisReference,
            Conversions conversions,
            ImmutableDictionary<string, DisplayClassVariable> displayClassVariables,
            BoundNode node,
            DiagnosticBag diagnostics)
        {
            var rewriter = new CapturedVariableRewriter(getThisReference, conversions, displayClassVariables, diagnostics);
            return rewriter.Visit(node);
        }

        private readonly GenerateThisReference _getThisReference;
        private readonly Conversions _conversions;
        private readonly ImmutableDictionary<string, DisplayClassVariable> _displayClassVariables;
        private readonly DiagnosticBag _diagnostics;

        private CapturedVariableRewriter(
            GenerateThisReference getThisReference,
            Conversions conversions,
            ImmutableDictionary<string, DisplayClassVariable> displayClassVariables,
            DiagnosticBag diagnostics)
        {
            _getThisReference = getThisReference;
            _conversions = conversions;
            _displayClassVariables = displayClassVariables;
            _diagnostics = diagnostics;
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            var rewrittenLocals = node.Locals.WhereAsArray(local => local.IsCompilerGenerated || local.Name == null || this.GetVariable(local.Name) == null);
            var rewrittenLocalFunctions = node.LocalFunctions;
            var rewrittenStatements = VisitList(node.Statements);
            return node.Update(rewrittenLocals, rewrittenLocalFunctions, rewrittenStatements);
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var local = node.LocalSymbol;
            if (!local.IsCompilerGenerated)
            {
                var variable = this.GetVariable(local.Name);
                if (variable != null)
                {
                    var result = variable.ToBoundExpression(node.Syntax);
                    Debug.Assert(TypeSymbol.Equals(node.Type, result.Type, TypeCompareKind.ConsiderEverything2));
                    return result;
                }
            }
            return node;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            var parameter = node.ParameterSymbol;
            var variable = this.GetVariable(parameter.Name);
            if (variable == null)
            {
                return node;
            }
            return variable.ToBoundExpression(node.Syntax);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            var rewrittenThis = GenerateThisReference(node);
            Debug.Assert(rewrittenThis.Type.Equals(node.Type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            return rewrittenThis;
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            var syntax = node.Syntax;
            var rewrittenThis = GenerateThisReference(node);
            var baseType = node.Type;
            HashSet<DiagnosticInfo> unusedUseSiteDiagnostics = null;
            var conversion = _conversions.ClassifyImplicitConversionFromExpression(rewrittenThis, baseType, ref unusedUseSiteDiagnostics);
            Debug.Assert(unusedUseSiteDiagnostics == null || !conversion.IsValid || unusedUseSiteDiagnostics.All(d => d.Severity < DiagnosticSeverity.Error));

            // It would be nice if we could just call BoundConversion.Synthesized, but it doesn't seem worthwhile to
            // introduce a bunch of new overloads to accommodate isBaseConversion.
            return new BoundConversion(
                syntax,
                rewrittenThis,
                conversion,
                isBaseConversion: true,
                @checked: false,
                explicitCastInCode: false,
                conversionGroupOpt: null,
                constantValueOpt: null,
                type: baseType,
                hasErrors: !conversion.IsValid)
            { WasCompilerGenerated = true };
        }

        private BoundExpression GenerateThisReference(BoundExpression node)
        {
            var syntax = node.Syntax;
            var rewrittenThis = _getThisReference(syntax);
            if (rewrittenThis != null)
            {
                return rewrittenThis;
            }
            var boundKind = node.Kind;
            Debug.Assert(boundKind == BoundKind.ThisReference || boundKind == BoundKind.BaseReference);
            var errorCode = boundKind == BoundKind.BaseReference
                ? ErrorCode.ERR_BaseInBadContext
                : ErrorCode.ERR_ThisInBadContext;
            _diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(errorCode), syntax.Location));
            return node;
        }

        private DisplayClassVariable GetVariable(string name)
        {
            DisplayClassVariable variable;
            _displayClassVariables.TryGetValue(name, out variable);
            return variable;
        }
    }
}
