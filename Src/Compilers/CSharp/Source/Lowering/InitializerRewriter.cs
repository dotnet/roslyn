// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal static BoundStatementList Rewrite(ImmutableArray<BoundInitializer> boundInitializers, MethodSymbol constructor)
        {
            Debug.Assert(!boundInitializers.IsDefault);

            var boundStatements = new BoundStatement[boundInitializers.Length];
            for (int i = 0; i < boundStatements.Length; i++)
            {
                var init = boundInitializers[i];
                var syntax = init.Syntax;
                switch (init.Kind)
                {
                    case BoundKind.FieldInitializer:
                        var fieldInit = (BoundFieldInitializer)init;

                        var boundReceiver = fieldInit.Field.IsStatic ? null :
                            new BoundThisReference(syntax, fieldInit.Field.ContainingType);

                        // Mark this as CompilerGenerated so that the local rewriter doesn't add a sequence point.
                        boundStatements[i] =
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
                        Debug.Assert(syntax.Parent.Kind == SyntaxKind.EqualsValueClause);
                        Debug.Assert(syntax.Parent.Parent.Kind == SyntaxKind.VariableDeclarator);
                        Debug.Assert(syntax.Parent.Parent.Parent.Kind == SyntaxKind.VariableDeclaration);

                        var declaratorSyntax = (VariableDeclaratorSyntax)syntax.Parent.Parent;
                        boundStatements[i] = LocalRewriter.AddSequencePoint(declaratorSyntax, boundStatements[i]);
                        break;

                    case BoundKind.GlobalStatementInitializer:
                        var stmtInit = (BoundGlobalStatementInitializer)init;

                        // the value of the last expression statement (if any) is stored to a ref parameter of the submission constructor:
                        if (constructor.IsSubmissionConstructor && i == boundStatements.Length - 1 && stmtInit.Statement.Kind == BoundKind.ExpressionStatement)
                        {
                            var submissionResultVariable = new BoundParameter(syntax, constructor.Parameters[1]);
                            var expr = ((BoundExpressionStatement)stmtInit.Statement).Expression;

                            // The expression is converted to the submission result type when the initializer is bound, 
                            // so we just need to assign it to the out parameter:
                            if ((object)expr.Type != null && expr.Type.SpecialType != SpecialType.System_Void)
                            {
                                boundStatements[i] =
                                    new BoundExpressionStatement(syntax,
                                        new BoundAssignmentOperator(syntax,
                                            submissionResultVariable,
                                            expr,
                                            expr.Type
                                        )
                                    );

                                break;
                            }
                        }

                        boundStatements[i] = stmtInit.Statement;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(init.Kind);
                }
            }

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

            return new BoundStatementList(listSyntax, boundStatements.AsImmutableOrNull());
        }
    }
}
