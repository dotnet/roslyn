// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class LocalDeclarationRewriter
    {
        internal static BoundStatement Rewrite(
            CSharpCompilation compilation,
            HashSet<LocalSymbol> declaredLocals,
            BoundStatement node,
            ImmutableArray<LocalSymbol> declaredLocalsArray,
            DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            foreach (var local in declaredLocalsArray)
            {
                CreateLocal(compilation, declaredLocals, builder, local, node.Syntax, diagnostics);
            }

            // Rewrite top-level declarations only.
            switch (node.Kind)
            {
                case BoundKind.LocalDeclaration:
                    Debug.Assert(declaredLocals.Contains(((BoundLocalDeclaration)node).LocalSymbol));
                    RewriteLocalDeclaration(builder, (BoundLocalDeclaration)node);
                    break;

                case BoundKind.MultipleLocalDeclarations:
                    foreach (var declaration in ((BoundMultipleLocalDeclarations)node).LocalDeclarations)
                    {
                        Debug.Assert(declaredLocals.Contains(declaration.LocalSymbol));
                        RewriteLocalDeclaration(builder, declaration);
                    }

                    break;

                default:
                    if (builder.Count == 0)
                    {
                        builder.Free();
                        return node;
                    }

                    builder.Add(node);
                    break;
            }

            return BoundBlock.SynthesizedNoLocals(node.Syntax, builder.ToImmutableAndFree());
        }

        private static void RewriteLocalDeclaration(
            ArrayBuilder<BoundStatement> statements,
            BoundLocalDeclaration node)
        {
            Debug.Assert(node.ArgumentsOpt.IsDefault);

            var initializer = node.InitializerOpt;
            if (initializer != null)
            {
                var local = node.LocalSymbol;
                var syntax = node.Syntax;

                // Generate assignment to local. The assignment will
                // be rewritten in PlaceholderLocalRewriter.
                var type = local.Type;
                var assignment = new BoundAssignmentOperator(
                    syntax,
                    new BoundLocal(syntax, local, constantValueOpt: null, type: type),
                    initializer,
                    false,
                    type);
                statements.Add(new BoundExpressionStatement(syntax, assignment));
            }
        }

        private static void CreateLocal(
            CSharpCompilation compilation,
            HashSet<LocalSymbol> declaredLocals,
            ArrayBuilder<BoundStatement> statements,
            LocalSymbol local,
            SyntaxNode syntax,
            DiagnosticBag diagnostics)
        {
            // CreateVariable(Type type, string name)
            var method = PlaceholderLocalSymbol.GetIntrinsicMethod(compilation, ExpressionCompilerConstants.CreateVariableMethodName);
            if ((object)method == null)
            {
                diagnostics.Add(ErrorCode.ERR_DeclarationExpressionNotPermitted, local.GetFirstLocation());
                return;
            }

            declaredLocals.Add(local);

            var typeType = compilation.GetWellKnownType(WellKnownType.System_Type);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var guidConstructor = (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Guid__ctor);
            var type = new BoundTypeOfOperator(syntax, new BoundTypeExpression(syntax, aliasOpt: null, type: local.Type), null, typeType);
            var name = new BoundLiteral(syntax, ConstantValue.Create(local.Name), stringType);
            bool hasCustomTypeInfoPayload;
            var customTypeInfoPayload = GetCustomTypeInfoPayload(local, syntax, compilation, out hasCustomTypeInfoPayload);
            var customTypeInfoPayloadId = GetCustomTypeInfoPayloadId(syntax, guidConstructor, hasCustomTypeInfoPayload);
            var call = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                method: method,
                arguments: ImmutableArray.Create(type, name, customTypeInfoPayloadId, customTypeInfoPayload));
            statements.Add(new BoundExpressionStatement(syntax, call));
        }

        private static BoundExpression GetCustomTypeInfoPayloadId(SyntaxNode syntax, MethodSymbol guidConstructor, bool hasCustomTypeInfoPayload)
        {
            if (!hasCustomTypeInfoPayload)
            {
                return new BoundDefaultExpression(syntax, targetType: null, constantValueOpt: null, guidConstructor.ContainingType);
            }

            var value = ConstantValue.Create(CustomTypeInfo.PayloadTypeId.ToString());
            return new BoundObjectCreationExpression(
                syntax,
                guidConstructor,
                new BoundLiteral(syntax, value, guidConstructor.ContainingType));
        }

        private static BoundExpression GetCustomTypeInfoPayload(LocalSymbol local, SyntaxNode syntax, CSharpCompilation compilation, out bool hasCustomTypeInfoPayload)
        {
            var byteArrayType = ArrayTypeSymbol.CreateSZArray(
                compilation.Assembly,
                TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Byte)));

            var bytes = compilation.GetCustomTypeInfoPayload(local.Type, customModifiersCount: 0, refKind: RefKind.None);
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
                new BoundArrayInitialization(syntax, isInferred: false, initializerExprs.ToImmutableAndFree()),
                byteArrayType);
        }
    }
}
