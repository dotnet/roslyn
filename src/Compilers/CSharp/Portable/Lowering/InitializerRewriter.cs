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

        internal static BoundTypeOrInstanceInitializers RewriteScriptInitializer(ImmutableArray<BoundInitializer> boundInitializers, SynthesizedInteractiveInitializerMethod method, out bool hasTrailingExpression)
        {
            Debug.Assert(!boundInitializers.IsDefault);

            var boundStatements = ArrayBuilder<BoundStatement>.GetInstance(boundInitializers.Length);
            var submissionResultType = method.ResultType;
            var hasSubmissionResultType = (object)submissionResultType != null;
            BoundStatement lastStatement = null;
            BoundExpression trailingExpression = null;

            foreach (var initializer in boundInitializers)
            {
                // The value of the last expression statement (if any) is returned from the submission initializer,
                // unless this is a #load'ed tree.  I the #load'ed tree case, we'll execute the trailing expression
                // but discard its result.
                if (hasSubmissionResultType &&
                    (initializer == boundInitializers.Last()) &&
                    (initializer.Kind == BoundKind.GlobalStatementInitializer) &&
                    method.DeclaringCompilation.IsSubmissionSyntaxTree(initializer.SyntaxTree))
                {
                    lastStatement = ((BoundGlobalStatementInitializer)initializer).Statement;
                    var expression = GetTrailingScriptExpression(lastStatement);
                    if (expression != null &&
                        (object)expression.Type != null &&
                        expression.Type.SpecialType != SpecialType.System_Void)
                    {
                        trailingExpression = expression;
                        continue;
                    }
                }

                boundStatements.Add(RewriteInitializersAsStatements(initializer));
            }

            if (hasSubmissionResultType && (trailingExpression != null))
            {
                Debug.Assert(submissionResultType.SpecialType != SpecialType.System_Void);

                // Note: The trailing expression was already converted to the submission result type in Binder.BindGlobalStatement.
                boundStatements.Add(new BoundReturnStatement(lastStatement.Syntax, RefKind.None, trailingExpression));
                hasTrailingExpression = true;
            }
            else
            {
                hasTrailingExpression = false;
            }

            return new BoundTypeOrInstanceInitializers(method.GetNonNullSyntaxNode(), boundStatements.ToImmutableAndFree());
        }

        /// <summary>
        /// Returns the expression if the statement is actually an expression (ExpressionStatementSyntax with no trailing semicolon).
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
