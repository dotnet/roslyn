// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

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
                node = BoundBlock.SynthesizedNoLocals(node.Syntax, builder.ToImmutable());
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
            var guidConstructor = (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Guid__ctor);

            // CreateVariable(Type type, string name)
            var method = PlaceholderLocalSymbol.GetIntrinsicMethod(compilation, ExpressionCompilerConstants.CreateVariableMethodName);
            var type = new BoundTypeOfOperator(syntax, new BoundTypeExpression(syntax, aliasOpt: null, type: local.Type), null, typeType);
            var name = new BoundLiteral(syntax, ConstantValue.Create(local.Name), stringType);

            bool hasCustomTypeInfoPayload;
            var customTypeInfoPayload = GetCustomTypeInfoPayload(local, syntax, compilation, out hasCustomTypeInfoPayload);
            var customTypeInfoPayloadId = GetCustomTypeInfoPayloadId(syntax, guidConstructor, hasCustomTypeInfoPayload);
            var call = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                method: method,
                arguments: ImmutableArray.Create(type, name, customTypeInfoPayloadId, customTypeInfoPayload));
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

        private static BoundExpression GetCustomTypeInfoPayloadId(CSharpSyntaxNode syntax, MethodSymbol guidConstructor, bool hasCustomTypeInfoPayload)
        {
            if (!hasCustomTypeInfoPayload)
            {
                return new BoundDefaultOperator(syntax, guidConstructor.ContainingType);
            }

            var value = ConstantValue.Create(DynamicFlagsCustomTypeInfo.PayloadTypeId.ToString());
            return new BoundObjectCreationExpression(
                syntax,
                guidConstructor,
                new BoundLiteral(syntax, value, guidConstructor.ContainingType));
        }

        private static BoundExpression GetCustomTypeInfoPayload(LocalSymbol local, CSharpSyntaxNode syntax, CSharpCompilation compilation, out bool hasCustomTypeInfoPayload)
        {
            var byteArrayType = ArrayTypeSymbol.CreateSZArray(
                compilation.Assembly,
                compilation.GetSpecialType(SpecialType.System_Byte));

            var flags = CSharpCompilation.DynamicTransformsEncoder.Encode(local.Type, customModifiersCount: 0, refKind: RefKind.None);
            var bytes = DynamicFlagsCustomTypeInfo.Create(flags).GetCustomTypeInfoPayload();
            hasCustomTypeInfoPayload = bytes != null;
            if (!hasCustomTypeInfoPayload)
            {
                return new BoundLiteral(syntax, ConstantValue.Null, byteArrayType);
            }

            var byteType = byteArrayType.ElementType;
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            var numBytes = bytes.Count;
            var initializerExprs = ArrayBuilder<BoundExpression>.GetInstance(numBytes);
            foreach (var b in bytes)
            {
                initializerExprs.Add(new BoundLiteral(syntax, ConstantValue.Create(b), byteType));
            }

            var lengthExpr = new BoundLiteral(syntax, ConstantValue.Create(numBytes), intType);
            return new BoundArrayCreation(
                syntax,
                ImmutableArray.Create<BoundExpression>(lengthExpr),
                new BoundArrayInitialization(syntax, initializerExprs.ToImmutableAndFree()),
                byteArrayType);
        }
    }
}
