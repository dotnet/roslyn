// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class NewLineUserSettingFormattingRule : BaseFormattingRule
    {
        private bool IsControlBlock(SyntaxNode node)
        {
            Debug.Assert(node != null);

            if (node.Kind() == SyntaxKind.SwitchStatement)
            {
                return true;
            }

            var parentKind = node.Parent?.Kind();

            switch (parentKind.GetValueOrDefault())
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.ElseClause:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.TryStatement:
                case SyntaxKind.CatchClause:
                case SyntaxKind.FinallyClause:
                case SyntaxKind.LockStatement:
                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                case SyntaxKind.SwitchSection:
                case SyntaxKind.FixedStatement:
                case SyntaxKind.UnsafeStatement:
                    return true;
                default:
                    return false;
            }
        }

        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
        {
            var operation = nextOperation.Invoke();

            // } else in the if else context
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken)
                && currentToken.IsKind(SyntaxKind.ElseKeyword)
                && previousToken.Parent.Parent == currentToken.Parent.Parent)
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
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && (currentToken.Parent is BaseTypeDeclarationSyntax || currentToken.Parent is NamespaceDeclarationSyntax))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInTypes))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Anonymous object creation
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null && currentToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Object Initialization
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null && currentToken.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent is MemberDeclarationSyntax _)
            {
                var option = currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? CSharpFormattingOptions.NewLinesForBracesInProperties
                    : CSharpFormattingOptions.NewLinesForBracesInMethods;

                if (!optionSet.GetOption(option))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent is AccessorDeclarationSyntax)
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAccessors))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the local function context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInMethods))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the Lambda context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null &&
               (currentTokenParentParent.IsKind(SyntaxKind.SimpleLambdaExpression) || currentTokenParentParent.IsKind(SyntaxKind.ParenthesizedLambdaExpression)))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the control statement context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && IsControlBlock(currentToken.Parent))
            {
                if (!optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            return operation;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            var operation = nextOperation.Invoke();

            // else condition is actually handled in the GetAdjustSpacesOperation()

            // For Object Initialization Expression
            if (previousToken.Kind() == SyntaxKind.CommaToken && previousToken.Parent.Kind() == SyntaxKind.ObjectInitializerExpression)
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
            if (previousToken.Kind() == SyntaxKind.CommaToken && previousToken.Parent.Kind() == SyntaxKind.AnonymousObjectCreationExpression)
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
                if (optionSet.GetOption(CSharpFormattingOptions.NewLineForElse)
                    || previousToken.Parent.Parent != currentToken.Parent.Parent)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * catch in the try catch context
            if (currentToken.Kind() == SyntaxKind.CatchKeyword)
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
            if (currentToken.Kind() == SyntaxKind.FinallyKeyword)
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
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && (currentToken.Parent is BaseTypeDeclarationSyntax || currentToken.Parent is NamespaceDeclarationSyntax))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInTypes))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new { - Anonymous object creation
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentToken.Parent != null && currentToken.Parent.Kind() == SyntaxKind.AnonymousObjectCreationExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new MyObject { - Object Initialization
            // new List<int> { - Collection Initialization
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentToken.Parent != null &&
                (currentToken.Parent.Kind() == SyntaxKind.ObjectInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.CollectionInitializerExpression))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // Array Initialization Expression
            // int[] arr = new int[] {
            //             new[] {
            //             { - Implicit Array
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null &&
                (currentToken.Parent.Kind() == SyntaxKind.ArrayInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.ImplicitArrayCreationExpression))
            {
                return null;
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent is MemberDeclarationSyntax)
            {
                var option = currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? CSharpFormattingOptions.NewLinesForBracesInProperties
                    : CSharpFormattingOptions.NewLinesForBracesInMethods;

                if (optionSet.GetOption(option))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the property accessor context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent is AccessorDeclarationSyntax)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAccessors))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent.Kind() == SyntaxKind.AnonymousMethodExpression)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the local function context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent.Kind() == SyntaxKind.LocalFunctionStatement)
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInMethods))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the simple Lambda context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
               (currentTokenParentParent.Kind() == SyntaxKind.SimpleLambdaExpression || currentTokenParentParent.Kind() == SyntaxKind.ParenthesizedLambdaExpression))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the control statement context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && IsControlBlock(currentToken.Parent))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks))
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
            if (previousToken.Kind() == SyntaxKind.SemicolonToken
                && (previousToken.Parent is StatementSyntax && !previousToken.Parent.IsKind(SyntaxKind.ForStatement))
                && !optionSet.GetOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine))
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            return operation;
        }
    }
}
