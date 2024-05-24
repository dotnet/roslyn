// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.AutomaticCompletion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;

/// <summary>
/// When user types <c>;</c> in a statement, semicolon is added and caret is placed after the semicolon
/// </summary>
[Export(typeof(ICommandHandler))]
[Export]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(nameof(CompleteStatementCommandHandler))]
[Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CompleteStatementCommandHandler(
    ITextUndoHistoryRegistry textUndoHistoryRegistry,
    IEditorOperationsFactoryService editorOperationsFactoryService,
    IGlobalOptionService globalOptions) : IChainedCommandHandler<TypeCharCommandArgs>
{
    private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry = textUndoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    private enum SemicolonBehavior
    {
        /// <summary>
        /// This command handler does not participate in handling the typing behavior of the current semicolon.
        /// </summary>
        None,

        /// <summary>
        /// This command handler moves the caret, but does not insert the necessary semicolon.
        /// </summary>
        Insert,

        /// <summary>
        /// This command handler moves the caret, and overtypes an existing semicolon in the process. No additional
        /// semicolon is needed.
        /// </summary>
        Overtype,
    }

    public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler) => nextCommandHandler();

    public string DisplayName => CSharpEditorResources.Complete_statement_on_semicolon;

    public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        var willMoveSemicolon = BeforeExecuteCommand(speculative: true, args, executionContext);
        if (willMoveSemicolon == SemicolonBehavior.None)
        {
            // Pass this on without altering the undo stack
            nextCommandHandler();
            return;
        }

        using var transaction = CaretPreservingEditTransaction.TryCreate(CSharpEditorResources.Complete_statement_on_semicolon, args.TextView, _textUndoHistoryRegistry, _editorOperationsFactoryService);

        // Determine where semicolon should be placed and move caret to location
        if (BeforeExecuteCommand(speculative: false, args, executionContext) != SemicolonBehavior.Overtype)
        {
            // Insert the semicolon using next command handler
            nextCommandHandler();
        }

        transaction?.Complete();
    }

    private SemicolonBehavior BeforeExecuteCommand(bool speculative, TypeCharCommandArgs args, CommandExecutionContext executionContext)
    {
        if (args.TypedChar != ';' || !args.TextView.Selection.IsEmpty)
        {
            return SemicolonBehavior.None;
        }

        var caretOpt = args.TextView.GetCaretPoint(args.SubjectBuffer);
        if (!caretOpt.HasValue)
        {
            return SemicolonBehavior.None;
        }

        if (!_globalOptions.GetOption(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon))
        {
            return SemicolonBehavior.None;
        }

        var caret = caretOpt.Value;
        var document = caret.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return SemicolonBehavior.None;
        }

        var cancellationToken = executionContext.OperationContext.UserCancellationToken;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);

        if (!TryGetStartingNode(root, caret, out var currentNode, cancellationToken))
        {
            return SemicolonBehavior.None;
        }

        return MoveCaretToSemicolonPosition(speculative, args, document, root, originalCaret: caret, caret, syntaxFacts, currentNode,
            isInsideDelimiters: false, cancellationToken);
    }

    /// <summary>
    /// Determines which node the caret is in.  
    /// Must be called on the UI thread.
    /// </summary>
    private static bool TryGetStartingNode(
        SyntaxNode root,
        SnapshotPoint caret,
        [NotNullWhen(true)] out SyntaxNode? startingNode,
        CancellationToken cancellationToken)
    {
        // on the UI thread
        startingNode = null;
        var caretPosition = caret.Position;

        var token = root.FindTokenOnLeftOfPosition(caretPosition);

        if (token.SyntaxTree == null
            || token.SyntaxTree.IsInNonUserCode(caretPosition, cancellationToken))
        {
            return false;
        }

        startingNode = token.GetRequiredParent();

        // If the caret is before an opening delimiter or after a closing delimeter,
        // start analysis with node outside of delimiters.
        //
        // Examples, 
        //    `obj.ToString$()` where `token` references `(` but the caret isn't actually inside the argument list.
        //    `obj.ToString()$` or `obj.method()$ .method()` where `token` references `)` but the caret isn't inside the argument list.
        //    `defa$$ult(object)` where `token` references `default` but the caret isn't inside the parentheses.
        if (HasDelimitersButCaretIsOutside(startingNode, caretPosition))
        {
            startingNode = startingNode.GetRequiredParent();
        }

        return true;
    }

    private static SemicolonBehavior MoveCaretToSemicolonPosition(
        bool speculative,
        TypeCharCommandArgs args,
        Document document,
        SyntaxNode root,
        SnapshotPoint originalCaret,
        SnapshotPoint caret,
        ISyntaxFactsService syntaxFacts,
        SyntaxNode? currentNode,
        bool isInsideDelimiters,
        CancellationToken cancellationToken)
    {
        if (currentNode == null ||
            IsInAStringOrCharacter(currentNode, caret))
        {
            // Don't complete statement.  Return without moving the caret.
            return SemicolonBehavior.None;
        }

        if (currentNode.Kind() is
                SyntaxKind.ArgumentList or
                SyntaxKind.ArrayRankSpecifier or
                SyntaxKind.BracketedArgumentList or
                SyntaxKind.ParenthesizedExpression or
                SyntaxKind.ParameterList or
                SyntaxKind.DefaultExpression or
                SyntaxKind.CheckedExpression or
                SyntaxKind.UncheckedExpression or
                SyntaxKind.TypeOfExpression or
                SyntaxKind.TupleExpression or
                SyntaxKind.ObjectInitializerExpression or
                SyntaxKind.ArrayInitializerExpression or
                SyntaxKind.CollectionInitializerExpression or
                SyntaxKind.CollectionExpression or
                SyntaxKind.SwitchExpression)
        {
            // make sure the closing delimiter exists
            if (RequiredDelimiterIsMissing(currentNode))
            {
                return SemicolonBehavior.None;
            }

            // set caret to just outside the delimited span and analyze again
            // if caret was already in that position, return to avoid infinite loop
            var newCaretPosition = currentNode.Span.End;
            if (newCaretPosition == caret.Position)
            {
                return SemicolonBehavior.None;
            }

            // We know the current node has delimiters due to the Kind() check above, so isInsideDelimiters is
            // simply the inverse of being outside the delimiters.
            isInsideDelimiters = !HasDelimitersButCaretIsOutside(currentNode, caret.Position);

            var newCaret = args.SubjectBuffer.CurrentSnapshot.GetPoint(newCaretPosition);
            if (!TryGetStartingNode(root, newCaret, out currentNode, cancellationToken))
                return SemicolonBehavior.None;

            return MoveCaretToSemicolonPosition(
                speculative, args, document, root, originalCaret, newCaret, syntaxFacts, currentNode, isInsideDelimiters, cancellationToken);
        }
        else if (currentNode.IsKind(SyntaxKind.DoStatement))
        {
            if (IsInConditionOfDoStatement(currentNode, caret))
            {
                return MoveCaretToFinalPositionInStatement(speculative, currentNode, args, originalCaret, caret, isInsideDelimiters: true);
            }

            return SemicolonBehavior.None;
        }
        else if (syntaxFacts.IsStatement(currentNode)
            || CanHaveSemicolon(currentNode))
        {
            return MoveCaretToFinalPositionInStatement(speculative, currentNode, args, originalCaret, caret, isInsideDelimiters);
        }
        else
        {
            // keep caret the same, but continue analyzing with the parent of the current node
            currentNode = currentNode.Parent;
            return MoveCaretToSemicolonPosition(
                speculative, args, document, root, originalCaret, caret, syntaxFacts, currentNode, isInsideDelimiters, cancellationToken);
        }
    }

    private static bool CanHaveSemicolon(SyntaxNode currentNode)
    {
        if (currentNode.Kind() is SyntaxKind.FieldDeclaration or SyntaxKind.DelegateDeclaration or SyntaxKind.ArrowExpressionClause)
        {
            return true;
        }

        if (currentNode.IsKind(SyntaxKind.EqualsValueClause) && currentNode.IsParentKind(SyntaxKind.PropertyDeclaration))
        {
            return true;
        }

        if (currentNode is TypeDeclarationSyntax { OpenBraceToken.IsMissing: true })
        {
            return true;
        }

        if (currentNode is MethodDeclarationSyntax method)
        {
            if (method.Modifiers.Any(SyntaxKind.AbstractKeyword) || method.Modifiers.Any(SyntaxKind.ExternKeyword) ||
                method.IsParentKind(SyntaxKind.InterfaceDeclaration))
            {
                return true;
            }

            if (method.Modifiers.Any(SyntaxKind.PartialKeyword) && method.Body is null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInConditionOfDoStatement(SyntaxNode currentNode, SnapshotPoint caret)
    {
        if (currentNode is not DoStatementSyntax doStatement)
        {
            return false;
        }

        var condition = doStatement.Condition;
        return (caret >= condition.Span.Start && caret <= condition.Span.End);
    }

    private static SemicolonBehavior MoveCaretToFinalPositionInStatement(bool speculative, SyntaxNode statementNode, TypeCharCommandArgs args, SnapshotPoint originalCaret, SnapshotPoint caret, bool isInsideDelimiters)
    {
        if (StatementClosingDelimiterIsMissing(statementNode))
        {
            // Don't complete statement.  Return without moving the caret.
            return SemicolonBehavior.None;
        }

        if (TryGetCaretPositionToMove(statementNode, caret, isInsideDelimiters, out var targetPosition)
            && targetPosition != originalCaret)
        {
            var overtypedExisting = AdjustPositionForExistingSemicolon(statementNode, ref targetPosition);

            // If this operation is speculative, return an indication that moving the caret is required, but don't
            // actually move it.
            if (!speculative)
            {
                Logger.Log(FunctionId.CommandHandler_CompleteStatement, KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    m[nameof(isInsideDelimiters)] = isInsideDelimiters;
                    m[nameof(statementNode)] = statementNode.Kind();
                }));

                if (!args.TextView.TryMoveCaretToAndEnsureVisible(targetPosition))
                    return SemicolonBehavior.None;
            }

            return overtypedExisting ? SemicolonBehavior.Overtype : SemicolonBehavior.Insert;
        }

        return SemicolonBehavior.None;
    }

    private static bool AdjustPositionForExistingSemicolon(SyntaxNode statementNode, ref SnapshotPoint targetPosition)
    {
        var existingSemicolon = statementNode.FindTokenOnRightOfPosition(targetPosition, includeSkipped: true);
        if (existingSemicolon.IsKind(SyntaxKind.SemicolonToken) && !existingSemicolon.IsMissing)
        {
            targetPosition = new SnapshotPoint(targetPosition.Snapshot, existingSemicolon.Span.End);
            return true;
        }

        return false;
    }

    private static bool TryGetCaretPositionToMove(SyntaxNode statementNode, SnapshotPoint caret, bool isInsideDelimiters, out SnapshotPoint targetPosition)
    {
        targetPosition = default;

        switch (statementNode.Kind())
        {
            case SyntaxKind.DoStatement:
                //  Move caret after the do statement's closing paren.
                targetPosition = caret.Snapshot.GetPoint(((DoStatementSyntax)statementNode).CloseParenToken.Span.End);
                return true;
            case SyntaxKind.ForStatement:
                // `For` statements can have semicolon after initializer/declaration or after condition.
                // If caret is in initialer/declaration or condition, AND is inside other delimiters, complete statement
                // Otherwise, return without moving the caret.
                return isInsideDelimiters && TryGetForStatementCaret(caret, (ForStatementSyntax)statementNode, out targetPosition);
            case SyntaxKind.ExpressionStatement:
            case SyntaxKind.GotoCaseStatement:
            case SyntaxKind.LocalDeclarationStatement:
            case SyntaxKind.ReturnStatement:
            case SyntaxKind.YieldReturnStatement:
            case SyntaxKind.ThrowStatement:
            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.DelegateDeclaration:
            case SyntaxKind.ArrowExpressionClause:
            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.EqualsValueClause:
            case SyntaxKind.RecordStructDeclaration:
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.InterfaceDeclaration:
                // These statement types end in a semicolon. 
                // if the original caret was inside any delimiters, `caret` will be after the outermost delimiter
                targetPosition = caret;
                return isInsideDelimiters;
            default:
                // For all other statement types, don't complete statement.  Return without moving the caret.
                return false;
        }
    }

    private static bool TryGetForStatementCaret(SnapshotPoint originalCaret, ForStatementSyntax forStatement, out SnapshotPoint forStatementCaret)
    {
        if (CaretIsInForStatementCondition(originalCaret, forStatement, out var condition))
        {
            forStatementCaret = GetCaretAtPosition(condition.Span.End);
        }
        else if (CaretIsInForStatementDeclaration(originalCaret, forStatement, out var declaration))
        {
            forStatementCaret = GetCaretAtPosition(declaration.Span.End);
        }
        else if (CaretIsInForStatementInitializers(originalCaret, forStatement, out var relocatedPosition))
        {
            forStatementCaret = GetCaretAtPosition(relocatedPosition);
        }
        else
        {
            // set caret to default, we will return false
            forStatementCaret = default;
        }

        return (forStatementCaret != default);

        // Locals
        SnapshotPoint GetCaretAtPosition(int position) => originalCaret.Snapshot.GetPoint(position);
    }

    private static bool CaretIsInForStatementCondition(int caretPosition, ForStatementSyntax forStatementSyntax, [NotNullWhen(true)] out ExpressionSyntax? condition)
    {
        condition = forStatementSyntax.Condition;
        if (condition == null)
            return false;

        // If condition is null and caret is in the condition section, as in `for ( ; $$; )`, 
        // we will have bailed earlier due to not being inside supported delimiters
        return caretPosition > condition.SpanStart && caretPosition <= condition.Span.End;
    }

    private static bool CaretIsInForStatementDeclaration(int caretPosition, ForStatementSyntax forStatementSyntax, [NotNullWhen(true)] out VariableDeclarationSyntax? declaration)
    {
        declaration = forStatementSyntax.Declaration;
        if (declaration == null)
            return false;

        return caretPosition > declaration.Span.Start && caretPosition <= declaration.Span.End;
    }

    private static bool CaretIsInForStatementInitializers(int caretPosition, ForStatementSyntax forStatementSyntax, out int relocatedPosition)
    {
        if (forStatementSyntax.Initializers.Count != 0
            && caretPosition > forStatementSyntax.Initializers.Span.Start
            && caretPosition <= forStatementSyntax.Initializers.Span.End)
        {
            // Move the caret to the first missing separator, or to the end of the initializers list if all
            // separators are present.
            for (var separatorIndex = 0; separatorIndex < forStatementSyntax.Initializers.SeparatorCount; separatorIndex++)
            {
                var separator = forStatementSyntax.Initializers.GetSeparator(separatorIndex);
                if (separator.IsMissing)
                {
                    // We can't rely on the position of the missing separator, so move to the end of the last
                    // initializer preceding the missing separator.
                    relocatedPosition = forStatementSyntax.Initializers[separatorIndex].Span.End;
                    return true;
                }
            }

            relocatedPosition = forStatementSyntax.Initializers.Span.End;
            return true;
        }

        relocatedPosition = default;
        return false;
    }

    private static bool IsInAStringOrCharacter(SyntaxNode currentNode, SnapshotPoint caret)
    {
        // Check to see if caret is before or after string
        if (currentNode.Kind() is not (SyntaxKind.InterpolatedStringExpression or SyntaxKind.StringLiteralExpression or SyntaxKind.Utf8StringLiteralExpression or SyntaxKind.CharacterLiteralExpression))
            return false;

        if (currentNode.IsKind(SyntaxKind.StringLiteralExpression, out LiteralExpressionSyntax? literalExpression)
            && literalExpression.Token.Text.StartsWith("@"))
        {
            // Verbatim strings start with @", so we only consider the caret to be inside the string if it's after the "
            if (caret.Position <= currentNode.SpanStart + 1)
                return false;
        }
        else if (caret.Position <= currentNode.SpanStart)
        {
            return false;
        }

        if (currentNode.IsKind(SyntaxKind.StringLiteralExpression, out literalExpression)
            && (literalExpression.Token.Text.Length == 1 || literalExpression.Token.Text[^1] != '"'))
        {
            // This is an unterminated string literal, so we count the end of the span as included in the string
            return caret.Position <= currentNode.Span.End;
        }
        else if (currentNode.IsKind(SyntaxKind.CharacterLiteralExpression, out literalExpression)
            && (literalExpression.Token.Text.Length == 1 || literalExpression.Token.Text[^1] != '\''))
        {
            // This is an unterminated character literal, so we count the end of the span as included in the character
            return caret.Position <= currentNode.Span.End;
        }
        else if (currentNode.IsKind(SyntaxKind.Utf8StringLiteralExpression))
        {
            // String literals are only considered utf-8 literals if they are terminated with a "u8 sequence. Only
            // consider the caret inside the string if it precedes the position of the " in this sequence.
            return caret.Position < currentNode.Span.End - 2;
        }

        return caret.Position < currentNode.Span.End;
    }

    /// <summary>
    /// Determines if a statement ends with a closing delimiter, and that closing delimiter exists.
    /// </summary>
    /// <remarks>
    /// <para>Statements such as <c>do { } while (expression);</c> contain embedded enclosing delimiters immediately
    /// preceding the semicolon. These delimiters are not part of the expression, but they behave like an argument
    /// list for the purposes of identifying relevant places for statement completion:</para>
    /// <list type="bullet">
    /// <item><description>The closing delimiter is typically inserted by the Automatic Brace Completion feature.</description></item>
    /// <item><description>It is not syntactically valid to place a semicolon <em>directly</em> within the delimiters.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="currentNode"></param>
    /// <returns><see langword="true"/> if <paramref name="currentNode"/> is a statement that ends with a closing
    /// delimiter, and that closing delimiter exists in the source code; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool StatementClosingDelimiterIsMissing(SyntaxNode currentNode)
    {
        switch (currentNode.Kind())
        {
            case SyntaxKind.DoStatement:
                var dostatement = (DoStatementSyntax)currentNode;
                return dostatement.CloseParenToken.IsMissing;
            case SyntaxKind.ForStatement:
                var forStatement = (ForStatementSyntax)currentNode;
                return forStatement.CloseParenToken.IsMissing;
            default:
                return false;
        }
    }

    /// <summary>
    /// Determines if a syntax node includes all required closing delimiters.
    /// </summary>
    /// <remarks>
    /// <para>Some syntax nodes, such as parenthesized expressions, require a matching closing delimiter to end the
    /// syntax node. If this node is omitted from the source code, the parser will automatically insert a zero-width
    /// "missing" closing delimiter token to produce a valid syntax tree. This method determines if required closing
    /// delimiters are present in the original source.</para>
    /// </remarks>
    /// <param name="currentNode"></param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description><see langword="true"/> if <paramref name="currentNode"/> requires a closing delimiter and the closing delimiter is present in the source (i.e. not missing)</description></item>
    /// <item><description><see langword="true"/> if <paramref name="currentNode"/> does not require a closing delimiter</description></item>
    /// <item><description>otherwise, <see langword="false"/>.</description></item>
    /// </list>
    /// </returns>
    private static bool RequiredDelimiterIsMissing(SyntaxNode currentNode)
    {
        if (currentNode.GetBrackets().closeBracket.IsMissing
            || currentNode.GetParentheses().closeParen.IsMissing)
        {
            return true;
        }

        // If the current node has braces, we also need to make sure parent constructs are not missing braces
        var braces = currentNode.GetBraces();
        if (braces.openBrace.IsKind(SyntaxKind.OpenBraceToken))
        {
            if (braces.closeBrace.IsMissing)
            {
                return true;
            }

            for (var node = currentNode.Parent; node is not null; node = node.Parent)
            {
                if (node.GetBraces().closeBrace.IsMissing)
                    return true;
            }
        }

        return false;
    }

    private static bool HasDelimitersButCaretIsOutside(SyntaxNode currentNode, int caretPosition)
    {
        if (currentNode.GetParentheses() is ((not SyntaxKind.None) openParenthesis, (not SyntaxKind.None) closeParenthesis))
        {
            return openParenthesis.SpanStart >= caretPosition || closeParenthesis.Span.End <= caretPosition;
        }
        else if (currentNode.GetBrackets() is ((not SyntaxKind.None) openBracket, (not SyntaxKind.None) closeBracket))
        {
            return openBracket.SpanStart >= caretPosition || closeBracket.Span.End <= caretPosition;
        }
        else if (currentNode.GetBraces() is ((not SyntaxKind.None) openBrace, (not SyntaxKind.None) closeBrace))
        {
            return openBrace.SpanStart >= caretPosition || closeBrace.Span.End <= caretPosition;
        }
        else
        {
            return false;
        }
    }
}
