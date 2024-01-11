// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class SyntaxNodeExtensions
{
    public static LanguageVersion GetLanguageVersion(this SyntaxNode node)
        => ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion;

    public static void Deconstruct(this SyntaxNode node, out SyntaxKind kind)
        => kind = node.Kind();

    public static bool IsKind<TNode>([NotNullWhen(true)] this SyntaxNode? node, SyntaxKind kind, [NotNullWhen(true)] out TNode? result)
        where TNode : SyntaxNode
    {
        if (node.IsKind(kind))
        {
            result = (TNode)node;
            return true;
        }

        result = null;
        return false;
    }

    public static bool IsParentKind([NotNullWhen(true)] this SyntaxNode? node, SyntaxKind kind)
        => CodeAnalysis.CSharpExtensions.IsKind(node?.Parent, kind);

    public static bool IsParentKind<TNode>([NotNullWhen(true)] this SyntaxNode? node, SyntaxKind kind, [NotNullWhen(true)] out TNode? result)
        where TNode : SyntaxNode
    {
        if (node.IsParentKind(kind))
        {
            result = (TNode)node.Parent!;
            return true;
        }

        result = null;
        return false;
    }

    public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(
        this SyntaxNode node, SourceText? sourceText = null,
        bool includePreviousTokenTrailingTriviaOnlyIfOnSameLine = false)
        => node.GetFirstToken().GetAllPrecedingTriviaToPreviousToken(
            sourceText, includePreviousTokenTrailingTriviaOnlyIfOnSameLine);

    /// <summary>
    /// Returns all of the trivia to the left of this token up to the previous token (concatenates
    /// the previous token's trailing trivia and this token's leading trivia).
    /// </summary>
    public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(
        this SyntaxToken token, SourceText? sourceText = null,
        bool includePreviousTokenTrailingTriviaOnlyIfOnSameLine = false)
    {
        var prevToken = token.GetPreviousToken(includeSkipped: true);
        if (prevToken.Kind() == SyntaxKind.None)
        {
            return token.LeadingTrivia;
        }

        Contract.ThrowIfTrue(sourceText == null && includePreviousTokenTrailingTriviaOnlyIfOnSameLine, "If we are including previous token trailing trivia, we need the text too.");
        if (includePreviousTokenTrailingTriviaOnlyIfOnSameLine &&
            !sourceText!.AreOnSameLine(prevToken, token))
        {
            return token.LeadingTrivia;
        }

        return prevToken.TrailingTrivia.Concat(token.LeadingTrivia);
    }

    public static bool IsAnyArgumentList([NotNullWhen(true)] this SyntaxNode? node)
    {
        return node?.Kind()
            is SyntaxKind.ArgumentList
            or SyntaxKind.AttributeArgumentList
            or SyntaxKind.BracketedArgumentList
            or SyntaxKind.TypeArgumentList;
    }

    public static (SyntaxToken openBrace, SyntaxToken closeBrace) GetBraces(this SyntaxNode? node)
        => node switch
        {
            NamespaceDeclarationSyntax namespaceNode => (namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken),
            BaseTypeDeclarationSyntax baseTypeNode => (baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken),
            AccessorListSyntax accessorListNode => (accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken),
            BlockSyntax blockNode => (blockNode.OpenBraceToken, blockNode.CloseBraceToken),
            SwitchStatementSyntax switchStatementNode => (switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken),
            AnonymousObjectCreationExpressionSyntax anonymousObjectCreationExpression => (anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken),
            InitializerExpressionSyntax initializeExpressionNode => (initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken),
            SwitchExpressionSyntax switchExpression => (switchExpression.OpenBraceToken, switchExpression.CloseBraceToken),
            PropertyPatternClauseSyntax property => (property.OpenBraceToken, property.CloseBraceToken),
            WithExpressionSyntax withExpr => (withExpr.Initializer.OpenBraceToken, withExpr.Initializer.CloseBraceToken),
            ImplicitObjectCreationExpressionSyntax { Initializer: { } initializer } => (initializer.OpenBraceToken, initializer.CloseBraceToken),
            _ => default,
        };

    public static bool IsEmbeddedStatementOwner([NotNullWhen(true)] this SyntaxNode? node)
    {
        return node is DoStatementSyntax or
               ElseClauseSyntax or
               FixedStatementSyntax or
               CommonForEachStatementSyntax or
               ForStatementSyntax or
               IfStatementSyntax or
               LabeledStatementSyntax or
               LockStatementSyntax or
               UsingStatementSyntax or
               WhileStatementSyntax;
    }

    public static StatementSyntax? GetEmbeddedStatement(this SyntaxNode? node)
        => node switch
        {
            DoStatementSyntax n => n.Statement,
            ElseClauseSyntax n => n.Statement,
            FixedStatementSyntax n => n.Statement,
            CommonForEachStatementSyntax n => n.Statement,
            ForStatementSyntax n => n.Statement,
            IfStatementSyntax n => n.Statement,
            LabeledStatementSyntax n => n.Statement,
            LockStatementSyntax n => n.Statement,
            UsingStatementSyntax n => n.Statement,
            WhileStatementSyntax n => n.Statement,
            _ => null,
        };

    public static BaseParameterListSyntax? GetParameterList(this SyntaxNode? declaration)
        => declaration switch
        {
            DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.ParameterList,
            BaseMethodDeclarationSyntax methodDeclaration => methodDeclaration.ParameterList,
            IndexerDeclarationSyntax indexerDeclaration => indexerDeclaration.ParameterList,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList,
            LocalFunctionStatementSyntax localFunction => localFunction.ParameterList,
            AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.ParameterList,
            TypeDeclarationSyntax typeDeclaration => typeDeclaration.ParameterList,
            _ => null,
        };

    public static SyntaxList<AttributeListSyntax> GetAttributeLists(this SyntaxNode? declaration)
        => declaration switch
        {
            MemberDeclarationSyntax memberDecl => memberDecl.AttributeLists,
            AccessorDeclarationSyntax accessor => accessor.AttributeLists,
            ParameterSyntax parameter => parameter.AttributeLists,
            CompilationUnitSyntax compilationUnit => compilationUnit.AttributeLists,
            StatementSyntax statementSyntax => statementSyntax.AttributeLists,
            TypeParameterSyntax typeParameter => typeParameter.AttributeLists,
            LambdaExpressionSyntax lambdaExpressionSyntax => lambdaExpressionSyntax.AttributeLists,
            _ => default,
        };

    public static ConditionalAccessExpressionSyntax? GetParentConditionalAccessExpression(this SyntaxNode? node)
    {
        // Walk upwards based on the grammar/parser rules around ?. expressions (can be seen in
        // LanguageParser.ParseConsequenceSyntax).

        // These are the parts of the expression that the ?... expression can end with.  Specifically:
        //
        //  1.      x?.y.M()            // invocation
        //  2.      x?.y[...];          // element access
        //  3.      x?.y.z              // member access
        //  4.      x?.y                // member binding
        //  5.      x?[y]               // element binding
        var current = node;

        if ((current?.Parent is MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) memberAccess && memberAccess.Name == current) ||
            (current?.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == current))
        {
            current = current.Parent;
        }

        // Effectively, if we're on the RHS of the ? we have to walk up the RHS spine first until we hit the first
        // conditional access.

        while (current is (kind:
            SyntaxKind.InvocationExpression or
            SyntaxKind.ElementAccessExpression or
            SyntaxKind.SimpleMemberAccessExpression or
            SyntaxKind.MemberBindingExpression or
            SyntaxKind.ElementBindingExpression or
            // Optional exclamations might follow the conditional operation. For example: a.b?.$$c!!!!()
            SyntaxKind.SuppressNullableWarningExpression) &&
            current.Parent is not ConditionalAccessExpressionSyntax)
        {
            current = current.Parent;
        }

        // Two cases we have to care about:
        //
        //      1. a?.b.$$c.d        and
        //      2. a?.b.$$c.d?.e...
        //
        // Note that `a?.b.$$c.d?.e.f?.g.h.i` falls into the same bucket as two.  i.e. the parts after `.e` are
        // lower in the tree and are not seen as we walk upwards.
        //
        //
        // To get the root ?. (the one after the `a`) we have to potentially consume the first ?. on the RHS of the
        // right spine (i.e. the one after `d`).  Once we do this, we then see if that itself is on the RHS of a
        // another conditional, and if so we hten return the one on the left.  i.e. for '2' this goes in this direction:
        //
        //      a?.b.$$c.d?.e           // it will do:
        //           ----->
        //       <---------
        //
        // Note that this only one CAE consumption on both sides.  GetRootConditionalAccessExpression can be used to
        // get the root parent in a case like:
        //
        //      x?.y?.z?.a?.b.$$c.d?.e.f?.g.h.i         // it will do:
        //                    ----->
        //                <---------
        //             <---
        //          <---
        //       <---

        if (current?.Parent is ConditionalAccessExpressionSyntax conditional1 &&
            conditional1.Expression == current)
        {
            current = conditional1;
        }

        if (current?.Parent is ConditionalAccessExpressionSyntax conditional2 &&
            conditional2.WhenNotNull == current)
        {
            current = conditional2;
        }

        return current as ConditionalAccessExpressionSyntax;
    }

    /// <summary>
    /// <inheritdoc cref="ISyntaxFacts.GetRootConditionalAccessExpression(SyntaxNode)"/>
    /// </summary>>
    public static ConditionalAccessExpressionSyntax? GetRootConditionalAccessExpression(this SyntaxNode? node)
    {
        // Once we've walked up the entire RHS, now we continually walk up the conditional accesses until we're at
        // the root. For example, if we have `a?.b` and we're on the `.b`, this will give `a?.b`.  Similarly with
        // `a?.b?.c` if we're on either `.b` or `.c` this will result in `a?.b?.c` (i.e. the root of this CAE
        // sequence).

        var current = node.GetParentConditionalAccessExpression();
        while (current?.Parent is ConditionalAccessExpressionSyntax conditional &&
            conditional.WhenNotNull == current)
        {
            current = conditional;
        }

        return current;
    }

    public static ConditionalAccessExpressionSyntax? GetInnerMostConditionalAccessExpression(this SyntaxNode node)
    {
        if (node is not ConditionalAccessExpressionSyntax result)
            return null;

        while (result.WhenNotNull is ConditionalAccessExpressionSyntax syntax)
            result = syntax;

        return result;
    }

    public static bool IsAsyncSupportingFunctionSyntax([NotNullWhen(true)] this SyntaxNode? node)
        => node is MethodDeclarationSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

    public static bool IsCompoundAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
        => node is AssignmentExpressionSyntax(kind: not SyntaxKind.SimpleAssignmentExpression);

    public static bool IsLeftSideOfAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
        => node?.Parent is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) assignment &&
           assignment.Left == node;

    public static bool IsLeftSideOfAnyAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
        => node?.Parent is AssignmentExpressionSyntax assignment && assignment.Left == node;

    public static bool IsRightSideOfAnyAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
        => node?.Parent is AssignmentExpressionSyntax assignment && assignment.Right == node;

    public static bool IsLeftSideOfCompoundAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
    {
        return node?.Parent != null &&
            node.Parent.IsCompoundAssignExpression() &&
            ((AssignmentExpressionSyntax)node.Parent).Left == node;
    }

    /// <summary>
    /// Returns the list of using directives that affect <paramref name="node"/>. The list will be returned in
    /// top down order.  
    /// </summary>
    public static IEnumerable<UsingDirectiveSyntax> GetEnclosingUsingDirectives(this SyntaxNode node)
    {
        return node.GetAncestorOrThis<CompilationUnitSyntax>()!.Usings
                   .Concat(node.GetAncestorsOrThis<BaseNamespaceDeclarationSyntax>()
                               .Reverse()
                               .SelectMany(n => n.Usings));
    }

    public static IEnumerable<ExternAliasDirectiveSyntax> GetEnclosingExternAliasDirectives(this SyntaxNode node)
    {
        return node.GetAncestorOrThis<CompilationUnitSyntax>()!.Externs
                   .Concat(node.GetAncestorsOrThis<BaseNamespaceDeclarationSyntax>()
                               .Reverse()
                               .SelectMany(n => n.Externs));
    }

    public static bool IsUnsafeContext(this SyntaxNode node)
    {
        if (node.GetAncestor<UnsafeStatementSyntax>() != null)
        {
            return true;
        }

        return node.GetAncestors<MemberDeclarationSyntax>().Any(
            m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword));
    }

    public static bool IsInStaticContext(this SyntaxNode node)
    {
        for (var current = node; current != null; current = current.Parent)
        {
            switch (current)
            {
                // this/base calls are always static.
                case ConstructorInitializerSyntax:
                    return true;

                case LocalFunctionStatementSyntax localFunction when localFunction.Modifiers.Any(SyntaxKind.StaticKeyword):
                    return true;

                case AnonymousFunctionExpressionSyntax anonymousFunction when anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword):
                    return true;

                case BaseMethodDeclarationSyntax or IndexerDeclarationSyntax or EventDeclarationSyntax:
                    return current.GetModifiers().Any(SyntaxKind.StaticKeyword);

                case PropertyDeclarationSyntax property:
                    return property.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                        node.IsFoundUnder((PropertyDeclarationSyntax p) => p.Initializer);

                case FieldDeclarationSyntax or EventFieldDeclarationSyntax:
                    // Inside a field one can only access static members of a type (unless it's top-level).
                    return !current.Parent.IsKind(SyntaxKind.CompilationUnit);

                case GlobalStatementSyntax:
                    // Global statements are not a static context.
                    return false;
            }
        }

        // any other location is considered static
        return true;
    }

    public static BaseNamespaceDeclarationSyntax? GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
    {
        var usingDirectiveAncestor = contextNode.GetAncestor<UsingDirectiveSyntax>();
        if (usingDirectiveAncestor == null)
        {
            return contextNode.GetAncestorsOrThis<BaseNamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
        }
        else
        {
            // We are inside a using directive. In this case, we should find and return the first 'parent' namespace with usings.
            var containingNamespace = usingDirectiveAncestor.GetAncestor<BaseNamespaceDeclarationSyntax>();
            if (containingNamespace == null)
            {
                // We are inside a top level using directive (i.e. one that's directly in the compilation unit).
                return null;
            }
            else
            {
                return containingNamespace.GetAncestors<BaseNamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
            }
        }
    }

    public static bool IsBreakableConstruct(this SyntaxNode node)
    {
        switch (node.Kind())
        {
            case SyntaxKind.DoStatement:
            case SyntaxKind.WhileStatement:
            case SyntaxKind.SwitchStatement:
            case SyntaxKind.ForStatement:
            case SyntaxKind.ForEachStatement:
            case SyntaxKind.ForEachVariableStatement:
                return true;
        }

        return false;
    }

    public static bool IsContinuableConstruct(this SyntaxNode node)
    {
        switch (node.Kind())
        {
            case SyntaxKind.DoStatement:
            case SyntaxKind.WhileStatement:
            case SyntaxKind.ForStatement:
            case SyntaxKind.ForEachStatement:
            case SyntaxKind.ForEachVariableStatement:
                return true;
        }

        return false;
    }

    public static bool IsReturnableConstruct(this SyntaxNode node)
    {
        switch (node.Kind())
        {
            case SyntaxKind.AnonymousMethodExpression:
            case SyntaxKind.SimpleLambdaExpression:
            case SyntaxKind.ParenthesizedLambdaExpression:
            case SyntaxKind.LocalFunctionStatement:
            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.DestructorDeclaration:
            case SyntaxKind.GetAccessorDeclaration:
            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.InitAccessorDeclaration:
            case SyntaxKind.OperatorDeclaration:
            case SyntaxKind.ConversionOperatorDeclaration:
            case SyntaxKind.AddAccessorDeclaration:
            case SyntaxKind.RemoveAccessorDeclaration:
                return true;
        }

        return false;
    }

    public static bool ContainsYield(this SyntaxNode node)
        => node.DescendantNodes(n => n == node || !n.IsReturnableConstruct()).Any(n => n is YieldStatementSyntax);

    public static bool IsReturnableConstructOrTopLevelCompilationUnit(this SyntaxNode node)
        => node.IsReturnableConstruct() || (node is CompilationUnitSyntax compilationUnit && compilationUnit.Members.Any(SyntaxKind.GlobalStatement));

    public static bool SpansPreprocessorDirective<TSyntaxNode>(this IEnumerable<TSyntaxNode> list) where TSyntaxNode : SyntaxNode
        => CSharpSyntaxFacts.Instance.SpansPreprocessorDirective(list);

    [return: NotNullIfNotNull(nameof(node))]
    public static TNode? ConvertToSingleLine<TNode>(this TNode? node, bool useElasticTrivia = false)
        where TNode : SyntaxNode
    {
        if (node == null)
        {
            return node;
        }

        var rewriter = new SingleLineRewriter(useElasticTrivia);
        return (TNode)rewriter.Visit(node);
    }

    /// <summary>
    /// Returns true if the passed in node contains an interleaved pp directive.
    /// 
    /// i.e. The following returns false:
    /// 
    ///   void Goo() {
    /// #if true
    /// #endif
    ///   }
    /// 
    /// #if true
    ///   void Goo() {
    ///   }
    /// #endif
    /// 
    /// but these return true:
    /// 
    /// #if true
    ///   void Goo() {
    /// #endif
    ///   }
    /// 
    ///   void Goo() {
    /// #if true
    ///   }
    /// #endif
    /// 
    /// #if true
    ///   void Goo() {
    /// #else
    ///   }
    /// #endif
    /// 
    /// i.e. the method returns true if it contains a PP directive that belongs to a grouping
    /// constructs (like #if/#endif or #region/#endregion), but the grouping construct isn't
    /// entirely contained within the span of the node.
    /// </summary>
    public static bool ContainsInterleavedDirective(this SyntaxNode syntaxNode, CancellationToken cancellationToken)
        => CSharpSyntaxFacts.Instance.ContainsInterleavedDirective(syntaxNode, cancellationToken);

    /// <summary>
    /// Similar to <see cref="ContainsInterleavedDirective(SyntaxNode, CancellationToken)"/> except that the span to check
    /// for interleaved directives can be specified separately to the node passed in.
    /// </summary>
    public static bool ContainsInterleavedDirective(this SyntaxNode syntaxNode, TextSpan span, CancellationToken cancellationToken)
        => CSharpSyntaxFacts.Instance.ContainsInterleavedDirective(span, syntaxNode, cancellationToken);

    public static bool ContainsInterleavedDirective(
        this SyntaxToken token,
        TextSpan textSpan,
        CancellationToken cancellationToken)
    {
        return
            ContainsInterleavedDirective(textSpan, token.LeadingTrivia, cancellationToken) ||
            ContainsInterleavedDirective(textSpan, token.TrailingTrivia, cancellationToken);
    }

    private static bool ContainsInterleavedDirective(
        TextSpan textSpan,
        SyntaxTriviaList list,
        CancellationToken cancellationToken)
    {
        foreach (var trivia in list)
        {
            if (textSpan.Contains(trivia.Span))
            {
                if (ContainsInterleavedDirective(textSpan, trivia, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsInterleavedDirective(
        TextSpan textSpan,
        SyntaxTrivia trivia,
        CancellationToken cancellationToken)
    {
        if (trivia.HasStructure)
        {
            var structure = trivia.GetStructure()!;
            if (trivia.GetStructure() is (kind: SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia or SyntaxKind.IfDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia))
            {
                var match = ((DirectiveTriviaSyntax)structure).GetMatchingDirective(cancellationToken);
                if (match != null)
                {
                    var matchSpan = match.Span;
                    if (!textSpan.Contains(matchSpan.Start))
                    {
                        // The match for this pp directive is outside
                        // this node.
                        return true;
                    }
                }
            }
            else if (trivia.GetStructure() is (kind: SyntaxKind.ElseDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia))
            {
                var directives = ((DirectiveTriviaSyntax)structure).GetMatchingConditionalDirectives(cancellationToken);
                if (directives.Length > 0)
                {
                    if (!textSpan.Contains(directives[0].SpanStart) ||
                        !textSpan.Contains(directives.Last().SpanStart))
                    {
                        // This else/elif belongs to a pp span that isn't 
                        // entirely within this node.
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Breaks up the list of provided nodes, based on how they are interspersed with pp
    /// directives, into groups.  Within these groups nodes can be moved around safely, without
    /// breaking any pp constructs.
    /// </summary>
    public static IList<IList<TSyntaxNode>> SplitNodesOnPreprocessorBoundaries<TSyntaxNode>(
        this IEnumerable<TSyntaxNode> nodes,
        CancellationToken cancellationToken)
        where TSyntaxNode : SyntaxNode
    {
        var result = new List<IList<TSyntaxNode>>();

        var currentGroup = new List<TSyntaxNode>();
        foreach (var node in nodes)
        {
            var hasUnmatchedInteriorDirective = node.ContainsInterleavedDirective(cancellationToken);
            var hasLeadingDirective = node.GetLeadingTrivia().Any(t => SyntaxFacts.IsPreprocessorDirective(t.Kind()));

            if (hasUnmatchedInteriorDirective)
            {
                // we have a #if/#endif/#region/#endregion/#else/#elif in
                // this node that belongs to a span of pp directives that
                // is not entirely contained within the node.  i.e.:
                //
                //   void Goo() {
                //      #if ...
                //   }
                //
                // This node cannot be moved at all.  It is in a group that
                // only contains itself (and thus can never be moved).

                // add whatever group we've built up to now. And reset the 
                // next group to empty.
                result.Add(currentGroup);
                currentGroup = new List<TSyntaxNode>();

                result.Add(new List<TSyntaxNode> { node });
            }
            else if (hasLeadingDirective)
            {
                // We have a PP directive before us.  i.e.:
                // 
                //   #if ...
                //      void Goo() {
                //
                // That means we start a new group that is contained between
                // the above directive and the following directive.

                // add whatever group we've built up to now. And reset the 
                // next group to empty.
                result.Add(currentGroup);
                currentGroup = new List<TSyntaxNode>();

                currentGroup.Add(node);
            }
            else
            {
                // simple case.  just add ourselves to the current group
                currentGroup.Add(node);
            }
        }

        // add the remainder of the final group.
        result.Add(currentGroup);

        // Now, filter out any empty groups.
        result = result.Where(group => !group.IsEmpty()).ToList();
        return result;
    }

    public static ImmutableArray<SyntaxTrivia> GetLeadingBlankLines<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
        => CSharpFileBannerFacts.Instance.GetLeadingBlankLines(node);

    public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
        => CSharpFileBannerFacts.Instance.GetNodeWithoutLeadingBlankLines(node);

    public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(this TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia) where TSyntaxNode : SyntaxNode
        => CSharpFileBannerFacts.Instance.GetNodeWithoutLeadingBlankLines(node, out strippedTrivia);

    public static ImmutableArray<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
        => CSharpFileBannerFacts.Instance.GetLeadingBannerAndPreprocessorDirectives(node);

    public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
        => CSharpFileBannerFacts.Instance.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node);

    public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(this TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia) where TSyntaxNode : SyntaxNode
        => CSharpFileBannerFacts.Instance.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node, out strippedTrivia);

    public static bool IsVariableDeclaratorValue([NotNullWhen(true)] this SyntaxNode? node)
        => node?.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax } equalsValue &&
           equalsValue.Value == node;

    public static BlockSyntax? FindInnermostCommonBlock(this IEnumerable<SyntaxNode> nodes)
        => nodes.FindInnermostCommonNode<BlockSyntax>();

    public static IEnumerable<SyntaxNode> GetAncestorsOrThis(this SyntaxNode? node, Func<SyntaxNode, bool> predicate)
    {
        var current = node;
        while (current != null)
        {
            if (predicate(current))
            {
                yield return current;
            }

            current = current.Parent;
        }
    }

    public static (SyntaxToken openParen, SyntaxToken closeParen) GetParentheses(this SyntaxNode node)
        => node switch
        {
            ParenthesizedExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            MakeRefExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            RefTypeExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            RefValueExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            CheckedExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            DefaultExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            TypeOfExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            SizeOfExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            ArgumentListSyntax n => (n.OpenParenToken, n.CloseParenToken),
            CastExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            WhileStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            DoStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            ForStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            CommonForEachStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            UsingStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            FixedStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            LockStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            IfStatementSyntax n => (n.OpenParenToken, n.CloseParenToken),
            SwitchStatementSyntax n when n.OpenParenToken != default => (n.OpenParenToken, n.CloseParenToken),
            TupleExpressionSyntax n => (n.OpenParenToken, n.CloseParenToken),
            CatchDeclarationSyntax n => (n.OpenParenToken, n.CloseParenToken),
            AttributeArgumentListSyntax n => (n.OpenParenToken, n.CloseParenToken),
            ConstructorConstraintSyntax n => (n.OpenParenToken, n.CloseParenToken),
            ParameterListSyntax n => (n.OpenParenToken, n.CloseParenToken),
            _ => default,
        };

    public static (SyntaxToken openBracket, SyntaxToken closeBracket) GetBrackets(this SyntaxNode? node)
        => node switch
        {
            ArrayRankSpecifierSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            BracketedArgumentListSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            ImplicitArrayCreationExpressionSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            AttributeListSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            BracketedParameterListSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            ListPatternSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            CollectionExpressionSyntax n => (n.OpenBracketToken, n.CloseBracketToken),
            _ => default,
        };

    public static SyntaxTokenList GetModifiers(this SyntaxNode? member)
        => member switch
        {
            AccessorDeclarationSyntax accessor => accessor.Modifiers,
            AnonymousFunctionExpressionSyntax anonymous => anonymous.Modifiers,
            LocalDeclarationStatementSyntax localDeclaration => localDeclaration.Modifiers,
            LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
            MemberDeclarationSyntax memberDecl => memberDecl.Modifiers,
            ParameterSyntax parameter => parameter.Modifiers,
            _ => default,
        };

    public static SyntaxNode? WithModifiers(this SyntaxNode? member, SyntaxTokenList modifiers)
        => member switch
        {
            MemberDeclarationSyntax memberDecl => memberDecl.WithModifiers(modifiers),
            AccessorDeclarationSyntax accessor => accessor.WithModifiers(modifiers),
            AnonymousFunctionExpressionSyntax anonymous => anonymous.WithModifiers(modifiers),
            LocalFunctionStatementSyntax localFunction => localFunction.WithModifiers(modifiers),
            LocalDeclarationStatementSyntax localDeclaration => localDeclaration.WithModifiers(modifiers),
            _ => null,
        };

    public static IEnumerable<MemberDeclarationSyntax> GetMembers(this SyntaxNode? node)
        => node switch
        {
            CompilationUnitSyntax compilation => compilation.Members,
            BaseNamespaceDeclarationSyntax @namespace => @namespace.Members,
            TypeDeclarationSyntax type => type.Members,
            EnumDeclarationSyntax @enum => @enum.Members,
            _ => SpecializedCollections.EmptyEnumerable<MemberDeclarationSyntax>(),
        };

    public static bool IsInExpressionTree(
        [NotNullWhen(true)] this SyntaxNode? node,
        SemanticModel semanticModel,
        [NotNullWhen(true)] INamedTypeSymbol? expressionType,
        CancellationToken cancellationToken)
    {
        if (expressionType != null)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current is LambdaExpressionSyntax)
                {
                    var typeInfo = semanticModel.GetTypeInfo(current, cancellationToken);
                    if (expressionType.Equals(typeInfo.ConvertedType?.OriginalDefinition))
                        return true;
                }
                else if (current is SelectOrGroupClauseSyntax or OrderingSyntax)
                {
                    var info = semanticModel.GetSymbolInfo(current, cancellationToken);
                    if (TakesExpressionTree(info, expressionType))
                        return true;
                }
                else if (current is QueryClauseSyntax queryClause)
                {
                    var info = semanticModel.GetQueryClauseInfo(queryClause, cancellationToken);
                    if (TakesExpressionTree(info.CastInfo, expressionType) ||
                        TakesExpressionTree(info.OperationInfo, expressionType))
                    {
                        return true;
                    }
                }
            }
        }

        return false;

        static bool TakesExpressionTree(SymbolInfo info, INamedTypeSymbol expressionType)
        {
            foreach (var symbol in info.GetAllSymbols())
            {
                if (symbol is IMethodSymbol method &&
                    method.Parameters.Length > 0 &&
                    expressionType.Equals(method.Parameters[0].Type?.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static bool IsInDeconstructionLeft(
        [NotNullWhen(true)] this SyntaxNode? node,
        [NotNullWhen(true)] out SyntaxNode? deconstructionLeft)
    {
        SyntaxNode? previous = null;
        for (var current = node; current != null; current = current.Parent)
        {
            if ((current is AssignmentExpressionSyntax assignment && previous == assignment.Left && assignment.IsDeconstruction()) ||
                (current is ForEachVariableStatementSyntax @foreach && previous == @foreach.Variable))
            {
                deconstructionLeft = previous;
                return true;
            }

            if (current is StatementSyntax)
            {
                break;
            }

            previous = current;
        }

        deconstructionLeft = null;
        return false;
    }

    public static T WithCommentsFrom<T>(this T node, SyntaxToken leadingToken, SyntaxToken trailingToken)
        where T : SyntaxNode
        => node.WithCommentsFrom(
            SyntaxNodeOrTokenExtensions.GetTrivia(leadingToken),
            SyntaxNodeOrTokenExtensions.GetTrivia(trailingToken));

    public static T WithCommentsFrom<T>(
        this T node,
        IEnumerable<SyntaxToken> leadingTokens,
        IEnumerable<SyntaxToken> trailingTokens)
        where T : SyntaxNode
        => node.WithCommentsFrom(leadingTokens.GetTrivia(), trailingTokens.GetTrivia());

    public static T WithCommentsFrom<T>(
        this T node,
        IEnumerable<SyntaxTrivia> leadingTrivia,
        IEnumerable<SyntaxTrivia> trailingTrivia,
        params SyntaxNodeOrToken[] trailingNodesOrTokens)
        where T : SyntaxNode
        => node
            .WithLeadingTrivia(leadingTrivia.Concat(node.GetLeadingTrivia()).FilterComments(addElasticMarker: false))
            .WithTrailingTrivia(
                node.GetTrailingTrivia().Concat(SyntaxNodeOrTokenExtensions.GetTrivia(trailingNodesOrTokens).Concat(trailingTrivia)).FilterComments(addElasticMarker: false));

    public static T KeepCommentsAndAddElasticMarkers<T>(this T node) where T : SyntaxNode
        => node
        .WithTrailingTrivia(node.GetTrailingTrivia().FilterComments(addElasticMarker: true))
        .WithLeadingTrivia(node.GetLeadingTrivia().FilterComments(addElasticMarker: true));

    public static SyntaxNode WithPrependedNonIndentationTriviaFrom(
        this SyntaxNode to, SyntaxNode from)
    {
        // get all the preceding trivia from the 'from' node, not counting the leading
        // indentation trivia is has.
        var finalTrivia = from.GetLeadingTrivia().ToList();
        while (finalTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia)])
            finalTrivia.RemoveAt(finalTrivia.Count - 1);

        // Also, add on the trailing trivia if there are trailing comments.
        var hasTrailingComments = from.GetTrailingTrivia().Any(t => t.IsRegularComment());
        if (hasTrailingComments)
            finalTrivia.AddRange(from.GetTrailingTrivia());

        // Merge this trivia with the existing trivia on the node.  Format in case
        // we added comments and need them indented properly.
        return to.WithPrependedLeadingTrivia(finalTrivia);
    }
}
