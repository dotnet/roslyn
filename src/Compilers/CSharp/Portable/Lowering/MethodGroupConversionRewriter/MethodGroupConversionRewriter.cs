// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodGroupConversionRewriter : BoundTreeRewriterWithStackGuard
    {
        private MethodSymbol CurrentMethod;
        private TypeCompilationState CompilationState;
        private DiagnosticBag Diagnostics;

        private MethodGroupConversionRewriter() { }

        public static BoundStatement Rewrite(
            MethodSymbol currentMethod,
            BoundStatement loweredBody,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            if (IsCurrentMethodNotSuitableForRewrite(currentMethod))
            {
                return loweredBody;
            }

            var rewriter = new MethodGroupConversionRewriter
            {
                CurrentMethod = currentMethod,
                CompilationState = compilationState,
                Diagnostics = diagnostics,
            };

            var result = (BoundStatement)rewriter.Visit(loweredBody);
            return result;
        }

        internal static bool IsConversionRewritable(BoundConversion conversion, BoundExpression operand)
        {
            // We only target implicit method group conversion
            if (conversion.ExplicitCastInCode || conversion.ConversionKind != ConversionKind.MethodGroup)
            {
                return false;
            }

            // Static constructors are not suitable to cache
            if (IsCurrentMethodNotSuitableForRewrite(conversion.SymbolOpt))
            {
                return false;
            }

            // Make sure the operand is just ordinary static method
            var targetMethod = (operand as BoundMethodGroup)?.LookupSymbolOpt as MethodSymbol;
            return (targetMethod != null
                && targetMethod.IsStatic
                && targetMethod.MethodKind == MethodKind.Ordinary);
        }

        private static bool IsCurrentMethodNotSuitableForRewrite(MethodSymbol currentMethod)
        {
            return currentMethod?.MethodKind == MethodKind.StaticConstructor;
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (IsConversionRewritable(node, node.Operand))
            {
                return RewriteConversion(node);
            }

            return base.VisitConversion(node);
        }

        private BoundNode RewriteConversion(BoundConversion node)
        {

        }
    }
}
