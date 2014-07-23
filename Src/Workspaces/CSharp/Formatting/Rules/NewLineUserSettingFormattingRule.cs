// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class NewLineUserSettingFormattingRule : BaseFormattingRule
    {
        private bool IsControlBlock(SyntaxNode node)
        {
            var parent = node != null ? node.Parent : null;

            return (node != null && node.CSharpKind() == SyntaxKind.SwitchStatement) ||
                   (parent != null &&
                   (parent.CSharpKind() == SyntaxKind.IfStatement || parent.CSharpKind() == SyntaxKind.ElseClause ||
                    parent.CSharpKind() == SyntaxKind.WhileStatement || parent.CSharpKind() == SyntaxKind.DoStatement ||
                    parent.CSharpKind() == SyntaxKind.ForEachStatement || parent.CSharpKind() == SyntaxKind.UsingStatement ||
                    parent.CSharpKind() == SyntaxKind.ForStatement || parent.CSharpKind() == SyntaxKind.TryStatement ||
                    parent.CSharpKind() == SyntaxKind.CatchClause || parent.CSharpKind() == SyntaxKind.FinallyClause ||
                    parent.CSharpKind() == SyntaxKind.LockStatement || parent.CSharpKind() == SyntaxKind.CheckedStatement ||
                    parent.CSharpKind() == SyntaxKind.UncheckedStatement));
        }

        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
        {
            var operation = nextOperation.Invoke();

            // } else in the if else context
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.IsKind(SyntaxKind.ElseKeyword))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLineForElse))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * catch in the try catch context
            if (currentToken.IsKind(SyntaxKind.CatchKeyword))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLineForCatch))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * finally
            if (currentToken.IsKind(SyntaxKind.FinallyKeyword))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLineForFinally))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { in the type declaration context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && (currentToken.Parent is BaseTypeDeclarationSyntax || currentToken.Parent is NamespaceDeclarationSyntax))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForTypes))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Anonymous object creation
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null && currentToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForAnonymousType))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Object Initialization
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null && currentToken.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForObjectInitializers))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null &&
               (currentTokenParentParent is MemberDeclarationSyntax || currentTokenParentParent is AccessorDeclarationSyntax))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForMethods))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForAnonymousMethods))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the Lambda context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null &&
               (currentTokenParentParent.IsKind(SyntaxKind.SimpleLambdaExpression) || currentTokenParentParent.IsKind(SyntaxKind.ParenthesizedLambdaExpression)))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForLambda))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the control statement context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && IsControlBlock(currentToken.Parent))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForControl))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            return operation;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            var operation = nextOperation.Invoke();

            // else condition is actually handled in the GetAdjustSpacesOperation()

            // For Object Initialization Expression
            if (previousToken.CSharpKind() == SyntaxKind.CommaToken && previousToken.Parent.CSharpKind() == SyntaxKind.ObjectInitializerExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLineForMembersInObjectInit))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    // we never force it to move up unless it is already on same line
                    return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                }
            }

            // For Anonymous Object Creation Expression
            if (previousToken.CSharpKind() == SyntaxKind.CommaToken && previousToken.Parent.CSharpKind() == SyntaxKind.AnonymousObjectCreationExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    // we never force it to move up unless it is already on same line
                    return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                }
            }

            // } else in the if else context
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.IsKind(SyntaxKind.ElseKeyword))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLineForElse))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * catch in the try catch context
            if (currentToken.CSharpKind() == SyntaxKind.CatchKeyword)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLineForCatch))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * Finally
            if (currentToken.CSharpKind() == SyntaxKind.FinallyKeyword)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLineForFinally))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the type declaration context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && (currentToken.Parent is BaseTypeDeclarationSyntax || currentToken.Parent is NamespaceDeclarationSyntax))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForTypes))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new { - Anonymous object creation
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && currentToken.Parent != null && currentToken.Parent.CSharpKind() == SyntaxKind.AnonymousObjectCreationExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForAnonymousType))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new { - Object Initialization
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && currentToken.Parent != null && currentToken.Parent.CSharpKind() == SyntaxKind.ObjectInitializerExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForObjectInitializers))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken &&
                currentTokenParentParent != null && (currentTokenParentParent is MemberDeclarationSyntax || currentTokenParentParent is AccessorDeclarationSyntax))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForMethods))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent.CSharpKind() == SyntaxKind.AnonymousMethodExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForAnonymousMethods))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the simple Lambda context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
               (currentTokenParentParent.CSharpKind() == SyntaxKind.SimpleLambdaExpression || currentTokenParentParent.CSharpKind() == SyntaxKind.ParenthesizedLambdaExpression))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForLambda))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the control statement context
            if (currentToken.CSharpKind() == SyntaxKind.OpenBraceToken && IsControlBlock(currentToken.Parent))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForControl))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // Wrapping - Leave statements on same line (false): 
            // Insert a newline between the previous statement and this one.
            // ; *
            if (previousToken.CSharpKind() == SyntaxKind.SemicolonToken
                && (previousToken.Parent is StatementSyntax && !previousToken.Parent.IsKind(SyntaxKind.ForStatement))
                && !optionSet.GetOption(CSharpFormattingOptions.LeaveStatementMethodDeclarationSameLine))
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            return operation;
        }
    }
}
