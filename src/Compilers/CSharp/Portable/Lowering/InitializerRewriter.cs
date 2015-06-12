// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class InitializerRewriter
    {
        internal static BoundTypeOrInstanceInitializers Rewrite(ImmutableArray<BoundInitializer> boundInitializers, MethodSymbol method, TypeSymbol submissionResultTypeOpt)
        {
            Debug.Assert(!boundInitializers.IsDefault);
            Debug.Assert(method.IsSubmissionInitializer == ((object)submissionResultTypeOpt != null));

            var boundStatements = ArrayBuilder<BoundStatement>.GetInstance(boundInitializers.Length);
            BoundExpression submissionResult = null;

            for (int i = 0; i < boundInitializers.Length; i++)
            {
                var init = boundInitializers[i];

                switch (init.Kind)
                {
                    case BoundKind.FieldInitializer:
                        boundStatements.Add(RewriteFieldInitializer((BoundFieldInitializer)init));
                        break;

                    case BoundKind.GlobalStatementInitializer:
                        var statement = ((BoundGlobalStatementInitializer)init).Statement;
                        // The value of the last expression statement (if any) is returned from the submission initializer.
                        if ((object)submissionResultTypeOpt != null &&
                            i == boundInitializers.Length - 1 &&
                            statement.Kind == BoundKind.ExpressionStatement)
                        {
                            var expr = ((BoundExpressionStatement)statement).Expression;
                            if ((object)expr.Type != null && expr.Type.SpecialType != SpecialType.System_Void)
                            {
                                submissionResult = expr;
                                break;
                            }
                        }
                        boundStatements.Add(statement);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(init.Kind);
                }
            }

            var sourceMethod = method as SourceMethodSymbol;
            var syntax = ((object)sourceMethod != null) ? sourceMethod.SyntaxNode : method.GetNonNullSyntaxNode();

            if ((object)submissionResultTypeOpt != null)
            {
                if (submissionResult == null)
                {
                    // Return null if submission does not have a trailing expression.
                    Debug.Assert(submissionResultTypeOpt.IsReferenceType);
                    submissionResult = new BoundLiteral(syntax, ConstantValue.Null, submissionResultTypeOpt);
                }

                Debug.Assert((object)submissionResult.Type != null);
                Debug.Assert(submissionResult.Type.SpecialType != SpecialType.System_Void);

                // The expression is converted to the submission result type when the initializer is bound.
                boundStatements.Add(new BoundReturnStatement(submissionResult.Syntax, submissionResult));
            }

            return new BoundTypeOrInstanceInitializers(syntax, boundStatements.ToImmutableAndFree());
        }

        private static BoundStatement RewriteFieldInitializer(BoundFieldInitializer fieldInit)
        {
            var syntax = fieldInit.Syntax;
            var boundReceiver = fieldInit.Field.IsStatic ? null :
                                        new BoundThisReference(syntax, fieldInit.Field.ContainingType);

            // Mark this as CompilerGenerated so that the local rewriter doesn't add a sequence point.
            BoundStatement boundStatement =
                new BoundExpressionStatement(syntax,
                    new BoundAssignmentOperator(syntax,
                        new BoundFieldAccess(syntax,
                            boundReceiver,
                            fieldInit.Field,
                            constantValueOpt: null),
                        fieldInit.InitialValue,
                        fieldInit.Field.Type)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = true };

            Debug.Assert(syntax is ExpressionSyntax); // Should be the initial value.
            Debug.Assert(syntax.Parent.Kind() == SyntaxKind.EqualsValueClause);
            switch (syntax.Parent.Parent.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    var declaratorSyntax = (VariableDeclaratorSyntax)syntax.Parent.Parent;
                    boundStatement = LocalRewriter.AddSequencePoint(declaratorSyntax, boundStatement);
                    break;

                case SyntaxKind.PropertyDeclaration:
                    var declaration = (PropertyDeclarationSyntax)syntax.Parent.Parent;
                    boundStatement = LocalRewriter.AddSequencePoint(declaration, boundStatement);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Parent.Parent.Kind());
            }

            return boundStatement;
        }
    }
}
