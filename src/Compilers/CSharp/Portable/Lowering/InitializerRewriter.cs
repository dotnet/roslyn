// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class InitializerRewriter
    {
        internal static BoundTypeOrInstanceInitializers Rewrite(ImmutableArray<BoundInitializer> boundInitializers, MethodSymbol constructor)
        {
            Debug.Assert(!boundInitializers.IsDefault);

            var boundStatements = ArrayBuilder<BoundStatement>.GetInstance(boundInitializers.Length);

            for (int i = 0; i < boundInitializers.Length; i++)
            {
                var init = boundInitializers[i];

                switch (init.Kind)
                {
                    case BoundKind.FieldInitializer:
                        boundStatements.Add(RewriteFieldInitializer((BoundFieldInitializer)init));
                        break;

                    case BoundKind.GlobalStatementInitializer:
                        var stmtInit = (BoundGlobalStatementInitializer)init;

                        // the value of the last expression statement (if any) is stored to a ref parameter of the submission constructor:
                        if (constructor.IsSubmissionConstructor && i == boundInitializers.Length - 1 && stmtInit.Statement.Kind == BoundKind.ExpressionStatement)
                        {
                            var syntax = stmtInit.Syntax;
                            var submissionResultVariable = new BoundParameter(syntax, constructor.Parameters[1]);
                            var expr = ((BoundExpressionStatement)stmtInit.Statement).Expression;

                            // The expression is converted to the submission result type when the initializer is bound, 
                            // so we just need to assign it to the out parameter:
                            if ((object)expr.Type != null && expr.Type.SpecialType != SpecialType.System_Void)
                            {
                                boundStatements.Add(
                                    new BoundExpressionStatement(syntax,
                                        new BoundAssignmentOperator(syntax,
                                            submissionResultVariable,
                                            expr,
                                            expr.Type
                                        )
                                    ));

                                break;
                            }
                        }

                        boundStatements.Add(stmtInit.Statement);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(init.Kind);
                }
            }

            Debug.Assert(boundStatements.Count == boundInitializers.Length);

            CSharpSyntaxNode listSyntax;

            SourceMethodSymbol sourceConstructor = constructor as SourceMethodSymbol;
            if ((object)sourceConstructor != null)
            {
                listSyntax = sourceConstructor.SyntaxNode;
            }
            else
            {
                listSyntax = constructor.GetNonNullSyntaxNode();
            }

            return new BoundTypeOrInstanceInitializers(listSyntax, boundStatements.ToImmutableAndFree());
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
