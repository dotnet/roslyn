// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        public static LanguageVersion GetLanguageVersion(this SyntaxNode node)
            => ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion;

        public static void Deconstruct(this SyntaxNode node, out SyntaxKind kind)
        {
            kind = node.Kind();
        }

        public static bool IsKind<TNode>([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind, [NotNullWhen(returnValue: true)] out TNode? result)
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

        public static bool IsParentKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind)
            => CodeAnalysis.CSharpExtensions.IsKind(node?.Parent, kind);

        public static bool IsParentKind<TNode>([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind, [NotNullWhen(returnValue: true)] out TNode? result)
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

        public static bool IsParentKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2)
            => IsKind(node?.Parent, kind1, kind2);

        public static bool IsParentKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
            => IsKind(node?.Parent, kind1, kind2, kind3);

        public static bool IsParentKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
            => IsKind(node?.Parent, kind1, kind2, kind3, kind4);

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6, SyntaxKind kind7)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6 || csharpKind == kind7;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6, SyntaxKind kind7, SyntaxKind kind8)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6 || csharpKind == kind7 || csharpKind == kind8;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6, SyntaxKind kind7, SyntaxKind kind8, SyntaxKind kind9, SyntaxKind kind10)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6 || csharpKind == kind7 || csharpKind == kind8 || csharpKind == kind9 || csharpKind == kind10;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6, SyntaxKind kind7, SyntaxKind kind8, SyntaxKind kind9, SyntaxKind kind10, SyntaxKind kind11)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6 || csharpKind == kind7 || csharpKind == kind8 || csharpKind == kind9 || csharpKind == kind10 || csharpKind == kind11;
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

        public static bool IsAnyArgumentList([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node.IsKind(SyntaxKind.ArgumentList) ||
                   node.IsKind(SyntaxKind.AttributeArgumentList) ||
                   node.IsKind(SyntaxKind.BracketedArgumentList) ||
                   node.IsKind(SyntaxKind.TypeArgumentList);
        }

        public static (SyntaxToken openBrace, SyntaxToken closeBrace) GetBraces(this SyntaxNode? node)
        {
            switch (node)
            {
                case NamespaceDeclarationSyntax namespaceNode:
                    return (namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken);
                case BaseTypeDeclarationSyntax baseTypeNode:
                    return (baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken);
                case AccessorListSyntax accessorListNode:
                    return (accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken);
                case BlockSyntax blockNode:
                    return (blockNode.OpenBraceToken, blockNode.CloseBraceToken);
                case SwitchStatementSyntax switchStatementNode:
                    return (switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken);
                case AnonymousObjectCreationExpressionSyntax anonymousObjectCreationExpression:
                    return (anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken);
                case InitializerExpressionSyntax initializeExpressionNode:
                    return (initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken);
                case SwitchExpressionSyntax switchExpression:
                    return (switchExpression.OpenBraceToken, switchExpression.CloseBraceToken);
                case PropertyPatternClauseSyntax property:
                    return (property.OpenBraceToken, property.CloseBraceToken);
                case WithExpressionSyntax withExpr:
                    return (withExpr.Initializer.OpenBraceToken, withExpr.Initializer.CloseBraceToken);
                case ImplicitObjectCreationExpressionSyntax { Initializer: { } initializer }:
                    return (initializer.OpenBraceToken, initializer.CloseBraceToken);
            }

            return default;
        }

        public static bool IsEmbeddedStatementOwner([NotNullWhen(returnValue: true)] this SyntaxNode? node)
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
            => declaration?.Kind() switch
            {
                SyntaxKind.DelegateDeclaration => ((DelegateDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.OperatorDeclaration => ((OperatorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.ConversionOperatorDeclaration => ((ConversionOperatorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.ConstructorDeclaration => ((ConstructorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.DestructorDeclaration => ((DestructorDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax)declaration).ParameterList,
                SyntaxKind.ParenthesizedLambdaExpression => ((ParenthesizedLambdaExpressionSyntax)declaration).ParameterList,
                SyntaxKind.LocalFunctionStatement => ((LocalFunctionStatementSyntax)declaration).ParameterList,
                SyntaxKind.AnonymousMethodExpression => ((AnonymousMethodExpressionSyntax)declaration).ParameterList,
                SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration => ((RecordDeclarationSyntax)declaration).ParameterList,
                _ => null,
            };

        public static SyntaxList<AttributeListSyntax> GetAttributeLists(this SyntaxNode? declaration)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.AttributeLists,
                AccessorDeclarationSyntax accessor => accessor.AttributeLists,
                ParameterSyntax parameter => parameter.AttributeLists,
                CompilationUnitSyntax compilationUnit => compilationUnit.AttributeLists,
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

            if ((current.IsParentKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax? memberAccess) && memberAccess.Name == current) ||
                (current.IsParentKind(SyntaxKind.MemberBindingExpression, out MemberBindingExpressionSyntax? memberBinding) && memberBinding.Name == current))
            {
                current = current.Parent;
            }

            // Effectively, if we're on the RHS of the ? we have to walk up the RHS spine first until we hit the first
            // conditional access.

            while (current.IsKind(
                SyntaxKind.InvocationExpression,
                SyntaxKind.ElementAccessExpression,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.MemberBindingExpression,
                SyntaxKind.ElementBindingExpression,
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

            if (current.IsParentKind(SyntaxKind.ConditionalAccessExpression, out ConditionalAccessExpressionSyntax? conditional) &&
                conditional.Expression == current)
            {
                current = conditional;
            }

            if (current.IsParentKind(SyntaxKind.ConditionalAccessExpression, out conditional) &&
                conditional.WhenNotNull == current)
            {
                current = conditional;
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
            while (current.IsParentKind(SyntaxKind.ConditionalAccessExpression, out ConditionalAccessExpressionSyntax? conditional) &&
                conditional.WhenNotNull == current)
            {
                current = conditional;
            }

            return current;
        }

        public static ConditionalAccessExpressionSyntax? GetInnerMostConditionalAccessExpression(this SyntaxNode node)
        {
            if (node is not ConditionalAccessExpressionSyntax)
            {
                return null;
            }

            var result = (ConditionalAccessExpressionSyntax)node;
            while (result.WhenNotNull is ConditionalAccessExpressionSyntax)
            {
                result = (ConditionalAccessExpressionSyntax)result.WhenNotNull;
            }

            return result;
        }

        public static bool IsAsyncSupportingFunctionSyntax([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node.IsKind(SyntaxKind.MethodDeclaration)
                || node.IsAnyLambdaOrAnonymousMethod()
                || node.IsKind(SyntaxKind.LocalFunctionStatement);
        }

        public static bool IsAnyLambda([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return
                node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                node.IsKind(SyntaxKind.SimpleLambdaExpression);
        }

        public static bool IsAnyLambdaOrAnonymousMethod([NotNullWhen(returnValue: true)] this SyntaxNode? node)
            => node.IsAnyLambda() || node.IsKind(SyntaxKind.AnonymousMethodExpression);

        public static bool IsAnyAssignExpression(this SyntaxNode node)
            => SyntaxFacts.IsAssignmentExpression(node.Kind());

        public static bool IsCompoundAssignExpression(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CoalesceAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                    return true;
            }

            return false;
        }

        public static bool IsLeftSideOfAssignExpression([NotNullWhen(returnValue: true)] this SyntaxNode? node)
            => node.IsParentKind(SyntaxKind.SimpleAssignmentExpression, out AssignmentExpressionSyntax? assignment) &&
               assignment.Left == node;

        public static bool IsLeftSideOfAnyAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
        {
            return node?.Parent != null &&
                node.Parent.IsAnyAssignExpression() &&
                ((AssignmentExpressionSyntax)node.Parent).Left == node;
        }

        public static bool IsRightSideOfAnyAssignExpression([NotNullWhen(true)] this SyntaxNode? node)
        {
            return node?.Parent != null &&
                node.Parent.IsAnyAssignExpression() &&
                ((AssignmentExpressionSyntax)node.Parent).Right == node;
        }

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
            // this/base calls are always static.
            if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null)
            {
                return true;
            }

            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null)
            {
                return false;
            }

            switch (memberDeclaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword);

                case SyntaxKind.PropertyDeclaration:
                    return memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword) ||
                        node.IsFoundUnder((PropertyDeclarationSyntax p) => p.Initializer);

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    // Inside a field one can only access static members of a type (unless it's top-level).
                    return !memberDeclaration.Parent.IsKind(SyntaxKind.CompilationUnit);

                case SyntaxKind.DestructorDeclaration:
                    return false;
            }

            // Global statements are not a static context.
            if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null)
            {
                return false;
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

        public static bool SpansPreprocessorDirective<TSyntaxNode>(this IEnumerable<TSyntaxNode> list) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFacts.Instance.SpansPreprocessorDirective(list);

        [return: NotNullIfNotNull("node")]
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
                if (trivia.GetStructure().IsKind(SyntaxKind.RegionDirectiveTrivia,
                                                 SyntaxKind.EndRegionDirectiveTrivia,
                                                 SyntaxKind.IfDirectiveTrivia,
                                                 SyntaxKind.EndIfDirectiveTrivia))
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
                else if (trivia.GetStructure().IsKind(SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ElifDirectiveTrivia))
                {
                    var directives = ((DirectiveTriviaSyntax)structure).GetMatchingConditionalDirectives(cancellationToken);
                    if (directives != null && directives.Count > 0)
                    {
                        if (!textSpan.Contains(directives[0].SpanStart) ||
                            !textSpan.Contains(directives[directives.Count - 1].SpanStart))
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

        public static bool IsVariableDeclaratorValue(this SyntaxNode node)
            => node.IsParentKind(SyntaxKind.EqualsValueClause, out EqualsValueClauseSyntax? equalsValue) &&
               equalsValue.IsParentKind(SyntaxKind.VariableDeclarator) &&
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

        /// <summary>
        /// Returns child node or token that contains given position.
        /// </summary>
        /// <remarks>
        /// This is a copy of <see cref="SyntaxNode.ChildThatContainsPosition"/> that also returns the index of the child node.
        /// </remarks>
        internal static SyntaxNodeOrToken ChildThatContainsPosition(this SyntaxNode self, int position, out int childIndex)
        {
            var childList = self.ChildNodesAndTokens();

            var left = 0;
            var right = childList.Count - 1;

            while (left <= right)
            {
                var middle = left + ((right - left) / 2);
                var node = childList[middle];

                var span = node.FullSpan;
                if (position < span.Start)
                {
                    right = middle - 1;
                }
                else if (position >= span.End)
                {
                    left = middle + 1;
                }
                else
                {
                    childIndex = middle;
                    return node;
                }
            }

            // we could check up front that index is within FullSpan,
            // but we wan to optimize for the common case where position is valid.
            Debug.Assert(!self.FullSpan.Contains(position), "Position is valid. How could we not find a child?");
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        public static (SyntaxToken openParen, SyntaxToken closeParen) GetParentheses(this SyntaxNode node)
        {
            switch (node)
            {
                case ParenthesizedExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case MakeRefExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case RefTypeExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case RefValueExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CheckedExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case DefaultExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case TypeOfExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case SizeOfExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ArgumentListSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CastExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case WhileStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case DoStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ForStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CommonForEachStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case UsingStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case FixedStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case LockStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case IfStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case SwitchStatementSyntax n when n.OpenParenToken != default: return (n.OpenParenToken, n.CloseParenToken);
                case TupleExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CatchDeclarationSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case AttributeArgumentListSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ConstructorConstraintSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ParameterListSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                default: return default;
            }
        }

        public static (SyntaxToken openBracket, SyntaxToken closeBracket) GetBrackets(this SyntaxNode? node)
        {
            switch (node)
            {
                case ArrayRankSpecifierSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case BracketedArgumentListSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case ImplicitArrayCreationExpressionSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case AttributeListSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case BracketedParameterListSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case ListPatternSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                default: return default;
            }
        }

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
        {
            switch (member)
            {
                case MemberDeclarationSyntax memberDecl: return memberDecl.WithModifiers(modifiers);
                case AccessorDeclarationSyntax accessor: return accessor.WithModifiers(modifiers);
                case AnonymousFunctionExpressionSyntax anonymous: return anonymous.WithModifiers(modifiers);
                case LocalFunctionStatementSyntax localFunction: return localFunction.WithModifiers(modifiers);
                case LocalDeclarationStatementSyntax localDeclaration: return localDeclaration.WithModifiers(modifiers);
            }

            return null;
        }

        public static bool CheckTopLevel(this SyntaxNode node, TextSpan span)
        {
            switch (node)
            {
                case BlockSyntax block:
                    return block.ContainsInBlockBody(span);
                case ArrowExpressionClauseSyntax expressionBodiedMember:
                    return expressionBodiedMember.ContainsInExpressionBodiedMemberBody(span);
                case FieldDeclarationSyntax field:
                    {
                        foreach (var variable in field.Declaration.Variables)
                        {
                            if (variable.Initializer != null && variable.Initializer.Span.Contains(span))
                            {
                                return true;
                            }
                        }

                        break;
                    }

                case GlobalStatementSyntax _:
                    return true;
                case ConstructorInitializerSyntax constructorInitializer:
                    return constructorInitializer.ContainsInArgument(span);
            }

            return false;
        }

        public static bool ContainsInArgument(this ConstructorInitializerSyntax initializer, TextSpan textSpan)
        {
            if (initializer == null)
            {
                return false;
            }

            return initializer.ArgumentList.Arguments.Any(a => a.Span.Contains(textSpan));
        }

        public static bool ContainsInBlockBody(this BlockSyntax block, TextSpan textSpan)
        {
            if (block == null)
            {
                return false;
            }

            var blockSpan = TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart);
            return blockSpan.Contains(textSpan);
        }

        public static bool ContainsInExpressionBodiedMemberBody(this ArrowExpressionClauseSyntax expressionBodiedMember, TextSpan textSpan)
        {
            if (expressionBodiedMember == null)
            {
                return false;
            }

            var expressionBodiedMemberBody = TextSpan.FromBounds(expressionBodiedMember.Expression.SpanStart, expressionBodiedMember.Expression.Span.End);
            return expressionBodiedMemberBody.Contains(textSpan);
        }

        public static IEnumerable<MemberDeclarationSyntax> GetMembers(this SyntaxNode? node)
        {
            switch (node)
            {
                case CompilationUnitSyntax compilation:
                    return compilation.Members;
                case BaseNamespaceDeclarationSyntax @namespace:
                    return @namespace.Members;
                case TypeDeclarationSyntax type:
                    return type.Members;
                case EnumDeclarationSyntax @enum:
                    return @enum.Members;
            }

            return SpecializedCollections.EmptyEnumerable<MemberDeclarationSyntax>();
        }

        public static bool IsInExpressionTree(
            [NotNullWhen(returnValue: true)] this SyntaxNode? node,
            SemanticModel semanticModel,
            [NotNullWhen(returnValue: true)] INamedTypeSymbol? expressionTypeOpt,
            CancellationToken cancellationToken)
        {
            if (expressionTypeOpt != null)
            {
                for (var current = node; current != null; current = current.Parent)
                {
                    if (current.IsAnyLambda())
                    {
                        var typeInfo = semanticModel.GetTypeInfo(current, cancellationToken);
                        if (expressionTypeOpt.Equals(typeInfo.ConvertedType?.OriginalDefinition))
                            return true;
                    }
                    else if (current is SelectOrGroupClauseSyntax or
                             OrderingSyntax)
                    {
                        var info = semanticModel.GetSymbolInfo(current, cancellationToken);
                        if (TakesExpressionTree(info, expressionTypeOpt))
                            return true;
                    }
                    else if (current is QueryClauseSyntax queryClause)
                    {
                        var info = semanticModel.GetQueryClauseInfo(queryClause, cancellationToken);
                        if (TakesExpressionTree(info.CastInfo, expressionTypeOpt) ||
                            TakesExpressionTree(info.OperationInfo, expressionTypeOpt))
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
            [NotNullWhen(returnValue: true)] this SyntaxNode? node,
            [NotNullWhen(returnValue: true)] out SyntaxNode? deconstructionLeft)
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

        public static bool IsTopLevelOfUsingAliasDirective(this SyntaxToken node)
            => node switch
            {
                { Parent: NameEqualsSyntax { Parent: UsingDirectiveSyntax _ } } => true,
                { Parent: IdentifierNameSyntax { Parent: UsingDirectiveSyntax _ } } => true,
                _ => false
            };

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
    }
}
