// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal class ElasticTriviaFormattingRule : BaseFormattingRule
{
    internal const string Name = "CSharp Elastic trivia Formatting Rule";

    public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
    {
        nextOperation.Invoke();

        if (!node.ContainsAnnotations)
        {
            return;
        }

        AddPropertyDeclarationSuppressOperations(list, node);

        AddInitializerSuppressOperations(list, node);

        AddCollectionExpressionSuppressOperations(list, node);
    }

    private static void AddPropertyDeclarationSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
    {
        if (node is BasePropertyDeclarationSyntax basePropertyDeclaration && basePropertyDeclaration.AccessorList != null &&
            basePropertyDeclaration.AccessorList.Accessors.All(a => a.Body == null) &&
            basePropertyDeclaration.GetAnnotatedTrivia(SyntaxAnnotation.ElasticAnnotation).Any())
        {
            var (firstToken, lastToken) = basePropertyDeclaration.GetFirstAndLastMemberDeclarationTokensAfterAttributes();

            list.Add(FormattingOperations.CreateSuppressOperation(firstToken, lastToken, SuppressOption.NoWrapping | SuppressOption.IgnoreElasticWrapping));
        }
    }

    private static void AddInitializerSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
    {
        var initializer = GetInitializerNode(node);
        var lastTokenOfType = GetLastTokenOfType(node);
        if (initializer != null && lastTokenOfType != null)
        {
            AddSuppressWrappingIfOnSingleLineOperation(list, lastTokenOfType.Value, initializer.CloseBraceToken, SuppressOption.IgnoreElasticWrapping);
            return;
        }

        if (node is AnonymousObjectCreationExpressionSyntax anonymousCreationNode)
        {
            AddSuppressWrappingIfOnSingleLineOperation(list, anonymousCreationNode.NewKeyword, anonymousCreationNode.CloseBraceToken, SuppressOption.IgnoreElasticWrapping);
            return;
        }
    }

    private static void AddCollectionExpressionSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
    {
        if (node is CollectionExpressionSyntax { OpenBracketToken.IsMissing: false, CloseBracketToken.IsMissing: false } collectionExpression)
        {
            AddSuppressWrappingIfOnSingleLineOperation(list, collectionExpression.OpenBracketToken, collectionExpression.CloseBracketToken, SuppressOption.IgnoreElasticWrapping);
            return;
        }
    }

    private static InitializerExpressionSyntax? GetInitializerNode(SyntaxNode node)
        => node switch
        {
            ObjectCreationExpressionSyntax objectCreationNode => objectCreationNode.Initializer,
            ArrayCreationExpressionSyntax arrayCreationNode => arrayCreationNode.Initializer,
            ImplicitArrayCreationExpressionSyntax implicitArrayNode => implicitArrayNode.Initializer,
            _ => null,
        };

    private static SyntaxToken? GetLastTokenOfType(SyntaxNode node)
    {
        if (node is ObjectCreationExpressionSyntax objectCreationNode)
        {
            return objectCreationNode.Type.GetLastToken();
        }

        if (node is ArrayCreationExpressionSyntax arrayCreationNode)
        {
            return arrayCreationNode.Type.GetLastToken();
        }

        if (node is ImplicitArrayCreationExpressionSyntax implicitArrayNode)
        {
            return implicitArrayNode.CloseBracketToken;
        }

        return null;
    }

    public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
    {
        var operation = nextOperation.Invoke(in previousToken, in currentToken);
        if (operation == null)
        {
            // If there are more than one Type Parameter Constraint Clause then each go in separate line
            if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) &&
                currentToken.IsKind(SyntaxKind.WhereKeyword) &&
                currentToken.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
            {
                RoslynDebug.AssertNotNull(previousToken.Parent);

                // Check if there is another TypeParameterConstraintClause before
                if (previousToken.Parent.Ancestors().OfType<TypeParameterConstraintClauseSyntax>().Any())
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }

                // Check if there is another TypeParameterConstraintClause after
                var firstTokenAfterTypeConstraint = currentToken.Parent.GetLastToken().GetNextToken();
                var lastTokenForTypeConstraint = currentToken.Parent.GetLastToken().GetNextToken();
                if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(lastTokenForTypeConstraint, firstTokenAfterTypeConstraint) &&
                    firstTokenAfterTypeConstraint.IsKind(SyntaxKind.WhereKeyword) &&
                    firstTokenAfterTypeConstraint.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
            }

            return null;
        }

        // Special case for formatting if-statements blocks on new lines
        if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) &&
            currentToken.IsKind(SyntaxKind.OpenBraceToken) &&
            currentToken.Parent.IsParentKind(SyntaxKind.IfStatement))
        {
            var num = LineBreaksAfter(previousToken, currentToken);

            return CreateAdjustNewLinesOperation(num, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
        }

        // if operation is already forced, return as it is.
        if (operation.Option == AdjustNewLinesOption.ForceLines)
            return operation;

        if (!CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken))
            return operation;

        var afterFileScopedNamespaceOperation = GetAdjustNewLinesOperationAfterFileScopedNamespace(previousToken, currentToken);
        if (afterFileScopedNamespaceOperation != null)
            return afterFileScopedNamespaceOperation;

        var betweenMemberOperation = GetAdjustNewLinesOperationBetweenMembers(previousToken, currentToken);
        if (betweenMemberOperation != null)
            return betweenMemberOperation;

        var line = Math.Max(LineBreaksAfter(previousToken, currentToken), operation.Line);
        if (line == 0)
        {
            return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
        }

        return CreateAdjustNewLinesOperation(line, AdjustNewLinesOption.ForceLines);
    }

    private static AdjustNewLinesOperation? GetAdjustNewLinesOperationAfterFileScopedNamespace(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        if (previousToken.Kind() != SyntaxKind.SemicolonToken)
            return null;

        if (currentToken.Kind() == SyntaxKind.EndOfFileToken)
            return null;

        if (currentToken.Kind() == SyntaxKind.CloseBraceToken)
            return null;

        if (previousToken.Parent is not FileScopedNamespaceDeclarationSyntax)
            return null;

        if (TryGetOperationBeforeDocComment(currentToken, out var operation))
            return operation;

        return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
    }

    private static AdjustNewLinesOperation? GetAdjustNewLinesOperationBetweenMembers(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        if (!FormattingRangeHelper.InBetweenTwoMembers(previousToken, currentToken))
        {
            return null;
        }

        var previousMember = FormattingRangeHelper.GetEnclosingMember(previousToken);
        var nextMember = FormattingRangeHelper.GetEnclosingMember(currentToken);
        if (previousMember == null || nextMember == null)
        {
            return null;
        }

        if (TryGetOperationBeforeDocComment(currentToken, out var operation))
            return operation;

        // If we have two members of the same kind, we won't insert a blank line if both members
        // have any content (e.g. accessors bodies, non-empty method bodies, etc.).
        if (previousMember.Kind() == nextMember.Kind())
        {
            // Easy cases:
            if (previousMember.Kind() is SyntaxKind.FieldDeclaration or
                SyntaxKind.EventFieldDeclaration)
            {
                // Ensure that fields and events are each declared on a separate line.
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
            }

            // Don't insert a blank line between properties, indexers or events with no accessors
            if (previousMember is BasePropertyDeclarationSyntax previousProperty)
            {
                var nextProperty = (BasePropertyDeclarationSyntax)nextMember;

                if (previousProperty?.AccessorList?.Accessors.All(a => a.Body == null) == true &&
                    nextProperty?.AccessorList?.Accessors.All(a => a.Body == null) == true)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
            }

            // Don't insert a blank line between methods with no bodies
            if (previousMember is BaseMethodDeclarationSyntax previousMethod)
            {
                var nextMethod = (BaseMethodDeclarationSyntax)nextMember;

                if (previousMethod.Body == null &&
                    nextMethod.Body == null)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
            }
        }

        return FormattingOperations.CreateAdjustNewLinesOperation(2 /* +1 for member itself and +1 for a blank line*/, AdjustNewLinesOption.ForceLines);
    }

    private static bool TryGetOperationBeforeDocComment(SyntaxToken currentToken, [NotNullWhen(true)] out AdjustNewLinesOperation? operation)
    {
        // see whether first non whitespace trivia after before the current member is a comment or not
        var triviaList = currentToken.LeadingTrivia;
        var firstNonWhitespaceTrivia = triviaList.FirstOrDefault(trivia => !IsWhitespace(trivia));
        if (!firstNonWhitespaceTrivia.IsRegularOrDocComment())
        {
            operation = null;
            return false;
        }

        // the first one is a comment, add two more lines than existing number of lines
        var numberOfLines = GetNumberOfLines(triviaList);
        var numberOfLinesBeforeComment = GetNumberOfLines(triviaList.Take(triviaList.IndexOf(firstNonWhitespaceTrivia)));
        var addedLines = numberOfLinesBeforeComment < 1 ? 2 : 1;
        operation = CreateAdjustNewLinesOperation(numberOfLines + addedLines, AdjustNewLinesOption.ForceLines);
        return true;
    }

    public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
    {
        var operation = nextOperation.Invoke(in previousToken, in currentToken);
        if (operation == null)
        {
            return null;
        }

        // if operation is already forced, return as it is.
        if (operation.Option == AdjustSpacesOption.ForceSpaces)
        {
            return operation;
        }

        if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken))
        {
            // current implementation of engine gives higher priority on new line operations over space operations if
            // two are conflicting.
            // ex) new line operation says add 1 line between tokens, and
            //     space operation says give 1 space between two tokens (basically means remove new lines)
            //     then, engine will pick new line operation and ignore space operation

            // make attributes have a space following
            if (previousToken.IsKind(SyntaxKind.CloseBracketToken) &&
                previousToken.Parent is AttributeListSyntax &&
                currentToken.Parent is not AttributeListSyntax)
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
            }

            // make every operation forced
            return CreateAdjustSpacesOperation(Math.Max(0, operation.Space), AdjustSpacesOption.ForceSpaces);
        }

        return operation;
    }

    // copied from compiler formatter to have same base forced format
    private static int LineBreaksAfter(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        if (currentToken.Kind() == SyntaxKind.None)
        {
            return 0;
        }

        switch (previousToken.Kind())
        {
            case SyntaxKind.None:
                return 0;

            case SyntaxKind.OpenBraceToken:
            case SyntaxKind.FinallyKeyword:
                return 1;

            case SyntaxKind.CloseBraceToken:
                return LineBreaksAfterCloseBrace(currentToken);

            case SyntaxKind.CloseParenToken:
                return (((previousToken.Parent is StatementSyntax) && currentToken.Parent != previousToken.Parent)
                    || currentToken.Kind() == SyntaxKind.OpenBraceToken) ? 1 : 0;

            case SyntaxKind.CloseBracketToken:
                // Assembly and module-level attributes followed by non-attributes should have a blank line after
                // them, unless it's the end of the file which will already have a blank line.
                if (previousToken.Parent is AttributeListSyntax parent)
                {
                    if (parent.Target != null &&
                        (parent.Target.Identifier.IsKindOrHasMatchingText(SyntaxKind.AssemblyKeyword) ||
                         parent.Target.Identifier.IsKindOrHasMatchingText(SyntaxKind.ModuleKeyword)))
                    {
                        if (!currentToken.IsKind(SyntaxKind.EndOfFileToken) && !(currentToken.Parent is AttributeListSyntax))
                        {
                            return 2;
                        }
                    }

                    if (previousToken.GetAncestor<ParameterSyntax>() == null
                        && previousToken.GetAncestor<TypeParameterSyntax>() == null)
                    {
                        return 1;
                    }
                }

                break;

            case SyntaxKind.SemicolonToken:
                return LineBreaksAfterSemicolon(previousToken, currentToken);

            case SyntaxKind.CommaToken:
                return previousToken.Parent is EnumDeclarationSyntax ? 1 : 0;

            case SyntaxKind.ElseKeyword:
                return currentToken.Kind() != SyntaxKind.IfKeyword ? 1 : 0;

            case SyntaxKind.ColonToken:
                if (previousToken.Parent is LabeledStatementSyntax or SwitchLabelSyntax)
                {
                    return 1;
                }

                break;
        }

        if ((currentToken.Kind() == SyntaxKind.FromKeyword && currentToken.Parent.IsKind(SyntaxKind.FromClause)) ||
            (currentToken.Kind() == SyntaxKind.LetKeyword && currentToken.Parent.IsKind(SyntaxKind.LetClause)) ||
            (currentToken.Kind() == SyntaxKind.WhereKeyword && currentToken.Parent.IsKind(SyntaxKind.WhereClause)) ||
            (currentToken.Kind() == SyntaxKind.JoinKeyword && currentToken.Parent.IsKind(SyntaxKind.JoinClause)) ||
            (currentToken.Kind() == SyntaxKind.JoinKeyword && currentToken.Parent.IsKind(SyntaxKind.JoinIntoClause)) ||
            (currentToken.Kind() == SyntaxKind.OrderByKeyword && currentToken.Parent.IsKind(SyntaxKind.OrderByClause)) ||
            (currentToken.Kind() == SyntaxKind.SelectKeyword && currentToken.Parent.IsKind(SyntaxKind.SelectClause)) ||
            (currentToken.Kind() == SyntaxKind.GroupKeyword && currentToken.Parent.IsKind(SyntaxKind.GroupClause)))
        {
            return 1;
        }

        switch (currentToken.Kind())
        {
            case SyntaxKind.OpenBraceToken:
            case SyntaxKind.CloseBraceToken:
            case SyntaxKind.ElseKeyword:
            case SyntaxKind.FinallyKeyword:
                return 1;

            case SyntaxKind.OpenBracketToken:
                // Assembly and module-level attributes preceded by non-attributes should have
                // a blank line separating them.
                if (currentToken.Parent is AttributeListSyntax parent)
                {
                    if (parent.Target != null &&
                        parent.Target.Identifier.Kind() is SyntaxKind.AssemblyKeyword or SyntaxKind.ModuleKeyword &&
                        previousToken.Parent is not AttributeListSyntax)
                    {
                        return 2;
                    }

                    // Attributes on parameters should have no lines between them.
                    if (parent.Parent is ParameterSyntax)
                    {
                        return 0;
                    }

                    return 1;
                }

                break;

            case SyntaxKind.WhereKeyword:
                return previousToken.Parent is TypeParameterListSyntax ? 1 : 0;
        }

        return 0;
    }

    private static int LineBreaksAfterCloseBrace(SyntaxToken nextToken)
    {
        if (nextToken.Kind() == SyntaxKind.CloseBraceToken)
        {
            return 1;
        }
        else if (
            nextToken.Kind() is SyntaxKind.CatchKeyword or
            SyntaxKind.FinallyKeyword or
            SyntaxKind.ElseKeyword)
        {
            return 1;
        }
        else if (
            nextToken.Kind() == SyntaxKind.WhileKeyword &&
            nextToken.Parent.IsKind(SyntaxKind.DoStatement))
        {
            return 1;
        }
        else if (nextToken.Kind() == SyntaxKind.EndOfFileToken)
        {
            return 0;
        }
        else
        {
            return 2;
        }
    }

    private static int LineBreaksAfterSemicolon(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        if (previousToken.Parent is ForStatementSyntax)
        {
            return 0;
        }
        else if (currentToken.Kind() == SyntaxKind.CloseBraceToken)
        {
            return 1;
        }
        else if (previousToken.Parent is UsingDirectiveSyntax)
        {
            return currentToken.Parent is UsingDirectiveSyntax ? 1 : 2;
        }
        else if (previousToken.Parent is ExternAliasDirectiveSyntax)
        {
            return currentToken.Parent is ExternAliasDirectiveSyntax ? 1 : 2;
        }
        else if (currentToken.Parent is LocalFunctionStatementSyntax)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }

    private static bool IsWhitespace(SyntaxTrivia trivia)
    {
        return trivia.Kind() is SyntaxKind.WhitespaceTrivia
            or SyntaxKind.EndOfLineTrivia;
    }

    private static int GetNumberOfLines(IEnumerable<SyntaxTrivia> triviaList)
        => triviaList.Sum(t => t.ToFullString().Replace("\r\n", "\r").Cast<char>().Count(c => SyntaxFacts.IsNewLine(c)));
}
