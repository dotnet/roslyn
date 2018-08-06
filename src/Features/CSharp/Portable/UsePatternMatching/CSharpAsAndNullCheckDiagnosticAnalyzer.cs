// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the forms:
    /// 
    ///     var x = o as Type;
    ///     if (x != null) ...
    ///     
    ///     T x;
    ///     if/while ((x = e as T) != null)
    /// 
    /// and converts it to:
    /// 
    ///     if/while (o is Type x) ...
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpAsAndNullCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public override bool OpenFileOnly(Workspace workspace) => false;

        public CSharpAsAndNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineAsTypeCheckId,
                    new LocalizableResourceString(
                        nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction,
                SyntaxKind.IfStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.LocalDeclarationStatement);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var node = syntaxContext.Node;
            var syntaxTree = node.SyntaxTree;

            // "x is Type y" is only available in C# 7.0 and above. Don't offer this refactoring
            // in projects targeting a lesser version.
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            var options = syntaxContext.Options;
            var cancellationToken = syntaxContext.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var targetStatement = (StatementSyntax)node;
            var leftmostCondition = GetLeftmostCondition(targetStatement);
            if (leftmostCondition == null)
            {
                return;
            }

            bool isNegativeNullCheck;
            switch (leftmostCondition.Kind())
            {
                case SyntaxKind.NotEqualsExpression:
                    isNegativeNullCheck = false;
                    break;
                case SyntaxKind.EqualsExpression:
                    isNegativeNullCheck = true;
                    break;
                default:
                    return;
            }

            // The pattern variable is only definitely assigned if the pattern succeeded,
            // so in the following cases it would not be safe to use pattern-matching.
            // For example:
            //
            //      if ((x = o as string) == null && SomeExpression)
            //      if ((x = o as string) != null || SomeExpression)
            //
            // x would never be definitely assigned if pattern matching were used
            switch ((leftmostCondition.Parent as ExpressionSyntax)?.WalkUpParentheses().Kind())
            {
                case SyntaxKind.LogicalAndExpression when isNegativeNullCheck:
                case SyntaxKind.LogicalOrExpression when !isNegativeNullCheck:
                    return;
            }

            var comparison = (BinaryExpressionSyntax)leftmostCondition;
            var operand = GetNullCheckOperand(comparison.Left, comparison.Right)?.WalkDownParentheses();
            if (operand == null)
            {
                return;
            }

            var semanticModel = syntaxContext.SemanticModel;
            if (operand.IsKind(SyntaxKind.CastExpression, out CastExpressionSyntax castExpression))
            {
                // Unwrap object cast
                var castType = semanticModel.GetTypeInfo(castExpression.Type).Type;
                if (castType.IsObjectType())
                {
                    operand = castExpression.Expression;
                }
            }

            // if/while has to be in a block so we can at least look for a preceding local variable declaration.
            if (!targetStatement.Parent.IsKind(SyntaxKind.Block, out BlockSyntax parentBlock))
            {
                return;
            }

            if (!TryGetTypeCheckParts(operand, targetStatement, parentBlock,
                    out var declarator, out var asExpression))
            {
                return;
            }

            if (semanticModel.GetSymbolInfo(comparison).GetAnySymbol().IsUserDefinedOperator())
            {
                return;
            }

            var typeNode = ((BinaryExpressionSyntax)asExpression).Right;
            var asType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (asType.IsNullable())
            {
                // Not legal to write "x is int? y"
                return;
            }

            if (asType?.TypeKind == TypeKind.Dynamic)
            {
                // Not legal to use dynamic in a pattern.
                return;
            }

            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);
            if (!localSymbol.Type.Equals(asType))
            {
                // We have something like:
                //
                //      BaseType b = x as DerivedType;
                //      if (b != null) { ... }
                //
                // It's not necessarily safe to convert this to:
                //
                //      if (x is DerivedType b) { ... }
                //
                // That's because there may be later code that wants to do something like assign a
                // 'BaseType' into 'b'.  As we've now claimed that it must be DerivedType, that
                // won't work.  This might also cause unintended changes like changing overload
                // resolution.  So, we conservatively do not offer the change in a situation like this.
                return;
            }

            var declarationStatement = (StatementSyntax)declarator.Parent.Parent;

            // If we convert this to 'if (o is Type x)' then 'x' will not be definitely assigned
            // in the Else branch of the IfStatement, or after the IfStatement. Make sure
            // that doesn't cause definite assignment issues.
            if (!CheckDefiniteAssignment(semanticModel, localSymbol,
                    declarationStatement, targetStatement, parentBlock, isNegativeNullCheck))
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                declarationStatement.GetLocation(),
                targetStatement.GetLocation(),
                leftmostCondition.GetLocation(),
                asExpression.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                declarationStatement.GetLocation(),
                styleOption.Notification.Severity,
                additionalLocations,
                properties: null));
        }

        // Ensure that all usages of the pattern variable are
        // in scope and definitely assigned after replacement.
        private static bool CheckDefiniteAssignment(
            SemanticModel semanticModel,
            ISymbol localVariable,
            StatementSyntax declarationStatement,
            StatementSyntax targetStatement,
            BlockSyntax parentBlock,
            bool isNegativeNullCheck)
        {
            var statements = parentBlock.Statements;
            var targetIndex = statements.IndexOf(targetStatement);
            var declarationIndex = statements.IndexOf(declarationStatement);

            // Since we're going to remove this declaration-statement,
            // we need to first ensure that it's not used up to the target statement.
            if (declarationIndex + 1 < targetIndex)
            {
                var dataFlow = semanticModel.AnalyzeDataFlow(
                    statements[declarationIndex + 1], 
                    statements[targetIndex - 1]);

                if (dataFlow.ReadInside.Contains(localVariable) || 
                    dataFlow.WrittenInside.Contains(localVariable))
                {
                    return false;
                }
            }

            // In case of an if-statement, we need to check if the variable
            // is being accessed before assignment in the opposite branch.
            if (targetStatement.IsKind(SyntaxKind.IfStatement, out IfStatementSyntax ifStatement))
            {
                var statement = isNegativeNullCheck ? ifStatement.Statement : ifStatement.Else?.Statement;
                if (statement != null)
                {
                    var dataFlow = semanticModel.AnalyzeDataFlow(statement);
                    if (dataFlow.DataFlowsIn.Contains(localVariable))
                    {
                        // Access before assignment is not safe in the opposite branch
                        // as the variable is not definitely assgined at this point.
                        // For example:
                        //
                        //    if (o is string x) { }
                        //    else { Use(x); }
                        //
                        return false;
                    }
                    
                    if (dataFlow.AlwaysAssigned.Contains(localVariable))
                    {
                        // If the variable is always assigned here, we don't need to check
                        // subsequent statements as it's definitely assigned afterwards.
                        // For example:
                        //
                        //     if (o is string x) { }
                        //     else { x = null; }
                        //
                        return true;
                    }
                }
            }

            // Make sure that no access is made to the variable before assignment in the subsequent statements
            if (targetIndex + 1 < statements.Count)
            {
                var dataFlow = semanticModel.AnalyzeDataFlow(
                    statements[targetIndex + 1],
                    statements[statements.Count - 1]);

                if (targetStatement.Kind() == SyntaxKind.WhileStatement)
                {
                    // The scope of pattern variables in a while-statement does not leak out to
                    // the enclosing block so we bail also if there is any assignments afterwards.
                    if (dataFlow.ReadInside.Contains(localVariable) ||
                        dataFlow.WrittenInside.Contains(localVariable))
                    {
                        return false;
                    }
                }
                else if (dataFlow.DataFlowsIn.Contains(localVariable))
                {
                    // Access before assignment here is only valid if we have a negative
                    // pattern-matching in an if-statement with an unreachable endpoint.
                    // For example:
                    //
                    //      if (!(o is string x)) {
                    //        return;
                    //      }
                    //
                    //      // The 'return' statement above ensures x is definitely assigned here
                    //      Console.WriteLine(x);
                    //
                    return isNegativeNullCheck &&
                       ifStatement != null &&
                       !semanticModel.AnalyzeControlFlow(ifStatement.Statement).EndPointIsReachable;
                }
            }

            return true;
        }

        private static bool TryGetTypeCheckParts(
            SyntaxNode operand,
            StatementSyntax targetStatement,
            BlockSyntax parentBlock,
            out SyntaxNode variableDeclarator,
            out SyntaxNode asExpression)
        {
            switch (operand.Kind())
            {
                // We have something like:
                //
                //      var x = e as T;
                //      while (b != null) { ... }
                //
                // It's not necessarily safe to convert this to:
                //
                //      while (x is T b) { ... }
                //
                // That's because in this case, unlike the original code, we're type-checking in every iteration
                // so we do not replace simple null check with the "is" operator if it's in a while loop
                case SyntaxKind.IdentifierName when targetStatement.Kind() != SyntaxKind.WhileStatement:
                {
                    // var x = e as T;
                    // if (x != null) F(x);
                    var identifier = (IdentifierNameSyntax)operand;
                    var declarator = TryFindVariableDeclarator(identifier, targetStatement, parentBlock);
                    var initializerValue = declarator?.Initializer?.Value;
                    if (!initializerValue.IsKind(SyntaxKind.AsExpression))
                    {
                        break;
                    }

                    variableDeclarator = declarator;
                    asExpression = initializerValue;
                    return true;
                }

                case SyntaxKind.SimpleAssignmentExpression:
                {
                    // T x;
                    // if ((x = e as T) != null) F(x);
                    var assignment = (AssignmentExpressionSyntax)operand;
                    if (!assignment.Right.IsKind(SyntaxKind.AsExpression) ||
                        !assignment.Left.IsKind(SyntaxKind.IdentifierName))
                    {
                        break;
                    }

                    var identifier = (IdentifierNameSyntax)assignment.Left;
                    var declarator = TryFindVariableDeclarator(identifier, targetStatement, parentBlock);
                    if (declarator == null)
                    {
                        break;
                    }

                    variableDeclarator = declarator;
                    asExpression = assignment.Right;
                    return true;
                }
            }

            variableDeclarator = null;
            asExpression = null;
            return false;
        }

        private static VariableDeclaratorSyntax TryFindVariableDeclarator(
            IdentifierNameSyntax identifier, StatementSyntax targetStatement, BlockSyntax parentBlock)
        {
            var statement = parentBlock.Statements;
            var targetIndex = statement.IndexOf(targetStatement);
            for (var index = targetIndex - 1; index >= 0; index--)
            {
                if (!statement[index].IsKind(SyntaxKind.LocalDeclarationStatement,
                        out LocalDeclarationStatementSyntax declarationStatement))
                {
                    continue;
                }

                var declarators = declarationStatement.Declaration.Variables;
                var declarator = declarators.FirstOrDefault(d => d.Identifier.ValueText == identifier.Identifier.ValueText);
                if (declarator != null)
                {
                    // We require this to be the only declarator in the declaration statement
                    // to simplify definitive assignment check and the code fix for now
                    return declarators.Count == 1 ? declarator : null;
                }
            }

            return null;
        }

        private static ExpressionSyntax GetNullCheckOperand(ExpressionSyntax left, ExpressionSyntax right)
        {
            if (left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return right;
            }

            if (right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return left;
            }

            return null;
        }

        private static SyntaxNode GetLeftmostCondition(SyntaxNode node)
        {
            while (true)
            {
                if (node == null)
                {
                    return null;
                }

                switch (node.Kind())
                {
                    case SyntaxKind.WhileStatement:
                        node = ((WhileStatementSyntax)node).Condition;
                        continue;
                    case SyntaxKind.IfStatement:
                        node = ((IfStatementSyntax)node).Condition;
                        continue;
                    case SyntaxKind.ReturnStatement:
                        node = ((ReturnStatementSyntax)node).Expression;
                        continue;
                    case SyntaxKind.LocalDeclarationStatement:
                        var declarators = ((LocalDeclarationStatementSyntax)node).Declaration.Variables;
                        // We require this to be the only declarator in the declaration statement
                        // to simplify definitive assignment check and the code fix for now
                        node = declarators.Count == 1 ? declarators[0].Initializer?.Value : null;
                        continue;
                    case SyntaxKind.ParenthesizedExpression:
                        node = ((ParenthesizedExpressionSyntax)node).Expression;
                        continue;
                    case SyntaxKind.ConditionalExpression:
                        node = ((ConditionalExpressionSyntax)node).Condition;
                        continue;
                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.LogicalOrExpression:
                        node = ((BinaryExpressionSyntax)node).Left;
                        continue;
                }

                return node;
            }
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
