// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion;

[Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
internal class CurlyBraceCompletionService : AbstractCurlyBraceOrBracketCompletionService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CurlyBraceCompletionService()
    {
    }

    protected override char OpeningBrace => CurlyBrace.OpenCharacter;

    protected override char ClosingBrace => CurlyBrace.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
        => AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken);

    public override bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
    {
        // Only potentially valid for curly brace completion if not in an interpolation brace completion context.
        if (OpeningBrace == brace && InterpolationBraceCompletionService.IsPositionInInterpolationContext(document, openingPosition))
        {
            return false;
        }

        return base.CanProvideBraceCompletion(brace, openingPosition, document, cancellationToken);
    }

    protected override bool IsValidOpeningBraceToken(SyntaxToken token)
        => token.IsKind(SyntaxKind.OpenBraceToken) && !token.Parent.IsKind(SyntaxKind.Interpolation);

    protected override bool IsValidClosingBraceToken(SyntaxToken token)
        => token.IsKind(SyntaxKind.CloseBraceToken);

    protected override int AdjustFormattingEndPoint(ParsedDocument document, int startPoint, int endPoint)
    {
        // Only format outside of the completed braces if they're on the same line for array/collection/object initializer expressions.
        // Example:   `var x = new int[]{}`:
        // Correct:   `var x = new int[] {}`
        // Incorrect: `var x = new int[] { }`
        // This is a heuristic to prevent brace completion from breaking user expectation/muscle memory in common scenarios.
        // see bug Devdiv:823958
        if (document.Text.Lines.GetLineFromPosition(startPoint) == document.Text.Lines.GetLineFromPosition(endPoint))
        {
            var startToken = document.Root.FindToken(startPoint, findInsideTrivia: true);
            if (IsValidOpeningBraceToken(startToken) &&
                (startToken.Parent?.IsInitializerForArrayOrCollectionCreationExpression() == true ||
                 startToken.Parent is AnonymousObjectCreationExpressionSyntax))
            {
                // Since the braces are next to each other the span to format is everything up to the opening brace start.
                endPoint = startToken.SpanStart;
            }
        }

        return endPoint;
    }

    protected override ImmutableArray<AbstractFormattingRule> GetBraceFormattingIndentationRulesAfterReturn(IndentationOptions options)
    {
        var indentStyle = options.IndentStyle;
        return [BraceCompletionFormattingRule.ForIndentStyle(indentStyle)];
    }

    private sealed class BraceCompletionFormattingRule : BaseFormattingRule
    {
        private static readonly Predicate<SuppressOperation> s_predicate = o => o.Option.IsOn(SuppressOption.NoWrapping);

        private static readonly ImmutableArray<BraceCompletionFormattingRule> s_instances =
        [
            new BraceCompletionFormattingRule(FormattingOptions2.IndentStyle.None),
            new BraceCompletionFormattingRule(FormattingOptions2.IndentStyle.Block),
            new BraceCompletionFormattingRule(FormattingOptions2.IndentStyle.Smart),
        ];

        private readonly FormattingOptions2.IndentStyle _indentStyle;
        private readonly CSharpSyntaxFormattingOptions _options;

        public BraceCompletionFormattingRule(FormattingOptions2.IndentStyle indentStyle)
            : this(indentStyle, CSharpSyntaxFormattingOptions.Default)
        {
        }

        private BraceCompletionFormattingRule(FormattingOptions2.IndentStyle indentStyle, CSharpSyntaxFormattingOptions options)
        {
            _indentStyle = indentStyle;
            _options = options;
        }

        public static AbstractFormattingRule ForIndentStyle(FormattingOptions2.IndentStyle indentStyle)
        {
            Debug.Assert(s_instances[(int)indentStyle]._indentStyle == indentStyle);
            return s_instances[(int)indentStyle];
        }

        public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
        {
            var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;
            if (_options.NewLines == newOptions.NewLines)
            {
                return this;
            }

            return new BraceCompletionFormattingRule(_indentStyle, newOptions);
        }

        private static bool? NeedsNewLine(in SyntaxToken currentToken, CSharpSyntaxFormattingOptions options)
        {
            if (!currentToken.IsKind(SyntaxKind.OpenBraceToken))
            {
                return null;
            }

            // If we're inside any of the following expressions check if the option for
            // braces on new lines in object / array initializers is set before we attempt
            // to move the open brace location to a new line.
            // new MyObject {
            // new List<int> {
            // int[] arr = {
            //           = new[] {
            //           = new int[] {
            if (currentToken.Parent is (kind:
                    SyntaxKind.ObjectInitializerExpression or
                    SyntaxKind.CollectionInitializerExpression or
                    SyntaxKind.ArrayInitializerExpression or
                    SyntaxKind.ImplicitArrayCreationExpression or
                    SyntaxKind.WithInitializerExpression or
                    SyntaxKind.PropertyPatternClause))
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers);
            }

            var currentTokenParentParent = currentToken.Parent?.Parent;

            // * { - in the property accessor context
            if (currentTokenParentParent is AccessorDeclarationSyntax)
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors);
            }

            // * { - in the anonymous Method context
            if (currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods);
            }

            // new { - Anonymous object creation
            if (currentToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes);
            }

            // * { - in the control statement context
            if (IsControlBlock(currentToken.Parent))
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks);
            }

            // * { - in the simple Lambda context
            if (currentTokenParentParent is (kind: SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression))
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody);
            }

            // * { - in the member declaration context
            if (currentTokenParentParent is MemberDeclarationSyntax)
            {
                return currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties)
                    : options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods);
            }

            // * { - in the type declaration context
            if (currentToken.Parent is BaseTypeDeclarationSyntax or NamespaceDeclarationSyntax)
            {
                return options.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes);
            }

            return null;
        }

        private static bool IsControlBlock(SyntaxNode? node)
        {
            if (node.IsKind(SyntaxKind.SwitchStatement))
            {
                return true;
            }

            var parentKind = node?.Parent?.Kind();

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

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            var needsNewLine = NeedsNewLine(currentToken, _options);
            return needsNewLine switch
            {
                true => CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines),
                false => null,
                _ => base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation),
            };
        }

        public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, in NextAlignTokensOperationAction nextOperation)
        {
            base.AddAlignTokensOperations(list, node, in nextOperation);
            if (_indentStyle == FormattingOptions2.IndentStyle.Block)
            {
                var bracePair = node.GetBracePair();
                if (bracePair.IsValidBracketOrBracePair())
                {
                    // If the user has set block style indentation and we're in a valid brace pair
                    // then make sure we align the close brace to the open brace.
                    AddAlignIndentationOfTokensToBaseTokenOperation(list, node, bracePair.openBrace,
                        SpecializedCollections.SingletonEnumerable(bracePair.closeBrace), AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine);
                }
            }
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
        {
            base.AddSuppressOperations(list, node, in nextOperation);

            // not sure exactly what is happening here, but removing the bellow causesthe indentation to be wrong.

            // remove suppression rules for array and collection initializer
            if (node.IsInitializerForArrayOrCollectionCreationExpression())
            {
                // remove any suppression operation
                list.RemoveAll(s_predicate);
            }
        }
    }
}
