// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class InitializerRewriter
    {
        internal static BoundTypeOrInstanceInitializers RewriteConstructor(ImmutableArray<BoundInitializer> boundInitializers, MethodSymbol method)
        {
            Debug.Assert(!boundInitializers.IsDefault);
            Debug.Assert((method.MethodKind == MethodKind.Constructor) || (method.MethodKind == MethodKind.StaticConstructor));

            var sourceMethod = method as SourceMethodSymbol;
            var syntax = ((object)sourceMethod != null) ? sourceMethod.SyntaxNode : method.GetNonNullSyntaxNode();
            return new BoundTypeOrInstanceInitializers(syntax, boundInitializers.SelectAsArray(RewriteInitializersAsStatements));
        }

        internal static BoundTypeOrInstanceInitializers RewriteScriptInitializer(ImmutableArray<BoundInitializer> boundInitializers, SynthesizedInteractiveInitializerMethod method)
        {
            Debug.Assert(!boundInitializers.IsDefault);

            var boundStatements = ArrayBuilder<BoundStatement>.GetInstance(boundInitializers.Length);
            var submissionResultType = method.ResultType;
            BoundExpression submissionResult = null;

            foreach (var initializer in boundInitializers)
            {
                // The value of the last expression statement (if any) is returned from the submission initializer.
                if (((object)submissionResultType != null) &&
                    (initializer == boundInitializers.Last()) &&
                    (initializer.Kind == BoundKind.GlobalStatementInitializer))
                {
                    var expr = GetTrailingScriptExpression(((BoundGlobalStatementInitializer)initializer).Statement);
                    if (expr != null &&
                        (object)expr.Type != null &&
                        expr.Type.SpecialType != SpecialType.System_Void)
                    {
                        submissionResult = expr;
                        continue;
                    }
                }
                boundStatements.Add(RewriteInitializersAsStatements(initializer));
            }

            var syntax = method.GetNonNullSyntaxNode();
            if ((object)submissionResultType != null)
            {
                if (submissionResult == null)
                {
                    // Return default(T) if submission does not have a trailing expression.
                    submissionResult = new BoundDefaultOperator(syntax, submissionResultType);
                }
                Debug.Assert(submissionResult.Type.SpecialType != SpecialType.System_Void);

                // The expression is converted to the submission result type when the initializer is bound.
                boundStatements.Add(new BoundReturnStatement(submissionResult.Syntax, submissionResult));
            }

            return new BoundTypeOrInstanceInitializers(syntax, boundStatements.ToImmutableAndFree());
        }

        /// <summary>
        /// Returns the expression if the statement is actually an
        /// expression (ExpressionStatementSyntax with no trailing semicolon).
        /// </summary>
        internal static BoundExpression GetTrailingScriptExpression(BoundStatement statement)
        {
            return (statement.Kind == BoundKind.ExpressionStatement) && ((ExpressionStatementSyntax)statement.Syntax).SemicolonToken.IsMissing ?
                ((BoundExpressionStatement)statement).Expression :
                null;
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
                        fieldInit.Field.Type.TypeSymbol)
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

        private static BoundStatement RewriteInitializersAsStatements(BoundInitializer initializer)
        {
            switch (initializer.Kind)
            {
                case BoundKind.FieldInitializer:
                    return RewriteFieldInitializer((BoundFieldInitializer)initializer);
                case BoundKind.GlobalStatementInitializer:
                    return ((BoundGlobalStatementInitializer)initializer).Statement;
                default:
                    throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
            }
        }
    }
}
