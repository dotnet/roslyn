// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class IndentBlockFormattingRule : BaseFormattingRule
{
    internal const string Name = "CSharp IndentBlock Formatting Rule";

    private readonly CSharpSyntaxFormattingOptions _options;

    public IndentBlockFormattingRule()
        : this(CSharpSyntaxFormattingOptions.Default)
    {
    }

    private IndentBlockFormattingRule(CSharpSyntaxFormattingOptions options)
    {
        _options = options;
    }

    public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
    {
        var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

        if (_options.LabelPositioning == newOptions.LabelPositioning &&
            _options.Indentation == newOptions.Indentation &&
            _options.WrapCallChains == newOptions.WrapCallChains &&
            _options.IndentWrappedCallChains == newOptions.IndentWrappedCallChains)
        {
            return this;
        }

        return new IndentBlockFormattingRule(newOptions);
    }

    public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
    {
        nextOperation.Invoke();

        AddAlignmentBlockOperation(list, node);

        AddBlockIndentationOperation(list, node);

        AddBracketIndentationOperation(list, node);

        AddLabelIndentationOperation(list, node);

        AddSwitchIndentationOperation(list, node);

        AddEmbeddedStatementsIndentationOperation(list, node);

        AddCaseSectionIndentBlockOperation(list, node);

        AddCallChainAlignmentOperation(list, node);

        AddParameterAlignmentOperation(list, node);

        AddTypeParameterConstraintClauseOperation(list, node);
    }

    private static void AddTypeParameterConstraintClauseOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        if (node is TypeParameterConstraintClauseSyntax { Parent: { } declaringNode })
        {
            var baseToken = declaringNode.GetFirstToken();
            AddIndentBlockOperation(list, baseToken, node.GetFirstToken(), node.GetLastToken());
        }
    }

    private void AddSwitchIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        if (node is not SwitchSectionSyntax section)
        {
            return;
        }

        // can this ever happen?
        if (section.Labels.Count == 0 &&
            section.Statements.Count == 0)
        {
            return;
        }

        if (!_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents) && !_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock))
        {
            // Never indent
            return;
        }

        var alwaysIndent = _options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents) && _options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock);
        if (!alwaysIndent)
        {
            // Only one of these values can be true at this point.
            Debug.Assert(_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents) != _options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock));

            var firstStatementIsBlock = section.Statements is [(kind: SyntaxKind.Block), ..];
            if (_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock) != firstStatementIsBlock)
            {
                return;
            }
        }

        // see whether we are the last statement
        RoslynDebug.AssertNotNull(node.Parent);
        var switchStatement = (SwitchStatementSyntax)node.Parent;
        var lastSection = switchStatement.Sections.Last() == node;

        if (section.Statements is not ([var firstStatement, ..] and [.., var lastStatement]))
        {
            // even if there is no statement under section, we still want indent operation
            var lastTokenOfLabel = section.Labels.Last().GetLastToken(includeZeroWidth: true);
            var nextToken = lastTokenOfLabel.GetNextToken(includeZeroWidth: true);

            AddIndentBlockOperation(list, lastTokenOfLabel, lastTokenOfLabel,
                lastSection ?
                    TextSpan.FromBounds(lastTokenOfLabel.FullSpan.End, nextToken.SpanStart) : TextSpan.FromBounds(lastTokenOfLabel.FullSpan.End, lastTokenOfLabel.FullSpan.End));
            return;
        }

        var startToken = firstStatement.GetFirstToken(includeZeroWidth: true);
        var endToken = lastStatement.GetLastToken(includeZeroWidth: true);

        // see whether we are the last statement
        var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);
        span = lastSection ? span : TextSpan.FromBounds(span.Start, endToken.FullSpan.End);

        AddIndentBlockOperation(list, startToken, endToken, span);
    }

    private void AddLabelIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // label statement
        if (node is LabeledStatementSyntax labeledStatement)
        {
            if (_options.LabelPositioning == LabelPositionOptionsInternal.OneLess)
            {
                AddUnindentBlockOperation(list, labeledStatement.Identifier, labeledStatement.ColonToken);
            }
            else if (_options.LabelPositioning == LabelPositionOptionsInternal.LeftMost)
            {
                AddAbsoluteZeroIndentBlockOperation(list, labeledStatement.Identifier, labeledStatement.ColonToken);
            }
        }
    }

    private static void AddAlignmentBlockOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        switch (node)
        {
            case SimpleLambdaExpressionSyntax simpleLambda:
                SetAlignmentBlockOperation(list, simpleLambda, simpleLambda.Body);
                return;
            case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                SetAlignmentBlockOperation(list, parenthesizedLambda, parenthesizedLambda.Body);
                return;
            case AnonymousMethodExpressionSyntax anonymousMethod:
                SetAlignmentBlockOperation(list, anonymousMethod, anonymousMethod.Block);
                return;
            case BaseObjectCreationExpressionSyntax objectCreation when objectCreation.Initializer != null:
                SetAlignmentBlockOperation(list, objectCreation, objectCreation.Initializer);
                return;
            case AnonymousObjectCreationExpressionSyntax anonymousObjectCreation:
                SetAlignmentBlockOperation(list, anonymousObjectCreation.NewKeyword, anonymousObjectCreation.OpenBraceToken, anonymousObjectCreation.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case ArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, arrayCreation.NewKeyword, arrayCreation.Initializer.OpenBraceToken, arrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case ImplicitArrayCreationExpressionSyntax implicitArrayCreation when implicitArrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, implicitArrayCreation.NewKeyword, implicitArrayCreation.Initializer.OpenBraceToken, implicitArrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case StackAllocArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, arrayCreation.StackAllocKeyword, arrayCreation.Initializer.OpenBraceToken, arrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation when implicitArrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, implicitArrayCreation.StackAllocKeyword, implicitArrayCreation.Initializer.OpenBraceToken, implicitArrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case SwitchExpressionSyntax switchExpression:
                SetAlignmentBlockOperation(list, switchExpression.GetFirstToken(), switchExpression.OpenBraceToken, switchExpression.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case WithExpressionSyntax withExpression:
                SetAlignmentBlockOperation(list, withExpression.GetFirstToken(), withExpression.Initializer.OpenBraceToken, withExpression.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case PropertyPatternClauseSyntax propertyPatternClause:
                if (propertyPatternClause.Parent is RecursivePatternSyntax { Parent: { } recursivePatternParent })
                {
                    var baseTokenForAlignment = recursivePatternParent.GetFirstToken();
                    if (baseTokenForAlignment == propertyPatternClause.OpenBraceToken)
                    {
                        // It only makes sense to set the alignment for the '{' when it's on a separate line from
                        // the base token for alignment. This is never the case when they are the same token.
                        return;
                    }

                    SetAlignmentBlockOperation(list, baseTokenForAlignment, propertyPatternClause.OpenBraceToken, propertyPatternClause.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine | IndentBlockOption.IndentIfConditionOfAnchorToken);
                }

                return;
        }
    }

    private static void SetAlignmentBlockOperation(List<IndentBlockOperation> list, SyntaxNode baseNode, SyntaxNode body)
    {
        var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;

        var baseToken = baseNode.GetFirstToken(includeZeroWidth: true);
        var firstToken = body.GetFirstToken(includeZeroWidth: true);
        var lastToken = body.GetLastToken(includeZeroWidth: true);

        SetAlignmentBlockOperation(list, baseToken, firstToken, lastToken, option);
    }

    private void AddBlockIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        var bracePair = node.GetBracePair();

        // don't put block indentation operation if the block only contains label statement
        if (!bracePair.IsValidBracketOrBracePair())
        {
            return;
        }

        // for lambda, set alignment around braces so that users can put brace wherever they want
        if (node.IsLambdaBodyBlock() || node.IsAnonymousMethodBlock() || node.Kind() is SyntaxKind.PropertyPatternClause or SyntaxKind.SwitchExpression)
        {
            AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, bracePair);
        }

        // For ArrayInitializationExpression, set indent to relative to the open brace so the content is properly indented
        if (node.IsKind(SyntaxKind.ArrayInitializerExpression) && node.Parent != null && node.Parent.IsKind(SyntaxKind.ArrayCreationExpression))
        {
            AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, bracePair);
        }

        if (node is BlockSyntax && !_options.Indentation.HasFlag(IndentationPlacement.BlockContents))
        {
            // do not add indent operation for block
            return;
        }

        if (node is SwitchStatementSyntax && !_options.Indentation.HasFlag(IndentationPlacement.SwitchSection))
        {
            // do not add indent operation for switch statement
            return;
        }

        AddIndentBlockOperation(list, bracePair.openBrace.GetNextToken(includeZeroWidth: true), bracePair.closeBrace.GetPreviousToken(includeZeroWidth: true));
    }

    private static void AddBracketIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // Indentation inside the pattern of a switch statement is handled by AddBlockIndentationOperation. This continue ensures that bracket-specific
        // operations are skipped for switch patterns, as they are not formatted like blocks.
        if (node.Parent is SwitchExpressionArmSyntax arm && arm.Pattern == node)
        {
            return;
        }

        var bracketPair = node.GetBracketPair();

        if (!bracketPair.IsValidBracketOrBracePair())
            return;

        if (node.Parent != null && node.Kind() is SyntaxKind.ListPattern or SyntaxKind.CollectionExpression)
        {
            AddIndentBlockOperation(list, bracketPair.openBracket.GetNextToken(includeZeroWidth: true), bracketPair.closeBracket.GetPreviousToken(includeZeroWidth: true));

            // If we have:
            //
            // return Goo([ //<-- determining indentation here.
            //
            // Then we want to compute the indentation relative to the construct that the collection expression is
            // attached to.  So ask to be relative to the start of the line the prior token is on if we're on the
            // same line as it.
            var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;

            var firstToken = node.GetFirstToken(includeZeroWidth: true);
            var lastToken = node.GetLastToken(includeZeroWidth: true);
            var baseToken = firstToken.GetPreviousToken(includeZeroWidth: true);

            SetAlignmentBlockOperation(list, baseToken, firstToken, lastToken, option);
        }
    }

    private static void AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(List<IndentBlockOperation> list, (SyntaxToken openBrace, SyntaxToken closeBrace) bracePair)
    {
        var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;
        SetAlignmentBlockOperation(list, bracePair.openBrace, bracePair.openBrace.GetNextToken(includeZeroWidth: true), bracePair.closeBrace, option);
    }

    private static void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // increase indentation - embedded statement cases
        if (node is IfStatementSyntax ifStatement && ifStatement.Statement != null && !(ifStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, ifStatement.Statement);
            return;
        }

        if (node is ElseClauseSyntax elseClause && elseClause.Statement != null)
        {
            if (elseClause.Statement is not (BlockSyntax or IfStatementSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, elseClause.Statement);
            }

            return;
        }

        if (node is WhileStatementSyntax whileStatement && whileStatement.Statement != null && !(whileStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, whileStatement.Statement);
            return;
        }

        if (node is ForStatementSyntax forStatement && forStatement.Statement != null && !(forStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, forStatement.Statement);
            return;
        }

        if (node is CommonForEachStatementSyntax foreachStatement && foreachStatement.Statement != null && !(foreachStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, foreachStatement.Statement);
            return;
        }

        if (node is UsingStatementSyntax usingStatement && usingStatement.Statement != null && !(usingStatement.Statement is BlockSyntax or UsingStatementSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, usingStatement.Statement);
            return;
        }

        if (node is FixedStatementSyntax fixedStatement && fixedStatement.Statement != null && !(fixedStatement.Statement is BlockSyntax or FixedStatementSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, fixedStatement.Statement);
            return;
        }

        if (node is DoStatementSyntax doStatement && doStatement.Statement != null && !(doStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, doStatement.Statement);
            return;
        }

        if (node is LockStatementSyntax lockStatement && lockStatement.Statement != null && !(lockStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, lockStatement.Statement);
            return;
        }
    }

    private static void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, StatementSyntax statement)
    {
        var firstToken = statement.GetFirstToken(includeZeroWidth: true);
        var lastToken = statement.GetLastToken(includeZeroWidth: true);

        if (lastToken.IsMissing)
        {
            // embedded statement is not done, consider following as part of embedded statement
            AddIndentBlockOperation(list, firstToken, lastToken);
        }
        else
        {
            // embedded statement is done
            AddIndentBlockOperation(list, firstToken, lastToken, TextSpan.FromBounds(firstToken.FullSpan.Start, lastToken.FullSpan.End));
        }
    }

    private void AddCaseSectionIndentBlockOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        if (node is SwitchSectionSyntax section)
        {
            var firstStatement = section.Statements.FirstOrDefault();
            if (firstStatement != null && firstStatement is BlockSyntax)
            {
                var block = (BlockSyntax)firstStatement;
                AddIndentBlockOperation(list, block.OpenBraceToken, block.CloseBraceToken);
            }
        }
    }

    private void AddCallChainAlignmentOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // Only process member access expressions that are part of method call chains
        if (node is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Check if this member access is part of a method call chain
        if (!IsPartOfCallChain(memberAccess))
        {
            return;
        }

        // Only add indentation operations if both wrapping and indentation are enabled
        if (!_options.WrapCallChains || !_options.IndentWrappedCallChains)
        {
            return;
        }

        // Add indentation for the method call chain
        AddIndentBlockOperationForCallChain(list, memberAccess);
    }

    private static bool IsPartOfCallChain(MemberAccessExpressionSyntax memberAccess)
    {
        // Check if the left side is an invocation or another member access
        return memberAccess.Expression is InvocationExpressionSyntax or MemberAccessExpressionSyntax;
    }

    private static void AddIndentBlockOperationForCallChain(List<IndentBlockOperation> list, MemberAccessExpressionSyntax memberAccess)
    {
        // Get the dot token and the right side of the expression
        var dotToken = memberAccess.OperatorToken;
        var rightSide = memberAccess.Name;
        
        // Find the base expression for the method call chain
        var baseExpression = GetBaseExpressionForCallChain(memberAccess);
        var baseToken = baseExpression.GetFirstToken(includeZeroWidth: true);
        
        var startToken = dotToken;
        var endToken = rightSide.GetLastToken(includeZeroWidth: true);
        
        // Check if the base expression is a simple identifier
        // If it's a simple identifier (like 'y'), use regular indentation
        // If it's a complex expression (like 'log.Entries'), use alignment
        if (baseExpression is IdentifierNameSyntax)
        {
            // Simple identifier case: indent by one level
            AddIndentBlockOperation(list, baseToken, startToken, endToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
        }
        else
        {
            // Complex expression case: align to the dot position
            SetAlignmentBlockOperation(list, baseToken, startToken, endToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
        }
    }

    private static ExpressionSyntax GetBaseExpressionForCallChain(MemberAccessExpressionSyntax memberAccess)
    {
        // Find the topmost expression in the chain
        var current = memberAccess;
        while (current.Expression is MemberAccessExpressionSyntax parentMemberAccess)
        {
            current = parentMemberAccess;
        }
        
        // Return the base expression (left side of the topmost member access)
        return current.Expression;
    }

    private void AddParameterAlignmentOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // Only process if parameter wrapping is enabled
        if (!_options.WrapParameters)
        {
            return;
        }

        // Process parameter lists in method declarations
        if (node is ParameterListSyntax parameterList)
        {
            AddParameterListAlignmentOperation(list, parameterList);
            return;
        }

        // Process argument lists in method calls
        if (node is ArgumentListSyntax argumentList)
        {
            AddArgumentListAlignmentOperation(list, argumentList);
            return;
        }

        // Process bracket parameter lists (indexers, attributes)
        if (node is BracketedParameterListSyntax bracketedParameterList)
        {
            AddBracketedParameterListAlignmentOperation(list, bracketedParameterList);
            return;
        }

        // Process bracketed argument lists (indexers, attributes)
        if (node is BracketedArgumentListSyntax bracketedArgumentList)
        {
            AddBracketedArgumentListAlignmentOperation(list, bracketedArgumentList);
            return;
        }
    }

    private void AddParameterListAlignmentOperation(List<IndentBlockOperation> list, ParameterListSyntax parameterList)
    {
        // Only process if there are multiple parameters
        if (parameterList.Parameters.Count <= 1)
        {
            return;
        }

        var openParen = parameterList.OpenParenToken;
        var closeParen = parameterList.CloseParenToken;

        if (_options.WrapParametersOnNewLine)
        {
            // First parameter on new line case
            var firstParameter = parameterList.Parameters[0];
            var lastParameter = parameterList.Parameters[parameterList.Parameters.Count - 1];
            
            if (_options.AlignWrappedParameters)
            {
                // Align all parameters with the opening parenthesis
                AddIndentBlockOperation(list, openParen, firstParameter.GetFirstToken(), lastParameter.GetLastToken(), 
                    IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
            }
            else
            {
                // Just indent one level from the method declaration
                AddIndentBlockOperation(list, openParen, firstParameter.GetFirstToken(), lastParameter.GetLastToken());
            }
        }
        else if (_options.AlignWrappedParameters)
        {
            // Align parameters with the first parameter
            if (parameterList.Parameters.Count > 1)
            {
                var firstParameter = parameterList.Parameters[0];
                var lastParameter = parameterList.Parameters[parameterList.Parameters.Count - 1];
                
                // Align remaining parameters with the first parameter
                for (int i = 1; i < parameterList.Parameters.Count; i++)
                {
                    var parameter = parameterList.Parameters[i];
                    AddIndentBlockOperation(list, firstParameter.GetFirstToken(), parameter.GetFirstToken(), parameter.GetLastToken(),
                        IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                }
            }
        }
        else
        {
            // Basic wrapping - just indent one level
            for (int i = 1; i < parameterList.Parameters.Count; i++)
            {
                var parameter = parameterList.Parameters[i];
                AddIndentBlockOperation(list, openParen, parameter.GetFirstToken(), parameter.GetLastToken());
            }
        }
    }

    private void AddArgumentListAlignmentOperation(List<IndentBlockOperation> list, ArgumentListSyntax argumentList)
    {
        // Only process if there are multiple arguments
        if (argumentList.Arguments.Count <= 1)
        {
            return;
        }

        var openParen = argumentList.OpenParenToken;
        var closeParen = argumentList.CloseParenToken;

        if (_options.WrapParametersOnNewLine)
        {
            // First argument on new line case
            var firstArgument = argumentList.Arguments[0];
            var lastArgument = argumentList.Arguments[argumentList.Arguments.Count - 1];
            
            if (_options.AlignWrappedParameters)
            {
                // Align all arguments with the opening parenthesis
                AddIndentBlockOperation(list, openParen, firstArgument.GetFirstToken(), lastArgument.GetLastToken(), 
                    IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
            }
            else
            {
                // Just indent one level from the method call
                AddIndentBlockOperation(list, openParen, firstArgument.GetFirstToken(), lastArgument.GetLastToken());
            }
        }
        else if (_options.AlignWrappedParameters)
        {
            // Align arguments with the first argument
            if (argumentList.Arguments.Count > 1)
            {
                var firstArgument = argumentList.Arguments[0];
                var lastArgument = argumentList.Arguments[argumentList.Arguments.Count - 1];
                
                // Align remaining arguments with the first argument
                for (int i = 1; i < argumentList.Arguments.Count; i++)
                {
                    var argument = argumentList.Arguments[i];
                    AddIndentBlockOperation(list, firstArgument.GetFirstToken(), argument.GetFirstToken(), argument.GetLastToken(),
                        IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                }
            }
        }
        else
        {
            // Basic wrapping - just indent one level
            for (int i = 1; i < argumentList.Arguments.Count; i++)
            {
                var argument = argumentList.Arguments[i];
                AddIndentBlockOperation(list, openParen, argument.GetFirstToken(), argument.GetLastToken());
            }
        }
    }

    private void AddBracketedParameterListAlignmentOperation(List<IndentBlockOperation> list, BracketedParameterListSyntax bracketedParameterList)
    {
        // Only process if there are multiple parameters
        if (bracketedParameterList.Parameters.Count <= 1)
        {
            return;
        }

        var openBracket = bracketedParameterList.OpenBracketToken;
        var closeBracket = bracketedParameterList.CloseBracketToken;

        if (_options.WrapParametersOnNewLine)
        {
            // First parameter on new line case
            var firstParameter = bracketedParameterList.Parameters[0];
            var lastParameter = bracketedParameterList.Parameters[bracketedParameterList.Parameters.Count - 1];
            
            if (_options.AlignWrappedParameters)
            {
                // Align all parameters with the opening bracket
                AddIndentBlockOperation(list, openBracket, firstParameter.GetFirstToken(), lastParameter.GetLastToken(), 
                    IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
            }
            else
            {
                // Just indent one level from the declaration
                AddIndentBlockOperation(list, openBracket, firstParameter.GetFirstToken(), lastParameter.GetLastToken());
            }
        }
        else if (_options.AlignWrappedParameters)
        {
            // Align parameters with the first parameter
            if (bracketedParameterList.Parameters.Count > 1)
            {
                var firstParameter = bracketedParameterList.Parameters[0];
                var lastParameter = bracketedParameterList.Parameters[bracketedParameterList.Parameters.Count - 1];
                
                // Align remaining parameters with the first parameter
                for (int i = 1; i < bracketedParameterList.Parameters.Count; i++)
                {
                    var parameter = bracketedParameterList.Parameters[i];
                    AddIndentBlockOperation(list, firstParameter.GetFirstToken(), parameter.GetFirstToken(), parameter.GetLastToken(),
                        IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                }
            }
        }
        else
        {
            // Basic wrapping - just indent one level
            for (int i = 1; i < bracketedParameterList.Parameters.Count; i++)
            {
                var parameter = bracketedParameterList.Parameters[i];
                AddIndentBlockOperation(list, openBracket, parameter.GetFirstToken(), parameter.GetLastToken());
            }
        }
    }

    private void AddBracketedArgumentListAlignmentOperation(List<IndentBlockOperation> list, BracketedArgumentListSyntax bracketedArgumentList)
    {
        // Only process if there are multiple arguments
        if (bracketedArgumentList.Arguments.Count <= 1)
        {
            return;
        }

        var openBracket = bracketedArgumentList.OpenBracketToken;
        var closeBracket = bracketedArgumentList.CloseBracketToken;

        if (_options.WrapParametersOnNewLine)
        {
            // First argument on new line case
            var firstArgument = bracketedArgumentList.Arguments[0];
            var lastArgument = bracketedArgumentList.Arguments[bracketedArgumentList.Arguments.Count - 1];
            
            if (_options.AlignWrappedParameters)
            {
                // Align all arguments with the opening bracket
                AddIndentBlockOperation(list, openBracket, firstArgument.GetFirstToken(), lastArgument.GetLastToken(), 
                    IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
            }
            else
            {
                // Just indent one level from the call
                AddIndentBlockOperation(list, openBracket, firstArgument.GetFirstToken(), lastArgument.GetLastToken());
            }
        }
        else if (_options.AlignWrappedParameters)
        {
            // Align arguments with the first argument
            if (bracketedArgumentList.Arguments.Count > 1)
            {
                var firstArgument = bracketedArgumentList.Arguments[0];
                var lastArgument = bracketedArgumentList.Arguments[bracketedArgumentList.Arguments.Count - 1];
                
                // Align remaining arguments with the first argument
                for (int i = 1; i < bracketedArgumentList.Arguments.Count; i++)
                {
                    var argument = bracketedArgumentList.Arguments[i];
                    AddIndentBlockOperation(list, firstArgument.GetFirstToken(), argument.GetFirstToken(), argument.GetLastToken(),
                        IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                }
            }
        }
        else
        {
            // Basic wrapping - just indent one level
            for (int i = 1; i < bracketedArgumentList.Arguments.Count; i++)
            {
                var argument = bracketedArgumentList.Arguments[i];
                AddIndentBlockOperation(list, openBracket, argument.GetFirstToken(), argument.GetLastToken());
            }
        }
    }
}
