// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
                SyntaxKind.IfStatement, SyntaxKind.WhileStatement);

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

            var ifOrWhileStatement = (StatementSyntax)node;
            var leftmostCondition = GetLeftmostCondition(ifOrWhileStatement);
            if (!leftmostCondition.IsKind(SyntaxKind.NotEqualsExpression, out BinaryExpressionSyntax comparison))
            {
                return;
            }

            var operand = GetNullCheckOperand(comparison.Left, comparison.Right)?.WalkDownParentheses();
            if (operand == null)
            {
                return;
            }

            // if/while has to be in a block so we can at least look for a preceding local variable declaration.
            if (!ifOrWhileStatement.Parent.IsKind(SyntaxKind.Block, out BlockSyntax parentBlock))
            {
                return;
            }

            var blockStatements = parentBlock.Statements;
            if (!TryGetTypeCheckParts(operand, ifOrWhileStatement, blockStatements,
                    out var declarator, out var asExpression))
            {
                return;
            }

            var semanticModel = syntaxContext.SemanticModel;
            var typeNode = ((BinaryExpressionSyntax)asExpression).Right;
            var asType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (asType.IsNullable())
            {
                // Not legal to write "x is int? y"
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
            if (IsAccessedBeforeAssignment(semanticModel, localSymbol,
                    declarationStatement, ifOrWhileStatement, blockStatements, cancellationToken))
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                declarationStatement.GetLocation(),
                ifOrWhileStatement.GetLocation(),
                leftmostCondition.GetLocation(),
                asExpression.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                GetDescriptorWithSeverity(styleOption.Notification.Value),
                declarationStatement.GetLocation(),
                additionalLocations));
        }

        private static bool IsAccessedBeforeAssignment(
            SemanticModel semanticModel,
            ISymbol localVariable,
            StatementSyntax declarationStatement,
            StatementSyntax usageStatement,
            SyntaxList<StatementSyntax> blockStatements,
            CancellationToken cancellationToken)
        {
            var isAssigned = false;
            var isAccessedBeforeAssignment = false;

            var usageIndex = blockStatements.IndexOf(usageStatement);
            var declarationIndex = blockStatements.IndexOf(declarationStatement);

            // Since we're going to remove this declaration-statement,
            // we need to first ensure that it's not used up to the target statement.
            for (var index = declarationIndex + 1; index < usageIndex; index++)
            {
                CheckDefiniteAssignment(
                    semanticModel, localVariable, blockStatements[index],
                    out isAssigned, out isAccessedBeforeAssignment,
                    cancellationToken);

                if (isAssigned || isAccessedBeforeAssignment)
                {
                    return true;
                }
            }

            // In case of an if-statement, we need to check if the variable
            // is being accessed before assignment in the else clause.
            if (usageStatement is IfStatementSyntax ifStatement)
            {
                CheckDefiniteAssignment(
                    semanticModel, localVariable, ifStatement.Else,
                    out isAssigned, out isAccessedBeforeAssignment,
                    cancellationToken);

                if (isAccessedBeforeAssignment)
                {
                    return true;
                }

                if (isAssigned)
                {
                    return false;
                }
            }

            // Make sure that no access is made to the variable before assignment in the subsequent statements
            for (int index = usageIndex + 1, n = blockStatements.Count; index < n; index++)
            {
                CheckDefiniteAssignment(
                    semanticModel, localVariable, blockStatements[index],
                    out isAssigned, out isAccessedBeforeAssignment,
                    cancellationToken);

                if (isAccessedBeforeAssignment)
                {
                    return true;
                }

                if (isAssigned)
                {
                    // The scope of pattern variables in a while-statement does not leak out to
                    // the enclosing block so we bail also if there is any assignments afterwards.
                    return usageStatement.Kind() == SyntaxKind.WhileStatement;
                }
            }

            return false;
        }

        private static void CheckDefiniteAssignment(
            SemanticModel semanticModel, ISymbol localVariable, SyntaxNode node,
            out bool isAssigned, out bool isAccessedBeforeAssignment,
            CancellationToken cancellationToken)
        {
            if (node != null)
            {
                foreach (var descendantNode in node.DescendantNodes())
                {
                    if(!descendantNode.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax id))
                    {
                        continue;
                    }

                    var symbol = semanticModel.GetSymbolInfo(id, cancellationToken).GetAnySymbol();
                    if (localVariable.Equals(symbol))
                    {
                        isAssigned = id.IsOnlyWrittenTo();
                        isAccessedBeforeAssignment = !isAssigned;
                        return;
                    }
                }
            }

            isAssigned = false;
            isAccessedBeforeAssignment = false;
        }

        private static bool TryGetTypeCheckParts(
            SyntaxNode operand,
            StatementSyntax usageStatement,
            SyntaxList<StatementSyntax> blockStatements,
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
                case SyntaxKind.IdentifierName when usageStatement.Kind() != SyntaxKind.WhileStatement:
                {
                    // var x = e as T;
                    // if (x != null) F(x);
                    var identifier = (IdentifierNameSyntax)operand;
                    var declarator = TryFindVariableDeclarator(identifier, usageStatement, blockStatements);
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
                    var declarator = TryFindVariableDeclarator(identifier, usageStatement, blockStatements);
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
            IdentifierNameSyntax identifier, StatementSyntax usageStatement, SyntaxList<StatementSyntax> blockStatements)
        {
            var usageIndex = blockStatements.IndexOf(usageStatement);
            for (var index = usageIndex - 1; index >= 0; index--)
            {
                if (!blockStatements[index].IsKind(SyntaxKind.LocalDeclarationStatement,
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
                switch (node.Kind())
                {
                    case SyntaxKind.WhileStatement:
                        node = ((WhileStatementSyntax)node).Condition;
                        continue;
                    case SyntaxKind.IfStatement:
                        node = ((IfStatementSyntax)node).Condition;
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
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}
