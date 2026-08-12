// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConvertToConditional
{
    internal abstract class ConditionalAnalyzer
    {
        protected readonly IfStatementSyntax IfStatement;
        protected readonly SemanticModel SemanticModel;

        protected ConditionalAnalyzer(IfStatementSyntax ifStatement, SemanticModel semanticModel)
        {
            IfStatement = ifStatement;
            SemanticModel = semanticModel;
        }

        private bool ConversionExists(ExpressionSyntax whenTrue, ExpressionSyntax whenFalse)
        {
            TypeInfo whenTrueInfo = SemanticModel.GetTypeInfo(whenTrue);
            TypeInfo whenFalseInfo = SemanticModel.GetTypeInfo(whenFalse);

            return whenTrueInfo.Type != null
                && whenFalseInfo.Type != null
                && SemanticModel.ClassifyConversion(whenFalse, whenTrueInfo.Type).Exists
                && SemanticModel.ClassifyConversion(whenTrue, whenFalseInfo.Type).Exists;
        }

        protected abstract ExpressionSyntax CreateConditional();

        protected ExpressionSyntax CreateConditional(ExpressionSyntax whenTrue, ExpressionSyntax whenFalse, ITypeSymbol targetType)
        {
            Debug.Assert(whenTrue != null);
            Debug.Assert(whenTrue.FirstAncestorOrSelf<CompilationUnitSyntax>() == SemanticModel.SyntaxTree.GetRoot());
            Debug.Assert(whenFalse != null);
            Debug.Assert(whenFalse.FirstAncestorOrSelf<CompilationUnitSyntax>() == SemanticModel.SyntaxTree.GetRoot());
            Debug.Assert(targetType != null);

            // If there is no conversion between when-true and when-false, we need to insert a cast in
            // one or both of the branches.
            if (!ConversionExists(whenTrue, whenFalse))
            {
                Conversion whenTrueConversion = SemanticModel.ClassifyConversion(whenTrue, targetType);
                Conversion whenFalseConversion = SemanticModel.ClassifyConversion(whenFalse, targetType);

                if (whenTrueConversion.IsExplicit)
                {
                    whenTrue = whenTrue.CastTo(targetType);
                }
                else if (whenFalseConversion.IsExplicit)
                {
                    whenFalse = whenFalse.CastTo(targetType);
                }
                else if (whenTrueConversion.IsImplicit && whenFalseConversion.IsImplicit)
                {
                    whenTrue = whenTrue.CastTo(targetType);
                }
            }

            ExpressionSyntax condition = IfStatement.Condition.Kind() == SyntaxKind.SimpleAssignmentExpression
                ? IfStatement.Condition.Parenthesize()
                : IfStatement.Condition;

            ExpressionSyntax result = SyntaxFactory.ConditionalExpression(condition, whenTrue, whenFalse);

            // Ensure that the conditional is implicitly convertible to the target type; otherwise,
            // insert a cast. We do this be speculatively determining the conversion classification
            // of the conditional expression to the target type in the same scope as the original
            // if-statement.
            Conversion conversion = SemanticModel.ClassifyConversion(IfStatement.Span.Start, result, targetType);
            if (conversion.IsExplicit)
            {
                result = result.CastTo(targetType);
            }

            return result;
        }
    }
}
