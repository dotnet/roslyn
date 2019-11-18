// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static partial class SyntaxTokenExtensions
    {
        public static bool IsUsingOrExternKeyword(this SyntaxToken token)
        {
            return
                token.Kind() == SyntaxKind.UsingKeyword ||
                token.Kind() == SyntaxKind.ExternKeyword;
        }

        public static bool IsUsingKeywordInUsingDirective(this SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.UsingKeyword))
            {
                var usingDirective = token.GetAncestor<UsingDirectiveSyntax>();
                if (usingDirective != null &&
                    usingDirective.UsingKeyword == token)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsStaticKeywordInUsingDirective(this SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.StaticKeyword))
            {
                var usingDirective = token.GetAncestor<UsingDirectiveSyntax>();
                if (usingDirective != null &&
                    usingDirective.StaticKeyword == token)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsBeginningOfStatementContext(this SyntaxToken token)
        {
            // cases:
            //    {
            //      |

            // }
            // |

            // Note, the following is *not* a legal statement context: 
            //    do { } |

            // ...;
            // |

            // case 0:
            //   |

            // default:
            //   |

            // label:
            //   |

            // if (goo)
            //   |

            // while (true)
            //   |

            // do
            //   |

            // for (;;)
            //   |

            // foreach (var v in c)
            //   |

            // else
            //   |

            // using (expr)
            //   |

            // fixed (void* v = &expr)
            //   |

            // lock (expr)
            //   |

            // for ( ; ; Goo(), |

            switch (token.Kind())
            {
                case SyntaxKind.OpenBraceToken when token.Parent.IsKind(SyntaxKind.Block):
                    return true;

                case SyntaxKind.SemicolonToken:
                    var statement = token.GetAncestor<StatementSyntax>();
                    return statement != null && !statement.IsParentKind(SyntaxKind.GlobalStatement) &&
                           statement.GetLastToken(includeZeroWidth: true) == token;

                case SyntaxKind.CloseBraceToken:
                    if (token.Parent.IsKind(SyntaxKind.Block))
                    {
                        if (token.Parent.Parent is StatementSyntax)
                        {
                            // Most blocks that are the child of statement are places
                            // that we can follow with another statement.  i.e.:
                            // if { }
                            // while () { }
                            // There are two exceptions.
                            // try {}
                            // do {}
                            if (!token.Parent.IsParentKind(SyntaxKind.TryStatement) &&
                                !token.Parent.IsParentKind(SyntaxKind.DoStatement))
                            {
                                return true;
                            }
                        }
                        else if (
                            token.Parent.IsParentKind(SyntaxKind.ElseClause) ||
                            token.Parent.IsParentKind(SyntaxKind.FinallyClause) ||
                            token.Parent.IsParentKind(SyntaxKind.CatchClause) ||
                            token.Parent.IsParentKind(SyntaxKind.SwitchSection))
                        {
                            return true;
                        }
                    }

                    if (token.Parent.IsKind(SyntaxKind.SwitchStatement))
                    {
                        return true;
                    }

                    return false;

                case SyntaxKind.ColonToken:
                    return token.Parent.IsKind(SyntaxKind.CaseSwitchLabel,
                                               SyntaxKind.DefaultSwitchLabel,
                                               SyntaxKind.CasePatternSwitchLabel,
                                               SyntaxKind.LabeledStatement);

                case SyntaxKind.DoKeyword when token.Parent.IsKind(SyntaxKind.DoStatement):
                    return true;

                case SyntaxKind.CloseParenToken:
                    var parent = token.Parent;
                    return parent.IsKind(SyntaxKind.ForStatement) ||
                           parent.IsKind(SyntaxKind.ForEachStatement) ||
                           parent.IsKind(SyntaxKind.ForEachVariableStatement) ||
                           parent.IsKind(SyntaxKind.WhileStatement) ||
                           parent.IsKind(SyntaxKind.IfStatement) ||
                           parent.IsKind(SyntaxKind.LockStatement) ||
                           parent.IsKind(SyntaxKind.UsingStatement) ||
                           parent.IsKind(SyntaxKind.FixedStatement);

                case SyntaxKind.ElseKeyword:
                    return true;
            }

            return false;
        }

        public static bool IsBeginningOfGlobalStatementContext(this SyntaxToken token)
        {
            // cases:
            // }
            // |

            // ...;
            // |

            // extern alias Goo;
            // using System;
            // |

            // [assembly: Goo]
            // |

            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                if (memberDeclaration != null && memberDeclaration.GetLastToken(includeZeroWidth: true) == token &&
                    memberDeclaration.IsParentKind(SyntaxKind.CompilationUnit))
                {
                    return true;
                }
            }

            if (token.Kind() == SyntaxKind.SemicolonToken)
            {
                var globalStatement = token.GetAncestor<GlobalStatementSyntax>();
                if (globalStatement != null && globalStatement.GetLastToken(includeZeroWidth: true) == token)
                {
                    return true;
                }

                var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                if (memberDeclaration != null && memberDeclaration.GetLastToken(includeZeroWidth: true) == token &&
                    memberDeclaration.IsParentKind(SyntaxKind.CompilationUnit))
                {
                    return true;
                }

                var compUnit = token.GetAncestor<CompilationUnitSyntax>();
                if (compUnit != null)
                {
                    if (compUnit.Usings.Count > 0 && compUnit.Usings.Last().GetLastToken(includeZeroWidth: true) == token)
                    {
                        return true;
                    }

                    if (compUnit.Externs.Count > 0 && compUnit.Externs.Last().GetLastToken(includeZeroWidth: true) == token)
                    {
                        return true;
                    }
                }
            }

            if (token.Kind() == SyntaxKind.CloseBracketToken)
            {
                var compUnit = token.GetAncestor<CompilationUnitSyntax>();
                if (compUnit != null)
                {
                    if (compUnit.AttributeLists.Count > 0 && compUnit.AttributeLists.Last().GetLastToken(includeZeroWidth: true) == token)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsAfterPossibleCast(this SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.CloseParenToken)
            {
                if (token.Parent.IsKind(SyntaxKind.CastExpression))
                {
                    return true;
                }

                if (token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    var parenExpr = token.Parent as ParenthesizedExpressionSyntax;
                    var expr = parenExpr.Expression;

                    if (expr is TypeSyntax)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsLastTokenOfQueryClause(this SyntaxToken token)
        {
            if (token.IsLastTokenOfNode<QueryClauseSyntax>())
            {
                return true;
            }

            if (token.Kind() == SyntaxKind.IdentifierToken &&
                token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.IntoKeyword)
            {
                return true;
            }

            return false;
        }

        public static bool IsPreProcessorExpressionContext(this SyntaxToken targetToken)
        {
            // cases:
            //   #if |
            //   #if goo || |
            //   #if goo && |
            //   #if ( |
            //   #if ! |
            // Same for elif

            if (targetToken.GetAncestor<ConditionalDirectiveTriviaSyntax>() == null)
            {
                return false;
            }

            // #if
            // #elif
            if (targetToken.Kind() == SyntaxKind.IfKeyword ||
                targetToken.Kind() == SyntaxKind.ElifKeyword)
            {
                return true;
            }

            // ( |
            if (targetToken.Kind() == SyntaxKind.OpenParenToken &&
                targetToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                return true;
            }

            // ! |
            if (targetToken.Parent is PrefixUnaryExpressionSyntax)
            {
                var prefix = targetToken.Parent as PrefixUnaryExpressionSyntax;
                return prefix.OperatorToken == targetToken;
            }

            // a &&
            // a ||
            if (targetToken.Parent is BinaryExpressionSyntax)
            {
                var binary = targetToken.Parent as BinaryExpressionSyntax;
                return binary.OperatorToken == targetToken;
            }

            return false;
        }

        public static bool IsOrderByDirectionContext(this SyntaxToken targetToken)
        {
            // cases:
            //   orderby a |
            //   orderby a a|
            //   orderby a, b |
            //   orderby a, b a|

            if (!targetToken.IsKind(SyntaxKind.IdentifierToken, SyntaxKind.CloseParenToken, SyntaxKind.CloseBracketToken))
            {
                return false;
            }

            var ordering = targetToken.GetAncestor<OrderingSyntax>();
            if (ordering == null)
            {
                return false;
            }

            // orderby a |
            // orderby a, b |
            var lastToken = ordering.Expression.GetLastToken(includeSkipped: true);

            if (targetToken == lastToken)
            {
                return true;
            }

            return false;
        }

        public static bool IsSwitchLabelContext(this SyntaxToken targetToken)
        {
            // cases:
            //   case X: |
            //   default: |
            //   switch (e) { |
            //
            //   case X: Statement(); |

            if (targetToken.Kind() == SyntaxKind.OpenBraceToken &&
                targetToken.Parent.IsKind(SyntaxKind.SwitchStatement))
            {
                return true;
            }

            if (targetToken.Kind() == SyntaxKind.ColonToken)
            {
                if (targetToken.Parent.IsKind(
                        SyntaxKind.CaseSwitchLabel, SyntaxKind.DefaultSwitchLabel, SyntaxKind.CasePatternSwitchLabel))
                {
                    return true;
                }
            }

            if (targetToken.Kind() == SyntaxKind.SemicolonToken ||
                targetToken.Kind() == SyntaxKind.CloseBraceToken)
            {
                var section = targetToken.GetAncestor<SwitchSectionSyntax>();
                if (section != null)
                {
                    foreach (var statement in section.Statements)
                    {
                        if (targetToken == statement.GetLastToken(includeSkipped: true))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsXmlCrefParameterModifierContext(this SyntaxToken targetToken)
        {
            return targetToken.IsKind(SyntaxKind.CommaToken, SyntaxKind.OpenParenToken)
                && targetToken.Parent.IsKind(SyntaxKind.CrefBracketedParameterList, SyntaxKind.CrefParameterList);
        }

        public static bool IsConstructorOrMethodParameterArgumentContext(this SyntaxToken targetToken)
        {
            // cases:
            //   Goo( |
            //   Goo(expr, |
            //   Goo(bar: |
            //   new Goo( |
            //   new Goo(expr, |
            //   new Goo(bar: |
            //   Goo : base( |
            //   Goo : base(bar: |
            //   Goo : this( |
            //   Goo : this(bar: |

            // Goo(bar: |
            if (targetToken.Kind() == SyntaxKind.ColonToken &&
                targetToken.Parent.IsKind(SyntaxKind.NameColon) &&
                targetToken.Parent.IsParentKind(SyntaxKind.Argument) &&
                targetToken.Parent.Parent.IsParentKind(SyntaxKind.ArgumentList))
            {
                var owner = targetToken.Parent.Parent.Parent.Parent;
                if (owner.IsKind(SyntaxKind.InvocationExpression) ||
                    owner.IsKind(SyntaxKind.ObjectCreationExpression) ||
                    owner.IsKind(SyntaxKind.BaseConstructorInitializer) ||
                    owner.IsKind(SyntaxKind.ThisConstructorInitializer))
                {
                    return true;
                }
            }


            if (targetToken.Kind() == SyntaxKind.OpenParenToken ||
                targetToken.Kind() == SyntaxKind.CommaToken)
            {
                if (targetToken.Parent.IsKind(SyntaxKind.ArgumentList))
                {
                    if (targetToken.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression) ||
                        targetToken.Parent.IsParentKind(SyntaxKind.BaseConstructorInitializer) ||
                        targetToken.Parent.IsParentKind(SyntaxKind.ThisConstructorInitializer))
                    {
                        return true;
                    }

                    // var( |
                    // var(expr, |
                    // Those are more likely to be deconstruction-declarations being typed than invocations a method "var"
                    if (targetToken.Parent.IsParentKind(SyntaxKind.InvocationExpression) && !targetToken.IsInvocationOfVarExpression())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsUnaryOperatorContext(this SyntaxToken targetToken)
        {
            if (targetToken.Kind() == SyntaxKind.OperatorKeyword &&
                targetToken.GetPreviousToken(includeSkipped: true).IsLastTokenOfNode<TypeSyntax>())
            {
                return true;
            }

            return false;
        }

        public static bool IsUnsafeContext(this SyntaxToken targetToken)
        {
            return
                targetToken.GetAncestors<StatementSyntax>().Any(s => s.IsKind(SyntaxKind.UnsafeStatement)) ||
                targetToken.GetAncestors<MemberDeclarationSyntax>().Any(m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword));
        }

        public static bool IsAfterYieldKeyword(this SyntaxToken targetToken)
        {
            // yield |
            // yield r|

            return targetToken.IsKindOrHasMatchingText(SyntaxKind.YieldKeyword);
        }

        public static bool IsAnyAccessorDeclarationContext(this SyntaxToken targetToken, int position, SyntaxKind kind = SyntaxKind.None)
        {
            return targetToken.IsAccessorDeclarationContext<EventDeclarationSyntax>(position, kind) ||
                targetToken.IsAccessorDeclarationContext<PropertyDeclarationSyntax>(position, kind) ||
                targetToken.IsAccessorDeclarationContext<IndexerDeclarationSyntax>(position, kind);
        }

        public static bool IsAccessorDeclarationContext<TMemberNode>(this SyntaxToken targetToken, int position, SyntaxKind kind = SyntaxKind.None)
            where TMemberNode : SyntaxNode
        {
            if (!IsAccessorDeclarationContextWorker(ref targetToken))
            {
                return false;
            }

            var list = targetToken.GetAncestor<AccessorListSyntax>();
            if (list == null)
            {
                return false;
            }

            // Check if we already have this accessor.  (however, don't count it
            // if the user is *on* that accessor.
            var existingAccessor = list.Accessors
                .Select(a => a.Keyword)
                .FirstOrDefault(a => !a.IsMissing && a.IsKindOrHasMatchingText(kind));

            if (existingAccessor.Kind() != SyntaxKind.None)
            {
                var existingAccessorSpan = existingAccessor.Span;
                if (!existingAccessorSpan.IntersectsWith(position))
                {
                    return false;
                }
            }

            var decl = targetToken.GetAncestor<TMemberNode>();
            return decl != null;
        }

        private static bool IsAccessorDeclarationContextWorker(ref SyntaxToken targetToken)
        {
            // cases:
            //   int Goo { |
            //   int Goo { private |
            //   int Goo { set { } |
            //   int Goo { set; |
            //   int Goo { [Bar]|

            // Consume all preceding access modifiers
            while (targetToken.Kind() == SyntaxKind.InternalKeyword ||
                targetToken.Kind() == SyntaxKind.PublicKeyword ||
                targetToken.Kind() == SyntaxKind.ProtectedKeyword ||
                targetToken.Kind() == SyntaxKind.PrivateKeyword)
            {
                targetToken = targetToken.GetPreviousToken(includeSkipped: true);
            }

            // int Goo { |
            // int Goo { private |
            if (targetToken.Kind() == SyntaxKind.OpenBraceToken &&
                targetToken.Parent.IsKind(SyntaxKind.AccessorList))
            {
                return true;
            }

            // int Goo { set { } |
            // int Goo { set { } private |
            if (targetToken.Kind() == SyntaxKind.CloseBraceToken &&
                targetToken.Parent.IsKind(SyntaxKind.Block) &&
                targetToken.Parent.Parent is AccessorDeclarationSyntax)
            {
                return true;
            }

            // int Goo { set; |
            if (targetToken.Kind() == SyntaxKind.SemicolonToken &&
                targetToken.Parent is AccessorDeclarationSyntax)
            {
                return true;
            }

            // int Goo { [Bar]|
            if (targetToken.Kind() == SyntaxKind.CloseBracketToken &&
                targetToken.Parent.IsKind(SyntaxKind.AttributeList) &&
                targetToken.Parent.Parent is AccessorDeclarationSyntax)
            {
                return true;
            }

            return false;
        }

        private static bool IsGenericInterfaceOrDelegateTypeParameterList(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.TypeParameterList))
            {
                if (node.IsParentKind(SyntaxKind.InterfaceDeclaration))
                {
                    var decl = node.Parent as TypeDeclarationSyntax;
                    return decl.TypeParameterList == node;
                }
                else if (node.IsParentKind(SyntaxKind.DelegateDeclaration))
                {
                    var decl = node.Parent as DelegateDeclarationSyntax;
                    return decl.TypeParameterList == node;
                }
            }

            return false;
        }

        public static bool IsTypeParameterVarianceContext(this SyntaxToken targetToken)
        {
            // cases:
            // interface IGoo<|
            // interface IGoo<A,|
            // interface IGoo<[Bar]|

            // delegate X D<|
            // delegate X D<A,|
            // delegate X D<[Bar]|
            if (targetToken.Kind() == SyntaxKind.LessThanToken &&
                IsGenericInterfaceOrDelegateTypeParameterList(targetToken.Parent))
            {
                return true;
            }

            if (targetToken.Kind() == SyntaxKind.CommaToken &&
                IsGenericInterfaceOrDelegateTypeParameterList(targetToken.Parent))
            {
                return true;
            }

            if (targetToken.Kind() == SyntaxKind.CloseBracketToken &&
                targetToken.Parent.IsKind(SyntaxKind.AttributeList) &&
                targetToken.Parent.IsParentKind(SyntaxKind.TypeParameter) &&
                IsGenericInterfaceOrDelegateTypeParameterList(targetToken.Parent.Parent.Parent))
            {
                return true;
            }

            return false;
        }

        public static bool IsMandatoryNamedParameterPosition(this SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.CommaToken && token.Parent is BaseArgumentListSyntax)
            {
                var argumentList = (BaseArgumentListSyntax)token.Parent;

                foreach (var item in argumentList.Arguments.GetWithSeparators())
                {
                    if (item.IsToken && item.AsToken() == token)
                    {
                        return false;
                    }

                    if (item.IsNode)
                    {
                        if (item.AsNode() is ArgumentSyntax { NameColon: { } } node)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsNumericTypeContext(this SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(token.Parent is MemberAccessExpressionSyntax memberAccessExpression))
            {
                return false;
            }

            var typeInfo = semanticModel.GetTypeInfo(memberAccessExpression.Expression, cancellationToken);
            return typeInfo.Type.IsNumericType();
        }
    }
}
