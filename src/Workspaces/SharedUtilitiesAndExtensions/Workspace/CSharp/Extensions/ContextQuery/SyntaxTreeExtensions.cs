// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#pragma warning disable IDE0060 // Remove unused parameter - Majority of extension methods in this file have an unused 'SyntaxTree' this parameter for consistency with other Context related extension methods.

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

internal static partial class SyntaxTreeExtensions
{
    private static readonly ISet<SyntaxKind> s_validLocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ExternKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.AsyncKeyword,
            SyntaxKind.UnsafeKeyword,
        };

    public static bool IsAttributeNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        // cases:
        //   [ |
        if (token.IsKind(SyntaxKind.OpenBracketToken) &&
            token.Parent.IsKind(SyntaxKind.AttributeList))
        {
            return true;
        }

        // cases:
        //   [Goo(1), |
        if (token.IsKind(SyntaxKind.CommaToken) &&
            token.Parent.IsKind(SyntaxKind.AttributeList))
        {
            return true;
        }

        // cases:
        //   [specifier: |
        if (token.IsKind(SyntaxKind.ColonToken) &&
            token.Parent.IsKind(SyntaxKind.AttributeTargetSpecifier))
        {
            return true;
        }

        // cases:
        //   [Namespace.|
        if (token.Parent.IsKind(SyntaxKind.QualifiedName) &&
            token.Parent.IsParentKind(SyntaxKind.Attribute))
        {
            return true;
        }

        // cases:
        //   [global::|
        if (token.Parent.IsKind(SyntaxKind.AliasQualifiedName) &&
            token.Parent.IsParentKind(SyntaxKind.Attribute))
        {
            return true;
        }

        return false;
    }

    public static bool IsGlobalMemberDeclarationContext(
        this SyntaxTree syntaxTree,
        int position,
        ISet<SyntaxKind> validModifiers,
        CancellationToken cancellationToken)
    {
        if (!syntaxTree.IsScript())
        {
            return false;
        }

        var tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);
        var parent = token.Parent;

        var modifierTokens = syntaxTree.GetPrecedingModifiers(position, cancellationToken);
        if (modifierTokens.IsEmpty())
        {
            if (token.IsKind(SyntaxKind.CloseBracketToken)
                && parent is AttributeListSyntax attributeList
                && !IsGlobalAttributeList(attributeList))
            {
                // Allow empty modifier tokens if we have an attribute list
                parent = attributeList.Parent;
            }
            else
            {
                return false;
            }
        }

        if (modifierTokens.IsSubsetOf(validModifiers))
        {
            // the parent is the member
            // the grandparent is the container of the member
            // in interactive, it's possible that there might be an intervening "incomplete" member for partially
            // typed declarations that parse ambiguously. For example, "internal e". It's also possible for a
            // complete member to be parsed based on data after the caret, e.g. "unsafe $$ void L() { }".
            if (parent.IsKind(SyntaxKind.CompilationUnit) ||
               (parent is MemberDeclarationSyntax && parent.IsParentKind(SyntaxKind.CompilationUnit)))
            {
                return true;
            }
        }

        return false;

        // Local functions
        static bool IsGlobalAttributeList(AttributeListSyntax attributeList)
        {
            if (attributeList.Target is { Identifier.RawKind: var kind })
            {
                return kind is ((int)SyntaxKind.AssemblyKeyword)
                    or ((int)SyntaxKind.ModuleKeyword);
            }

            return false;
        }
    }

    public static bool IsMemberDeclarationContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        // cases:
        // class C {
        //   |

        // class C {
        //   void Goo() {
        //   }
        //   |

        // class C {
        //   int i;
        //   |

        // class C {
        //   [Goo]
        //   |

        var originalToken = tokenOnLeftOfPosition;
        var token = originalToken;

        // If we're touching the right of an identifier, move back to
        // previous token.
        token = token.GetPreviousTokenIfTouchingWord(position);

        // class C {
        //   |
        if (token.IsKind(SyntaxKind.OpenBraceToken))
        {
            if (token.Parent is BaseTypeDeclarationSyntax)
            {
                return true;
            }
        }

        // class C {
        //   int i;
        //   |
        if (token.IsKind(SyntaxKind.SemicolonToken))
        {
            if (token.Parent is MemberDeclarationSyntax &&
                token.Parent.Parent is BaseTypeDeclarationSyntax)
            {
                return true;
            }
        }

        // class A {
        //   class C {}
        //   |

        // class C {
        //    void Goo() {
        //    }
        //    |
        if (token.IsKind(SyntaxKind.CloseBraceToken))
        {
            if (token.Parent is BaseTypeDeclarationSyntax &&
                token.Parent.Parent is BaseTypeDeclarationSyntax)
            {
                // after a nested type
                return true;
            }
            else if (token.Parent is AccessorListSyntax)
            {
                // after a property
                return true;
            }
            else if (
                token.Parent.IsKind(SyntaxKind.Block) &&
                token.Parent.Parent is MemberDeclarationSyntax)
            {
                // after a method/operator/etc.
                return true;
            }
        }

        // namespace Goo {
        //   [Bar]
        //   |

        if (token.IsKind(SyntaxKind.CloseBracketToken) &&
            token.Parent.IsKind(SyntaxKind.AttributeList))
        {
            // attributes belong to a member which itself is in a
            // container.

            // the parent is the attribute
            // the grandparent is the owner of the attribute
            // the great-grandparent is the container that the owner is in
            var container = token.Parent.Parent?.Parent;
            if (container is BaseTypeDeclarationSyntax)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsMemberDeclarationContext(
        this SyntaxTree syntaxTree,
        int position,
        CSharpSyntaxContext? context,
        ISet<SyntaxKind>? validModifiers,
        ISet<SyntaxKind>? validTypeDeclarations,
        bool canBePartial,
        CancellationToken cancellationToken)
    {
        var typeDecl = context != null
            ? context.ContainingTypeOrEnumDeclaration
            : syntaxTree.GetContainingTypeOrEnumDeclaration(position, cancellationToken);

        if (typeDecl == null)
        {
            return false;
        }

        validTypeDeclarations ??= SpecializedCollections.EmptySet<SyntaxKind>();

        if (!validTypeDeclarations.Contains(typeDecl.Kind()))
        {
            return false;
        }

        // Check many of the simple cases first.
        var leftToken = context != null
            ? context.LeftToken
            : syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

        var token = context != null
            ? context.TargetToken
            : leftToken.GetPreviousTokenIfTouchingWord(position);

        if (token.IsAnyAccessorDeclarationContext(position))
        {
            return false;
        }

        if (syntaxTree.IsMemberDeclarationContext(position, leftToken))
        {
            return true;
        }

        // A member can also show up after certain types of modifiers
        if (canBePartial &&
            token.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
        {
            return true;
        }

        var modifierTokens = context?.PrecedingModifiers ?? syntaxTree.GetPrecedingModifiers(position, cancellationToken);
        if (modifierTokens.IsEmpty())
            return false;

        validModifiers ??= SpecializedCollections.EmptySet<SyntaxKind>();

        if (modifierTokens.IsSubsetOf(validModifiers))
        {
            var member = token.Parent;
            if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
            {
                // rule out async lambdas inside a method
                if (token.GetAncestor<StatementSyntax>() == null)
                {
                    member = token.GetAncestor<MemberDeclarationSyntax>();
                }
            }

            // cases:
            // public |
            // async |
            // public async |
            return member != null &&
                member.Parent is BaseTypeDeclarationSyntax;
        }

        return false;
    }

    public static bool IsLambdaDeclarationContext(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxKind otherModifier,
        CancellationToken cancellationToken)
    {
        var modifierTokens = syntaxTree.GetPrecedingModifiers(position, cancellationToken, out position);
        if (modifierTokens.Count >= 2)
            return false;

        if (modifierTokens.Count == 1)
            return modifierTokens.Contains(otherModifier) && IsLambdaDeclarationContext(syntaxTree, position, SyntaxKind.None, cancellationToken);

        var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        return syntaxTree.IsExpressionContext(position, leftToken, attributes: false, cancellationToken);
    }

    public static bool IsLocalFunctionDeclarationContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        => IsLocalFunctionDeclarationContext(syntaxTree, position, s_validLocalFunctionModifiers, cancellationToken);

    public static bool IsLocalFunctionDeclarationContext(
        this SyntaxTree syntaxTree,
        int position,
        ISet<SyntaxKind> validModifiers,
        CancellationToken cancellationToken)
    {
        var modifierTokens = syntaxTree.GetPrecedingModifiers(position, cancellationToken, out position);

        // if we had modifiers, they have to be legal in this context.
        if (!modifierTokens.IsSubsetOf(validModifiers))
            return false;

        // if we had modifiers, restart the search at the point prior to them.
        if (modifierTokens.Count > 0)
            return IsLocalFunctionDeclarationContext(syntaxTree, position, validModifiers, cancellationToken);

        var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        var token = leftToken.GetPreviousTokenIfTouchingWord(position);

        // if we're after an attribute, restart the check at teh start of the attribute.
        if (token.Kind() == SyntaxKind.CloseBracketToken && token.Parent is AttributeListSyntax)
            return syntaxTree.IsLocalFunctionDeclarationContext(token.Parent.SpanStart, validModifiers, cancellationToken);

        if (syntaxTree.IsStatementContext(position, leftToken, cancellationToken))
            return true;

        return !syntaxTree.IsScript() && syntaxTree.IsGlobalStatementContext(position, cancellationToken);
    }

    public static bool IsTypeDeclarationContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        // cases:
        // root: |

        // extern alias a;
        // |

        // using Goo;
        // |

        // using Goo = Bar;
        // |

        // namespace N {}
        // |

        // namespace N {
        // |

        // class C {}
        // |

        // class C {
        // |

        // class C {
        //   void Goo() {
        //   }
        //   |

        // class C {
        //   int i;
        //   |

        // class C {
        //   [Goo]
        //   |

        // (all the class cases apply to structs, interfaces and records).

        var originalToken = tokenOnLeftOfPosition;
        var token = originalToken;

        // If we're touching the right of an identifier, move back to
        // previous token.
        token = token.GetPreviousTokenIfTouchingWord(position);

        // a type decl can't come before usings/externs
        var nextToken = originalToken.GetNextToken(includeSkipped: true);
        if (nextToken.IsUsingOrExternKeyword() ||
            (nextToken.Kind() == SyntaxKind.GlobalKeyword && nextToken.GetAncestor<UsingDirectiveSyntax>()?.GlobalKeyword == nextToken))
        {
            return false;
        }

        // root: |
        if (token.IsKind(SyntaxKind.None))
        {
            // root namespace

            // a type decl can't come before usings/externs
            if (syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit &&
                (compilationUnit.Externs.Count > 0 || compilationUnit.Usings.Count > 0))
            {
                return false;
            }

            return true;
        }

        if (token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent is NamespaceDeclarationSyntax or TypeDeclarationSyntax)
            return true;

        // extern alias a;
        // |

        // using Goo;
        // |

        // class C {
        //   int i;
        //   |

        // namespace NS;
        // |
        if (token.IsKind(SyntaxKind.SemicolonToken))
        {
            if (token.Parent is (kind: SyntaxKind.ExternAliasDirective or SyntaxKind.UsingDirective))
            {
                return true;
            }
            else if (token.Parent is MemberDeclarationSyntax)
            {
                return true;
            }
        }

        // class C {}
        // |

        // namespace N {}
        // |

        // class C {
        //    void Goo() {
        //    }
        //    |
        if (token.IsKind(SyntaxKind.CloseBraceToken))
        {
            if (token.Parent is BaseTypeDeclarationSyntax)
            {
                return true;
            }
            else if (token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                return true;
            }
            else if (token.Parent is AccessorListSyntax)
            {
                return true;
            }
            else if (
                token.Parent.IsKind(SyntaxKind.Block) &&
                token.Parent.Parent is MemberDeclarationSyntax)
            {
                return true;
            }
        }

        // namespace Goo {
        //   [Bar]
        //   |

        // namespace NS;
        // [Attr]
        // |

        if (token.IsKind(SyntaxKind.CloseBracketToken) &&
            token.Parent.IsKind(SyntaxKind.AttributeList))
        {
            // assembly attributes belong to the containing compilation unit
            if (token.Parent.IsParentKind(SyntaxKind.CompilationUnit))
                return true;

            // other attributes belong to a member which itself is in a
            // container.

            // the parent is the attribute
            // the grandparent is the owner of the attribute
            // the great-grandparent is the container that the owner is in
            var container = token.Parent?.Parent?.Parent;
            if (container is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax)
                return true;
        }

        return false;
    }

    public static bool IsTypeDeclarationContext(
        this SyntaxTree syntaxTree,
        int position,
        CSharpSyntaxContext? context,
        ISet<SyntaxKind>? validModifiers,
        ISet<SyntaxKind>? validTypeDeclarations,
        bool canBePartial,
        CancellationToken cancellationToken)
    {
        // We only allow nested types inside a class, struct, or interface, not inside a
        // an enum.
        var typeDecl = context != null
            ? context.ContainingTypeDeclaration
            : syntaxTree.GetContainingTypeDeclaration(position, cancellationToken);

        validTypeDeclarations ??= SpecializedCollections.EmptySet<SyntaxKind>();

        if (typeDecl != null)
        {
            if (!validTypeDeclarations.Contains(typeDecl.Kind()))
            {
                return false;
            }
        }

        // Check many of the simple cases first.
        var leftToken = context != null
            ? context.LeftToken
            : syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

        // If we're touching the right of an identifier, move back to
        // previous token.
        var token = context != null
            ? context.TargetToken
            : leftToken.GetPreviousTokenIfTouchingWord(position);

        if (token.IsAnyAccessorDeclarationContext(position))
        {
            return false;
        }

        if (syntaxTree.IsTypeDeclarationContext(position, leftToken, cancellationToken))
        {
            return true;
        }

        // A type can also show up after certain types of modifiers
        if (canBePartial &&
            token.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
        {
            return true;
        }

        // using directive is never a type declaration context
        if (token.GetAncestor<UsingDirectiveSyntax>() is not null)
        {
            return false;
        }

        var modifierTokens = context?.PrecedingModifiers ?? syntaxTree.GetPrecedingModifiers(position, cancellationToken);
        if (modifierTokens.IsEmpty())
            return false;

        validModifiers ??= SpecializedCollections.EmptySet<SyntaxKind>();

        if (modifierTokens.IsProperSubsetOf(validModifiers))
        {
            // the parent is the member
            // the grandparent is the container of the member
            var container = token.Parent?.Parent;

            // ref $$
            // readonly ref $$
            if (container is IncompleteMemberSyntax incompleteMember)
                return incompleteMember.Type.IsKind(SyntaxKind.RefType);

            if (container is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax)
                return true;

            if (container is VariableDeclarationSyntax && modifierTokens.Contains(SyntaxKind.FileKeyword))
                return true;
        }

        return false;
    }

    public static bool IsNamespaceContext(
        this SyntaxTree syntaxTree,
        int position,
        CancellationToken cancellationToken,
        SemanticModel? semanticModel = null)
    {
        // first do quick exit check
        if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
            syntaxTree.IsRightOfDotOrArrow(position, cancellationToken))
        {
            return false;
        }

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                              .GetPreviousTokenIfTouchingWord(position);

        // global::
        if (token.IsKind(SyntaxKind.ColonColonToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.GlobalKeyword))
        {
            return true;
        }

        // using |
        // but not:
        // using | = Bar

        // Note: we take care of the using alias case in the IsTypeContext
        // call below.

        if (token.IsKind(SyntaxKind.UsingKeyword))
        {
            var usingDirective = token.GetAncestor<UsingDirectiveSyntax>();
            if (usingDirective != null)
            {
                if (token.GetNextToken(includeSkipped: true).Kind() != SyntaxKind.EqualsToken &&
                    usingDirective.Alias == null)
                {
                    return true;
                }
            }
        }

        // using static |
        // using static unsafe |
        if (token.IsStaticKeywordContextInUsingDirective())
        {
            return true;
        }

        // if it is not using directive location, most of places where 
        // type can appear, namespace can appear as well
        return syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
    }

    public static bool IsNamespaceDeclarationNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (syntaxTree.IsScript() || syntaxTree.IsInNonUserCode(position, cancellationToken))
            return false;

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                              .GetPreviousTokenIfTouchingWord(position);
        if (token == default)
            return false;

        var declaration = token.GetAncestor<BaseNamespaceDeclarationSyntax>();
        if (declaration?.NamespaceKeyword == token)
            return true;

        return declaration?.Name.Span.IntersectsWith(position) == true;
    }

    public static bool IsPartialTypeDeclarationNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, [NotNullWhen(true)] out TypeDeclarationSyntax? declarationSyntax)
    {
        if (!syntaxTree.IsInNonUserCode(position, cancellationToken))
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                  .GetPreviousTokenIfTouchingWord(position);

            if (token.Kind()
                    is SyntaxKind.ClassKeyword
                    or SyntaxKind.StructKeyword
                    or SyntaxKind.InterfaceKeyword &&
                token.GetPreviousToken().IsKind(SyntaxKind.PartialKeyword))
            {
                declarationSyntax = token.GetAncestor<TypeDeclarationSyntax>();
                return declarationSyntax != null && declarationSyntax.Keyword == token;
            }
        }

        declarationSyntax = null;
        return false;
    }

    public static bool IsDefinitelyNotTypeContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
            return true;

        return
            syntaxTree.IsInNonUserCode(position, cancellationToken) ||
            syntaxTree.IsRightOfDotOrArrow(position, cancellationToken);
    }

    public static bool IsTypeContext(
        this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, SemanticModel? semanticModel = null)
    {
        // first do quick exit check
        if (syntaxTree.IsDefinitelyNotTypeContext(position, cancellationToken))
            return false;

        // okay, now it is a case where we can't use parse tree (valid or error recovery) to
        // determine whether it is a right place to put type. use lex based one Cyrus created.

        var tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        return
            syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.RefKeyword, cancellationToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.ReadOnlyKeyword, cancellationToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.CaseKeyword, cancellationToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.EventKeyword, cancellationToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.StackAllocKeyword, cancellationToken) ||
            syntaxTree.IsAttributeNameContext(position, cancellationToken) ||
            syntaxTree.IsBaseClassOrInterfaceContext(position, cancellationToken) ||
            syntaxTree.IsCatchVariableDeclarationContext(position, cancellationToken) ||
            syntaxTree.IsDefiniteCastTypeContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsDelegateReturnTypeContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: true, cancellationToken: cancellationToken, semanticModel: semanticModel) ||
            syntaxTree.IsPrimaryFunctionExpressionContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsGenericTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken, semanticModel) ||
            syntaxTree.IsFunctionPointerTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken) ||
            syntaxTree.IsFixedVariableDeclarationContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsIsOrAsTypeContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsLocalVariableDeclarationContext(position, tokenOnLeftOfPosition, cancellationToken) ||
            syntaxTree.IsObjectCreationTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
            syntaxTree.IsParameterTypeContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
            syntaxTree.IsStatementContext(position, tokenOnLeftOfPosition, cancellationToken) ||
            syntaxTree.IsGlobalStatementContext(position, cancellationToken) ||
            syntaxTree.IsTypeParameterConstraintContext(position, tokenOnLeftOfPosition) ||
            syntaxTree.IsUsingAliasTypeContext(position, cancellationToken) ||
            syntaxTree.IsUsingStaticContext(position, cancellationToken) ||
            syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            syntaxTree.IsPossibleTupleContext(tokenOnLeftOfPosition, position) ||
            syntaxTree.IsMemberDeclarationContext(
                position,
                context: null,
                validModifiers: SyntaxKindSet.AllMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.NonEnumTypeDeclarations,
                canBePartial: true,
                cancellationToken: cancellationToken) ||
            syntaxTree.IsLocalFunctionDeclarationContext(position, cancellationToken);
    }

    public static bool IsBaseClassOrInterfaceContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        // class C : |
        // class C : Bar, |
        // NOT enum E : |

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is SyntaxKind.ColonToken or SyntaxKind.CommaToken &&
            token.Parent is BaseListSyntax { Parent: not EnumDeclarationSyntax })
        {
            return true;
        }

        return false;
    }

    public static bool IsUsingAliasTypeContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        // using Goo = |

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.EqualsToken) &&
            token.GetAncestor<UsingDirectiveSyntax>() != null)
        {
            return true;
        }

        return false;
    }

    public static bool IsUsingStaticContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        // using static |
        // using static unsafe |

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        return token.IsStaticKeywordContextInUsingDirective();
    }

    public static bool IsTypeArgumentOfConstraintClause(
        this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        // cases:
        //   where |
        //   class Goo<T> : Object where |

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.WhereKeyword) &&
            token.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
        {
            return true;
        }

        if (token.IsKind(SyntaxKind.IdentifierToken) &&
            token.HasMatchingText(SyntaxKind.WhereKeyword) &&
            token.Parent.IsKind(SyntaxKind.IdentifierName) &&
            token.Parent.IsParentKind(SyntaxKind.SimpleBaseType) &&
            token.Parent.Parent.IsParentKind(SyntaxKind.BaseList))
        {
            return true;
        }

        return false;
    }

    public static bool IsTypeParameterConstraintStartContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        // cases:
        //   where T : |

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.ColonToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.IdentifierToken) &&
            token.GetPreviousToken(includeSkipped: true).GetPreviousToken().IsKind(SyntaxKind.WhereKeyword))
        {
            return true;
        }

        return false;
    }

    public static bool IsTypeParameterConstraintContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        if (syntaxTree.IsTypeParameterConstraintStartContext(position, tokenOnLeftOfPosition))
        {
            return true;
        }

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        // Can't come after new()
        //
        //    where T : |
        //    where T : class, |
        //    where T : struct, |
        //    where T : Goo, |
        if (token.IsKind(SyntaxKind.CommaToken) &&
            token.Parent is TypeParameterConstraintClauseSyntax constraintClause)
        {
            // Check if there's a 'new()' constraint.  If there isn't, or we're before it, then
            // this is a type parameter constraint context. 
            var firstConstructorConstraint = constraintClause.Constraints.FirstOrDefault(t => t is ConstructorConstraintSyntax);
            if (firstConstructorConstraint == null || firstConstructorConstraint.SpanStart > token.Span.End)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsTypeOfExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OpenParenToken) && token.Parent.IsKind(SyntaxKind.TypeOfExpression))
        {
            return true;
        }

        return false;
    }

    public static bool IsDefaultExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OpenParenToken) && token.Parent.IsKind(SyntaxKind.DefaultExpression))
        {
            return true;
        }

        return false;
    }

    public static bool IsSizeOfExpressionContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OpenParenToken) && token.Parent.IsKind(SyntaxKind.SizeOfExpression))
        {
            return true;
        }

        return false;
    }

    public static bool IsFunctionPointerTypeArgumentContext(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        CancellationToken cancellationToken)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        switch (token.Kind())
        {
            case SyntaxKind.LessThanToken:
            case SyntaxKind.CommaToken:
                return token.Parent.IsKind(SyntaxKind.FunctionPointerParameterList);
        }

        return token switch
        {
            // ref modifiers
            { Parent.RawKind: (int)SyntaxKind.FunctionPointerParameter } => true,
            // Regular type specifiers
            { Parent: TypeSyntax { Parent.RawKind: (int)SyntaxKind.FunctionPointerParameter } } => true,
            _ => false
        };
    }

    public static bool IsGenericConstraintContext(this SyntaxTree syntaxTree, SyntaxToken targetToken)
        => targetToken.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause) &&
           targetToken.Kind() is SyntaxKind.ColonToken or SyntaxKind.CommaToken;

    public static bool IsGenericTypeArgumentContext(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        CancellationToken cancellationToken,
        SemanticModel? semanticModelOpt = null)
    {
        // cases: 
        //    Goo<|
        //    Goo<Bar,|
        //    Goo<Bar<Baz<int[],|
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is not SyntaxKind.LessThanToken and not SyntaxKind.CommaToken)
        {
            return false;
        }

        if (token.Parent is TypeArgumentListSyntax)
        {
            // Easy case, it was known to be a generic name, so this is a type argument context.
            return true;
        }

        if (!syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out var nameToken))
        {
            return false;
        }

        if (nameToken.Parent is not NameSyntax name)
        {
            return false;
        }

        // Looks viable!  If they provided a binding, then check if it binds properly to
        // an actual generic entity.
        if (semanticModelOpt == null)
        {
            // No binding.  Just make the decision based on the syntax tree.
            return true;
        }

        // '?' is syntactically ambiguous in incomplete top-level statements:
        //
        // T ? goo<| 
        //
        // Might be an incomplete conditional expression or an incomplete declaration of a method returning a nullable type.
        // Bind T to see if it is a type. If it is we don't show signature help.
        if (name.IsParentKind(SyntaxKind.LessThanExpression) &&
            name.Parent?.Parent is ConditionalExpressionSyntax conditional &&
            conditional.IsParentKind(SyntaxKind.ExpressionStatement) &&
            conditional.Parent.IsParentKind(SyntaxKind.GlobalStatement))
        {
            var conditionOrType = semanticModelOpt.GetSymbolInfo(conditional.Condition, cancellationToken);
            if (conditionOrType.GetBestOrAllSymbols().FirstOrDefault() is { Kind: SymbolKind.NamedType })
            {
                return false;
            }
        }

        // We have reached the expression:
        //
        // goo.Baz<|
        //
        // This could either be an incomplete generic type or method, or a binary less than operator
        // To ensure that we are in the generic case, we need to match at least one generic method or type,
        // and all other candidates to be types or methods.
        var symbols = semanticModelOpt.LookupName(nameToken, cancellationToken);
        if (symbols.Length == 0)
            return false;

        var anyGeneric = symbols.Any(static s =>
        {
            return s switch
            {
                INamedTypeSymbol nt => nt.Arity > 0,
                IMethodSymbol m => m.Arity > 0,
                _ => false,
            };
        });

        if (!anyGeneric)
            return false;

        return symbols.All(static s => s is INamedTypeSymbol or IMethodSymbol);
    }

    public static bool IsParameterModifierContext(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        bool includeOperators,
        out int parameterIndex,
        out SyntaxKind previousModifier)
    {
        // cases:
        //   Goo(|
        //   Goo(int i, |
        //   Goo([Bar]|
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        parameterIndex = -1;
        previousModifier = SyntaxKind.None;

        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.Parent is ParameterListSyntax parameterList1 &&
            IsSuitableParameterList(parameterList1, includeOperators))
        {
            parameterIndex = 0;
            return true;
        }

        if (token.IsKind(SyntaxKind.LessThanToken) && token.Parent.IsKind(SyntaxKind.FunctionPointerParameterList))
        {
            parameterIndex = 0;
            return true;
        }

        if (token.IsKind(SyntaxKind.CommaToken) &&
            token.Parent is ParameterListSyntax parameterList2 &&
            IsSuitableParameterList(parameterList2, includeOperators))
        {
            var commaIndex = parameterList2.Parameters.GetWithSeparators().IndexOf(token);

            parameterIndex = commaIndex / 2 + 1;
            return true;
        }

        if (token.IsKind(SyntaxKind.CommaToken) &&
            token.Parent is FunctionPointerParameterListSyntax funcPtrParamList)
        {
            var commaIndex = funcPtrParamList.Parameters.GetWithSeparators().IndexOf(token);

            parameterIndex = commaIndex / 2 + 1;
            return true;
        }

        if (token.IsKind(SyntaxKind.CloseBracketToken) &&
            token.Parent.IsKind(SyntaxKind.AttributeList) &&
            token.Parent.Parent is ParameterSyntax parameter3 &&
            parameter3.Parent is ParameterListSyntax parameterList3 &&
            IsSuitableParameterList(parameterList3, includeOperators))
        {
            parameterIndex = parameterList3.Parameters.IndexOf(parameter3);
            return true;
        }

        ParameterSyntax? parameter4 = null;
        if (token.Kind() is SyntaxKind.RefKeyword or SyntaxKind.InKeyword or SyntaxKind.ReadOnlyKeyword or SyntaxKind.OutKeyword or SyntaxKind.ThisKeyword or SyntaxKind.ParamsKeyword or SyntaxKind.ScopedKeyword)
        {
            parameter4 = token.Parent as ParameterSyntax;
            previousModifier = token.Kind();
        }
        else if (token.IsKind(SyntaxKind.IdentifierToken) && token.Text == "scoped" && token.Parent is IdentifierNameSyntax scopedIdentifierName)
        {
            parameter4 = scopedIdentifierName.Parent as ParameterSyntax;
            previousModifier = SyntaxKind.ScopedKeyword;
        }

        if (parameter4 is { Parent: ParameterListSyntax parameterList4 } &&
            IsSuitableParameterList(parameterList4, includeOperators))
        {
            parameterIndex = parameterList4.Parameters.IndexOf(parameter4);
            return true;
        }

        return false;

        static bool IsSuitableParameterList(ParameterListSyntax parameterList, bool includeOperators)
            => parameterList.Parent switch
            {
                MethodDeclarationSyntax or LocalFunctionStatementSyntax or ConstructorDeclarationSyntax or DelegateDeclarationSyntax or TypeDeclarationSyntax => true,
                OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax when includeOperators => true,
                _ => false,
            };
    }

    public static bool IsParamsModifierContext(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        CancellationToken cancellationToken)
    {
        if (syntaxTree.IsParameterModifierContext(position, tokenOnLeftOfPosition, includeOperators: false, out _, out var previousModifier) &&
            previousModifier == SyntaxKind.None)
        {
            return true;
        }

        if (syntaxTree.IsPossibleLambdaParameterModifierContext(position, tokenOnLeftOfPosition, cancellationToken))
        {
            return true;
        }

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken)
        {
            return token.Parent.IsKind(SyntaxKind.BracketedParameterList);
        }

        return false;
    }

    public static bool IsDelegateReturnTypeContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.DelegateKeyword) &&
            token.Parent.IsKind(SyntaxKind.DelegateDeclaration))
        {
            return true;
        }

        return false;
    }

    public static bool IsImplicitOrExplicitOperatorTypeContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OperatorKeyword) &&
            token.GetPreviousToken(includeSkipped: true).Kind() is SyntaxKind.ImplicitKeyword or SyntaxKind.ExplicitKeyword)
        {
            return true;
        }

        return false;
    }

    public static bool IsParameterTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);
        if (syntaxTree.IsParameterModifierContext(position, tokenOnLeftOfPosition, includeOperators: true, out _, out _))
            return true;

        // int this[ |
        // int this[int i, |
        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken &&
            token.Parent is (kind: SyntaxKind.ParameterList or SyntaxKind.BracketedParameterList))
        {
            return true;
        }

        return false;
    }

    public static bool IsPossibleExtensionMethodContext(this SyntaxTree syntaxTree, SyntaxToken tokenOnLeftOfPosition)
    {
        var method = tokenOnLeftOfPosition.Parent.GetAncestorOrThis<MethodDeclarationSyntax>();
        var typeDecl = method.GetAncestorOrThis<TypeDeclarationSyntax>();

        return method != null && typeDecl != null &&
               typeDecl.IsKind(SyntaxKind.ClassDeclaration) &&
               method.Modifiers.Any(SyntaxKind.StaticKeyword) &&
               typeDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    public static bool IsPossibleLambdaParameterModifierContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
        {
            if (token.Parent.IsKind(SyntaxKind.ParameterList) &&
                token.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                return true;
            }

            // TODO(cyrusn): Tie into semantic analysis system to only consider this a lambda if this is a location
            // where the lambda's type would be inferred because of a delegate or Expression<T> type.
            //
            // ERROR tolerance.  Cast expressions can show up with partially written lambdas like so:
            //
            //      var lambda = (x$$)
            //      NextStatement();
            //
            // Check if the expression of the cast is on the same line as us or not to see if we want to
            // consider this a lambda, or just a cast.

            if (token.Parent is (kind: SyntaxKind.ParenthesizedExpression or SyntaxKind.TupleExpression or SyntaxKind.CastExpression))
                return true;
        }

        return false;
    }

    public static bool IsAnonymousMethodParameterModifierContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        SyntaxNode? parent;
        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
        {
            parent = token.Parent;
        }
        else if (token.IsKind(SyntaxKind.ScopedKeyword) && token.Parent.IsKind(SyntaxKind.Parameter))
        {
            parent = token.Parent.Parent;
        }
        else if (token.IsKind(SyntaxKind.IdentifierToken) && token.Text == "scoped" && token.Parent is IdentifierNameSyntax scopedIdentifierName && scopedIdentifierName.Parent.IsKind(SyntaxKind.Parameter))
        {
            parent = scopedIdentifierName.Parent.Parent;
        }
        else
        {
            return false;
        }

        return parent.IsKind(SyntaxKind.ParameterList) && parent.IsParentKind(SyntaxKind.AnonymousMethodExpression);
    }

    public static bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is SyntaxKind.RefKeyword or SyntaxKind.InKeyword or SyntaxKind.OutKeyword)
        {
            position = token.SpanStart;
            tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        }

        if (IsAnonymousMethodParameterModifierContext(syntaxTree, position, tokenOnLeftOfPosition) ||
            IsPossibleLambdaParameterModifierContext(syntaxTree, position, tokenOnLeftOfPosition, cancellationToken))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Are you possibly typing a tuple type or expression?
    /// This is used to suppress colon as a completion trigger (so that you can type element names).
    /// This is also used to recommend some keywords (like var).
    /// </summary>
    public static bool IsPossibleTupleContext(this SyntaxTree syntaxTree, SyntaxToken leftToken, int position)
    {
        leftToken = leftToken.GetPreviousTokenIfTouchingWord(position);

        // ($$
        // (a, $$
        if (IsPossibleTupleOpenParenOrComma(leftToken))
        {
            return true;
        }

        // ((a, b) $$
        // (..., (a, b) $$
        if (leftToken.IsKind(SyntaxKind.CloseParenToken))
        {
            if (leftToken.Parent is (kind:
                    SyntaxKind.ParenthesizedExpression or
                    SyntaxKind.TupleExpression or
                    SyntaxKind.TupleType))
            {
                var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent);
                if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
                {
                    return true;
                }
            }
        }

        // (a $$
        // (..., b $$
        if (leftToken.IsKind(SyntaxKind.IdentifierToken))
        {
            var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent!);
            if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
            {
                return true;
            }
        }

        // (a.b $$
        // (..., a.b $$
        if (leftToken.IsKind(SyntaxKind.IdentifierToken) &&
            leftToken.Parent.IsKind(SyntaxKind.IdentifierName) &&
            leftToken.Parent.Parent is (kind: SyntaxKind.QualifiedName or SyntaxKind.SimpleMemberAccessExpression))
        {
            var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent.Parent);
            if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsAtStartOfPattern(this SyntaxTree syntaxTree, SyntaxToken leftToken, int position)
    {
        leftToken = leftToken.GetPreviousTokenIfTouchingWord(position);

        if (leftToken.IsKind(SyntaxKind.OpenParenToken))
        {
            if (leftToken.Parent is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                // If we're dealing with an expression surrounded by one or more sets of open parentheses, we need to
                // walk up the parens in order to see if we're actually at the start of a valid pattern or not.
                return IsAtStartOfPattern(syntaxTree, parenthesizedExpression.GetFirstToken().GetPreviousToken(), parenthesizedExpression.SpanStart);
            }

            // e is ((($$ 1 or 2)))
            if (leftToken.Parent.IsKind(SyntaxKind.ParenthesizedPattern))
            {
                return true;
            }
        }

        // case $$
        // is $$
        if (leftToken.Kind() is SyntaxKind.CaseKeyword or SyntaxKind.IsKeyword)
        {
            return true;
        }

        // e switch { $$
        // e switch { ..., $$
        if (leftToken.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CommaToken && leftToken.Parent.IsKind(SyntaxKind.SwitchExpression))
        {
            return true;
        }

        // e is ($$
        // e is (..., $$
        if (leftToken.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken && leftToken.Parent.IsKind(SyntaxKind.PositionalPatternClause))
        {
            return true;
        }

        // e is [$$
        // e is [..., $$
        if (leftToken.Kind() is SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken && leftToken.Parent.IsKind(SyntaxKind.ListPattern))
        {
            return true;
        }

        // e is [..$$
        // e is [..., ..$$
        if (leftToken.IsKind(SyntaxKind.DotDotToken) && leftToken.Parent.IsKind(SyntaxKind.SlicePattern))
        {
            return true;
        }

        // e is { P: $$
        // e is { ..., P: $$
        // e is { ..., P.P2: $$
        if (leftToken.IsKind(SyntaxKind.ColonToken) && leftToken.Parent is (kind: SyntaxKind.NameColon or SyntaxKind.ExpressionColon) &&
            leftToken.Parent.IsParentKind(SyntaxKind.Subpattern))
        {
            return true;
        }

        // e is 1 and $$
        // e is 1 or $$
        // e is SomeEnum.SomeEnumValue and $$
        // e is SomeEnum.SomeEnumValue or $$
        // 'and' & 'or' are identifier in the last 2 examples because of lack of context
        if (leftToken.IsKindOrHasMatchingText(SyntaxKind.AndKeyword) || leftToken.IsKindOrHasMatchingText(SyntaxKind.OrKeyword))
        {
            return leftToken.Parent is BinaryPatternSyntax ||
                   leftToken.Parent is SingleVariableDesignationSyntax { Parent: DeclarationPatternSyntax };
        }

        // e is not $$
        if (leftToken.IsKind(SyntaxKind.NotKeyword) && leftToken.Parent.IsKind(SyntaxKind.NotPattern))
        {
            return true;
        }

        // e is > $$
        // e is >= $$
        // e is < $$
        // e is <= $$
        if (leftToken.Kind() is SyntaxKind.GreaterThanToken or SyntaxKind.GreaterThanEqualsToken or SyntaxKind.LessThanToken or SyntaxKind.LessThanEqualsToken &&
            leftToken.Parent.IsKind(SyntaxKind.RelationalPattern))
        {
            return true;
        }

        return false;
    }

    public static bool IsAtEndOfPattern(this SyntaxTree syntaxTree, SyntaxToken leftToken, int position)
    {
        var originalLeftToken = leftToken;
        leftToken = leftToken.GetPreviousTokenIfTouchingWord(position);

        // For instance:
        // e is { A.$$ }
        // e is { A->$$ }
        if (leftToken.IsKind(SyntaxKind.DotToken) ||
            leftToken.IsKind(SyntaxKind.MinusGreaterThanToken))
        {
            return false;
        }

        var patternSyntax = leftToken.GetAncestor<PatternSyntax>();
        if (patternSyntax != null)
        {
            var lastTokenInPattern = patternSyntax.GetLastToken();

            // This check should cover the majority of cases, e.g.:
            // e is 1 $$
            // e is >= 0 $$
            // e is { P: (1 $$
            // e is { P: (1) $$
            if (leftToken == lastTokenInPattern)
            {
                // Patterns such as 'e is not $$', 'e is 1 or $$', 'e is ($$', and 'e is null or global::$$' should be invalid here
                // as they are incomplete patterns.
                return leftToken.Kind() is not (SyntaxKind.OrKeyword
                    or SyntaxKind.AndKeyword
                    or SyntaxKind.NotKeyword
                    or SyntaxKind.OpenParenToken
                    or SyntaxKind.ColonColonToken
                    or SyntaxKind.DotDotToken
                    or SyntaxKind.OpenBraceToken);
            }

            // We want to make sure that IsAtEndOfPattern returns true even when the user is in the middle of typing a keyword
            // after a pattern.
            // For example, with the keyword 'and', we want to make sure that 'e is int an$$' is still recognized as valid.
            if (lastTokenInPattern.Parent is SingleVariableDesignationSyntax variableDesignationSyntax &&
                originalLeftToken.Parent == variableDesignationSyntax)
            {
                return patternSyntax is DeclarationPatternSyntax or RecursivePatternSyntax;
            }

            // e is (expr) a$$
            //
            // this will be parsed as a constant-pattern where the constant expression is a cast expression (if 'expr'
            // is a legal type).
            if (patternSyntax is ConstantPatternSyntax { Expression: CastExpressionSyntax { Expression: IdentifierNameSyntax } castExpression } &&
                leftToken == castExpression.CloseParenToken)
            {
                return true;
            }
        }

        // e is C.P $$
        // e is int $$
        if (leftToken.IsLastTokenOfNode<TypeSyntax>(out var typeSyntax))
        {
            // If typeSyntax is part of a qualified name, we want to get the fully-qualified name so that we can
            // later accurately perform the check comparing the right side of the BinaryExpressionSyntax to
            // the typeSyntax.
            while (typeSyntax.Parent is TypeSyntax parentTypeSyntax)
            {
                typeSyntax = parentTypeSyntax;
            }

            if (typeSyntax.Parent is BinaryExpressionSyntax binaryExpressionSyntax &&
                binaryExpressionSyntax.OperatorToken.IsKind(SyntaxKind.IsKeyword) &&
                binaryExpressionSyntax.Right == typeSyntax && !typeSyntax.IsVar)
            {
                return true;
            }
        }

        // We need to include a special case for switch statement cases, as some are not currently parsed as patterns, e.g. case (1 $$
        if (IsAtEndOfSwitchStatementPattern(leftToken))
        {
            return true;
        }

        return false;

        static bool IsAtEndOfSwitchStatementPattern(SyntaxToken leftToken)
        {
            SyntaxNode? node = leftToken.Parent as ExpressionSyntax;
            if (node == null)
                return false;

            // Walk up the right edge of all complete expressions.
            while (node is ExpressionSyntax && node.GetLastToken(includeZeroWidth: true) == leftToken)
                node = node.GetRequiredParent();

            // Getting rid of the extra parentheses to deal with cases such as 'case (((1 $$'
            while (node is ParenthesizedExpressionSyntax)
                node = node.GetRequiredParent();

            // case (1 $$
            if (node is CaseSwitchLabelSyntax { Parent: SwitchSectionSyntax })
                return true;

            return false;
        }
    }

    private static SyntaxToken FindTokenOnLeftOfNode(SyntaxNode node)
        => node.FindTokenOnLeftOfPosition(node.SpanStart);

    public static bool IsPossibleTupleOpenParenOrComma(this SyntaxToken possibleCommaOrParen)
    {
        if (possibleCommaOrParen.Kind() is not (SyntaxKind.OpenParenToken or SyntaxKind.CommaToken))
        {
            return false;
        }

        if (possibleCommaOrParen.Parent is (kind:
                SyntaxKind.ParenthesizedExpression or
                SyntaxKind.TupleExpression or
                SyntaxKind.TupleType or
                SyntaxKind.CastExpression))
        {
            return true;
        }

        // in script
        if (possibleCommaOrParen.Parent.IsKind(SyntaxKind.ParameterList) &&
            possibleCommaOrParen.Parent?.Parent is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            if (parenthesizedLambda.ArrowToken.IsMissing)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Are you possibly in the designation part of a deconstruction?
    /// This is used to enter suggestion mode (suggestions become soft-selected).
    /// </summary>
    public static bool IsPossibleDeconstructionDesignation(this SyntaxTree syntaxTree,
        int position, CancellationToken cancellationToken)
    {
        var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        leftToken = leftToken.GetPreviousTokenIfTouchingWord(position);

        // The well-formed cases:
        // var ($$, y) = e;
        // (var $$, var y) = e;
        if (leftToken.Parent.IsKind(SyntaxKind.ParenthesizedVariableDesignation) ||
            leftToken.Parent.IsParentKind(SyntaxKind.ParenthesizedVariableDesignation))
        {
            return true;
        }

        // (var $$, var y)
        // (var x, var y)
        if (syntaxTree.IsPossibleTupleContext(leftToken, position) && !IsPossibleTupleOpenParenOrComma(leftToken))
        {
            return true;
        }

        // var ($$)
        // var (x, $$)
        if (IsPossibleVarDeconstructionOpenParenOrComma(leftToken))
        {
            return true;
        }

        // var (($$), y)
        if (leftToken.IsKind(SyntaxKind.OpenParenToken) && leftToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
        {
            if (IsPossibleVarDeconstructionOpenParenOrComma(FindTokenOnLeftOfNode(leftToken.Parent)))
            {
                return true;
            }
        }

        // var ((x, $$), y)
        // var (($$, x), y)
        if (leftToken.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken && leftToken.Parent.IsKind(SyntaxKind.TupleExpression))
        {
            if (IsPossibleVarDeconstructionOpenParenOrComma(FindTokenOnLeftOfNode(leftToken.Parent)))
            {
                return true;
            }
        }

        // foreach (var ($$
        // foreach (var ((x, $$), y)
        if (leftToken.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
        {
            var outer = UnwrapPossibleTuple(leftToken.Parent!);
            if (outer.Parent is ForEachStatementSyntax @foreach)
            {
                if (@foreach.Expression == outer &&
                    @foreach.Type is IdentifierNameSyntax identifierName &&
                    identifierName.Identifier.ValueText == "var")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// If inside a parenthesized or tuple expression, unwrap the nestings and return the container.
    /// </summary>
    private static SyntaxNode UnwrapPossibleTuple(SyntaxNode node)
    {
        while (true)
        {
            if (node.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                node = node.Parent;
                continue;
            }

            if (node.Parent.IsKind(SyntaxKind.Argument) && node.Parent.Parent.IsKind(SyntaxKind.TupleExpression))
            {
                node = node.Parent.Parent;
                continue;
            }

            return node;
        }
    }

    private static bool IsPossibleVarDeconstructionOpenParenOrComma(SyntaxToken leftToken)
    {
        if (leftToken.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken &&
            leftToken.Parent.IsKind(SyntaxKind.ArgumentList) &&
            leftToken.Parent?.Parent is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == "var")
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasNames(this TupleExpressionSyntax tuple)
        => tuple.Arguments.Any(a => a.NameColon != null);

    public static bool IsValidContextForFromClause(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        CancellationToken cancellationToken,
        SemanticModel? semanticModelOpt = null)
    {
        if (syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: false, cancellationToken: cancellationToken, semanticModel: semanticModelOpt) &&
            !syntaxTree.IsConstantExpressionContext(position, tokenOnLeftOfPosition))
        {
            return true;
        }

        // cases:
        //   var q = |
        //   var q = f|
        //
        //   var q = from x in y
        //           |
        //
        //   var q = from x in y
        //           f|
        //
        // this list is *not* exhaustive.
        // the first two are handled by 'IsExpressionContext'

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        // var q = from x in y
        //         |
        if (!token.IntersectsWith(position) &&
            token.IsLastTokenOfQueryClause())
        {
            return true;
        }

        return false;
    }

    public static bool IsValidContextForJoinClause(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        // var q = from x in y
        //         |
        if (!token.IntersectsWith(position) &&
            token.IsLastTokenOfQueryClause())
        {
            return true;
        }

        return false;
    }

    public static bool IsLocalVariableDeclarationContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        // cases:
        //  const var
        //  out var
        //  for (var
        //  foreach (var
        //  await foreach (var
        //  using (var
        //  await using (var
        //  from var
        //  join var
        //  using var
        //  await using var
        //  scoped var

        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        // const |
        if (token.IsKind(SyntaxKind.ConstKeyword) &&
            token.Parent.IsKind(SyntaxKind.LocalDeclarationStatement))
        {
            return true;
        }

        // ref |
        // ref readonly |
        // for ( ref |
        // foreach ( ref | x
        if (token.Kind() is SyntaxKind.RefKeyword or SyntaxKind.ReadOnlyKeyword)
        {
            var parent = token.Parent;
            if (parent is (kind: SyntaxKind.RefType or SyntaxKind.RefExpression or SyntaxKind.LocalDeclarationStatement))
            {
                if (parent.IsParentKind(SyntaxKind.VariableDeclaration) &&
                    parent.Parent?.Parent is (kind:
                        SyntaxKind.LocalDeclarationStatement or
                        SyntaxKind.ForStatement or
                        SyntaxKind.ForEachVariableStatement))
                {
                    return true;
                }

                if (parent.Parent is (kind: SyntaxKind.ForEachStatement or SyntaxKind.ForEachVariableStatement))
                {
                    return true;
                }
            }
        }

        // out |
        if (token.IsKind(SyntaxKind.OutKeyword) &&
            token.Parent is ArgumentSyntax argument &&
            argument.RefKindKeyword == token)
        {
            return true;
        }

        // for ( |
        // foreach ( |
        // await foreach ( |
        // using ( |
        // await using ( |
        if (token.IsKind(SyntaxKind.OpenParenToken))
        {
            var previous = token.GetPreviousToken(includeSkipped: true);
            if (previous.Kind() is SyntaxKind.ForKeyword or SyntaxKind.ForEachKeyword or SyntaxKind.UsingKeyword)
                return true;
        }

        // using |
        // await using |
        if (token.IsKind(SyntaxKind.UsingKeyword) &&
            token.Parent is LocalDeclarationStatementSyntax)
        {
            return true;
        }

        // from |
        var tokenOnLeftOfStart = syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken);
        if (token.IsKindOrHasMatchingText(SyntaxKind.FromKeyword) &&
            syntaxTree.IsValidContextForFromClause(token.SpanStart, tokenOnLeftOfStart, cancellationToken))
        {
            return true;
        }

        // join |
        if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.JoinKeyword) &&
            syntaxTree.IsValidContextForJoinClause(token.SpanStart, tokenOnLeftOfStart))
        {
            return true;
        }

        // scoped |
        // The compiler parses this as an identifier whose parent is:
        // - ExpressionStatementSyntax when in method declaration.
        // - IncompleteMemberSyntax when in top-level code and there are no class declarations after it.
        // - BaseTypeDeclarationSyntax if it comes after scoped
        // - VariableDeclarationSyntax for `scoped X` inside method declaration
        if (token.IsKind(SyntaxKind.IdentifierToken) && token.Text == "scoped" && token.Parent.IsKind(SyntaxKind.IdentifierName) && token.Parent.Parent is VariableDeclarationSyntax or ExpressionStatementSyntax or IncompleteMemberSyntax)
        {
            return true;
        }

        // scoped v|
        if (token.IsKind(SyntaxKind.ScopedKeyword) && token.Parent is IncompleteMemberSyntax or ScopedTypeSyntax)
        {
            return true;
        }

        return false;
    }

    public static bool IsFixedVariableDeclarationContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        // cases:
        //  fixed (var

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.FixedKeyword))
        {
            return true;
        }

        return false;
    }

    public static bool IsCatchVariableDeclarationContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        // cases:
        //  catch (var

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.CatchKeyword))
        {
            return true;
        }

        return false;
    }

    public static bool IsIsOrAsTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is SyntaxKind.IsKeyword or SyntaxKind.AsKeyword)
            return true;

        return false;
    }

    public static bool IsObjectCreationTypeContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.NewKeyword))
        {
            // we can follow a 'new' if it's the 'new' for an expression.
            var start = token.SpanStart;
            var tokenOnLeftOfStart = syntaxTree.FindTokenOnLeftOfPosition(start, cancellationToken);
            return
                IsNonConstantExpressionContext(syntaxTree, token.SpanStart, tokenOnLeftOfStart, cancellationToken) ||
                syntaxTree.IsStatementContext(token.SpanStart, tokenOnLeftOfStart, cancellationToken) ||
                syntaxTree.IsGlobalStatementContext(token.SpanStart, cancellationToken);
        }

        return false;
    }

    private static bool IsNonConstantExpressionContext(SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        return
            syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: true, cancellationToken: cancellationToken) &&
            !syntaxTree.IsConstantExpressionContext(position, tokenOnLeftOfPosition);
    }

    public static bool IsPreProcessorDirectiveContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);

        return syntaxTree.IsPreProcessorDirectiveContext(position, leftToken, cancellationToken);
    }

    public static bool IsStatementContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
#if false
        // we're in a statement if the thing that comes before allows for
        // statements to follow.  Or if we're on a just started identifier
        // in the first position where a statement can go.
        if (syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken))
        {
            return false;
        }
#endif

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        return token.IsBeginningOfStatementContext();
    }

    public static bool IsGlobalStatementContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
            return false;

        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                              .GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.None))
        {
            // global statements can't come before usings/externs
            if (syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit &&
                (compilationUnit.Externs.Count > 0 || compilationUnit.Usings.Count > 0))
            {
                return false;
            }

            return true;
        }

        return token.IsBeginningOfGlobalStatementContext();
    }

    public static bool IsInstanceContext(this SyntaxTree syntaxTree, SyntaxToken targetToken, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
#if false
        if (syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken))
        {
            return false;
        }
#endif

        // It's possible the caller is asking about a speculative semantic model, and may have moved before the
        // bounds of that model (for example, while looking at the nearby tokens around an edit).  If so, ensure we
        // walk outwards to the correct model to actually ask this question of.
        var position = targetToken.SpanStart;
        if (semanticModel.IsSpeculativeSemanticModel && position < semanticModel.OriginalPositionForSpeculation)
            semanticModel = semanticModel.GetOriginalSemanticModel();

        var enclosingSymbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);

        while (enclosingSymbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction } method)
        {
            if (method.IsStatic)
                return false;

            // It is allowed to reference the instance (`this`) within a local function or anonymous function, as long as the containing method allows it
            enclosingSymbol = enclosingSymbol.ContainingSymbol;
        }

        return enclosingSymbol is { IsStatic: false };
    }

    public static bool IsPossibleCastTypeContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.OpenParenToken) &&
            syntaxTree.IsExpressionContext(token.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken), false, cancellationToken))
        {
            return true;
        }

        return false;
    }

    public static bool IsDefiniteCastTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.Parent.IsKind(SyntaxKind.CastExpression))
        {
            return true;
        }

        return false;
    }

    public static bool IsConstantExpressionContext(this SyntaxTree syntaxTree, int position,
        SyntaxToken tokenOnLeftOfPosition)
    {
        if (IsAtStartOfPattern(syntaxTree, tokenOnLeftOfPosition, position))
        {
            return true;
        }

        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        // goto case |
        if (token.IsKind(SyntaxKind.CaseKeyword) &&
            token.Parent.IsKind(SyntaxKind.GotoCaseStatement))
        {
            return true;
        }

        if (token.IsKind(SyntaxKind.EqualsToken) &&
            token.Parent is EqualsValueClauseSyntax equalsValue)
        {
            if (equalsValue.IsParentKind(SyntaxKind.VariableDeclarator) &&
                equalsValue.Parent.IsParentKind(SyntaxKind.VariableDeclaration))
            {
                // class C { const int i = |
                var fieldDeclaration = equalsValue.GetAncestor<FieldDeclarationSyntax>();
                if (fieldDeclaration != null)
                {
                    return fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword);
                }

                // void M() { const int i = |
                var localDeclaration = equalsValue.GetAncestor<LocalDeclarationStatementSyntax>();
                if (localDeclaration != null)
                {
                    return localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword);
                }
            }

            // enum E { A = |
            if (equalsValue.IsParentKind(SyntaxKind.EnumMemberDeclaration))
            {
                return true;
            }

            // void M(int i = |
            if (equalsValue.IsParentKind(SyntaxKind.Parameter))
            {
                return true;
            }
        }

        // [Goo( |
        // [Goo(x, |
        if (token.Parent.IsKind(SyntaxKind.AttributeArgumentList) &&
            token.Kind() is SyntaxKind.CommaToken or SyntaxKind.OpenParenToken)
        {
            return true;
        }

        // [Goo(x: |
        if (token.IsKind(SyntaxKind.ColonToken) &&
            token.Parent.IsKind(SyntaxKind.NameColon) &&
            token.Parent.IsParentKind(SyntaxKind.AttributeArgument))
        {
            return true;
        }

        // [Goo(X = |
        if (token.IsKind(SyntaxKind.EqualsToken) &&
            token.Parent.IsKind(SyntaxKind.NameEquals) &&
            token.Parent.IsParentKind(SyntaxKind.AttributeArgument))
        {
            return true;
        }

        // TODO: Fixed-size buffer declarations

        return false;
    }

    public static bool IsLabelContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

        var gotoStatement = token.GetAncestor<GotoStatementSyntax>();
        if (gotoStatement != null)
        {
            if (gotoStatement.GotoKeyword == token)
            {
                return true;
            }

            if (gotoStatement.Expression != null &&
                !gotoStatement.Expression.IsMissing &&
                gotoStatement.Expression is IdentifierNameSyntax &&
                ((IdentifierNameSyntax)gotoStatement.Expression).Identifier == token &&
                token.IntersectsWith(position))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsExpressionContext(
        this SyntaxTree syntaxTree,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        bool attributes,
        CancellationToken cancellationToken,
        SemanticModel? semanticModel = null)
    {
        // cases:
        //   var q = |
        //   var q = a|
        // this list is *not* exhaustive.

        var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

        if (token.GetAncestor<ConditionalDirectiveTriviaSyntax>() != null)
        {
            return false;
        }

        if (!attributes)
        {
            if (token.GetAncestor<AttributeListSyntax>() != null)
            {
                return false;
            }
        }

        if (syntaxTree.IsConstantExpressionContext(position, tokenOnLeftOfPosition))
        {
            return true;
        }

        // no expressions after .   ::   ->
        if (token.Kind()
                is SyntaxKind.DotToken
                or SyntaxKind.ColonColonToken
                or SyntaxKind.MinusGreaterThanToken)
        {
            return false;
        }

        // Normally you can have any sort of expression after an equals. However, this does not
        // apply to a "using Goo = ..." situation.
        if (token.IsKind(SyntaxKind.EqualsToken))
        {
            if (token.Parent.IsKind(SyntaxKind.NameEquals) &&
                token.Parent.IsParentKind(SyntaxKind.UsingDirective))
            {
                return false;
            }
        }

        // q = |
        // q -= |
        // q *= |
        // q += |
        // q /= |
        // q ^= |
        // q %= |
        // q &= |
        // q |= |
        // q <<= |
        // q >>= |
        // q >>>= |
        // q ??= |
        if (token.Kind()
                is SyntaxKind.EqualsToken
                or SyntaxKind.MinusEqualsToken
                or SyntaxKind.AsteriskEqualsToken
                or SyntaxKind.PlusEqualsToken
                or SyntaxKind.SlashEqualsToken
                or SyntaxKind.ExclamationEqualsToken
                or SyntaxKind.CaretEqualsToken
                or SyntaxKind.AmpersandEqualsToken
                or SyntaxKind.BarEqualsToken
                or SyntaxKind.PercentEqualsToken
                or SyntaxKind.LessThanLessThanEqualsToken
                or SyntaxKind.GreaterThanGreaterThanEqualsToken
                or SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken
                or SyntaxKind.QuestionQuestionEqualsToken)
        {
            return true;
        }

        // ( |
        if (token.IsKind(SyntaxKind.OpenParenToken))
        {
            if (token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
                return true;

            // If there's a string in the parenthesis in the code below, the parser would return
            // a CastExpression instead of ParenthesizedExpression. However, some features like keyword completion
            // might be able tolerate this and still want to treat it as a ParenthesizedExpression.
            //
            //         var data = (n$$)
            //         M();
            if (token.Parent is CastExpressionSyntax castExpression &&
                (castExpression.Expression.IsMissing || castExpression.CloseParenToken.TrailingTrivia.GetFirstNewLine().HasValue))
            {
                return true;
            }
        }

        // - |
        // + |
        // ~ |
        // ! |
        if (token.Parent is PrefixUnaryExpressionSyntax prefix)
        {
            return prefix.OperatorToken == token;
        }

        // not sure about these:
        //   ++ |
        //   -- |
#if false
            token.Kind == SyntaxKind.PlusPlusToken ||
            token.Kind == SyntaxKind.DashDashToken)
#endif
        // await |
        if (token.Parent is AwaitExpressionSyntax awaitExpression)
        {
            return awaitExpression.AwaitKeyword == token;
        }

        // Check for binary operators.
        // Note:
        //   - We handle < specially as it can be ambiguous with generics.
        //   - We handle * specially because it can be ambiguous with pointer types.

        // a *
        // a /
        // a %
        // a +
        // a -
        // a <<
        // a >>
        // a <
        // a >
        // a &&
        // a ||
        // a &
        // a |
        // a ^
        if (token.Parent is BinaryExpressionSyntax binary)
        {
            // If the client provided a binding, then check if this is actually generic.  If so,
            // then this is not an expression context. i.e. if we have "Goo < |" then it could
            // be an expression context, or it could be a type context if Goo binds to a type or
            // method.
            if (semanticModel != null && syntaxTree.IsGenericTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken, semanticModel))
            {
                return false;
            }

            if (binary.OperatorToken == token)
            {
                // If this is a multiplication expression and a semantic model was passed in,
                // check to see if the expression to the left is a type name. If it is, treat
                // this as a pointer type.
                if (token.IsKind(SyntaxKind.AsteriskToken) && semanticModel != null)
                {
                    if (binary.Left is TypeSyntax type && type.IsPotentialTypeName(semanticModel, cancellationToken))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Special case:
        //    Goo * bar
        //    Goo ? bar
        // This parses as a local decl called bar of type Goo* or Goo?
        if (tokenOnLeftOfPosition.IntersectsWith(position) &&
            tokenOnLeftOfPosition.IsKind(SyntaxKind.IdentifierToken))
        {
            var previousToken = tokenOnLeftOfPosition.GetPreviousToken(includeSkipped: true);
            if (previousToken.Kind() is SyntaxKind.AsteriskToken or SyntaxKind.QuestionToken &&
                previousToken.Parent?.Kind() is SyntaxKind.PointerType or SyntaxKind.NullableType)
            {
                var type = previousToken.Parent as TypeSyntax;
                if (type.IsParentKind(SyntaxKind.VariableDeclaration) &&
                    type.Parent?.Parent is LocalDeclarationStatementSyntax declStatement)
                {
                    // note, this doesn't apply for cases where we know it 
                    // absolutely is not multiplication or a conditional expression.
                    var underlyingType = type is PointerTypeSyntax pointerType
                        ? pointerType.ElementType
                        : ((NullableTypeSyntax)type).ElementType;

                    if (!underlyingType.IsPotentialTypeName(semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }
        }

        // new int[|
        // new int[expr, |
        if (token.Kind() is SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier))
        {
            return true;
        }

        // 1..|
        // not 1.|.
        if (token.IsKind(SyntaxKind.DotDotToken) &&
            token.Parent.IsKind(SyntaxKind.RangeExpression) &&
            position >= token.Span.End)
        {
            return true;
        }

        // goo ? |
        if (token.IsKind(SyntaxKind.QuestionToken) &&
            token.Parent is ConditionalExpressionSyntax conditionalExpression)
        {
            // If the condition is simply a TypeSyntax that binds to a type, treat this as a nullable type.
            return conditionalExpression.Condition is not TypeSyntax type
                || !type.IsPotentialTypeName(semanticModel, cancellationToken);
        }

        // goo ? bar : |
        if (token.IsKind(SyntaxKind.ColonToken) &&
            token.Parent.IsKind(SyntaxKind.ConditionalExpression))
        {
            return true;
        }

        // typeof(|
        // default(|
        // sizeof(|
        if (token.IsKind(SyntaxKind.OpenParenToken))
        {
            if (token.Parent is (kind: SyntaxKind.TypeOfExpression or SyntaxKind.DefaultExpression or SyntaxKind.SizeOfExpression))
            {
                return false;
            }
        }

        // var(|
        // var(id, |
        // Those are more likely to be deconstruction-declarations being typed than invocations a method "var"
        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken &&
            token.IsInvocationOfVarExpression())
        {
            return false;
        }

        // Goo(|
        // Goo(expr, |
        // this[|
        // var t = (1, |
        // var t = (| , 2)
        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken &&
            token.Parent is (kind: SyntaxKind.ArgumentList or SyntaxKind.BracketedArgumentList or SyntaxKind.TupleExpression))
        {
            return true;
        }

        // [Goo(|
        // [Goo(expr, |
        if (attributes)
        {
            if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
            {
                if (token.Parent.IsKind(SyntaxKind.AttributeArgumentList))
                {
                    return true;
                }
            }
        }

        // Goo(ref |
        // Goo(in |
        // Goo(out |
        // ref var x = ref |
        if (token.Kind() is SyntaxKind.RefKeyword or SyntaxKind.InKeyword or SyntaxKind.OutKeyword)
        {
            if (token.Parent.IsKind(SyntaxKind.Argument))
            {
                return true;
            }
            else if (token.Parent.IsKind(SyntaxKind.RefExpression))
            {
                // ( ref |
                // parenthesized expressions can't directly contain RefExpression, unless the user is typing an incomplete lambda expression.
                if (token.Parent.IsParentKind(SyntaxKind.ParenthesizedExpression))
                {
                    return false;
                }

                return true;
            }
        }

        // Goo(bar: |
        if (token.IsKind(SyntaxKind.ColonToken) &&
            token.Parent.IsKind(SyntaxKind.NameColon) &&
            token.Parent.IsParentKind(SyntaxKind.Argument))
        {
            return true;
        }

        // a => |
        if (token.IsKind(SyntaxKind.EqualsGreaterThanToken))
        {
            return true;
        }

        // new List<int> { |
        // new List<int> { expr, |
        if (token.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CommaToken)
        {
            if (token.Parent is InitializerExpressionSyntax)
            {
                // The compiler treats the ambiguous case as an object initializer, so we'll say
                // expressions are legal here
                if (token.Parent.IsKind(SyntaxKind.ObjectInitializerExpression) && token.IsKind(SyntaxKind.OpenBraceToken))
                {
                    // In this position { a$$ =, the user is trying to type an object initializer.
                    if (!token.IntersectsWith(position) && token.GetNextToken().GetNextToken().IsKind(SyntaxKind.EqualsToken))
                    {
                        return false;
                    }

                    return true;
                }

                // Perform a semantic check to determine whether or not the type being created
                // can support a collection initializer. If not, this must be an object initializer
                // and can't be an expression context.
                if (semanticModel != null &&
                    token.Parent?.Parent is ObjectCreationExpressionSyntax objectCreation)
                {
                    var containingSymbol = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
                    if (semanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol is ITypeSymbol type && !type.CanSupportCollectionInitializer(containingSymbol))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // for (; |
        // for (; ; |
        if (token.IsKind(SyntaxKind.SemicolonToken) &&
            token.Parent is ForStatementSyntax forStatement)
        {
            if (token == forStatement.FirstSemicolonToken ||
                token == forStatement.SecondSemicolonToken)
            {
                return true;
            }
        }

        // for ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.Parent is ForStatementSyntax forStatement2 &&
            token == forStatement2.OpenParenToken)
        {
            return true;
        }

        // for (; ; Goo(), | 
        // for ( Goo(), |
        if (token.IsKind(SyntaxKind.CommaToken) &&
            token.Parent.IsKind(SyntaxKind.ForStatement))
        {
            return true;
        }

        // foreach (var v in |
        // await foreach (var v in |
        // from a in |
        // join b in |
        if (token.IsKind(SyntaxKind.InKeyword))
        {
            if (token.Parent is (kind:
                    SyntaxKind.ForEachStatement or
                    SyntaxKind.ForEachVariableStatement or
                    SyntaxKind.FromClause or
                    SyntaxKind.JoinClause))
            {
                return true;
            }
        }

        // join x in y on |
        // join x in y on a equals |
        if (token.Kind() is SyntaxKind.OnKeyword or SyntaxKind.EqualsKeyword &&
            token.Parent.IsKind(SyntaxKind.JoinClause))
        {
            return true;
        }

        // where |
        if (token.IsKind(SyntaxKind.WhereKeyword) &&
            token.Parent.IsKind(SyntaxKind.WhereClause))
        {
            return true;
        }

        // orderby |
        // orderby a, |
        if (token.Kind() is SyntaxKind.OrderByKeyword or SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.OrderByClause))
        {
            return true;
        }

        // select |
        if (token.IsKind(SyntaxKind.SelectKeyword) &&
            token.Parent.IsKind(SyntaxKind.SelectClause))
        {
            return true;
        }

        // group |
        // group expr by |
        if (token.Kind() is SyntaxKind.GroupKeyword or SyntaxKind.ByKeyword &&
            token.Parent.IsKind(SyntaxKind.GroupClause))
        {
            return true;
        }

        // return |
        // yield return |
        // but not: [return |
        if (token.IsKind(SyntaxKind.ReturnKeyword))
        {
            if (token.GetPreviousToken(includeSkipped: true).Kind() != SyntaxKind.OpenBracketToken)
            {
                return true;
            }
        }

        // throw |
        if (token.IsKind(SyntaxKind.ThrowKeyword))
        {
            return true;
        }

        // while ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.WhileKeyword))
        {
            return true;
        }

        // todo: handle 'for' cases.

        // using ( |
        // await using ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) && token.Parent.IsKind(SyntaxKind.UsingStatement))
        {
            return true;
        }

        // lock ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.LockKeyword))
        {
            return true;
        }

        // lock ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.IfKeyword))
        {
            return true;
        }

        // switch ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.SwitchKeyword))
        {
            return true;
        }

        // checked ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.CheckedKeyword))
        {
            return true;
        }

        // unchecked ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.UncheckedKeyword))
        {
            return true;
        }

        // when ( |
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.WhenKeyword))
        {
            return true;
        }

        // case ... when |
        if (token.IsKind(SyntaxKind.WhenKeyword) && token.Parent.IsKind(SyntaxKind.WhenClause))
        {
            return true;
        }

        // (SomeType) |
        if (token.IsAfterPossibleCast())
        {
            return true;
        }

        // In anonymous type initializer.
        //
        // new { | We allow new inside of anonymous object member declarators, so that the user
        // can dot into a member afterward. For example:
        //
        // var a = new { new C().Goo };
        if (token.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
        {
            return true;
        }

        // List patterns
        // is [ |
        // is [ 0, |
        if (token.Kind() is SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.ListPattern))
        {
            return true;
        }

        // Collection expressions
        // [|
        // [0, |
        if (token.Kind() is SyntaxKind.OpenBracketToken or SyntaxKind.DotDotToken or SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.CollectionExpression))
        {
            return true;
        }

        // Spread elements in collection expressions
        // [.. |
        // [0, .. |
        if (token.Kind() is SyntaxKind.DotDotToken &&
            token.Parent.IsKind(SyntaxKind.SpreadElement))
        {
            return true;
        }

        // $"{ |
        // $@"{ |
        // $"""{ |
        // $"{x} { |
        // $@"{x} { |
        // $"""{x} { |
        if (token.IsKind(SyntaxKind.OpenBraceToken))
        {
            return token.Parent is InterpolationSyntax interpolation
                && interpolation.OpenBraceToken == token;
        }

        return false;
    }

    public static bool IsInvocationOfVarExpression(this SyntaxToken token)
        => token.Parent?.Parent is InvocationExpressionSyntax invocation &&
           invocation.Expression.ToString() == "var";

    public static bool IsNameOfContext(this SyntaxTree syntaxTree, int position, SemanticModel? semanticModelOpt = null, CancellationToken cancellationToken = default)
    {
        var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        // nameof(Goo.|
        // nameof(Goo.Bar.|
        // Locate the open paren.
        if (token.IsKind(SyntaxKind.DotToken))
        {
            // Could have been parsed as member access
            if (token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var parentMemberAccess = token.Parent;
                while (parentMemberAccess.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    parentMemberAccess = parentMemberAccess.Parent;
                }

                if (parentMemberAccess.Parent.IsKind(SyntaxKind.Argument) &&
                    parentMemberAccess.Parent.IsChildNode<ArgumentListSyntax>(a => a.Arguments.FirstOrDefault()))
                {
                    token = ((ArgumentListSyntax)parentMemberAccess.Parent.Parent!).OpenParenToken;
                }
            }

            // Could have been parsed as a qualified name.
            if (token.Parent.IsKind(SyntaxKind.QualifiedName))
            {
                var parentQualifiedName = token.Parent;
                while (parentQualifiedName.Parent.IsKind(SyntaxKind.QualifiedName))
                {
                    parentQualifiedName = parentQualifiedName.Parent;
                }

                if (parentQualifiedName.Parent.IsKind(SyntaxKind.Argument) &&
                    parentQualifiedName.Parent.IsChildNode<ArgumentListSyntax>(a => a.Arguments.FirstOrDefault()))
                {
                    token = ((ArgumentListSyntax)parentQualifiedName.Parent.Parent!).OpenParenToken;
                }
            }
        }

        ExpressionSyntax? parentExpression = null;

        // if the nameof expression has a missing close paren, it is parsed as an invocation expression.
        if (token.Parent.IsKind(SyntaxKind.ArgumentList) &&
            token.Parent.Parent is InvocationExpressionSyntax invocationExpression &&
            invocationExpression.IsNameOfInvocation())
        {
            parentExpression = invocationExpression;
        }

        if (parentExpression != null)
        {
            if (semanticModelOpt == null)
            {
                return true;
            }

            return semanticModelOpt.GetSymbolInfo(parentExpression, cancellationToken).Symbol == null;
        }

        return false;
    }

    public static bool IsIsOrAsOrSwitchOrWithExpressionContext(
        this SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        int position,
        SyntaxToken tokenOnLeftOfPosition,
        CancellationToken cancellationToken)
    {
        // cases:
        //    expr |

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        // Not if the position is a numeric literal
        if (token.IsKind(SyntaxKind.NumericLiteralToken))
            return false;

        if (token.GetAncestor<BlockSyntax>() == null &&
            token.GetAncestor<ArrowExpressionClauseSyntax>() == null)
        {
            return false;
        }

        // is/as/with are valid after expressions.
        if (token.IsLastTokenOfNode<ExpressionSyntax>(out var expression))
        {
            // 'is/as/with/switch' not allowed after a anonymous-method/lambda.
            if (expression is AnonymousFunctionExpressionSyntax)
                return false;

            // is/as/with/switch also not allowed after a naked method group reference (e.g. `this.ToString is`).
            // They are allowed after a method invocation (e.g. `this.ToString() is`).
            var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).GetAnySymbol();
            if (symbol is IMethodSymbol && expression is not InvocationExpressionSyntax)
                return false;

            // However, many names look like expressions.  For example:
            //    foreach (var |
            // ('var' is a TypeSyntax which is an expression syntax.

            var type = token.GetAncestors<TypeSyntax>().LastOrDefault();
            if (type == null)
                return true;

            if (type.Kind() is SyntaxKind.GenericName or SyntaxKind.AliasQualifiedName or SyntaxKind.PredefinedType)
                return false;

            ExpressionSyntax nameExpr = type;
            if (IsRightSideName(nameExpr))
            {
                nameExpr = (ExpressionSyntax)nameExpr.Parent!;
            }

            // If this name is the start of a local variable declaration context, we
            // shouldn't show is or as. For example: for(var |
            if (syntaxTree.IsLocalVariableDeclarationContext(token.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken), cancellationToken))
                return false;

            // Not on the left hand side of an object initializer
            if (token.IsKind(SyntaxKind.IdentifierToken) &&
                token.Parent.IsKind(SyntaxKind.IdentifierName) &&
                token.Parent.Parent is (kind: SyntaxKind.ObjectInitializerExpression or SyntaxKind.CollectionInitializerExpression))
            {
                return false;
            }

            // Not after an 'out' declaration expression. For example: M(out var |
            if (token.Kind() is SyntaxKind.IdentifierToken &&
                token.Parent.IsKind(SyntaxKind.IdentifierName))
            {
                if (token.Parent.Parent is ArgumentSyntax { RefOrOutKeyword.RawKind: (int)SyntaxKind.OutKeyword })
                    return false;
            }

            if (token.Text == SyntaxFacts.GetText(SyntaxKind.AsyncKeyword))
            {
                // async $$
                //
                // 'async' will look like a normal identifier.  But we don't want to follow it
                // with 'is' or 'as' or 'with' if it's actually the start of a lambda.
                var delegateType = CSharpTypeInferenceService.Instance.InferDelegateType(
                    semanticModel, token.SpanStart, cancellationToken);
                if (delegateType != null)
                {
                    return false;
                }
            }

            // case X $$
            //
            // while `X` is in an expr context, it's a limited one that doesn't support the full breadth of operators like these.
            var tokenBeforeName = syntaxTree.FindTokenOnLeftOfPosition(nameExpr.SpanStart, cancellationToken);
            if (tokenBeforeName.Kind() == SyntaxKind.CaseKeyword)
                return false;

            // Now, make sure the name was actually in a location valid for
            // an expression.  If so, then we know we can follow it.
            if (syntaxTree.IsExpressionContext(nameExpr.SpanStart, tokenBeforeName, attributes: false, cancellationToken))
                return true;
        }

        return false;
    }

    private static bool IsRightSideName(ExpressionSyntax name)
    {
        if (name.Parent != null)
        {
            switch (name.Parent.Kind())
            {
                case SyntaxKind.QualifiedName:
                    return ((QualifiedNameSyntax)name.Parent).Right == name;
                case SyntaxKind.AliasQualifiedName:
                    return ((AliasQualifiedNameSyntax)name.Parent).Name == name;
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)name.Parent).Name == name;
            }
        }

        return false;
    }

    public static bool IsCatchOrFinallyContext(
        this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        // cases:
        // try { 
        // } |

        // try {
        // } c|

        // try {
        // } catch {
        // } |

        // try {
        // } catch {
        // } c|

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.IsKind(SyntaxKind.CloseBraceToken))
        {
            var block = token.GetAncestor<BlockSyntax>();

            if (block != null &&
                token == block.GetLastToken(includeSkipped: true) &&
                block.Parent?.Kind() is SyntaxKind.TryStatement or SyntaxKind.CatchClause)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsCatchFilterContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
    {
        // cases:
        //  catch |
        //  catch i|
        //  catch (declaration) |
        //  catch (declaration) i|

        var token = tokenOnLeftOfPosition;
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.CatchKeyword))
        {
            return true;
        }

        if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.CloseParenToken) &&
            token.Parent.IsKind(SyntaxKind.CatchDeclaration))
        {
            return true;
        }

        return false;
    }

    public static bool IsBaseListContext(this SyntaxTree syntaxTree, SyntaxToken targetToken)
    {
        // Options:
        //  class E : |
        //  class E : i|
        //  class E : i, |
        //  class E : i, j|

        return
            targetToken is (kind: SyntaxKind.ColonToken or SyntaxKind.CommaToken) &&
            targetToken.Parent is BaseListSyntax { Parent: TypeDeclarationSyntax };
    }

    public static bool IsEnumBaseListContext(this SyntaxTree syntaxTree, SyntaxToken targetToken)
    {
        // Options:
        //  enum E : |
        //  enum E : i|

        return
            targetToken.IsKind(SyntaxKind.ColonToken) &&
            targetToken.Parent.IsKind(SyntaxKind.BaseList) &&
            targetToken.Parent.IsParentKind(SyntaxKind.EnumDeclaration);
    }

    public static bool IsEnumTypeMemberAccessContext(this SyntaxTree syntaxTree, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        var token = syntaxTree
            .FindTokenOnLeftOfPosition(position, cancellationToken)
            .GetPreviousTokenIfTouchingWord(position);

        if (!token.IsKind(SyntaxKind.DotToken))
        {
            return false;
        }

        SymbolInfo leftHandBinding;
        if (token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            var memberAccess = (MemberAccessExpressionSyntax)token.Parent;
            leftHandBinding = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken);
        }
        else if (token.Parent is QualifiedNameSyntax qualifiedName &&
            token.Parent?.Parent is BinaryExpressionSyntax(SyntaxKind.IsExpression) binaryExpression &&
            binaryExpression.Right == qualifiedName)
        {
            // The right-hand side of an is expression could be an enum
            leftHandBinding = semanticModel.GetSymbolInfo(qualifiedName.Left, cancellationToken);
        }
        else if (token.Parent is QualifiedNameSyntax qualifiedName1 &&
            token.Parent?.Parent is DeclarationPatternSyntax declarationExpression &&
            declarationExpression.Type == qualifiedName1)
        {
            // The right-hand side of an is declaration expression could be an enum
            leftHandBinding = semanticModel.GetSymbolInfo(qualifiedName1.Left, cancellationToken);
        }
        else
        {
            return false;
        }

        var symbol = leftHandBinding.GetBestOrAllSymbols().FirstOrDefault();

        if (symbol == null)
        {
            return false;
        }

        switch (symbol.Kind)
        {
            case SymbolKind.NamedType:
                return ((INamedTypeSymbol)symbol).TypeKind == TypeKind.Enum;
            case SymbolKind.Alias:
                var target = ((IAliasSymbol)symbol).Target;
                return target.IsType && ((ITypeSymbol)target).TypeKind == TypeKind.Enum;
        }

        return false;
    }

    public static bool IsFunctionPointerCallingConventionContext(this SyntaxTree syntaxTree, SyntaxToken targetToken)
    {
        return targetToken.IsKind(SyntaxKind.AsteriskToken) &&
               targetToken.GetPreviousToken().IsKind(SyntaxKind.DelegateKeyword);
    }
}
