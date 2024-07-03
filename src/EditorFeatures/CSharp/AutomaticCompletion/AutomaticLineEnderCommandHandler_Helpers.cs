// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal partial class AutomaticLineEnderCommandHandler
{
    #region NodeReplacementHelpers

    private static (SyntaxNode newRoot, int nextCaretPosition) ReplaceStatementOwnerAndInsertStatement(
        SolutionServices services,
        SyntaxNode root,
        SyntaxNode oldNode,
        SyntaxNode newNode,
        SyntaxNode anchorNode,
        ImmutableArray<StatementSyntax> nodesToInsert,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        var rootEditor = new SyntaxEditor(root, services);

        // 1. Insert the node before anchor node
        rootEditor.InsertAfter(anchorNode, nodesToInsert);

        // 2. Replace the old node with newNode. (new node is the node with correct braces)
        rootEditor.ReplaceNode(oldNode, newNode.WithAdditionalAnnotations(s_replacementNodeAnnotation));
        var newRoot = rootEditor.GetChangedRoot();

        // 4. Format the new node so that the inserted braces/blocks would have correct indentation and formatting.
        var newNodeAfterInsertion = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single();

        var formattedNewRoot = Formatter.Format(
            newRoot,
            newNodeAfterInsertion.Span,
            services,
            formattingOptions,
            cancellationToken);

        // 4. Use the annotation to find the end of the open brace, it would be the new caret position
        var nextCaretPosition = formattedNewRoot.GetAnnotatedTokens(s_openBracePositionAnnotation).Single().Span.End;
        return (formattedNewRoot, nextCaretPosition);
    }

    private static SyntaxNode ReplaceNodeAndFormat(
        SolutionServices services,
        SyntaxNode root,
        SyntaxNode oldNode,
        SyntaxNode newNode,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        // 1. Tag the new node so that it could be found later.
        var annotatedNewNode = newNode.WithAdditionalAnnotations(s_replacementNodeAnnotation);

        // 2. Replace the old node with newNode. (new node is the node with correct braces)
        var newRoot = root.ReplaceNode(
            oldNode,
            annotatedNewNode);

        // 3. Find the newNode in the new syntax root.
        var newNodeAfterInsertion = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single();

        // 4. Format the new node so that the inserted braces/blocks would have correct indentation and formatting.
        var formattedNewRoot = Formatter.Format(
            newRoot,
            newNodeAfterInsertion.Span,
            services,
            formattingOptions,
            cancellationToken);

        return formattedNewRoot;
    }

    #endregion

    #region EmbeddedStatementModificationHelpers

    private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToEmbeddedStatementOwner(
        SolutionServices services,
        SyntaxNode root,
        SyntaxNode embeddedStatementOwner,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        // If there is no inner statement, just add an empty block to it.
        // e.g.
        // class Bar
        // {
        //    if (true)$$
        // }
        // =>
        // class Bar
        // {
        //    if (true)
        //    {
        //    }
        // }
        var statement = embeddedStatementOwner.GetEmbeddedStatement();
        if (statement == null || statement.IsMissing)
        {
            var newRoot = ReplaceNodeAndFormat(
                services,
                root,
                embeddedStatementOwner,
                WithBraces(embeddedStatementOwner, formattingOptions),
                formattingOptions,
                cancellationToken);

            // Locate the open brace token, and move the caret after it.
            var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
            return (newRoot, nextCaretPosition);
        }

        // There is an inner statement, it needs to be handled differently in addition to adding the block,

        // For while, ForEach, Lock and Using statement,
        // If there is an statement in the embeddedStatementOwner,
        // move the old statement next to the statementOwner,
        // and insert a empty block into the statementOwner,
        // e.g.
        // before:
        // whi$$le(true)
        // var i = 1;
        // for this case 'var i = 1;' is thought as the inner statement,
        //
        // after:
        // while(true)
        // {
        //      $$
        // }
        // var i = 1;
        return embeddedStatementOwner switch
        {
            WhileStatementSyntax or ForEachStatementSyntax or ForStatementSyntax or LockStatementSyntax or UsingStatementSyntax
                => ReplaceStatementOwnerAndInsertStatement(
                      services,
                      root,
                      oldNode: embeddedStatementOwner,
                      newNode: AddBlockToEmbeddedStatementOwner(embeddedStatementOwner, formattingOptions),
                      anchorNode: embeddedStatementOwner,
                      nodesToInsert: [statement],
                      formattingOptions,
                      cancellationToken),
            DoStatementSyntax doStatementNode => AddBraceToDoStatement(services, root, doStatementNode, formattingOptions, statement, cancellationToken),
            IfStatementSyntax ifStatementNode => AddBraceToIfStatement(services, root, ifStatementNode, formattingOptions, statement, cancellationToken),
            ElseClauseSyntax elseClauseNode => AddBraceToElseClause(services, root, elseClauseNode, formattingOptions, statement, cancellationToken),
            _ => throw ExceptionUtilities.UnexpectedValue(embeddedStatementOwner),
        };
    }

    private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToDoStatement(
        SolutionServices services,
        SyntaxNode root,
        DoStatementSyntax doStatementNode,
        SyntaxFormattingOptions formattingOptions,
        StatementSyntax innerStatement,
        CancellationToken cancellationToken)
    {
        // If this do statement doesn't end with the 'while' parts
        // e.g:
        // before:
        // d$$o
        // Print("hello");
        // after:
        // do
        // {
        //     $$
        // }
        // Print("hello");
        if (doStatementNode.WhileKeyword.IsMissing
            && doStatementNode.SemicolonToken.IsMissing
            && doStatementNode.OpenParenToken.IsMissing
            && doStatementNode.CloseParenToken.IsMissing)
        {
            return ReplaceStatementOwnerAndInsertStatement(
                services,
                root,
                oldNode: doStatementNode,
                newNode: AddBlockToEmbeddedStatementOwner(doStatementNode, formattingOptions),
                anchorNode: doStatementNode,
                nodesToInsert: [innerStatement],
                formattingOptions,
                cancellationToken);
        }

        // if the do statement has 'while' as an end
        // e.g:
        // before:
        // d$$o
        // Print("hello");
        // while (true);
        // after:
        // do
        // {
        //     $$
        //     Print("hello");
        // } while(true);
        var newRoot = ReplaceNodeAndFormat(
            services,
            root,
            doStatementNode,
            AddBlockToEmbeddedStatementOwner(doStatementNode, formattingOptions, innerStatement),
            formattingOptions,
            cancellationToken);

        var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
        return (newRoot, nextCaretPosition);
    }

    private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToIfStatement(
        SolutionServices services,
        SyntaxNode root,
        IfStatementSyntax ifStatementNode,
        SyntaxFormattingOptions formattingOptions,
        StatementSyntax innerStatement,
        CancellationToken cancellationToken)
    {
        // This ifStatement doesn't have an else clause, and its parent is a Block.
        // Insert the innerStatement next to the ifStatement
        // e.g.
        // if ($$a)
        // Print();
        // =>
        // if (a)
        // {
        //     $$
        // }
        // Print();
        if (ifStatementNode.Else == null && ifStatementNode.Parent is BlockSyntax)
        {
            return ReplaceStatementOwnerAndInsertStatement(
                services,
                root,
                ifStatementNode,
                AddBlockToEmbeddedStatementOwner(ifStatementNode, formattingOptions),
                ifStatementNode,
                [innerStatement],
                formattingOptions,
                cancellationToken);
        }

        // If this IfStatement has an else statement after
        // e.g.
        // before:
        // if $$(true)
        //     print("Hello");
        // else {}
        // after:
        // if (true)
        // {
        //     $$
        //     print("Hello");
        // }
        // else {}
        var newRoot = ReplaceNodeAndFormat(
            services,
            root,
            ifStatementNode,
            AddBlockToEmbeddedStatementOwner(ifStatementNode, formattingOptions, innerStatement),
            formattingOptions,
            cancellationToken);

        var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
        return (newRoot, nextCaretPosition);
    }

    private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToElseClause(
        SolutionServices services,
        SyntaxNode root,
        ElseClauseSyntax elseClauseNode,
        SyntaxFormattingOptions formattingOptions,
        StatementSyntax innerStatement,
        CancellationToken cancellationToken)
    {
        // If this is an 'els$$e if(true)' statement,
        // then treat it as the selected node is the nested if statement
        if (elseClauseNode.Statement is IfStatementSyntax)
        {
            return AddBraceToEmbeddedStatementOwner(services, root, elseClauseNode.Statement, formattingOptions, cancellationToken);
        }

        // Otherwise, it is just an ending else clause.
        // if its parent is an ifStatement and the parent of ifStatement is a block, insert the innerStatement after the ifStatement
        // e.g. before:
        // if (true)
        // {
        // } els$$e
        // Print();
        // after:
        // if (true)
        // {
        // } els$$e
        // {
        //      $$
        // }
        // Print();
        if (elseClauseNode.Parent is IfStatementSyntax { Parent: BlockSyntax })
        {
            return ReplaceStatementOwnerAndInsertStatement(
                services,
                root,
                elseClauseNode,
                WithBraces(elseClauseNode, formattingOptions),
                elseClauseNode.Parent!,
                [innerStatement],
                formattingOptions,
                cancellationToken);
        }

        // For all the other cases,
        // Put the innerStatement into the block
        // e.g.
        // if (a)
        //     if (true)
        //     {
        //     }
        //     else
        //     {
        //         $$
        //         Print();
        //     }
        // =>
        // if (a)
        //     if (true)
        //     {
        //     }
        //     els$$e
        //         Print();
        var formattedNewRoot = ReplaceNodeAndFormat(
            services,
            root,
            elseClauseNode,
            AddBlockToEmbeddedStatementOwner(elseClauseNode, formattingOptions, innerStatement),
            formattingOptions,
            cancellationToken);

        var nextCaretPosition = formattedNewRoot.GetAnnotatedTokens(s_openBracePositionAnnotation).Single().Span.End;
        return (formattedNewRoot, nextCaretPosition);
    }

    #endregion

    #region ObjectCreationExpressionModificationHelpers

    private static (SyntaxNode newNode, SyntaxNode oldNode) ModifyObjectCreationExpressionNode(
        BaseObjectCreationExpressionSyntax baseObjectCreationExpressionNode,
        bool addOrRemoveInitializer,
        SyntaxFormattingOptions formattingOptions)
    {
        // 1. Add '()' after the type or new keyword.
        // e.g.
        // case 1: 'var c = new Bar' becomes 'var c = new Bar()'
        // case 2: 'Bar b = new' becomes 'Bar b = new()'
        var objectCreationNodeWithArgumentList = WithArgumentListIfNeeded(baseObjectCreationExpressionNode);

        // 2. Add or remove initializer
        // e.g. var c = new Bar() => var c = new Bar() { }
        var objectCreationNodeWithCorrectInitializer = addOrRemoveInitializer
            ? WithBraces(objectCreationNodeWithArgumentList, formattingOptions)
            : WithoutBraces(objectCreationNodeWithArgumentList);

        // 3. Handle the semicolon.
        // If the next token is a semicolon, e.g.
        // var l = new Ba$$r() { }  => var l = new Ba$$r() { };
        var nextToken = baseObjectCreationExpressionNode.GetLastToken(includeZeroWidth: true).GetNextToken(includeZeroWidth: true);
        if (nextToken.IsKind(SyntaxKind.SemicolonToken)
            && nextToken.Parent != null
            && nextToken.Parent.Contains(baseObjectCreationExpressionNode))
        {
            var objectCreationNodeContainer = nextToken.Parent;
            // Replace the old object creation node and add the semicolon token.
            // Note: need to move the trailing trivia of the objectCreationExpressionNode after the semicolon token
            // e.g.
            // var l = new Bar() {} // I am some comments
            // =>
            // var l = new Bar() {}; // I am some comments
            var replacementContainerNode = objectCreationNodeContainer.ReplaceSyntax(
                nodes: [baseObjectCreationExpressionNode],
                (_, _) => objectCreationNodeWithCorrectInitializer.WithoutTrailingTrivia(),
                tokens: [nextToken],
                computeReplacementToken: (_, _) =>
                    SemicolonToken.WithTrailingTrivia(objectCreationNodeWithCorrectInitializer.GetTrailingTrivia()),
                trivia: [],
                computeReplacementTrivia: (_, syntaxTrivia) => syntaxTrivia);
            return (replacementContainerNode, objectCreationNodeContainer);
        }
        else
        {
            // No need to change the semicolon, just return the objectCreationExpression with correct initializer
            return (objectCreationNodeWithCorrectInitializer, baseObjectCreationExpressionNode);
        }
    }

    /// <summary>
    /// Add argument list to the objectCreationExpression if needed.
    /// e.g. new Bar; => new Bar();
    /// </summary>
    private static BaseObjectCreationExpressionSyntax WithArgumentListIfNeeded(BaseObjectCreationExpressionSyntax baseObjectCreationExpressionNode)
    {
        var argumentList = baseObjectCreationExpressionNode.ArgumentList;
        if (argumentList is { IsMissing: false })
        {
            return baseObjectCreationExpressionNode;
        }

        RoslynDebug.Assert(!baseObjectCreationExpressionNode.NewKeyword.IsMissing);
        if (baseObjectCreationExpressionNode is ObjectCreationExpressionSyntax objectCreationExpressionNode)
        {
            var typeNode = objectCreationExpressionNode.Type;
            if (typeNode.IsMissing)
            {
                // There is only 'new' keyword in the object creation expression. Treat it as an ImplicitObjectCreationExpression.
                // This could happen because when only type 'new', parser would think it is an ObjectCreationExpression.
                var newKeywordToken = baseObjectCreationExpressionNode.NewKeyword;
                var newArgumentList = ArgumentList().WithTrailingTrivia(newKeywordToken.TrailingTrivia);
                return ImplicitObjectCreationExpression(newKeywordToken.WithoutTrailingTrivia(), newArgumentList, baseObjectCreationExpressionNode.Initializer);
            }
            else
            {
                // Make sure the trailing trivia is passed to the argument list
                // like var l = new List\r\n =>
                // var l = new List()\r\r
                var newArgumentList = ArgumentList().WithTrailingTrivia(typeNode.GetTrailingTrivia());
                var newTypeNode = typeNode.WithoutTrivia();
                return objectCreationExpressionNode.WithType(newTypeNode).WithArgumentList(newArgumentList);
            }
        }

        if (baseObjectCreationExpressionNode is ImplicitObjectCreationExpressionSyntax implicitObjectCreationExpressionNode)
        {
            var newKeywordToken = implicitObjectCreationExpressionNode.NewKeyword;
            var newArgumentList = ArgumentList().WithTrailingTrivia(newKeywordToken.TrailingTrivia);
            return ImplicitObjectCreationExpression(newKeywordToken.WithoutTrailingTrivia(), newArgumentList, baseObjectCreationExpressionNode.Initializer);
        }

        RoslynDebug.Assert(false, $"New derived type of {nameof(BaseObjectCreationExpressionSyntax)} is added");
        return baseObjectCreationExpressionNode;
    }

    #endregion

    #region ShouldAddBraceCheck

    private static bool ShouldAddBraces(SyntaxNode node, int caretPosition)
        => node switch
        {
            NamespaceDeclarationSyntax namespaceDeclarationNode => ShouldAddBraceForNamespaceDeclaration(namespaceDeclarationNode, caretPosition),
            BaseTypeDeclarationSyntax baseTypeDeclarationNode => ShouldAddBraceForBaseTypeDeclaration(baseTypeDeclarationNode, caretPosition),
            BaseMethodDeclarationSyntax baseMethodDeclarationNode => ShouldAddBraceForBaseMethodDeclaration(baseMethodDeclarationNode, caretPosition),
            LocalFunctionStatementSyntax localFunctionStatementNode => ShouldAddBraceForLocalFunctionStatement(localFunctionStatementNode, caretPosition),
            ObjectCreationExpressionSyntax objectCreationExpressionNode => ShouldAddBraceForObjectCreationExpression(objectCreationExpressionNode),
            BaseFieldDeclarationSyntax baseFieldDeclarationNode => ShouldAddBraceForBaseFieldDeclaration(baseFieldDeclarationNode),
            AccessorDeclarationSyntax accessorDeclarationNode => ShouldAddBraceForAccessorDeclaration(accessorDeclarationNode),
            IndexerDeclarationSyntax indexerDeclarationNode => ShouldAddBraceForIndexerDeclaration(indexerDeclarationNode, caretPosition),
            SwitchStatementSyntax switchStatementNode => ShouldAddBraceForSwitchStatement(switchStatementNode),
            TryStatementSyntax tryStatementNode => ShouldAddBraceForTryStatement(tryStatementNode, caretPosition),
            CatchClauseSyntax catchClauseNode => ShouldAddBraceForCatchClause(catchClauseNode, caretPosition),
            FinallyClauseSyntax finallyClauseNode => ShouldAddBraceForFinallyClause(finallyClauseNode, caretPosition),
            DoStatementSyntax doStatementNode => ShouldAddBraceForDoStatement(doStatementNode, caretPosition),
            CommonForEachStatementSyntax commonForEachStatementNode => ShouldAddBraceForCommonForEachStatement(commonForEachStatementNode, caretPosition),
            ForStatementSyntax forStatementNode => ShouldAddBraceForForStatement(forStatementNode, caretPosition),
            IfStatementSyntax ifStatementNode => ShouldAddBraceForIfStatement(ifStatementNode, caretPosition),
            ElseClauseSyntax elseClauseNode => ShouldAddBraceForElseClause(elseClauseNode, caretPosition),
            LockStatementSyntax lockStatementNode => ShouldAddBraceForLockStatement(lockStatementNode, caretPosition),
            UsingStatementSyntax usingStatementNode => ShouldAddBraceForUsingStatement(usingStatementNode, caretPosition),
            WhileStatementSyntax whileStatementNode => ShouldAddBraceForWhileStatement(whileStatementNode, caretPosition),
            CheckedStatementSyntax checkedStatementNode => ShouldAddBraceForCheckedStatement(checkedStatementNode, caretPosition),
            _ => false,
        };

    /// <summary>
    /// For namespace, make sure it has name there is no braces
    /// </summary>
    private static bool ShouldAddBraceForNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclarationNode, int caretPosition)
        => !namespaceDeclarationNode.Name.IsMissing
           && HasNoBrace(namespaceDeclarationNode)
           && !WithinAttributeLists(namespaceDeclarationNode, caretPosition)
           && !WithinBraces(namespaceDeclarationNode, caretPosition);

    /// <summary>
    /// For class/struct/enum ..., make sure it has name and there is no braces.
    /// </summary>
    private static bool ShouldAddBraceForBaseTypeDeclaration(BaseTypeDeclarationSyntax baseTypeDeclarationNode, int caretPosition)
        => !baseTypeDeclarationNode.Identifier.IsMissing
           && HasNoBrace(baseTypeDeclarationNode)
           && !WithinAttributeLists(baseTypeDeclarationNode, caretPosition)
           && !WithinBraces(baseTypeDeclarationNode, caretPosition);

    /// <summary>
    /// For method, make sure it has a ParameterList, because later braces would be inserted after the Parameterlist
    /// </summary>
    private static bool ShouldAddBraceForBaseMethodDeclaration(BaseMethodDeclarationSyntax baseMethodDeclarationNode, int caretPosition)
        => baseMethodDeclarationNode.ExpressionBody == null
           && baseMethodDeclarationNode.Body == null
           && !baseMethodDeclarationNode.ParameterList.IsMissing
           && baseMethodDeclarationNode.SemicolonToken.IsMissing
           && !WithinAttributeLists(baseMethodDeclarationNode, caretPosition)
           && !WithinMethodBody(baseMethodDeclarationNode, caretPosition)
           // Make sure we don't insert braces for method in Interface.
           && !baseMethodDeclarationNode.IsParentKind(SyntaxKind.InterfaceDeclaration);

    /// <summary>
    /// For local Function, make sure it has a ParameterList, because later braces would be inserted after the Parameterlist
    /// </summary>
    private static bool ShouldAddBraceForLocalFunctionStatement(LocalFunctionStatementSyntax localFunctionStatementNode, int caretPosition)
        => localFunctionStatementNode.ExpressionBody == null
           && localFunctionStatementNode.Body == null
           && !localFunctionStatementNode.ParameterList.IsMissing
           && !WithinAttributeLists(localFunctionStatementNode, caretPosition)
           && !WithinMethodBody(localFunctionStatementNode, caretPosition);

    /// <summary>
    /// Add brace for ObjectCreationExpression if it doesn't have initializer
    /// </summary>
    private static bool ShouldAddBraceForObjectCreationExpression(ObjectCreationExpressionSyntax objectCreationExpressionNode)
        => objectCreationExpressionNode.Initializer == null;

    /// <summary>
    /// Add braces for field and event field if they only have one variable, semicolon is missing and don't have readonly keyword
    /// Example:
    /// public int Bar$$ =>
    /// public int Bar
    /// {
    ///      $$
    /// }
    /// This would change field to property, and change event field to event declaration.
    /// </summary>
    private static bool ShouldAddBraceForBaseFieldDeclaration(BaseFieldDeclarationSyntax baseFieldDeclarationNode)
        => baseFieldDeclarationNode is { Declaration.Variables: [{ Initializer: null }], SemicolonToken.IsMissing: true }
           && !baseFieldDeclarationNode.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);

    private static bool ShouldAddBraceForAccessorDeclaration(AccessorDeclarationSyntax accessorDeclarationNode)
    {
        if (accessorDeclarationNode.Body == null
            && accessorDeclarationNode.ExpressionBody == null
            && accessorDeclarationNode.SemicolonToken.IsMissing)
        {
            // If the accessor doesn't have body, expression body and semicolon, let's check this case
            // for both event and property,
            // e.g.
            // int Bar
            // {
            //     get;
            //     se$$t
            // }
            // because if the getter doesn't have a body then setter also shouldn't have any body.
            // Don't check for indexer because the accessor for indexer should have body.
            var parent = accessorDeclarationNode.Parent;
            var parentOfParent = parent?.Parent;
            if (parent is AccessorListSyntax accessorListNode
                && parentOfParent is PropertyDeclarationSyntax)
            {
                var otherAccessors = accessorListNode.Accessors
                    .Except([accessorDeclarationNode])
                    .ToImmutableArray();
                if (!otherAccessors.IsEmpty)
                {
                    return !otherAccessors.Any(
                        static accessor => accessor.Body == null
                                    && accessor.ExpressionBody == null
                                    && !accessor.SemicolonToken.IsMissing);
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// For indexer, switch, try and catch syntax node without braces, if it is the last child of its parent, it would
    /// use its parent's close brace as its own.
    /// Example:
    /// class Bar
    /// {
    ///      int th$$is[int i]
    /// }
    /// In this case, parser would think the last '}' belongs to the indexer, not the class.
    /// Therefore, only check if the open brace is missing for these 4 types of SyntaxNode
    /// </summary>
    private static bool ShouldAddBraceForIndexerDeclaration(IndexerDeclarationSyntax indexerDeclarationNode, int caretPosition)
    {
        if (WithinAttributeLists(indexerDeclarationNode, caretPosition) ||
            WithinBraces(indexerDeclarationNode.AccessorList, caretPosition))
        {
            return false;
        }

        // Make sure it has brackets
        var (openBracket, closeBracket) = indexerDeclarationNode.ParameterList.GetBrackets();
        if (openBracket.IsMissing || closeBracket.IsMissing)
        {
            return false;
        }

        // If both accessorList and body is empty
        if ((indexerDeclarationNode.AccessorList == null || indexerDeclarationNode.AccessorList.IsMissing)
            && indexerDeclarationNode.ExpressionBody == null)
        {
            return true;
        }

        return indexerDeclarationNode.AccessorList != null
           && indexerDeclarationNode.AccessorList.OpenBraceToken.IsMissing;
    }

    // For the Switch, Try, Catch, Finally node
    // e.g.
    // class Bar
    // {
    //      void Main()
    //      {
    //          tr$$y
    //      }
    // }
    // In this case, the last close brace of 'void Main()' would be thought as a part of the try statement,
    // and the last close brace of 'Bar' would be thought as a part of Main()
    // So for these case, just check if the open brace is missing.
    private static bool ShouldAddBraceForSwitchStatement(SwitchStatementSyntax switchStatementNode)
        => !switchStatementNode.SwitchKeyword.IsMissing && switchStatementNode.OpenBraceToken.IsMissing;

    private static bool ShouldAddBraceForTryStatement(TryStatementSyntax tryStatementNode, int caretPosition)
        => !tryStatementNode.TryKeyword.IsMissing
           && tryStatementNode.Block.OpenBraceToken.IsMissing
           && !tryStatementNode.Block.Span.Contains(caretPosition);

    private static bool ShouldAddBraceForCatchClause(CatchClauseSyntax catchClauseSyntax, int caretPosition)
        => !catchClauseSyntax.CatchKeyword.IsMissing
           && catchClauseSyntax.Block.OpenBraceToken.IsMissing
           && !catchClauseSyntax.Block.Span.Contains(caretPosition);

    private static bool ShouldAddBraceForFinallyClause(FinallyClauseSyntax finallyClauseNode, int caretPosition)
        => !finallyClauseNode.FinallyKeyword.IsMissing
           && finallyClauseNode.Block.OpenBraceToken.IsMissing
           && !finallyClauseNode.Block.Span.Contains(caretPosition);

    private static bool ShouldAddBraceForCheckedStatement(CheckedStatementSyntax checkedStatementNode, int caretPosition)
        => checkedStatementNode.Block.OpenBraceToken.IsMissing
           && !checkedStatementNode.Block.Span.Contains(caretPosition);

    // For all the embeddedStatementOwners,
    // if the embeddedStatement is not block, insert the the braces if its statement is not block.
    private static bool ShouldAddBraceForDoStatement(DoStatementSyntax doStatementNode, int caretPosition)
        => !doStatementNode.DoKeyword.IsMissing
           && doStatementNode.Statement is not BlockSyntax
           && doStatementNode.DoKeyword.FullSpan.Contains(caretPosition);

    private static bool ShouldAddBraceForCommonForEachStatement(CommonForEachStatementSyntax commonForEachStatementNode, int caretPosition)
        => commonForEachStatementNode.Statement is not BlockSyntax
           && !commonForEachStatementNode.OpenParenToken.IsMissing
           && !commonForEachStatementNode.CloseParenToken.IsMissing
           && !WithinEmbeddedStatement(commonForEachStatementNode, caretPosition);

    private static bool ShouldAddBraceForForStatement(ForStatementSyntax forStatementNode, int caretPosition)
        => forStatementNode.Statement is not BlockSyntax
           && !forStatementNode.OpenParenToken.IsMissing
           && !forStatementNode.CloseParenToken.IsMissing
           && !WithinEmbeddedStatement(forStatementNode, caretPosition);

    private static bool ShouldAddBraceForIfStatement(IfStatementSyntax ifStatementNode, int caretPosition)
        => ifStatementNode.Statement is not BlockSyntax
           && !ifStatementNode.OpenParenToken.IsMissing
           && !ifStatementNode.CloseParenToken.IsMissing
           && !WithinEmbeddedStatement(ifStatementNode, caretPosition);

    private static bool ShouldAddBraceForElseClause(ElseClauseSyntax elseClauseNode, int caretPosition)
    {
        // In case it is an else-if clause, if the statement is IfStatement, use its insertion statement
        // otherwise, use the end of the else keyword
        // Example:
        // Before: if (a)
        //         {
        //         } else i$$f (b)
        // After: if (a)
        //        {
        //        } else if (b)
        //        {
        //            $$
        //        }
        if (elseClauseNode.Statement is IfStatementSyntax ifStatementNode)
        {
            return ShouldAddBraceForIfStatement(ifStatementNode, caretPosition);
        }
        else
        {
            // Here it should be an elseClause
            // like:
            // if (a)
            // {
            // } els$$e {
            // }
            // So only check the its statement
            return elseClauseNode.Statement is not BlockSyntax && !WithinEmbeddedStatement(elseClauseNode, caretPosition);
        }
    }

    private static bool ShouldAddBraceForLockStatement(LockStatementSyntax lockStatementNode, int caretPosition)
        => lockStatementNode.Statement is not BlockSyntax
           && !lockStatementNode.OpenParenToken.IsMissing
           && !lockStatementNode.CloseParenToken.IsMissing
           && !WithinEmbeddedStatement(lockStatementNode, caretPosition);

    private static bool ShouldAddBraceForUsingStatement(UsingStatementSyntax usingStatementNode, int caretPosition)
        => usingStatementNode.Statement is not BlockSyntax
           && !usingStatementNode.OpenParenToken.IsMissing
           && !usingStatementNode.CloseParenToken.IsMissing
           && !WithinEmbeddedStatement(usingStatementNode, caretPosition);

    private static bool ShouldAddBraceForWhileStatement(WhileStatementSyntax whileStatementNode, int caretPosition)
        => whileStatementNode.Statement is not BlockSyntax
           && !whileStatementNode.OpenParenToken.IsMissing
           && !whileStatementNode.CloseParenToken.IsMissing
           && !WithinEmbeddedStatement(whileStatementNode, caretPosition);

    private static bool WithinAttributeLists(SyntaxNode node, int caretPosition)
    {
        var attributeLists = node.GetAttributeLists();
        return attributeLists.Span.Contains(caretPosition);
    }

    private static bool WithinBraces(SyntaxNode? node, int caretPosition)
    {
        var (openBrace, closeBrace) = node.GetBraces();
        return TextSpan.FromBounds(openBrace.SpanStart, closeBrace.Span.End).Contains(caretPosition);
    }

    private static bool WithinMethodBody(SyntaxNode node, int caretPosition)
    {
        if (node is BaseMethodDeclarationSyntax { Body: { } baseMethodBody })
        {
            return baseMethodBody.Span.Contains(caretPosition);
        }

        if (node is LocalFunctionStatementSyntax { Body: { } localFunctionBody })
        {
            return localFunctionBody.Span.Contains(caretPosition);
        }

        return false;
    }

    private static bool HasNoBrace(SyntaxNode node)
    {
        var (openBrace, closeBrace) = node.GetBraces();
        return openBrace.IsKind(SyntaxKind.None) && closeBrace.IsKind(SyntaxKind.None)
            || openBrace.IsMissing && closeBrace.IsMissing;
    }

    private static bool WithinEmbeddedStatement(SyntaxNode node, int caretPosition)
        => node.GetEmbeddedStatement()?.Span.Contains(caretPosition) ?? false;

    #endregion

    #region ShouldRemoveBraceCheck

    private static bool ShouldRemoveBraces(SyntaxNode node, int caretPosition)
        => node switch
        {
            BaseObjectCreationExpressionSyntax baseObjectCreationExpressionNode => ShouldRemoveBraceForObjectCreationExpression(baseObjectCreationExpressionNode),
            AccessorDeclarationSyntax accessorDeclarationNode => ShouldRemoveBraceForAccessorDeclaration(accessorDeclarationNode, caretPosition),
            PropertyDeclarationSyntax propertyDeclarationNode => ShouldRemoveBraceForPropertyDeclaration(propertyDeclarationNode, caretPosition),
            EventDeclarationSyntax eventDeclarationNode => ShouldRemoveBraceForEventDeclaration(eventDeclarationNode, caretPosition),
            _ => false,
        };

    /// <summary>
    /// Remove the braces if the BaseObjectCreationExpression has an empty Initializer.
    /// </summary>
    private static bool ShouldRemoveBraceForObjectCreationExpression(BaseObjectCreationExpressionSyntax baseObjectCreationExpressionNode)
    {
        var initializer = baseObjectCreationExpressionNode.Initializer;
        return initializer != null && initializer.Expressions.IsEmpty();
    }

    // Only do this when it is an accessor in property
    // Since it is illegal to have something like
    // int this[int i] { get; set;}
    // event EventHandler Bar {add; remove;}
    private static bool ShouldRemoveBraceForAccessorDeclaration(AccessorDeclarationSyntax accessorDeclarationNode, int caretPosition)
        => accessorDeclarationNode.Body != null
           && accessorDeclarationNode.Body.Statements.IsEmpty()
           && accessorDeclarationNode.ExpressionBody == null
           && accessorDeclarationNode.Parent != null
           && accessorDeclarationNode.Parent.IsParentKind(SyntaxKind.PropertyDeclaration)
           && accessorDeclarationNode.Body.Span.Contains(caretPosition);

    private static bool ShouldRemoveBraceForPropertyDeclaration(PropertyDeclarationSyntax propertyDeclarationNode, int caretPosition)
    {
        // If a property just has an empty accessorList, like
        // int i $${ }
        // then remove the braces and change it to a field
        // int i;
        if (propertyDeclarationNode.AccessorList != null
            && propertyDeclarationNode.ExpressionBody == null)
        {
            var accessorList = propertyDeclarationNode.AccessorList;
            return accessorList.Span.Contains(caretPosition) && accessorList.Accessors.IsEmpty();
        }

        return false;
    }

    private static bool ShouldRemoveBraceForEventDeclaration(EventDeclarationSyntax eventDeclarationNode, int caretPosition)
    {
        // If an event declaration just has an empty accessorList,
        // like
        // event EventHandler e$$  { }
        // then change it to a event field declaration
        // event EventHandler e;
        var accessorList = eventDeclarationNode.AccessorList;
        return accessorList != null
               && accessorList.Span.Contains(caretPosition)
               && accessorList.Accessors.IsEmpty();
    }

    #endregion

    #region AddBrace

    private static AccessorListSyntax GetAccessorListNode(SyntaxFormattingOptions formattingOptions)
        => AccessorList().WithOpenBraceToken(GetOpenBrace(formattingOptions)).WithCloseBraceToken(GetCloseBrace(formattingOptions));

    private static InitializerExpressionSyntax GetInitializerExpressionNode(SyntaxFormattingOptions formattingOptions)
        => InitializerExpression(SyntaxKind.ObjectInitializerExpression)
            .WithOpenBraceToken(GetOpenBrace(formattingOptions));

    private static BlockSyntax GetBlockNode(SyntaxFormattingOptions formattingOptions)
        => Block().WithOpenBraceToken(GetOpenBrace(formattingOptions)).WithCloseBraceToken(GetCloseBrace(formattingOptions));

    private static SyntaxToken GetOpenBrace(SyntaxFormattingOptions formattingOptions)
        => Token(
                leading: SyntaxTriviaList.Empty,
                kind: SyntaxKind.OpenBraceToken,
                trailing: [GetNewLineTrivia(formattingOptions)])
            .WithAdditionalAnnotations(s_openBracePositionAnnotation);

    private static SyntaxToken GetCloseBrace(SyntaxFormattingOptions formattingOptions)
        => Token(
            leading: SyntaxTriviaList.Empty,
            kind: SyntaxKind.CloseBraceToken,
            trailing: [GetNewLineTrivia(formattingOptions)]);

    private static SyntaxTrivia GetNewLineTrivia(SyntaxFormattingOptions formattingOptions)
    {
        var newLineString = formattingOptions.NewLine;
        return EndOfLine(newLineString);
    }

    /// <summary>
    /// Add braces to the <param name="node"/>.
    /// For FieldDeclaration and EventFieldDeclaration, it will change them to PropertyDeclaration and EventDeclaration
    /// </summary>
    private static SyntaxNode WithBraces(SyntaxNode node, SyntaxFormattingOptions formattingOptions)
        => node switch
        {
            BaseTypeDeclarationSyntax baseTypeDeclarationNode => WithBracesForBaseTypeDeclaration(baseTypeDeclarationNode, formattingOptions),
            BaseObjectCreationExpressionSyntax objectCreationExpressionNode => GetObjectCreationExpressionWithInitializer(objectCreationExpressionNode, formattingOptions),
            BaseMethodDeclarationSyntax baseMethodDeclarationNode => AddBlockToBaseMethodDeclaration(baseMethodDeclarationNode, formattingOptions),
            LocalFunctionStatementSyntax localFunctionStatementNode => AddBlockToLocalFunctionDeclaration(localFunctionStatementNode, formattingOptions),
            AccessorDeclarationSyntax accessorDeclarationNode => AddBlockToAccessorDeclaration(accessorDeclarationNode, formattingOptions),
            _ when node.IsEmbeddedStatementOwner() => AddBlockToEmbeddedStatementOwner(node, formattingOptions),
            _ => throw ExceptionUtilities.UnexpectedValue(node),
        };

    /// <summary>
    /// Add braces to <param name="baseTypeDeclarationNode"/>.
    /// </summary>
    private static BaseTypeDeclarationSyntax WithBracesForBaseTypeDeclaration(
        BaseTypeDeclarationSyntax baseTypeDeclarationNode,
        SyntaxFormattingOptions formattingOptions)
        => baseTypeDeclarationNode.WithOpenBraceToken(GetOpenBrace(formattingOptions))
            .WithCloseBraceToken(CloseBraceToken);

    /// <summary>
    /// Add an empty initializer to <param name="objectCreationExpressionNode"/>.
    /// </summary>
    private static BaseObjectCreationExpressionSyntax GetObjectCreationExpressionWithInitializer(
        BaseObjectCreationExpressionSyntax objectCreationExpressionNode,
        SyntaxFormattingOptions formattingOptions)
        => objectCreationExpressionNode.WithInitializer(GetInitializerExpressionNode(formattingOptions));

    /// <summary>
    /// Add an empty block to <param name="baseMethodDeclarationNode"/>.
    /// </summary>
    private static BaseMethodDeclarationSyntax AddBlockToBaseMethodDeclaration(
        BaseMethodDeclarationSyntax baseMethodDeclarationNode,
        SyntaxFormattingOptions formattingOptions)
        => baseMethodDeclarationNode.WithBody(GetBlockNode(formattingOptions))
            // When the method declaration with no body is parsed, it has an invisible trailing semicolon. Make sure it is removed.
            .WithSemicolonToken(Token(SyntaxKind.None));

    /// <summary>
    /// Add an empty block to <param name="localFunctionStatementNode"/>.
    /// </summary>
    private static LocalFunctionStatementSyntax AddBlockToLocalFunctionDeclaration(
        LocalFunctionStatementSyntax localFunctionStatementNode,
        SyntaxFormattingOptions formattingOptions)
        => localFunctionStatementNode.WithBody(GetBlockNode(formattingOptions))
            // When the local method declaration with no body is parsed, it has an invisible trailing semicolon. Make sure it is removed.
            .WithSemicolonToken(Token(SyntaxKind.None));

    /// <summary>
    /// Add an empty block to <param name="accessorDeclarationNode"/>.
    /// </summary>
    private static AccessorDeclarationSyntax AddBlockToAccessorDeclaration(
        AccessorDeclarationSyntax accessorDeclarationNode,
        SyntaxFormattingOptions formattingOptions)
        => accessorDeclarationNode.WithBody(GetBlockNode(formattingOptions))
            // When the accessor with no body is parsed, it has an invisible trailing semicolon. Make sure it is removed.
            .WithSemicolonToken(Token(SyntaxKind.None));

    /// <summary>
    /// Add a block with <param name="extraNodeInsertedBetweenBraces"/> to <param name="embeddedStatementOwner"/>
    /// </summary>
    private static SyntaxNode AddBlockToEmbeddedStatementOwner(
        SyntaxNode embeddedStatementOwner,
        SyntaxFormattingOptions formattingOptions,
        StatementSyntax? extraNodeInsertedBetweenBraces = null)
    {
        var block = extraNodeInsertedBetweenBraces != null
            ? GetBlockNode(formattingOptions).WithStatements(new SyntaxList<StatementSyntax>(extraNodeInsertedBetweenBraces))
            : GetBlockNode(formattingOptions);

        return embeddedStatementOwner switch
        {
            DoStatementSyntax doStatementNode => doStatementNode.WithStatement(block),
            ForEachStatementSyntax forEachStatementNode => forEachStatementNode.WithStatement(block),
            ForStatementSyntax forStatementNode => forStatementNode.WithStatement(block),
            IfStatementSyntax ifStatementNode => ifStatementNode.WithStatement(block),
            ElseClauseSyntax elseClauseNode => elseClauseNode.WithStatement(block),
            WhileStatementSyntax whileStatementNode => whileStatementNode.WithStatement(block),
            UsingStatementSyntax usingStatementNode => usingStatementNode.WithStatement(block),
            LockStatementSyntax lockStatementNode => lockStatementNode.WithStatement(block),
            _ => throw ExceptionUtilities.UnexpectedValue(embeddedStatementOwner)
        };
    }

    #endregion

    #region RemoveBrace

    /// <summary>
    /// Remove the brace for the input syntax node
    /// For ObjectCreationExpressionSyntax, it would remove the initializer
    /// For PropertyDeclarationSyntax, it would change it to a FieldDeclaration
    /// For EventDeclarationSyntax, it would change it to eventFieldDeclaration
    /// For Accessor, it would change it to the empty version ending with semicolon.
    /// e.g get {} => get;
    /// </summary>
    private static SyntaxNode WithoutBraces(SyntaxNode node)
        => node switch
        {
            BaseObjectCreationExpressionSyntax baseObjectCreationExpressionNode => RemoveInitializerForBaseObjectCreationExpression(baseObjectCreationExpressionNode),
            PropertyDeclarationSyntax propertyDeclarationNode => ConvertPropertyDeclarationToFieldDeclaration(propertyDeclarationNode),
            EventDeclarationSyntax eventDeclarationNode => ConvertEventDeclarationToEventFieldDeclaration(eventDeclarationNode),
            AccessorDeclarationSyntax accessorDeclarationNode => RemoveBodyForAccessorDeclarationNode(accessorDeclarationNode),
            _ => throw ExceptionUtilities.UnexpectedValue(node),
        };

    /// <summary>
    /// Remove the initializer for <param name="baseObjectCreationExpressionNode"/>.
    /// </summary>
    private static BaseObjectCreationExpressionSyntax RemoveInitializerForBaseObjectCreationExpression(
        BaseObjectCreationExpressionSyntax baseObjectCreationExpressionNode)
    {
        var objectCreationNodeWithoutInitializer = baseObjectCreationExpressionNode.WithInitializer(null);
        // Filter the non-comments trivia
        // e.g.
        // Bar(new Foo() // I am some comments
        // {
        //      $$
        // });
        // =>
        // Bar(new Foo() // I am some comments);
        // In this case, 'I am somme comments' has an end of line triva, if not removed, it would make
        // the final result becomes
        // Bar(new Foo() // I am some comments
        // );
        var trivia = objectCreationNodeWithoutInitializer.GetTrailingTrivia().Where(trivia => trivia.IsSingleOrMultiLineComment());
        return objectCreationNodeWithoutInitializer.WithTrailingTrivia(trivia);
    }

    /// <summary>
    /// Convert <param name="propertyDeclarationNode"/> to fieldDeclaration.
    /// </summary>
    private static FieldDeclarationSyntax ConvertPropertyDeclarationToFieldDeclaration(
        PropertyDeclarationSyntax propertyDeclarationNode)
        => FieldDeclaration(
            propertyDeclarationNode.AttributeLists,
            propertyDeclarationNode.Modifiers,
            VariableDeclaration(
                propertyDeclarationNode.Type,
                [VariableDeclarator(propertyDeclarationNode.Identifier)]),
            SemicolonToken);

    /// <summary>
    /// Convert <param name="eventDeclarationNode"/> to EventFieldDeclaration.
    /// </summary>
    private static EventFieldDeclarationSyntax ConvertEventDeclarationToEventFieldDeclaration(
        EventDeclarationSyntax eventDeclarationNode)
        => EventFieldDeclaration(
            eventDeclarationNode.AttributeLists,
            eventDeclarationNode.Modifiers,
            VariableDeclaration(
                eventDeclarationNode.Type,
                [VariableDeclarator(eventDeclarationNode.Identifier)]));

    /// <summary>
    /// Remove the body of <param name="accessorDeclarationNode"/>.
    /// </summary>
    private static AccessorDeclarationSyntax RemoveBodyForAccessorDeclarationNode(AccessorDeclarationSyntax accessorDeclarationNode)
        => accessorDeclarationNode
            .WithBody(null).WithoutTrailingTrivia().WithSemicolonToken(
                Token(SyntaxTriviaList.Empty, SyntaxKind.SemicolonToken, SyntaxTriviaList.Empty));

    #endregion
}
