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
            ParameterSymbol targetMethodThisParameter,
            Conversions conversions,
            ImmutableDictionary<string, DisplayClassVariable> displayClassVariables,
            BoundNode node,
            DiagnosticBag diagnostics)
        {
            var rewriter = new CapturedVariableRewriter(targetMethodThisParameter, conversions, displayClassVariables, diagnostics);
            return rewriter.Visit(node);
        }

        private readonly ParameterSymbol _targetMethodThisParameter;
        private readonly Conversions _conversions;
        private readonly ImmutableDictionary<string, DisplayClassVariable> _displayClassVariables;
        private readonly DiagnosticBag _diagnostics;

        private CapturedVariableRewriter(
            ParameterSymbol targetMethodThisParameter,
            Conversions conversions,
            ImmutableDictionary<string, DisplayClassVariable> displayClassVariables,
            DiagnosticBag diagnostics)
        {
            _targetMethodThisParameter = targetMethodThisParameter;
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
                    Debug.Assert(node.Type == result.Type);
                    return result;
                }
            }
            return node;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            return RewriteParameter(node.Syntax, node.ParameterSymbol, node);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            if ((node.Flags & BoundMethodGroupFlags.HasImplicitReceiver) == BoundMethodGroupFlags.HasImplicitReceiver &&
                (object)_targetMethodThisParameter == null)
            {
                // This can happen in static contexts.
                // NOTE: LocalRewriter has already been run, so the receiver has already been replaced with an
                // appropriate type expression, if this is a static context.
                // NOTE: Don't go through VisitThisReference, because it'll produce a diagnostic.
                return node.Update(
                    node.TypeArgumentsOpt,
                    node.Name,
                    node.Methods,
                    node.LookupSymbolOpt,
                    node.LookupError,
                    node.Flags,
                    receiverOpt: null,
                    resultKind: node.ResultKind);
            }
            else
            {
                return base.VisitMethodGroup(node);
            }
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            return RewriteParameter(node.Syntax, _targetMethodThisParameter, node);
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            var syntax = node.Syntax;
            var rewrittenParameter = RewriteParameter(syntax, _targetMethodThisParameter, node);

            var baseType = node.Type;
            HashSet<DiagnosticInfo> unusedUseSiteDiagnostics = null;
            var conversion = _conversions.ClassifyImplicitConversionFromExpression(rewrittenParameter, baseType, ref unusedUseSiteDiagnostics);
            Debug.Assert(unusedUseSiteDiagnostics == null || !conversion.IsValid || unusedUseSiteDiagnostics.All(d => d.Severity < DiagnosticSeverity.Error));

            // It would be nice if we could just call BoundConversion.Synthesized, but it doesn't seem worthwhile to
            // introduce a bunch of new overloads to accommodate isBaseConversion.
            return new BoundConversion(
                syntax,
                rewrittenParameter,
                conversion.Kind,
                conversion.ResultKind,
                isBaseConversion: true,
                symbolOpt: conversion.Method,
                @checked: false,
                explicitCastInCode: false,
                isExtensionMethod: conversion.IsExtensionMethod,
                isArrayIndex: conversion.IsArrayIndex,
                constantValueOpt: null,
                type: baseType,
                hasErrors: !conversion.IsValid)
            { WasCompilerGenerated = true };
        }

        private BoundExpression RewriteParameter(CSharpSyntaxNode syntax, ParameterSymbol symbol, BoundExpression node)
        {
            // This can happen in error scenarios (e.g. user binds "this" in a lambda in a static method).
            if ((object)symbol == null)
            {
                ReportMissingThis(node.Kind, syntax);
                return node;
            }

            var variable = this.GetVariable(symbol.Name);
            if (variable == null)
            {
                var typeNameKind = GeneratedNames.GetKind(symbol.Type.Name);
                if (typeNameKind != GeneratedNameKind.None &&
                    typeNameKind != GeneratedNameKind.AnonymousType)
                {
                    // The state machine case is for async lambdas.  The state machine
                    // will have a hoisted "this" field if it needs to access the
                    // containing display class, but the display class may not have a
                    // "this" field.
                    Debug.Assert(typeNameKind == GeneratedNameKind.LambdaDisplayClass ||
                        typeNameKind == GeneratedNameKind.StateMachineType,
                        $"Unexpected typeNameKind '{typeNameKind}'");
                    ReportMissingThis(node.Kind, syntax);
                    return node;
                }

                return (node as BoundParameter) ?? new BoundParameter(syntax, symbol);
            }

            var result = variable.ToBoundExpression(syntax);
            Debug.Assert(node.Kind == BoundKind.BaseReference
                ? result.Type.BaseType.Equals(node.Type, ignoreDynamic: true)
                : result.Type.Equals(node.Type, ignoreDynamic: true));
            return result;
        }

        private void ReportMissingThis(BoundKind boundKind, CSharpSyntaxNode syntax)
        {
            Debug.Assert(boundKind == BoundKind.ThisReference || boundKind == BoundKind.BaseReference);
            var errorCode = boundKind == BoundKind.BaseReference
                ? ErrorCode.ERR_BaseInBadContext
                : ErrorCode.ERR_ThisInBadContext;
            _diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(errorCode), syntax.Location));
        }

        private DisplayClassVariable GetVariable(string name)
        {
            DisplayClassVariable variable;
            _displayClassVariables.TryGetValue(name, out variable);
            return variable;
        }
    }
}
