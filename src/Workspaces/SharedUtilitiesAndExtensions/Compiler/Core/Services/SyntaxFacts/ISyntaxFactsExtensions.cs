// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal static class ISyntaxFactsExtensions
{
    private static readonly ObjectPool<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>> s_stackPool
        = SharedPools.Default<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>>();

    public static bool IsMemberInitializerNamedAssignmentIdentifier(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => syntaxFacts.IsMemberInitializerNamedAssignmentIdentifier(node, out _);

    public static bool IsOnSingleLine(this ISyntaxFacts syntaxFacts, SyntaxNode node, bool fullSpan)
    {
        // The stack logic assumes the initial node is not null
        Contract.ThrowIfNull(node);

        // Use an actual Stack so we can write out deeply recursive structures without overflowing.
        // Note: algorithm is taken from GreenNode.WriteTo.
        //
        // General approach is that we recurse down the nodes, using a real stack object to
        // keep track of what node we're on.  If full-span is true we'll examine all tokens
        // and all the trivia on each token.  If full-span is false we'll examine all tokens
        // but we'll ignore the leading trivia on the very first trivia and the trailing trivia
        // on the very last token.
        using var _ = s_stackPool.GetPooledObject(out var stack);
        stack.Push((node, leading: fullSpan, trailing: fullSpan));

        var result = IsOnSingleLine(syntaxFacts, stack);

        return result;
    }

    private static bool IsOnSingleLine(
        ISyntaxFacts syntaxFacts, Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)> stack)
    {
        while (stack.TryPop(out var tuple))
        {
            var (currentNodeOrToken, currentLeading, currentTrailing) = tuple;
            if (currentNodeOrToken.IsToken)
            {
                // If this token isn't on a single line, then the original node definitely
                // isn't on a single line.
                if (!IsOnSingleLine(syntaxFacts, currentNodeOrToken.AsToken(), currentLeading, currentTrailing))
                    return false;
            }
            else
            {
                var currentNode = currentNodeOrToken.AsNode()!;

                var childNodesAndTokens = currentNode.ChildNodesAndTokens();
                var childCount = childNodesAndTokens.Count;

                // Walk the children of this node in reverse, putting on the stack to process.
                // This way we process the children in the actual child-order they are in for
                // this node.
                var index = 0;
                foreach (var child in childNodesAndTokens.Reverse())
                {
                    // Since we're walking the children in reverse, if we're on hte 0th item,
                    // that's the last child.
                    var last = index == 0;

                    // Once we get all the way to the end of the reversed list, we're actually
                    // on the first.
                    var first = index == childCount - 1;

                    // We want the leading trivia if we've asked for it, or if we're not the first
                    // token being processed.  We want the trailing trivia if we've asked for it,
                    // or if we're not the last token being processed.
                    stack.Push((child, currentLeading | !first, currentTrailing | !last));
                    index++;
                }
            }
        }

        // All tokens were on a single line.  This node is on a single line.
        return true;
    }

    private static bool IsOnSingleLine(ISyntaxFacts syntaxFacts, SyntaxToken token, bool leading, bool trailing)
    {
        // If any of our trivia is not on a single line, then we're not on a single line.
        if (!IsOnSingleLine(syntaxFacts, token.LeadingTrivia, leading) ||
            !IsOnSingleLine(syntaxFacts, token.TrailingTrivia, trailing))
        {
            return false;
        }

        // Only string literals can span multiple lines.  Only need to check those.
        if (syntaxFacts.SyntaxKinds.StringLiteralToken == token.RawKind ||
            syntaxFacts.SyntaxKinds.InterpolatedStringTextToken == token.RawKind)
        {
            // This allocated.  But we only do it in the string case. For all other tokens
            // we don't need any allocations.
            if (!IsOnSingleLine(token.ToString()))
            {
                return false;
            }
        }

        // Any other type of token is on a single line.
        return true;
    }

    private static bool IsOnSingleLine(ISyntaxFacts syntaxFacts, SyntaxTriviaList triviaList, bool checkTrivia)
    {
        if (checkTrivia)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.HasStructure)
                {
                    // For structured trivia, we recurse into the trivia to see if it
                    // is on a single line or not.  If it isn't, then we're definitely
                    // not on a single line.
                    if (!IsOnSingleLine(syntaxFacts, trivia.GetStructure()!, fullSpan: true))
                    {
                        return false;
                    }
                }
                else if (syntaxFacts.IsEndOfLineTrivia(trivia))
                {
                    // Contained an end-of-line trivia.  Definitely not on a single line.
                    return false;
                }
                else if (!syntaxFacts.IsWhitespaceTrivia(trivia))
                {
                    // Was some other form of trivia (like a comment).  Easiest thing
                    // to do is just stringify this and count the number of newlines.
                    // these should be rare.  So the allocation here is ok.
                    if (!IsOnSingleLine(trivia.ToString()))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool IsOnSingleLine(string value)
        => value.GetNumberOfLineBreaks() == 0;

    public static bool ContainsInterleavedDirective(
        this ISyntaxFacts syntaxFacts, ImmutableArray<SyntaxNode> nodes, CancellationToken cancellationToken)
    {
        if (nodes is [var firstNode, ..] and [.., var lastNode])
        {
            var span = TextSpan.FromBounds(firstNode.Span.Start, lastNode.Span.End);

            foreach (var node in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ContainsInterleavedDirective(syntaxFacts, span, node, cancellationToken))
                    return true;
            }
        }

        return false;
    }

    public static bool ContainsInterleavedDirective(this ISyntaxFacts syntaxFacts, SyntaxNode node, CancellationToken cancellationToken)
        => ContainsInterleavedDirective(syntaxFacts, node.Span, node, cancellationToken);

    public static bool ContainsInterleavedDirective(
        this ISyntaxFacts syntaxFacts, TextSpan span, SyntaxNode node, CancellationToken cancellationToken)
    {
        foreach (var token in node.DescendantTokens())
        {
            if (syntaxFacts.ContainsInterleavedDirective(span, token, cancellationToken))
                return true;
        }

        return false;
    }

    public static bool SpansPreprocessorDirective(this ISyntaxFacts syntaxFacts, IEnumerable<SyntaxNode> nodes)
    {
        if (nodes == null || nodes.IsEmpty())
        {
            return false;
        }

        return SpansPreprocessorDirective(syntaxFacts, nodes.SelectMany(n => n.DescendantTokens()));
    }

    /// <summary>
    /// Determines if there is preprocessor trivia *between* any of the <paramref name="tokens"/>
    /// provided.  The <paramref name="tokens"/> will be deduped and then ordered by position.
    /// Specifically, the first token will not have it's leading trivia checked, and the last
    /// token will not have it's trailing trivia checked.  All other trivia will be checked to
    /// see if it contains a preprocessor directive.
    /// </summary>
    public static bool SpansPreprocessorDirective(this ISyntaxFacts syntaxFacts, IEnumerable<SyntaxToken> tokens)
    {
        // we want to check all leading trivia of all tokens (except the 
        // first one), and all trailing trivia of all tokens (except the
        // last one).

        var first = true;
        var previousToken = default(SyntaxToken);

        // Allow duplicate nodes/tokens to be passed in.  Also, allow the nodes/tokens
        // to not be in any particular order when passed in.
        var orderedTokens = tokens.Distinct().OrderBy(t => t.SpanStart);

        foreach (var token in orderedTokens)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                // check the leading trivia of this token, and the trailing trivia
                // of the previous token.
                if (SpansPreprocessorDirective(syntaxFacts, token.LeadingTrivia) ||
                    SpansPreprocessorDirective(syntaxFacts, previousToken.TrailingTrivia))
                {
                    return true;
                }
            }

            previousToken = token;
        }

        return false;
    }

    private static bool SpansPreprocessorDirective(this ISyntaxFacts syntaxFacts, SyntaxTriviaList list)
        => list.Any(syntaxFacts.IsPreprocessorDirective);

    public static bool IsLegalIdentifier(this ISyntaxFacts syntaxFacts, string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        if (!syntaxFacts.IsIdentifierStartCharacter(name[0]))
        {
            return false;
        }

        for (var i = 1; i < name.Length; i++)
        {
            if (!syntaxFacts.IsIdentifierPartCharacter(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsReservedOrContextualKeyword(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => syntaxFacts.IsReservedKeyword(token) || syntaxFacts.IsContextualKeyword(token);

    public static bool IsWord(this ISyntaxFacts syntaxFacts, SyntaxToken token)
    {
        return syntaxFacts.IsIdentifier(token)
            || syntaxFacts.IsReservedOrContextualKeyword(token)
            || syntaxFacts.IsPreprocessorKeyword(token);
    }

    public static bool IsRegularOrDocumentationComment(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => syntaxFacts.IsRegularComment(trivia) || syntaxFacts.IsDocumentationComment(trivia);

    [return: NotNullIfNotNull(nameof(node))]
    public static SyntaxNode? WalkDownParentheses(this ISyntaxFacts syntaxFacts, SyntaxNode? node)
    {
        while (syntaxFacts.IsParenthesizedExpression(node))
        {
            syntaxFacts.GetPartsOfParenthesizedExpression(node, out _, out var child, out _);
            node = child;
        }

        return node;
    }

    [return: NotNullIfNotNull(nameof(node))]
    public static SyntaxNode? WalkUpParentheses(this ISyntaxFacts syntaxFacts, SyntaxNode? node)
    {
        while (syntaxFacts.IsParenthesizedExpression(node?.Parent))
            node = node.Parent;

        return node;
    }

    public static void GetPartsOfAssignmentStatement(
        this ISyntaxFacts syntaxFacts, SyntaxNode statement,
        out SyntaxNode left, out SyntaxNode right)
    {
        syntaxFacts.GetPartsOfAssignmentStatement(statement, out left, out _, out right);
    }

    public static SyntaxNode GetExpressionOfInvocationExpression(
        this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfInvocationExpression(node, out var expression, out _);
        return expression;
    }

    public static SyntaxNode Unparenthesize(
        this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        SyntaxToken openParenToken;
        SyntaxNode operand;
        SyntaxToken closeParenToken;

        if (syntaxFacts.IsParenthesizedPattern(node))
        {
            syntaxFacts.GetPartsOfParenthesizedPattern(node,
                out openParenToken, out operand, out closeParenToken);
        }
        else
        {
            syntaxFacts.GetPartsOfParenthesizedExpression(node,
                out openParenToken, out operand, out closeParenToken);
        }

        var leadingTrivia = openParenToken.LeadingTrivia
            .Concat(openParenToken.TrailingTrivia)
            .Where(t => !syntaxFacts.IsElastic(t))
            .Concat(operand.GetLeadingTrivia());

        var trailingTrivia = operand.GetTrailingTrivia()
            .Concat(closeParenToken.LeadingTrivia)
            .Where(t => !syntaxFacts.IsElastic(t))
            .Concat(closeParenToken.TrailingTrivia);

        var resultNode = operand
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(trailingTrivia);

        // If there's no trivia between the original node and the tokens around it, then add
        // elastic markers so the formatting engine will spaces if necessary to keep things
        // parseable.
        if (resultNode.GetLeadingTrivia().Count == 0)
        {
            var previousToken = node.GetFirstToken().GetPreviousToken();
            if (previousToken.TrailingTrivia.Count == 0 &&
                syntaxFacts.IsWordOrNumber(previousToken) &&
                syntaxFacts.IsWordOrNumber(resultNode.GetFirstToken()))
            {
                resultNode = resultNode.WithPrependedLeadingTrivia(syntaxFacts.ElasticMarker);
            }
        }

        if (resultNode.GetTrailingTrivia().Count == 0)
        {
            var nextToken = node.GetLastToken().GetNextToken();
            if (nextToken.LeadingTrivia.Count == 0 &&
                syntaxFacts.IsWordOrNumber(nextToken) &&
                syntaxFacts.IsWordOrNumber(resultNode.GetLastToken()))
            {
                resultNode = resultNode.WithAppendedTrailingTrivia(syntaxFacts.ElasticMarker);
            }
        }

        return resultNode;
    }

    private static bool IsWordOrNumber(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => syntaxFacts.IsWord(token) || syntaxFacts.IsNumericLiteral(token);

    public static bool SpansPreprocessorDirective(this ISyntaxFacts service, SyntaxNode node)
        => service.SpansPreprocessorDirective([node]);

    public static bool SpansPreprocessorDirective(this ISyntaxFacts service, params SyntaxNode[] nodes)
        => service.SpansPreprocessorDirective((IEnumerable<SyntaxNode>)nodes);

    public static bool IsWhitespaceOrEndOfLineTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => syntaxFacts.IsWhitespaceTrivia(trivia) || syntaxFacts.IsEndOfLineTrivia(trivia);

    public static void GetPartsOfBinaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node, out SyntaxNode left, out SyntaxNode right)
        => syntaxFacts.GetPartsOfBinaryExpression(node, out left, out _, out right);

    public static SyntaxNode GetPatternOfParenthesizedPattern(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfParenthesizedPattern(node, out _, out var pattern, out _);
        return pattern;
    }

    public static SyntaxToken GetOperatorTokenOfBinaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfBinaryExpression(node, out _, out var token, out _);
        return token;
    }

    public static bool IsAnonymousOrLocalFunction(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        => syntaxFacts.IsAnonymousFunctionExpression(node) ||
           syntaxFacts.IsLocalFunctionStatement(node);

    public static SyntaxNode? GetExpressionOfElementAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out _);
        return expression;
    }

    public static SyntaxNode? GetArgumentListOfElementAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfElementAccessExpression(node, out _, out var argumentList);
        return argumentList;
    }

    public static SyntaxNode GetExpressionOfConditionalAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfConditionalAccessExpression(node, out var expression, out _);
        return expression;
    }

    public static SyntaxToken GetOperatorTokenOfMemberAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfMemberAccessExpression(node, out _, out var operatorToken, out _);
        return operatorToken;
    }

    public static void GetPartsOfMemberAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node, out SyntaxNode expression, out SyntaxNode name)
        => syntaxFacts.GetPartsOfMemberAccessExpression(node, out expression, out _, out name);

    public static void GetPartsOfConditionalAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node, out SyntaxNode expression, out SyntaxNode whenNotNull)
        => syntaxFacts.GetPartsOfConditionalAccessExpression(node, out expression, out _, out whenNotNull);

    public static TextSpan GetSpanWithoutAttributes(this ISyntaxFacts syntaxFacts, SyntaxNode root, SyntaxNode node)
    {
        // Span without AttributeLists
        // - No AttributeLists -> original .Span
        // - Some AttributeLists -> (first non-trivia/comment Token.Span.Begin, original.Span.End)
        //   - We need to be mindful about comments due to:
        //      // [Test1]
        //      //Comment1
        //      [||]object Property1 { get; set; }
        //     the comment node being part of the next token's (`object`) leading trivia and not the AttributeList's node.
        // - In case only attribute is written we need to be careful to not to use next (unrelated) token as beginning current the node.
        var attributeList = syntaxFacts.GetAttributeLists(node);
        if (attributeList.Any())
        {
            var endOfAttributeLists = attributeList.Last().Span.End;
            var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

            var endOfNode = node.Span.End;
            var startOfNodeWithoutAttributes = Math.Min(afterAttributesToken.Span.Start, endOfNode);

            return TextSpan.FromBounds(startOfNodeWithoutAttributes, endOfNode);
        }

        return node.Span;
    }

    /// <summary>
    /// Similar to <see cref="ISyntaxFacts.GetStandaloneExpression(SyntaxNode)"/>, this gets the containing
    /// expression that is actually a language expression and not just typed as an ExpressionSyntax for convenience.
    /// However, this goes beyond that that method in that if this expression is the RHS of a conditional access
    /// (i.e. <c>a?.b()</c>) it will also return the root of the conditional access expression tree.
    /// <para/> The intuition here is that this will give the topmost expression node that could realistically be
    /// replaced with any other expression.  For example, with <c>a?.b()</c> technically <c>.b()</c> is an
    /// expression.  But that cannot be replaced with something like <c>(1 + 1)</c> (as <c>a?.(1 + 1)</c> is not
    /// legal).  However, in <c>a?.b()</c>, then <c>a</c> itself could be replaced with <c>(1 + 1)?.b()</c> to form
    /// a legal expression.
    /// </summary>
    public static SyntaxNode GetRootStandaloneExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        // First, make sure we're on a construct the language things is a standalone expression.
        var standalone = syntaxFacts.GetStandaloneExpression(node);

        // Then, if this is the RHS of a `?`, walk up to the top of that tree to get the final standalone expression.
        return syntaxFacts.GetRootConditionalAccessExpression(standalone) ?? standalone;
    }

    #region GetXXXOfYYY Members

    public static SyntaxNode? GetArgumentListOfInvocationExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfInvocationExpression(node, out _, out var argumentList);
        return argumentList;
    }

    public static SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        var argumentList = syntaxFacts.GetArgumentListOfInvocationExpression(node);
        return argumentList is null ? default : syntaxFacts.GetArgumentsOfArgumentList(argumentList);
    }

    public static SyntaxNode? GetArgumentListOfBaseObjectCreationExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfBaseObjectCreationExpression(node, out var argumentList, out _);
        return argumentList;
    }

    public static SyntaxNode? GetDefaultOfParameter(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfParameter(node, out _, out var @default);
        return @default;
    }

    public static SyntaxNode GetExpressionOfParenthesizedExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfParenthesizedExpression(node, out _, out var expression, out _);
        return expression;
    }

    public static SyntaxToken GetIdentifierOfGenericName(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfGenericName(node, out var identifier, out _);
        return identifier;
    }

    public static SyntaxToken GetIdentifierOfIdentifierName(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        => syntaxFacts.GetIdentifierOfSimpleName(node);

    public static SyntaxToken GetIdentifierOfParameter(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfParameter(node, out var identifier, out _);
        return identifier;
    }

    public static SyntaxList<SyntaxNode> GetImportsOfBaseNamespaceDeclaration(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfBaseNamespaceDeclaration(node, out _, out var imports, out _);
        return imports;
    }

    public static SyntaxList<SyntaxNode> GetImportsOfCompilationUnit(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfCompilationUnit(node, out var imports, out _, out _);
        return imports;
    }

    public static SyntaxNode? GetInitializerOfBaseObjectCreationExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfBaseObjectCreationExpression(node, out _, out var initializer);
        return initializer;
    }

    public static SyntaxList<SyntaxNode> GetMembersOfBaseNamespaceDeclaration(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfBaseNamespaceDeclaration(node, out _, out _, out var members);
        return members;
    }

    public static SyntaxList<SyntaxNode> GetMembersOfCompilationUnit(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfCompilationUnit(node, out _, out _, out var members);
        return members;
    }

    public static SyntaxNode GetNameOfAttribute(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfAttribute(node, out var name, out _);
        return name;
    }

    public static SyntaxNode GetNameOfBaseNamespaceDeclaration(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfBaseNamespaceDeclaration(node, out var name, out _, out _);
        return name;
    }

    public static SyntaxNode GetNameOfMemberAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfMemberAccessExpression(node, out _, out var name);
        return name;
    }

    public static SyntaxNode GetOperandOfPostfixUnaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfPostfixUnaryExpression(node, out var operand, out _);
        return operand;
    }

    public static SyntaxNode GetOperandOfPrefixUnaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfPrefixUnaryExpression(node, out _, out var operand);
        return operand;
    }

    public static SyntaxToken GetOperatorTokenOfPrefixUnaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfPrefixUnaryExpression(node, out var operatorToken, out _);
        return operatorToken;
    }

    public static SeparatedSyntaxList<SyntaxNode> GetTypeArgumentsOfGenericName(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfGenericName(node, out _, out var typeArguments);
        return typeArguments;
    }

    public static SyntaxNode GetTypeOfObjectCreationExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
    {
        syntaxFacts.GetPartsOfObjectCreationExpression(node, out _, out var type, out _, out _);
        return type;
    }

    #endregion

    #region IsXXXOfYYY members

    public static bool IsExpressionOfAwaitExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        var parent = node?.Parent;
        if (!syntaxFacts.IsAwaitExpression(parent))
            return false;

        return node == syntaxFacts.GetExpressionOfAwaitExpression(parent);
    }

    public static bool IsExpressionOfInvocationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        var parent = node?.Parent;
        if (!syntaxFacts.IsInvocationExpression(parent))
            return false;

        syntaxFacts.GetPartsOfInvocationExpression(parent, out var expression, out _);
        return node == expression;
    }

    public static bool IsExpressionOfMemberAccessExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        var parent = node?.Parent;
        if (!syntaxFacts.IsMemberAccessExpression(parent))
            return false;

        syntaxFacts.GetPartsOfMemberAccessExpression(parent, out var expression, out _);
        return node == expression;
    }

    public static bool IsNameOfAttribute(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        if (!syntaxFacts.IsAttribute(node?.Parent))
            return false;

        syntaxFacts.GetPartsOfAttribute(node.Parent, out var name, out _);
        return name == node;
    }

    public static bool IsRightOfQualifiedName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        var parent = node?.Parent;
        if (!syntaxFacts.IsQualifiedName(parent))
            return false;

        syntaxFacts.GetPartsOfQualifiedName(parent, out _, out _, out var right);
        return node == right;
    }

    public static bool IsRightOfAliasQualifiedName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        var parent = node?.Parent;
        if (!syntaxFacts.IsAliasQualifiedName(parent))
            return false;

        syntaxFacts.GetPartsOfAliasQualifiedName(parent, out _, out _, out var right);
        return node == right;
    }

    public static bool IsTypeOfObjectCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
    {
        var parent = node?.Parent;
        if (!syntaxFacts.IsObjectCreationExpression(parent))
            return false;

        syntaxFacts.GetPartsOfObjectCreationExpression(parent, out _, out var type, out _, out _);
        return type == node;
    }

    #endregion

    #region ISyntaxKinds forwarding methods

    #region trivia

    public static bool IsEndOfLineTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.EndOfLineTrivia;

    public static bool IsMultiLineCommentTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.MultiLineCommentTrivia;

    public static bool IsMultiLineDocCommentTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.MultiLineDocCommentTrivia;

    public static bool IsShebangDirectiveTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.ShebangDirectiveTrivia;

    public static bool IsSingleLineCommentTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.SingleLineCommentTrivia;

    public static bool IsSingleLineDocCommentTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.SingleLineDocCommentTrivia;

    public static bool IsWhitespaceTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
        => trivia.RawKind == syntaxFacts.SyntaxKinds.WhitespaceTrivia;

    public static bool IsSkippedTokensTrivia(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.SkippedTokensTrivia;

    #endregion

    #region keywords

    public static bool IsAwaitKeyword(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.AwaitKeyword;

    public static bool IsGlobalNamespaceKeyword(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.GlobalKeyword;

    #endregion

    #region literal tokens

    public static bool IsCharacterLiteral(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.CharacterLiteralToken;

    public static bool IsStringLiteral(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.StringLiteralToken;

    #endregion

    #region tokens

    public static bool IsIdentifier(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.IdentifierToken;

    public static bool IsHashToken(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.HashToken;

    public static bool IsInterpolatedStringTextToken(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.RawKind == syntaxFacts.SyntaxKinds.InterpolatedStringTextToken;

    #endregion

    #region names

    public static bool IsAliasQualifiedName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.AliasQualifiedName;

    public static bool IsGenericName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.GenericName;

    public static bool IsIdentifierName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IdentifierName;

    public static bool IsQualifiedName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.QualifiedName;

    #endregion

    #region types

    public static bool IsTupleType(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.TupleType;

    #endregion

    #region literal expressions

    public static bool IsCharacterLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.CharacterLiteralExpression;

    public static bool IsDefaultLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.DefaultLiteralExpression;

    public static bool IsFalseLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.FalseLiteralExpression;

    public static bool IsNumericLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.NumericLiteralExpression;

    public static bool IsNullLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.NullLiteralExpression;

    public static bool IsStringLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.StringLiteralExpression;

    public static bool IsTrueLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.TrueLiteralExpression;

    #endregion

    #region expressions

    public static bool IsArrayCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ArrayCreationExpression;

    public static bool IsAwaitExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.AwaitExpression;

    public static bool IsBaseExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.BaseExpression;

    public static bool IsConditionalExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ConditionalExpression;

    public static bool IsConditionalAccessExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ConditionalAccessExpression;

    public static bool IsFieldExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.FieldExpression;

    public static bool IsImplicitArrayCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ImplicitArrayCreationExpression;

    public static bool IsImplicitObjectCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ImplicitObjectCreationExpression;

    public static bool IsIndexExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IndexExpression;

    public static bool IsInterpolatedStringExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.InterpolatedStringExpression;

    public static bool IsInterpolation(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.Interpolation;

    public static bool IsInterpolatedStringText(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.InterpolatedStringText;

    public static bool IsInvocationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.InvocationExpression;

    public static bool IsIsTypeExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IsTypeExpression;

    public static bool IsIsNotTypeExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IsNotTypeExpression;

    public static bool IsIsPatternExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IsPatternExpression;

    public static bool IsLogicalAndExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.LogicalAndExpression;

    public static bool IsLogicalOrExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.LogicalOrExpression;

    public static bool IsLogicalNotExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.LogicalNotExpression;

    public static bool IsObjectCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ObjectCreationExpression;

    public static bool IsParenthesizedExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ParenthesizedExpression;

    public static bool IsQueryExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.QueryExpression;

    public static bool IsRangeExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.RangeExpression;

    public static bool IsRefExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.RefExpression;

    public static bool IsSimpleMemberAccessExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.SimpleMemberAccessExpression;

    public static bool IsSuppressNullableWarningExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.SuppressNullableWarningExpression;

    public static bool IsThisExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ThisExpression;

    public static bool IsThrowExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ThrowExpression;

    public static bool IsTupleExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.TupleExpression;

    public static bool ContainsGlobalStatement(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        => node.ChildNodes().Any(c => c.RawKind == syntaxFacts.SyntaxKinds.GlobalStatement);

    #endregion

    #region pattern

    public static bool IsAndPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.AndPattern;

    public static bool IsConstantPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ConstantPattern;

    public static bool IsDeclarationPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.DeclarationPattern;

    public static bool IsListPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ListPattern;

    public static bool IsNotPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.NotPattern;

    public static bool IsOrPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.OrPattern;

    public static bool IsParenthesizedPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ParenthesizedPattern;

    public static bool IsRecursivePattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.RecursivePattern;

    public static bool IsRelationalPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.RelationalPattern;

    public static bool IsTypePattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.TypePattern;

    public static bool IsVarPattern(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.VarPattern;

    #endregion

    #region statements

    public static bool IsExpressionStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ExpressionStatement;

    public static bool IsForEachStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ForEachStatement;

    public static bool IsForStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ForStatement;

    public static bool IsIfStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IfStatement;

    public static bool IsLocalDeclarationStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.LocalDeclarationStatement;

    public static bool IsLocalFunctionStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.LocalFunctionStatement;

    public static bool IsLockStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.LockStatement;

    public static bool IsReturnStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ReturnStatement;

    public static bool IsThrowStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ThrowStatement;

    public static bool IsUsingStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.UsingStatement;

    public static bool IsWhileStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.WhileStatement;

    public static bool IsYieldReturnStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.YieldReturnStatement;

    #endregion

    #region members/declarations

    public static bool IsAttribute(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.Attribute;

    public static bool IsClassDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ClassDeclaration;

    public static bool IsCollectionExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.CollectionExpression;

    public static bool IsConstructorDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ConstructorDeclaration;

    public static bool IsEnumDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.EnumDeclaration;

    public static bool IsGlobalAttribute(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => syntaxFacts.IsGlobalAssemblyAttribute(node) || syntaxFacts.IsGlobalModuleAttribute(node);

    public static bool IsInterfaceDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.InterfaceDeclaration;

    public static bool IsParameter(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.Parameter;

    public static bool IsTypeConstraint(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.TypeConstraint;

    public static bool IsVariableDeclarator(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.VariableDeclarator;

    public static bool IsFieldDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.FieldDeclaration;

    public static bool IsPropertyDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.PropertyDeclaration;

    public static bool IsStructDeclaration(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.StructDeclaration;

    public static bool IsTypeArgumentList(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.TypeArgumentList;

    #endregion

    #region clauses

    public static bool IsElseClause(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ElseClause;
    public static bool IsEqualsValueClause(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.EqualsValueClause;

    #endregion

    #region other

    public static bool IsExpressionElement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ExpressionElement;

    public static bool IsImplicitElementAccess(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.ImplicitElementAccess;

    public static bool IsIndexerMemberCref(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.IndexerMemberCref;

    public static bool IsPrimaryConstructorBaseType(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
        => node != null && node.RawKind == syntaxFacts.SyntaxKinds.PrimaryConstructorBaseType;

    #endregion

    #endregion
}
