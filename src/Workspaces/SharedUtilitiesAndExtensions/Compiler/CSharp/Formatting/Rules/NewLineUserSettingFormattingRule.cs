// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class NewLineUserSettingFormattingRule : BaseFormattingRule
    {
        private readonly CSharpSyntaxFormattingOptions _options;

        public NewLineUserSettingFormattingRule()
            : this(CSharpSyntaxFormattingOptions.Default)
        {
        }

        private NewLineUserSettingFormattingRule(CSharpSyntaxFormattingOptions options)
        {
            _options = options;
        }

        public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
        {
            var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

            if (_options.NewLines == newOptions.NewLines &&
                _options.WrappingKeepStatementsOnSingleLine == newOptions.WrappingKeepStatementsOnSingleLine)
            {
                return this;
            }

            return new NewLineUserSettingFormattingRule(newOptions);
        }

        private static bool IsControlBlock(SyntaxNode node)
        {
            RoslynDebug.Assert(node != null);

            if (node.IsKind(SyntaxKind.SwitchStatement))
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

        public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            RoslynDebug.AssertNotNull(currentToken.Parent);

            var operation = nextOperation.Invoke(in previousToken, in currentToken);

            // } else in the if else context
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken)
                && currentToken.IsKind(SyntaxKind.ElseKeyword)
                && previousToken.Parent!.Parent == currentToken.Parent.Parent)
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeElse))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * catch in the try catch context
            if (currentToken.IsKind(SyntaxKind.CatchKeyword))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeCatch))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * finally
            if (currentToken.IsKind(SyntaxKind.FinallyKeyword))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeFinally))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { in the type declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent is BaseTypeDeclarationSyntax or NamespaceDeclarationSyntax)
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Anonymous object creation
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Object Initialization, or with { - Record with initializer, or is { - property pattern clauses
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken))
            {
                if (currentToken.Parent.Kind() is SyntaxKind.ObjectInitializerExpression
                    or SyntaxKind.CollectionInitializerExpression
                    or SyntaxKind.ArrayInitializerExpression
                    or SyntaxKind.ImplicitArrayCreationExpression
                    or SyntaxKind.WithInitializerExpression)
                {
                    if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers))
                    {
                        operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                    }
                }
                else if (currentToken.Parent.IsKind(SyntaxKind.PropertyPatternClause))
                {
                    if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers))
                    {
                        // Allow property patterns in switch expressions to start on their own line:
                        //
                        // var x = y switch {
                        //    { Value: true } => false,    ⬅️ This line starts with an open brace
                        //    _ => true,
                        // };
                        var isFirstTokenOfSwitchArm = currentToken.Parent.IsParentKind(SyntaxKind.RecursivePattern, out RecursivePatternSyntax? recursivePattern)
                            && recursivePattern.IsParentKind(SyntaxKind.SwitchExpressionArm, out SwitchExpressionArmSyntax? switchExpressionArm)
                            && switchExpressionArm.GetFirstToken() == currentToken;

                        var spacesOption = isFirstTokenOfSwitchArm
                            ? AdjustSpacesOption.ForceSpacesIfOnSingleLine
                            : AdjustSpacesOption.ForceSpaces;
                        operation = CreateAdjustSpacesOperation(1, spacesOption);
                    }
                }
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent is MemberDeclarationSyntax)
            {
                var option = currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? _options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties)
                    : _options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods);

                if (!option)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent is AccessorDeclarationSyntax)
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the local function context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the Lambda context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) &&
               currentTokenParentParent is (kind: SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the switch expression context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.SwitchExpression))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the control statement context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && IsControlBlock(currentToken.Parent))
            {
                if (!_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks))
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            return operation;
        }

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            RoslynDebug.AssertNotNull(currentToken.Parent);

            var operation = nextOperation.Invoke(in previousToken, in currentToken);

            // else condition is actually handled in the GetAdjustSpacesOperation()

            // For Object Initialization Expression
            if (previousToken.IsKind(SyntaxKind.CommaToken) && previousToken.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers))
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
            if (previousToken.IsKind(SyntaxKind.CommaToken) && previousToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes))
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
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeElse)
                    || previousToken.Parent!.Parent != currentToken.Parent.Parent)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * catch in the try catch context
            if (currentToken.IsKind(SyntaxKind.CatchKeyword))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeCatch))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * Finally
            if (currentToken.IsKind(SyntaxKind.FinallyKeyword))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeFinally))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the type declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent is BaseTypeDeclarationSyntax or NamespaceDeclarationSyntax)
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new { - Anonymous object creation
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes))
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
            // with { - Record with initializer
            // is { - property pattern clauses
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) &&
                currentToken.Parent.Kind() is SyntaxKind.ObjectInitializerExpression or SyntaxKind.CollectionInitializerExpression or SyntaxKind.WithInitializerExpression or SyntaxKind.PropertyPatternClause)
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers))
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
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) &&
                currentToken.Parent.Kind() is SyntaxKind.ArrayInitializerExpression or SyntaxKind.ImplicitArrayCreationExpression)
            {
                return null;
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent is MemberDeclarationSyntax)
            {
                var option = currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? _options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties)
                    : _options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods);

                if (option)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the property accessor context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent is AccessorDeclarationSyntax)
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the local function context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the simple Lambda context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) &&
               currentTokenParentParent is (kind: SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the switch expression context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.SwitchExpression))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the control statement context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && IsControlBlock(currentToken.Parent))
            {
                if (_options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks))
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
            if (previousToken.IsKind(SyntaxKind.SemicolonToken)
                && (previousToken.Parent is StatementSyntax && !previousToken.Parent.IsKind(SyntaxKind.ForStatement))
                && !_options.WrappingKeepStatementsOnSingleLine)
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            return operation;
        }
    }
}
