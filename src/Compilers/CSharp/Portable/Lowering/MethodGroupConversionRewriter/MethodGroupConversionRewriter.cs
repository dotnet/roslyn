// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private readonly MethodSymbol CurrentMethod;
        private readonly TypeCompilationState CompilationState;
        private readonly DiagnosticBag Diagnostics;
        private readonly SyntheticBoundNodeFactory F;

        private MethodGroupConversionRewriter(MethodSymbol currentMethod, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            CurrentMethod = currentMethod;
            CompilationState = compilationState;
            Diagnostics = diagnostics;

            F = new SyntheticBoundNodeFactory(currentMethod, (CSharpSyntaxNode)CSharpSyntaxTree.Dummy.GetRoot(), compilationState, diagnostics);

            if (CompilationState.MethodGroupConversionCacheTargetFrames == null)
            {
                compilationState.MethodGroupConversionCacheTargetFrames = new Dictionary<KeyValuePair<NamedTypeSymbol, MethodSymbol>, MethodGroupConversionCacheTargetFrame>();
            }
        }

        public static BoundStatement Rewrite(
            MethodSymbol currentMethod,
            BoundStatement loweredBody,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(currentMethod != null);
            Debug.Assert(loweredBody?.HasErrors == false);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            var rewriter = new MethodGroupConversionRewriter(currentMethod, compilationState, diagnostics);
            return (BoundStatement)rewriter.Visit(loweredBody);
        }

        internal static bool IsConversionRewritable(BoundConversion conversion, BoundExpression operand)
        {
            // We only target implicit method group conversion
            if (conversion.ExplicitCastInCode || conversion.ConversionKind != ConversionKind.MethodGroup)
            {
                return false;
            }

            // Make sure the operand is just an ordinary static method
            var targetMethod = GetTargetMethod(conversion);
            return (targetMethod != null
                && targetMethod.IsStatic
                && targetMethod.MethodKind == MethodKind.Ordinary);
        }

        private static MethodSymbol GetTargetMethod(BoundConversion conversion)
            => conversion.ExpressionSymbol as MethodSymbol;

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (IsConversionRewritable(node, node.Operand))
            {
                return RewriteConversion(node);
            }

            return base.VisitConversion(node);
        }

        private BoundNode RewriteConversion(BoundConversion conversion)
        {
            var targetMethod = GetTargetMethod(conversion);
            var currentModule = CompilationState.ModuleBuilderOpt;

            Debug.Assert(targetMethod != null);
            Debug.Assert(currentModule != null);
            Debug.Assert(conversion.Type is NamedTypeSymbol);

            F.Syntax = conversion.Syntax;
            try
            {
                var boundDelegateField = GetBoundDelegateField(F, targetMethod, conversion);

                var boundDelegateCreation = new BoundDelegateCreationExpression(
                    conversion.Syntax,
                    conversion.Operand,
                    targetMethod,
                    targetMethod.IsExtensionMethod,
                    conversion.Type)
                {
                    WasCompilerGenerated = true
                };

                return F.Coalesce(boundDelegateField, F.AssignmentExpression(boundDelegateField, boundDelegateCreation));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember e)
            {
                Diagnostics.Add(e.Diagnostic);
                return new BoundBadExpression(F.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(conversion), conversion.Type);
            }
        }

        private BoundFieldAccess GetBoundDelegateField(SyntheticBoundNodeFactory F, MethodSymbol targetMethod, BoundConversion conversion)
        {
            var typeArguments = MethodGroupConversionCacheTargetFrame.GetTypeArgumentsFromTarget(targetMethod);

            MethodGroupConversionCacheTargetFrame targetFrame;
            var keyForTargetFrame = new KeyValuePair<NamedTypeSymbol, MethodSymbol>(CurrentMethod.ContainingType, targetMethod.OriginalDefinition);
            if (!CompilationState.MethodGroupConversionCacheTargetFrames.TryGetValue(keyForTargetFrame, out targetFrame))
            {
                targetFrame = MethodGroupConversionCacheTargetFrame.Create(CurrentMethod.ContainingType, targetMethod, typeArguments.Length);
                CompilationState.MethodGroupConversionCacheTargetFrames.Add(keyForTargetFrame, targetFrame);
            }

            var delegateType = (NamedTypeSymbol)conversion.Type;
            var delegateFrame = targetFrame.GetOrAddDelegateFrame(delegateType);
            var delegateField = delegateFrame.DelegateField;

            var constructedTargetFrame = typeArguments.Length == 0 ? targetFrame : targetFrame.Construct(typeArguments);
            var constructedDelegateFrame = delegateType.Arity == 0 ? delegateFrame : delegateFrame.AsMember(constructedTargetFrame).Construct(delegateType.TypeArguments);
            return F.Field(null, delegateField.AsMember(constructedDelegateFrame));
        }
    }
}
