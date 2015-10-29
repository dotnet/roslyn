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

            F.Syntax = conversion.Syntax;
            try
            {
                BoundFieldAccess boundDelegateField = GetBoundDelegateField(F, targetMethod, conversion);

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
                return new BoundBadExpression(F.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(conversion), conversion.Type);
            }
        }

        private BoundFieldAccess GetBoundDelegateField(SyntheticBoundNodeFactory F, MethodSymbol targetMethod, BoundConversion conversion)
        {
            var frameContainer = CompilationState.Compilation.Assembly;
            var typeArguments = GetTypeArgumentsForFrame(targetMethod);
            var frameSymbol = MethodGroupConversionCacheFrame.Create(frameContainer, typeArguments.Length, conversion.Type, targetMethod);

            frameSymbol.Sythesize(CompilationState);

            var constructedFrameSymbol = typeArguments.Length == 0 ? frameSymbol : frameSymbol.Construct(typeArguments);
            return F.Field(null, frameSymbol.FieldForCachedDelegate.AsMember(constructedFrameSymbol));
        }

        private ImmutableArray<TypeSymbol> GetTypeArgumentsForFrame(MethodSymbol targetMethod)
        {
            var typeArgumentsBuilder = ArrayBuilder<TypeSymbol>.GetInstance();

            if (targetMethod.Arity > 0)
            {
                var constructed = (ConstructedMethodSymbol)targetMethod;
                typeArgumentsBuilder.AddRange(constructed.TypeArguments);
            }

            var containingType = targetMethod.ContainingType;
            while (containingType != null)
            {
                if (containingType.Arity > 0)
                {
                    typeArgumentsBuilder.AddRange(containingType.TypeArguments);
                }

                containingType = containingType.ContainingType;
            }

            return typeArgumentsBuilder.ToImmutableAndFree();
        }
    }
}
