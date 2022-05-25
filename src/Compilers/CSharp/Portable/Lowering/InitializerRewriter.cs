// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class InitializerRewriter
    {
        internal static BoundTypeOrInstanceInitializers RewriteConstructor(ImmutableArray<BoundInitializer> boundInitializers, MethodSymbol method)
        {
            Debug.Assert(!boundInitializers.IsDefault);
            Debug.Assert((method.MethodKind == MethodKind.Constructor) || (method.MethodKind == MethodKind.StaticConstructor));

            var sourceMethod = method as SourceMemberMethodSymbol;
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
                        !expression.Type.IsVoidType())
                    {
                        trailingExpression = expression;
                        continue;
                    }
                }

                boundStatements.Add(RewriteInitializersAsStatements(initializer));
            }

            if (hasSubmissionResultType && (trailingExpression != null))
            {
                Debug.Assert(!submissionResultType.IsVoidType());

                // Note: The trailing expression was already converted to the submission result type in Binder.BindGlobalStatement.
                boundStatements.Add(new BoundReturnStatement(lastStatement.Syntax, RefKind.None, trailingExpression, @checked: false));
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

        private static BoundStatement RewriteFieldInitializer(BoundFieldEqualsValue fieldInit)
        {
            SyntaxNode syntax = fieldInit.Syntax;
            syntax = (syntax as EqualsValueClauseSyntax)?.Value ?? syntax; //we want the attached sequence point to indicate the value node
            var boundReceiver = fieldInit.Field.IsStatic ? null :
                                        new BoundThisReference(syntax, fieldInit.Field.ContainingType);

            BoundStatement boundStatement =
                new BoundExpressionStatement(syntax,
                    new BoundAssignmentOperator(syntax,
                        new BoundFieldAccess(syntax,
                            boundReceiver,
                            fieldInit.Field,
                            constantValueOpt: null),
                        fieldInit.Value,
                        fieldInit.Field.Type)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = !fieldInit.Locals.IsEmpty || fieldInit.WasCompilerGenerated };

            if (!fieldInit.Locals.IsEmpty)
            {
                boundStatement = new BoundBlock(syntax, fieldInit.Locals, ImmutableArray.Create(boundStatement)) { WasCompilerGenerated = fieldInit.WasCompilerGenerated };
            }

            Debug.Assert(LocalRewriter.IsFieldOrPropertyInitializer(boundStatement));
            return boundStatement;
        }

        private static BoundStatement RewriteInitializersAsStatements(BoundInitializer initializer)
        {
            switch (initializer.Kind)
            {
                case BoundKind.FieldEqualsValue:
                    return RewriteFieldInitializer((BoundFieldEqualsValue)initializer);
                case BoundKind.GlobalStatementInitializer:
                    return ((BoundGlobalStatementInitializer)initializer).Statement;
                default:
                    throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
            }
        }
    }
}
