// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

internal static partial class SyntaxTokenExtensions
{
    public static bool IsUsingOrExternKeyword(this SyntaxToken token)
    {
        return
            token.Kind() is SyntaxKind.UsingKeyword or
            SyntaxKind.ExternKeyword;
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

    public static bool IsStaticKeywordContextInUsingDirective(this SyntaxToken token)
    {
        // using static |
        if (token is { RawKind: (int)SyntaxKind.StaticKeyword, Parent: UsingDirectiveSyntax })
        {
            return true;
        }

        // using static unsafe |
        if (token.IsKind(SyntaxKind.UnsafeKeyword) &&
            token.GetPreviousToken() is { RawKind: (int)SyntaxKind.StaticKeyword, Parent: UsingDirectiveSyntax })
        {
            return true;
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

        // After attribute lists on a statement:
        //   [Bar]
        //   |

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
                        if (token.Parent.Parent.Kind() is not SyntaxKind.TryStatement and not SyntaxKind.DoStatement)
                            return true;
                    }
                    else if (token.Parent.Parent?.Kind()
                            is SyntaxKind.ElseClause
                            or SyntaxKind.FinallyClause
                            or SyntaxKind.CatchClause
                            or SyntaxKind.SwitchSection)
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
                return token.Parent is (kind: SyntaxKind.CaseSwitchLabel or SyntaxKind.DefaultSwitchLabel or SyntaxKind.CasePatternSwitchLabel or SyntaxKind.LabeledStatement);

            case SyntaxKind.DoKeyword when token.Parent.IsKind(SyntaxKind.DoStatement):
                return true;

            case SyntaxKind.CloseParenToken:
                var parent = token.Parent;
                return parent?.Kind()
                    is SyntaxKind.ForStatement
                    or SyntaxKind.ForEachStatement
                    or SyntaxKind.ForEachVariableStatement
                    or SyntaxKind.WhileStatement
                    or SyntaxKind.IfStatement
                    or SyntaxKind.LockStatement
                    or SyntaxKind.UsingStatement
                    or SyntaxKind.FixedStatement;

            case SyntaxKind.ElseKeyword:
                return token.Parent.IsKind(SyntaxKind.ElseClause);

            case SyntaxKind.CloseBracketToken:
                if (token.Parent.IsKind(SyntaxKind.AttributeList))
                {
                    // attributes can belong to a statement
                    var container = token.Parent.Parent;
                    if (container is StatementSyntax)
                        return true;
                }

                return false;
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
                return true;

            // Need this check to check file scoped namespace declarations prior to a catch-all of 
            // member declarations since otherwise it would return true.
            if (token.Parent is FileScopedNamespaceDeclarationSyntax namespaceDeclaration && namespaceDeclaration.SemicolonToken == token)
                return false;

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
                    return true;
            }

            if (token.Parent.IsKind(SyntaxKind.AttributeList))
            {
                var container = token.Parent.Parent;
                if (container is IncompleteMemberSyntax && container.Parent is CompilationUnitSyntax)
                    return true;
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

            if (token.Parent is ParenthesizedExpressionSyntax parenExpr)
            {
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
        if (targetToken.Kind() is SyntaxKind.IfKeyword or
            SyntaxKind.ElifKeyword)
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
        if (targetToken.Parent is PrefixUnaryExpressionSyntax prefix)
        {
            return prefix.OperatorToken == targetToken;
        }

        // a &&
        // a ||
        if (targetToken.Parent is BinaryExpressionSyntax binary)
        {
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

        if (targetToken.Kind() is not (SyntaxKind.IdentifierToken or SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken))
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
            if (targetToken.Parent is (kind: SyntaxKind.CaseSwitchLabel or SyntaxKind.DefaultSwitchLabel or SyntaxKind.CasePatternSwitchLabel))
            {
                return true;
            }
        }

        if (targetToken.Kind() is SyntaxKind.SemicolonToken or
            SyntaxKind.CloseBraceToken)
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
        return targetToken.Kind() is SyntaxKind.CommaToken or SyntaxKind.OpenParenToken &&
               targetToken.Parent is (kind: SyntaxKind.CrefBracketedParameterList or SyntaxKind.CrefParameterList);
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
            targetToken.Parent.Parent.IsKind(SyntaxKind.Argument) &&
            targetToken.Parent.Parent.Parent.IsKind(SyntaxKind.ArgumentList))
        {
            var owner = targetToken.Parent.Parent.Parent.Parent;
            if (owner?.Kind()
                    is SyntaxKind.InvocationExpression
                    or SyntaxKind.ObjectCreationExpression
                    or SyntaxKind.BaseConstructorInitializer
                    or SyntaxKind.ThisConstructorInitializer)
            {
                return true;
            }
        }

        if (targetToken.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
        {
            if (targetToken.Parent.IsKind(SyntaxKind.ArgumentList))
            {
                if (targetToken.Parent?.Parent?.Kind()
                        is SyntaxKind.ObjectCreationExpression
                        or SyntaxKind.BaseConstructorInitializer
                        or SyntaxKind.ThisConstructorInitializer)
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
            targetToken.GetAncestors<MemberDeclarationSyntax>().Any(m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword) ||
            targetToken.GetAncestors<LocalFunctionStatementSyntax>().Any(f => f.GetModifiers().Any(SyntaxKind.UnsafeKeyword))) ||
            targetToken.GetAncestors<UsingDirectiveSyntax>().Any(d => d.UnsafeKeyword.IsKind(SyntaxKind.UnsafeKeyword));
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
        //   int Goo { readonly |

        // Consume all preceding access modifiers
        while (targetToken.Kind() is SyntaxKind.InternalKeyword or
            SyntaxKind.PublicKeyword or
            SyntaxKind.ProtectedKeyword or
            SyntaxKind.PrivateKeyword or
            SyntaxKind.ReadOnlyKeyword)
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

    private static bool IsGenericInterfaceOrDelegateTypeParameterList([NotNullWhen(true)] SyntaxNode? node)
    {
        if (node.IsKind(SyntaxKind.TypeParameterList))
        {
            if (node?.Parent is TypeDeclarationSyntax(SyntaxKind.InterfaceDeclaration) typeDecl)
                return typeDecl.TypeParameterList == node;
            else if (node?.Parent is DelegateDeclarationSyntax delegateDecl)
                return delegateDecl.TypeParameterList == node;
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
            targetToken.Parent.Parent.IsKind(SyntaxKind.TypeParameter) &&
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
                    if (item.AsNode() is ArgumentSyntax node && node.NameColon != null)
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
        if (token.Parent is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return false;
        }

        var typeInfo = semanticModel.GetTypeInfo(memberAccessExpression.Expression, cancellationToken);
        return typeInfo.Type.IsNumericType();
    }

    public static bool IsTypeNamedDynamic(this SyntaxToken token)
        => token.Parent is IdentifierNameSyntax typedParent &&
           SyntaxFacts.IsInTypeOnlyContext(typedParent) &&
           token.Text == "dynamic";
}
