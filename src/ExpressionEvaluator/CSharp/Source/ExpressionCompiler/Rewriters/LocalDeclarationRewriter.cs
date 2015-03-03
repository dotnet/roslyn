// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class LocalDeclarationRewriter
    {
        internal static BoundNode Rewrite(CSharpCompilation compilation, EENamedTypeSymbol container, HashSet<LocalSymbol> declaredLocals, BoundNode node)
        {
            var builder = ArrayBuilder<BoundStatement>.GetInstance();
            bool hasChanged;

            // Rewrite top-level declarations only.
            switch (node.Kind)
            {
                case BoundKind.LocalDeclaration:
                    RewriteLocalDeclaration(compilation, container, declaredLocals, builder, (BoundLocalDeclaration)node);
                    hasChanged = true;
                    break;
                case BoundKind.MultipleLocalDeclarations:
                    foreach (var declaration in ((BoundMultipleLocalDeclarations)node).LocalDeclarations)
                    {
                        RewriteLocalDeclaration(compilation, container, declaredLocals, builder, declaration);
                    }
                    hasChanged = true;
                    break;
                default:
                    hasChanged = false;
                    break;
            }

            if (hasChanged)
            {
                node = new BoundBlock(node.Syntax, ImmutableArray<LocalSymbol>.Empty, builder.ToImmutable()) { WasCompilerGenerated = true };
            }

            builder.Free();
            return node;
        }

        private static void RewriteLocalDeclaration(
            CSharpCompilation compilation,
            EENamedTypeSymbol container,
            HashSet<LocalSymbol> declaredLocals,
            ArrayBuilder<BoundStatement> statements,
            BoundLocalDeclaration node)
        {
            Debug.Assert(node.ArgumentsOpt.IsDefault);

            var local = node.LocalSymbol;
            var syntax = node.Syntax;

            declaredLocals.Add(local);

            var typeType = compilation.GetWellKnownType(WellKnownType.System_Type);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // CreateVariable(Type type, string name)
            var method = PlaceholderLocalSymbol.GetIntrinsicMethod(compilation, ExpressionCompilerConstants.CreateVariableMethodName);
            var type = new BoundTypeOfOperator(syntax, new BoundTypeExpression(syntax, aliasOpt: null, type: local.Type), null, typeType);
            var name = new BoundLiteral(syntax, ConstantValue.Create(local.Name), stringType);
            var call = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                method: method,
                arguments: ImmutableArray.Create<BoundExpression>(type, name));
            statements.Add(new BoundExpressionStatement(syntax, call));

            var initializer = node.InitializerOpt;
            if (initializer != null)
            {
                // Generate assignment to local. The assignment will
                // be rewritten in PlaceholderLocalRewriter.
                var assignment = new BoundAssignmentOperator(
                    syntax,
                    new BoundLocal(syntax, local, constantValueOpt: null, type: local.Type),
                    initializer,
                    RefKind.None,
                    local.Type);
                statements.Add(new BoundExpressionStatement(syntax, assignment));
            }
        }
    }
}
