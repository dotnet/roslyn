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
        private readonly PEModuleBuilder ModuleBuilder;

        private MethodGroupConversionRewriter(MethodSymbol currentMethod, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            CurrentMethod = currentMethod;
            CompilationState = compilationState;
            Diagnostics = diagnostics;

            F = new SyntheticBoundNodeFactory(currentMethod, (CSharpSyntaxNode)CSharpSyntaxTree.Dummy.GetRoot(), compilationState, diagnostics);
            ModuleBuilder = compilationState.ModuleBuilderOpt;
        }

        public static BoundStatement Rewrite(
            MethodSymbol currentMethod,
            BoundStatement loweredBody,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(currentMethod != null);
            Debug.Assert(loweredBody?.HasErrors == false);
            Debug.Assert(diagnostics != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(compilationState.ModuleBuilderOpt != null);

            if (compilationState.MethodGroupConversionCacheTargetFrames == null)
            {
                compilationState.MethodGroupConversionCacheTargetFrames = new Dictionary<KeyValuePair<NamedTypeSymbol, MethodSymbol>, MethodGroupConversionCacheTargetFrame>();
            }

            var rewriter = new MethodGroupConversionRewriter(currentMethod, compilationState, diagnostics);
            return (BoundStatement)rewriter.Visit(loweredBody);
        }

        /// <remarks>
        /// This method is also used by <see cref="LocalRewriter.VisitConversion(BoundConversion)"/> to discover interested conversions.
        /// <see cref="LocalRewriter"/> steps inside expression lambdas, while this don't.
        /// </remarks>
        internal static bool IsInterestedConversion(BoundConversion conversion, BoundExpression operand, bool isInExpressionLambda)
        {
            // Not interested in expression lambdas
            if (isInExpressionLambda)
            {
                return false;
            }

            // We only target implicit method group conversions
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

        public override BoundNode VisitLambda(BoundLambda node)
            => node.Type.IsExpressionTree() ? node : base.VisitLambda(node);

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (IsInterestedConversion(node, node.Operand, isInExpressionLambda: false))
            {
                return RewriteConversion(node);
            }

            return base.VisitConversion(node);
        }

        private BoundNode RewriteConversion(BoundConversion conversion)
        {
            var targetMethod = GetTargetMethod(conversion);

            Debug.Assert(targetMethod != null);
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
            var container = CurrentMethod.ContainingType;
            var keyForTargetFrame = new KeyValuePair<NamedTypeSymbol, MethodSymbol>(container, targetMethod.OriginalDefinition);
            if (!CompilationState.MethodGroupConversionCacheTargetFrames.TryGetValue(keyForTargetFrame, out targetFrame))
            {
                targetFrame = MethodGroupConversionCacheTargetFrame.Create(container, targetMethod, typeArguments.Length);

                ModuleBuilder.AddSynthesizedDefinition(container, targetFrame);
                CompilationState.MethodGroupConversionCacheTargetFrames.Add(keyForTargetFrame, targetFrame);
            }

            var delegateType = (NamedTypeSymbol)conversion.Type;

            bool wasDelegateFrameAdded;
            var delegateFrame = targetFrame.GetOrAddDelegateFrame(delegateType, out wasDelegateFrameAdded);
            var delegateField = delegateFrame.DelegateField;

            if (wasDelegateFrameAdded)
            {
                ModuleBuilder.AddSynthesizedDefinition(targetFrame, delegateFrame);
            }

            var constructedTargetFrame = typeArguments.Length == 0 ? targetFrame : targetFrame.Construct(typeArguments);
            var substitutedDelegateFrame = delegateFrame.AsMember(constructedTargetFrame);
            var constructedDelegateFrame = delegateType.Arity == 0 ? substitutedDelegateFrame : substitutedDelegateFrame.Construct(delegateType.TypeArguments);
            return F.Field(null, delegateField.AsMember(constructedDelegateFrame));
        }
    }
}
