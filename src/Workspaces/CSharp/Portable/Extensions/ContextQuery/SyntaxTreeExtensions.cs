// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static class SyntaxTreeExtensions
    {
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

            var modifierTokens = syntaxTree.GetPrecedingModifiers(position, tokenOnLeftOfPosition);
            if (modifierTokens.IsEmpty())
            {
                return false;
            }

            if (modifierTokens.IsSubsetOf(validModifiers))
            {
                // the parent is the member
                // the grandparent is the container of the member
                // in interactive, it's possible that there might be an intervening "incomplete" member for partially
                // typed declarations that parse ambiguously. For example, "internal e".
                if (token.Parent.IsKind(SyntaxKind.CompilationUnit) ||
                   (token.Parent.IsKind(SyntaxKind.IncompleteMember) && token.Parent.IsParentKind(SyntaxKind.CompilationUnit)))
                {
                    return true;
                }
            }

            return false;
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
            CSharpSyntaxContext contextOpt,
            ISet<SyntaxKind> validModifiers,
            ISet<SyntaxKind> validTypeDeclarations,
            bool canBePartial,
            CancellationToken cancellationToken)
        {
            var typeDecl = contextOpt != null
                ? contextOpt.ContainingTypeOrEnumDeclaration
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
            var leftToken = contextOpt != null
                ? contextOpt.LeftToken
                : syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            var token = contextOpt != null
                ? contextOpt.TargetToken
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

            var modifierTokens = contextOpt != null
                ? contextOpt.PrecedingModifiers
                : syntaxTree.GetPrecedingModifiers(position, leftToken);

            if (modifierTokens.IsEmpty())
            {
                return false;
            }

            validModifiers ??= SpecializedCollections.EmptySet<SyntaxKind>();

            if (modifierTokens.IsSubsetOf(validModifiers))
            {
                var member = token.Parent;
                if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
                {
                    // second appearance of "async", not followed by modifier: treat it as type
                    if (syntaxTree.GetPrecedingModifiers(token.SpanStart, token).Any(x => x == SyntaxKind.AsyncKeyword))
                    {
                        return false;
                    }

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

        public static bool IsLocalFunctionDeclarationContext(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken)
        {
            var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var token = leftToken.GetPreviousTokenIfTouchingWord(position);

            // Local functions are always valid in a statement context
            if (syntaxTree.IsStatementContext(position, leftToken, cancellationToken))
            {
                return true;
            }

            // Also valid after certain modifiers
            var validModifiers = SyntaxKindSet.LocalFunctionModifiers;

            var modifierTokens = syntaxTree.GetPrecedingModifiers(
                position, token, out var beforeModifiersPosition);

            if (modifierTokens.IsSubsetOf(validModifiers))
            {
                if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
                {
                    // second appearance of "async" not followed by modifier: treat as type
                    if (syntaxTree.GetPrecedingModifiers(token.SpanStart, token)
                        .Contains(SyntaxKind.AsyncKeyword))
                    {
                        return false;
                    }
                }

                leftToken = syntaxTree.FindTokenOnLeftOfPosition(beforeModifiersPosition, cancellationToken);
                token = leftToken.GetPreviousTokenIfTouchingWord(beforeModifiersPosition);
                return syntaxTree.IsStatementContext(beforeModifiersPosition, token, cancellationToken);
            }

            return false;
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

            var originalToken = tokenOnLeftOfPosition;
            var token = originalToken;

            // If we're touching the right of an identifier, move back to
            // previous token.
            token = token.GetPreviousTokenIfTouchingWord(position);

            // a type decl can't come before usings/externs
            if (originalToken.GetNextToken(includeSkipped: true).IsUsingOrExternKeyword())
            {
                return false;
            }

            // root: |
            if (token.IsKind(SyntaxKind.None))
            {
                // root namespace

                // a type decl can't come before usings/externs
                if (syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit &&
                    (compilationUnit.Externs.Count > 0 ||
                    compilationUnit.Usings.Count > 0))
                {
                    return false;
                }

                return true;
            }

            if (token.IsKind(SyntaxKind.OpenBraceToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration))
                {
                    return true;
                }
                else if (token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    return true;
                }
            }

            // extern alias a;
            // |

            // using Goo;
            // |

            // class C {
            //   int i;
            //   |
            if (token.IsKind(SyntaxKind.SemicolonToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ExternAliasDirective, SyntaxKind.UsingDirective))
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

            if (token.IsKind(SyntaxKind.CloseBracketToken) &&
                token.Parent.IsKind(SyntaxKind.AttributeList))
            {
                // assembly attributes belong to the containing compilation unit
                if (token.Parent.IsParentKind(SyntaxKind.CompilationUnit))
                {
                    return true;
                }

                // other attributes belong to a member which itself is in a
                // container.

                // the parent is the attribute
                // the grandparent is the owner of the attribute
                // the great-grandparent is the container that the owner is in
                var container = token.Parent?.Parent?.Parent;
                if (container.IsKind(SyntaxKind.CompilationUnit) ||
                    container.IsKind(SyntaxKind.NamespaceDeclaration) ||
                    container.IsKind(SyntaxKind.ClassDeclaration) ||
                    container.IsKind(SyntaxKind.StructDeclaration))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTypeDeclarationContext(
            this SyntaxTree syntaxTree,
            int position,
            CSharpSyntaxContext contextOpt,
            ISet<SyntaxKind> validModifiers,
            ISet<SyntaxKind> validTypeDeclarations,
            bool canBePartial,
            CancellationToken cancellationToken)
        {
            // We only allow nested types inside a class, struct, or interface, not inside a
            // an enum.
            var typeDecl = contextOpt != null
                ? contextOpt.ContainingTypeDeclaration
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
            var leftToken = contextOpt != null
                ? contextOpt.LeftToken
                : syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            // If we're touching the right of an identifier, move back to
            // previous token.
            var token = contextOpt != null
                ? contextOpt.TargetToken
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

            // using static | is never a type declaration context
            if (token.IsStaticKeywordInUsingDirective())
            {
                return false;
            }

            var modifierTokens = contextOpt != null
                ? contextOpt.PrecedingModifiers
                : syntaxTree.GetPrecedingModifiers(position, leftToken);

            if (modifierTokens.IsEmpty())
            {
                return false;
            }

            validModifiers ??= SpecializedCollections.EmptySet<SyntaxKind>();

            if (modifierTokens.IsProperSubsetOf(validModifiers))
            {
                // the parent is the member
                // the grandparent is the container of the member
                var container = token.Parent?.Parent;

                // ref $$
                // readonly ref $$
                if (container.IsKind(SyntaxKind.IncompleteMember))
                {
                    return ((IncompleteMemberSyntax)container).Type.IsKind(SyntaxKind.RefType);
                }

                if (container.IsKind(SyntaxKind.CompilationUnit) ||
                    container.IsKind(SyntaxKind.NamespaceDeclaration) ||
                    container.IsKind(SyntaxKind.ClassDeclaration) ||
                    container.IsKind(SyntaxKind.StructDeclaration))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNamespaceContext(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            SemanticModel semanticModelOpt = null)
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
            if (token.IsStaticKeywordInUsingDirective())
            {
                return true;
            }

            // if it is not using directive location, most of places where 
            // type can appear, namespace can appear as well
            return syntaxTree.IsTypeContext(position, cancellationToken, semanticModelOpt);
        }

        public static bool IsNamespaceDeclarationNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree.IsScript() || syntaxTree.IsInNonUserCode(position, cancellationToken))
            {
                return false;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                  .GetPreviousTokenIfTouchingWord(position);

            var declaration = token.GetAncestor<NamespaceDeclarationSyntax>();

            return declaration != null && (declaration.Name.Span.IntersectsWith(position) || declaration.NamespaceKeyword == token);
        }

        public static bool IsPartialTypeDeclarationNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, out TypeDeclarationSyntax declarationSyntax)
        {
            if (!syntaxTree.IsInNonUserCode(position, cancellationToken))
            {
                var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                      .GetPreviousTokenIfTouchingWord(position);

                if ((token.IsKind(SyntaxKind.ClassKeyword) ||
                     token.IsKind(SyntaxKind.StructKeyword) ||
                     token.IsKind(SyntaxKind.InterfaceKeyword)) &&
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
            return
                syntaxTree.IsInNonUserCode(position, cancellationToken) ||
                syntaxTree.IsRightOfDotOrArrow(position, cancellationToken);
        }

        public static bool IsTypeContext(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, SemanticModel semanticModelOpt = null)
        {
            // first do quick exit check
            if (syntaxTree.IsDefinitelyNotTypeContext(position, cancellationToken))
            {
                return false;
            }

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
                syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: true, cancellationToken: cancellationToken, semanticModelOpt: semanticModelOpt) ||
                syntaxTree.IsPrimaryFunctionExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsGenericTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken, semanticModelOpt) ||
                syntaxTree.IsFixedVariableDeclarationContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsIsOrAsTypeContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsLocalVariableDeclarationContext(position, tokenOnLeftOfPosition, cancellationToken) ||
                syntaxTree.IsObjectCreationTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
                syntaxTree.IsParameterTypeContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
                syntaxTree.IsStatementContext(position, tokenOnLeftOfPosition, cancellationToken) ||
                syntaxTree.IsTypeParameterConstraintContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsUsingAliasContext(position, cancellationToken) ||
                syntaxTree.IsUsingStaticContext(position, cancellationToken) ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                syntaxTree.IsPossibleTupleContext(tokenOnLeftOfPosition, position) ||
                syntaxTree.IsMemberDeclarationContext(
                    position,
                    contextOpt: null,
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        public static bool IsBaseClassOrInterfaceContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            // class C : |
            // class C : Bar, |

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.ColonToken) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.BaseList))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsUsingAliasContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
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

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            return token.IsStaticKeywordInUsingDirective();
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
                token.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
            {
                var constraintClause = token.Parent as TypeParameterConstraintClauseSyntax;

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

        public static bool IsGenericTypeArgumentContext(
            this SyntaxTree syntaxTree,
            int position,
            SyntaxToken tokenOnLeftOfPosition,
            CancellationToken cancellationToken,
            SemanticModel semanticModelOpt = null)
        {
            // cases: 
            //    Goo<|
            //    Goo<Bar,|
            //    Goo<Bar<Baz<int[],|
            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() != SyntaxKind.LessThanToken && token.Kind() != SyntaxKind.CommaToken)
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

            if (!(nameToken.Parent is NameSyntax name))
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
                name.Parent.IsParentKind(SyntaxKind.ConditionalExpression) &&
                name.Parent.Parent.IsParentKind(SyntaxKind.ExpressionStatement) &&
                name.Parent.Parent.Parent.IsParentKind(SyntaxKind.GlobalStatement))
            {
                var conditionOrType = semanticModelOpt.GetSymbolInfo(
                    ((ConditionalExpressionSyntax)name.Parent.Parent).Condition, cancellationToken);
                if (conditionOrType.GetBestOrAllSymbols().FirstOrDefault() != null &&
                    conditionOrType.GetBestOrAllSymbols().FirstOrDefault().Kind == SymbolKind.NamedType)
                {
                    return false;
                }
            }

            var symbols = semanticModelOpt.LookupName(nameToken, namespacesAndTypesOnly: SyntaxFacts.IsInNamespaceOrTypeContext(name), cancellationToken: cancellationToken);
            return symbols.Any(s =>
            {
                switch (s)
                {
                    case INamedTypeSymbol nt: return nt.Arity > 0;
                    case IMethodSymbol m: return m.Arity > 0;
                    default: return false;
                }
            });
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
                token.Parent.IsDelegateOrConstructorOrLocalFunctionOrMethodOrOperatorParameterList(includeOperators))
            {
                parameterIndex = 0;
                return true;
            }

            if (token.IsKind(SyntaxKind.CommaToken) &&
                token.Parent.IsDelegateOrConstructorOrLocalFunctionOrMethodOrOperatorParameterList(includeOperators))
            {
                var parameterList = (ParameterListSyntax)token.Parent;
                var commaIndex = parameterList.Parameters.GetWithSeparators().IndexOf(token);

                parameterIndex = commaIndex / 2 + 1;
                return true;
            }

            if (token.IsKind(SyntaxKind.CloseBracketToken) &&
                token.Parent.IsKind(SyntaxKind.AttributeList) &&
                token.Parent.IsParentKind(SyntaxKind.Parameter) &&
                token.Parent.Parent.Parent.IsDelegateOrConstructorOrLocalFunctionOrMethodOrOperatorParameterList(includeOperators))
            {
                var parameter = (ParameterSyntax)token.Parent.Parent;
                var parameterList = (ParameterListSyntax)parameter.Parent;

                parameterIndex = parameterList.Parameters.IndexOf(parameter);
                return true;
            }

            if (token.IsKind(SyntaxKind.RefKeyword, SyntaxKind.InKeyword, SyntaxKind.OutKeyword,
                             SyntaxKind.ThisKeyword, SyntaxKind.ParamsKeyword) &&
                token.Parent.IsKind(SyntaxKind.Parameter) &&
                token.Parent.Parent.IsDelegateOrConstructorOrLocalFunctionOrMethodOrOperatorParameterList(includeOperators))
            {
                var parameter = (ParameterSyntax)token.Parent;
                var parameterList = (ParameterListSyntax)parameter.Parent;

                parameterIndex = parameterList.Parameters.IndexOf(parameter);
                previousModifier = token.Kind();
                return true;
            }

            return false;
        }

        public static bool IsParamsModifierContext(
            this SyntaxTree syntaxTree,
            int position,
            SyntaxToken tokenOnLeftOfPosition)
        {
            if (syntaxTree.IsParameterModifierContext(position, tokenOnLeftOfPosition, includeOperators: false, out _, out var previousModifier) &&
                previousModifier == SyntaxKind.None)
            {
                return true;
            }

            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.OpenBracketToken) || token.IsKind(SyntaxKind.CommaToken))
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

            if (token.IsKind(SyntaxKind.OperatorKeyword))
            {
                if (token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.ImplicitKeyword) ||
                    token.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.ExplicitKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsParameterTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

            if (syntaxTree.IsParameterModifierContext(position, tokenOnLeftOfPosition, includeOperators: true, out _, out _))
            {
                return true;
            }

            // int this[ |
            // int this[int i, |
            if (token.IsKind(SyntaxKind.OpenParenToken) ||
                token.IsKind(SyntaxKind.OpenBracketToken) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ParameterList, SyntaxKind.BracketedParameterList))
                {
                    return true;
                }
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
            this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.OpenParenToken) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ParameterList) &&
                    token.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
                {
                    return true;
                }

                // TODO(cyrusn): Tie into semantic analysis system to only 
                // consider this a lambda if this is a location where the
                // lambda's type would be inferred because of a delegate
                // or Expression<T> type.
                if (token.Parent.IsKind(SyntaxKind.ParenthesizedExpression) ||
                    token.Parent.IsKind(SyntaxKind.TupleExpression))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAnonymousMethodParameterModifierContext(
            this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.OpenParenToken) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ParameterList) &&
                    token.Parent.IsParentKind(SyntaxKind.AnonymousMethodExpression))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext(
            this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
        {
            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.RefKeyword) ||
                token.IsKind(SyntaxKind.InKeyword) ||
                token.IsKind(SyntaxKind.OutKeyword))
            {
                position = token.SpanStart;
                tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            }

            if (IsAnonymousMethodParameterModifierContext(syntaxTree, position, tokenOnLeftOfPosition) ||
                IsPossibleLambdaParameterModifierContext(syntaxTree, position, tokenOnLeftOfPosition))
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
                if (leftToken.Parent.IsKind(
                        SyntaxKind.ParenthesizedExpression,
                        SyntaxKind.TupleExpression,
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
                var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent);
                if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
                {
                    return true;
                }
            }

            // (a.b $$
            // (..., a.b $$
            if (leftToken.IsKind(SyntaxKind.IdentifierToken) &&
                leftToken.Parent.IsKind(SyntaxKind.IdentifierName) &&
                leftToken.Parent.IsParentKind(SyntaxKind.QualifiedName, SyntaxKind.SimpleMemberAccessExpression))
            {
                var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent.Parent);
                if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPatternContext(this SyntaxTree syntaxTree, SyntaxToken leftToken, int position)
        {
            leftToken = leftToken.GetPreviousTokenIfTouchingWord(position);

            // case $$
            // is $$
            if (leftToken.IsKind(SyntaxKind.CaseKeyword, SyntaxKind.IsKeyword))
            {
                return true;
            }

            // e switch { $$
            // e switch { ..., $$
            if (leftToken.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.CommaToken) && leftToken.Parent.IsKind(SyntaxKind.SwitchExpression))
            {
                return true;
            }

            // e is ($$
            // e is (..., $$
            if (leftToken.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) && leftToken.Parent.IsKind(SyntaxKind.PositionalPatternClause))
            {
                return true;
            }

            // e is { P: $$
            // e is { ..., P: $$
            if (leftToken.IsKind(SyntaxKind.ColonToken) && leftToken.Parent.IsKind(SyntaxKind.NameColon) &&
                leftToken.Parent.IsParentKind(SyntaxKind.Subpattern))
            {
                return true;
            }

            return false;
        }

        private static SyntaxToken FindTokenOnLeftOfNode(SyntaxNode node)
        {
            return node.FindTokenOnLeftOfPosition(node.SpanStart);
        }


        public static bool IsPossibleTupleOpenParenOrComma(this SyntaxToken possibleCommaOrParen)
        {
            if (!possibleCommaOrParen.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken))
            {
                return false;
            }

            if (possibleCommaOrParen.Parent.IsKind(
                    SyntaxKind.ParenthesizedExpression,
                    SyntaxKind.TupleExpression,
                    SyntaxKind.TupleType,
                    SyntaxKind.CastExpression))
            {
                return true;
            }

            // in script
            if (possibleCommaOrParen.Parent.IsKind(SyntaxKind.ParameterList) &&
                possibleCommaOrParen.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)possibleCommaOrParen.Parent.Parent;
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
            if (leftToken.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) && leftToken.Parent.IsKind(SyntaxKind.TupleExpression))
            {
                if (IsPossibleVarDeconstructionOpenParenOrComma(FindTokenOnLeftOfNode(leftToken.Parent)))
                {
                    return true;
                }
            }

            // foreach (var ($$
            // foreach (var ((x, $$), y)
            if (leftToken.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken))
            {
                var outer = UnwrapPossibleTuple(leftToken.Parent);
                if (outer.Parent.IsKind(SyntaxKind.ForEachStatement))
                {
                    var @foreach = (ForEachStatementSyntax)outer.Parent;

                    if (@foreach.Expression == outer &&
                        @foreach.Type.IsKind(SyntaxKind.IdentifierName) &&
                        ((IdentifierNameSyntax)@foreach.Type).Identifier.ValueText == "var")
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
                if (node.Parent.IsKind(SyntaxKind.Argument) && node.Parent.IsParentKind(SyntaxKind.TupleExpression))
                {
                    node = node.Parent.Parent;
                    continue;
                }

                return node;
            }
        }

        private static bool IsPossibleVarDeconstructionOpenParenOrComma(SyntaxToken leftToken)
        {
            if (leftToken.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) &&
                leftToken.Parent.IsKind(SyntaxKind.ArgumentList) &&
                leftToken.Parent.IsParentKind(SyntaxKind.InvocationExpression))
            {
                var invocation = (InvocationExpressionSyntax)leftToken.Parent.Parent;
                if (invocation.Expression.IsKind(SyntaxKind.IdentifierName) &&
                    ((IdentifierNameSyntax)invocation.Expression).Identifier.ValueText == "var")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasNames(this TupleExpressionSyntax tuple)
        {
            return tuple.Arguments.Any(a => a.NameColon != null);
        }

        public static bool IsValidContextForFromClause(
            this SyntaxTree syntaxTree,
            int position,
            SyntaxToken tokenOnLeftOfPosition,
            CancellationToken cancellationToken,
            SemanticModel semanticModelOpt = null)
        {
            if (syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: false, cancellationToken: cancellationToken, semanticModelOpt: semanticModelOpt) &&
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

        public static bool IsDeclarationExpressionContext(
            this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            // cases:
            //  M(out var
            //  var x = var

            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.OutKeyword) &&
                token.Parent.IsKind(SyntaxKind.Argument))
            {
                return true;
            }

            if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.EqualsToken) &&
                token.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
                token.Parent.IsParentKind(SyntaxKind.VariableDeclarator))
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
            if (token.IsKind(SyntaxKind.RefKeyword, SyntaxKind.ReadOnlyKeyword))
            {
                var parent = token.Parent;
                if (parent.IsKind(SyntaxKind.RefType) ||
                    parent.IsKind(SyntaxKind.RefExpression) ||
                    parent.IsKind(SyntaxKind.LocalDeclarationStatement))
                {
                    if (parent.IsParentKind(SyntaxKind.VariableDeclaration) &&
                        parent.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.ForEachVariableStatement))
                    {
                        return true;
                    }

                    if (parent.IsParentKind(SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement))
                    {
                        return true;
                    }
                }
            }

            // out |
            if (token.IsKind(SyntaxKind.OutKeyword) &&
                token.Parent.IsKind(SyntaxKind.Argument) &&
                ((ArgumentSyntax)token.Parent).RefKindKeyword == token)
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.OpenParenToken))
            {
                // for ( |
                // foreach ( |
                // await foreach ( |
                // using ( |
                // await using ( |
                var previous = token.GetPreviousToken(includeSkipped: true);
                if (previous.IsKind(SyntaxKind.ForKeyword) ||
                    previous.IsKind(SyntaxKind.ForEachKeyword) ||
                    previous.IsKind(SyntaxKind.UsingKeyword))
                {
                    return true;
                }
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

            if (token.IsKind(SyntaxKind.IsKeyword) ||
                token.IsKind(SyntaxKind.AsKeyword))
            {
                return true;
            }

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

        public static bool IsPreProcessorDirectiveContext(this SyntaxTree syntaxTree, int position, SyntaxToken preProcessorTokenOnLeftOfPosition, CancellationToken cancellationToken)
        {
            var token = preProcessorTokenOnLeftOfPosition;
            var directive = token.GetAncestor<DirectiveTriviaSyntax>();

            // Directives contain the EOL, so if the position is within the full span of the
            // directive, then it is on that line, the only exception is if the directive is on the
            // last line, the position at the end if technically not contained by the directive but
            // its also not on a new line, so it should be considered part of the preprocessor
            // context.
            if (directive == null)
            {
                return false;
            }

            return
                directive.FullSpan.Contains(position) ||
                directive.FullSpan.End == syntaxTree.GetRoot(cancellationToken).FullSpan.End;
        }

        public static bool IsPreProcessorDirectiveContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);

            return syntaxTree.IsPreProcessorDirectiveContext(position, leftToken, cancellationToken);
        }

        public static bool IsPreProcessorKeywordContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return IsPreProcessorKeywordContext(
                syntaxTree, position,
                syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true));
        }

        public static bool IsPreProcessorKeywordContext(this SyntaxTree syntaxTree, int position, SyntaxToken preProcessorTokenOnLeftOfPosition)
        {
            // cases:
            //  #|
            //  #d|
            //  # |
            //  # d|

            // note: comments are not allowed between the # and item.
            var token = preProcessorTokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.HashToken))
            {
                return true;
            }

            return false;
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
            if (!syntaxTree.IsScript())
            {
                return false;
            }

#if false
            if (syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken))
            {
                return false;
            }
#endif

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                  .GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.None))
            {
                // global statements can't come before usings/externs
                if (syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit &&
                    (compilationUnit.Externs.Count > 0 ||
                    compilationUnit.Usings.Count > 0))
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

            var enclosingSymbol = semanticModel.GetEnclosingSymbol(targetToken.SpanStart, cancellationToken);

            while (enclosingSymbol is IMethodSymbol method && (method.MethodKind == MethodKind.LocalFunction || method.MethodKind == MethodKind.AnonymousFunction))
            {
                if (method.IsStatic)
                {
                    return false;
                }

                // It is allowed to reference the instance (`this`) within a local function or anonymous function, as long as the containing method allows it
                enclosingSymbol = enclosingSymbol.ContainingSymbol;
            }

            return !enclosingSymbol.IsStatic;
        }

        private static bool IsInBlock(BlockSyntax bodyOpt, int position)
        {
            if (bodyOpt == null)
            {
                return false;
            }

            return bodyOpt.OpenBraceToken.Span.End <= position &&
                (bodyOpt.CloseBraceToken.IsMissing || position <= bodyOpt.CloseBraceToken.SpanStart);
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
            if (IsPatternContext(syntaxTree, tokenOnLeftOfPosition, position))
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
                token.Parent.IsKind(SyntaxKind.EqualsValueClause))
            {
                var equalsValue = (EqualsValueClauseSyntax)token.Parent;

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
               (token.IsKind(SyntaxKind.CommaToken) ||
                token.IsKind(SyntaxKind.OpenParenToken)))
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
            SemanticModel semanticModelOpt = null)
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
            if (token.IsKind(SyntaxKind.DotToken) ||
                token.IsKind(SyntaxKind.ColonColonToken) ||
                token.IsKind(SyntaxKind.MinusGreaterThanToken))
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
            // q ??= |
            if (token.IsKind(SyntaxKind.EqualsToken) ||
                token.IsKind(SyntaxKind.MinusEqualsToken) ||
                token.IsKind(SyntaxKind.AsteriskEqualsToken) ||
                token.IsKind(SyntaxKind.PlusEqualsToken) ||
                token.IsKind(SyntaxKind.SlashEqualsToken) ||
                token.IsKind(SyntaxKind.ExclamationEqualsToken) ||
                token.IsKind(SyntaxKind.CaretEqualsToken) ||
                token.IsKind(SyntaxKind.AmpersandEqualsToken) ||
                token.IsKind(SyntaxKind.BarEqualsToken) ||
                token.IsKind(SyntaxKind.PercentEqualsToken) ||
                token.IsKind(SyntaxKind.LessThanLessThanEqualsToken) ||
                token.IsKind(SyntaxKind.GreaterThanGreaterThanEqualsToken) ||
                token.IsKind(SyntaxKind.QuestionQuestionEqualsToken))
            {
                return true;
            }

            // ( |
            if (token.IsKind(SyntaxKind.OpenParenToken) &&
                token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                return true;
            }

            // - |
            // + |
            // ~ |
            // ! |
            if (token.Parent is PrefixUnaryExpressionSyntax)
            {
                var prefix = token.Parent as PrefixUnaryExpressionSyntax;
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
            if (token.Parent is AwaitExpressionSyntax)
            {
                var awaitExpression = token.Parent as AwaitExpressionSyntax;
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
            if (token.Parent is BinaryExpressionSyntax)
            {
                // If the client provided a binding, then check if this is actually generic.  If so,
                // then this is not an expression context. i.e. if we have "Goo < |" then it could
                // be an expression context, or it could be a type context if Goo binds to a type or
                // method.
                if (semanticModelOpt != null && syntaxTree.IsGenericTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken, semanticModelOpt))
                {
                    return false;
                }

                var binary = token.Parent as BinaryExpressionSyntax;
                if (binary.OperatorToken == token)
                {
                    // If this is a multiplication expression and a semantic model was passed in,
                    // check to see if the expression to the left is a type name. If it is, treat
                    // this as a pointer type.
                    if (token.IsKind(SyntaxKind.AsteriskToken) && semanticModelOpt != null)
                    {
                        if (binary.Left is TypeSyntax type && type.IsPotentialTypeName(semanticModelOpt, cancellationToken))
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
                if (previousToken.IsKind(SyntaxKind.AsteriskToken) ||
                    previousToken.IsKind(SyntaxKind.QuestionToken))
                {
                    if (previousToken.Parent.IsKind(SyntaxKind.PointerType) ||
                        previousToken.Parent.IsKind(SyntaxKind.NullableType))
                    {
                        var type = previousToken.Parent as TypeSyntax;
                        if (type.IsParentKind(SyntaxKind.VariableDeclaration) &&
                            type.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement))
                        {
                            var declStatement = type.Parent.Parent as LocalDeclarationStatementSyntax;

                            // note, this doesn't apply for cases where we know it 
                            // absolutely is not multiplication or a conditional expression.
                            var underlyingType = type is PointerTypeSyntax
                                ? ((PointerTypeSyntax)type).ElementType
                                : ((NullableTypeSyntax)type).ElementType;

                            if (!underlyingType.IsPotentialTypeName(semanticModelOpt, cancellationToken))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // new int[|
            // new int[expr, |
            if (token.IsKind(SyntaxKind.OpenBracketToken) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier))
                {
                    return true;
                }
            }

            // goo ? |
            if (token.IsKind(SyntaxKind.QuestionToken) &&
                token.Parent.IsKind(SyntaxKind.ConditionalExpression))
            {
                // If the condition is simply a TypeSyntax that binds to a type, treat this as a nullable type.
                var conditionalExpression = (ConditionalExpressionSyntax)token.Parent;

                return !(conditionalExpression.Condition is TypeSyntax type)
                    || !type.IsPotentialTypeName(semanticModelOpt, cancellationToken);
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
                if (token.Parent.IsKind(SyntaxKind.TypeOfExpression, SyntaxKind.DefaultExpression, SyntaxKind.SizeOfExpression))
                {
                    return false;
                }
            }

            // var(|
            // var(id, |
            // Those are more likely to be deconstruction-declarations being typed than invocations a method "var"
            if (token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) &&
                token.IsInvocationOfVarExpression())
            {
                return false;
            }

            // Goo(|
            // Goo(expr, |
            // this[|
            // var t = (1, |
            // var t = (| , 2)
            if (token.IsKind(SyntaxKind.OpenParenToken) ||
                token.IsKind(SyntaxKind.OpenBracketToken) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.BracketedArgumentList, SyntaxKind.TupleExpression))
                {
                    return true;
                }
            }

            // [Goo(|
            // [Goo(expr, |
            if (attributes)
            {
                if (token.IsKind(SyntaxKind.OpenParenToken) ||
                    token.IsKind(SyntaxKind.CommaToken))
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
            if (token.IsKind(SyntaxKind.RefKeyword) ||
                token.IsKind(SyntaxKind.InKeyword) ||
                token.IsKind(SyntaxKind.OutKeyword))
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
            if (token.IsKind(SyntaxKind.OpenBraceToken) ||
                token.IsKind(SyntaxKind.CommaToken))
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
                    if (semanticModelOpt != null &&
                        token.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression))
                    {
                        var objectCreation = (ObjectCreationExpressionSyntax)token.Parent.Parent;
                        var containingSymbol = semanticModelOpt.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
                        if (semanticModelOpt.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol is ITypeSymbol type && !type.CanSupportCollectionInitializer(containingSymbol))
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
                token.Parent.IsKind(SyntaxKind.ForStatement))
            {
                var forStatement = (ForStatementSyntax)token.Parent;
                if (token == forStatement.FirstSemicolonToken ||
                    token == forStatement.SecondSemicolonToken)
                {
                    return true;
                }
            }

            // for ( |
            if (token.IsKind(SyntaxKind.OpenParenToken) &&
                token.Parent.IsKind(SyntaxKind.ForStatement))
            {
                var forStatement = (ForStatementSyntax)token.Parent;
                if (token == forStatement.OpenParenToken)
                {
                    return true;
                }
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
                if (token.Parent.IsKind(SyntaxKind.ForEachStatement,
                                        SyntaxKind.ForEachVariableStatement,
                                        SyntaxKind.FromClause,
                                        SyntaxKind.JoinClause))
                {
                    return true;
                }
            }

            // join x in y on |
            // join x in y on a equals |
            if (token.IsKind(SyntaxKind.OnKeyword) ||
                token.IsKind(SyntaxKind.EqualsKeyword))
            {
                if (token.Parent.IsKind(SyntaxKind.JoinClause))
                {
                    return true;
                }
            }

            // where |
            if (token.IsKind(SyntaxKind.WhereKeyword) &&
                token.Parent.IsKind(SyntaxKind.WhereClause))
            {
                return true;
            }

            // orderby |
            // orderby a, |
            if (token.IsKind(SyntaxKind.OrderByKeyword) ||
                token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.OrderByClause))
                {
                    return true;
                }
            }

            // select |
            if (token.IsKind(SyntaxKind.SelectKeyword) &&
                token.Parent.IsKind(SyntaxKind.SelectClause))
            {
                return true;
            }

            // group |
            // group expr by |
            if (token.IsKind(SyntaxKind.GroupKeyword) ||
                token.IsKind(SyntaxKind.ByKeyword))
            {
                if (token.Parent.IsKind(SyntaxKind.GroupClause))
                {
                    return true;
                }
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
            if (token.IsKind(SyntaxKind.OpenBraceToken) || token.IsKind(SyntaxKind.CommaToken))
            {
                if (token.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
                {
                    return true;
                }
            }

            // $"{ |
            // $@"{ |
            // $"{x} { |
            // $@"{x} { |
            if (token.IsKind(SyntaxKind.OpenBraceToken))
            {
                return token.Parent.IsKind(SyntaxKind.Interpolation)
                    && ((InterpolationSyntax)token.Parent).OpenBraceToken == token;
            }

            return false;
        }

        public static bool IsInvocationOfVarExpression(this SyntaxToken token)
        {
            return token.Parent.Parent.IsKind(SyntaxKind.InvocationExpression) &&
                ((InvocationExpressionSyntax)token.Parent.Parent).Expression.ToString() == "var";
        }

        public static bool IsNameOfContext(this SyntaxTree syntaxTree, int position, SemanticModel semanticModelOpt = null, CancellationToken cancellationToken = default)
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
                    while (parentMemberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        parentMemberAccess = parentMemberAccess.Parent;
                    }

                    if (parentMemberAccess.IsParentKind(SyntaxKind.Argument) &&
                        parentMemberAccess.Parent.IsChildNode<ArgumentListSyntax>(a => a.Arguments.FirstOrDefault()))
                    {
                        token = ((ArgumentListSyntax)parentMemberAccess.Parent.Parent).OpenParenToken;
                    }
                }

                // Could have been parsed as a qualified name.
                if (token.Parent.IsKind(SyntaxKind.QualifiedName))
                {
                    var parentQualifiedName = token.Parent;
                    while (parentQualifiedName.IsParentKind(SyntaxKind.QualifiedName))
                    {
                        parentQualifiedName = parentQualifiedName.Parent;
                    }

                    if (parentQualifiedName.IsParentKind(SyntaxKind.Argument) &&
                        parentQualifiedName.Parent.IsChildNode<ArgumentListSyntax>(a => a.Arguments.FirstOrDefault()))
                    {
                        token = ((ArgumentListSyntax)parentQualifiedName.Parent.Parent).OpenParenToken;
                    }
                }
            }

            ExpressionSyntax parentExpression = null;

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

        public static bool IsIsOrAsOrSwitchExpressionContext(
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

            // Not if the position is *within* a numeric literal
            if (token.IsKind(SyntaxKind.NumericLiteralToken) && token.Span.Contains(position))
            {
                return false;
            }

            if (token.GetAncestor<BlockSyntax>() == null &&
                token.GetAncestor<ArrowExpressionClauseSyntax>() == null)
            {
                return false;
            }

            // is/as are valid after expressions.
            if (token.IsLastTokenOfNode<ExpressionSyntax>())
            {
                // However, many names look like expressions.  For example:
                //    foreach (var |
                // ('var' is a TypeSyntax which is an expression syntax.

                var type = token.GetAncestors<TypeSyntax>().LastOrDefault();
                if (type == null)
                {
                    return true;
                }

                if (type.IsKind(SyntaxKind.GenericName) ||
                    type.IsKind(SyntaxKind.AliasQualifiedName) ||
                    type.IsKind(SyntaxKind.PredefinedType))
                {
                    return false;
                }

                ExpressionSyntax nameExpr = type;
                if (IsRightSideName(nameExpr))
                {
                    nameExpr = (ExpressionSyntax)nameExpr.Parent;
                }

                // If this name is the start of a local variable declaration context, we
                // shouldn't show is or as. For example: for(var |
                if (syntaxTree.IsLocalVariableDeclarationContext(token.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken), cancellationToken))
                {
                    return false;
                }

                // Not on the left hand side of an object initializer
                if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.IdentifierToken) &&
                    token.Parent.IsKind(SyntaxKind.IdentifierName) &&
                    (token.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression) || token.Parent.IsParentKind(SyntaxKind.CollectionInitializerExpression)))
                {
                    return false;
                }

                // Not after an 'out' declaration expression. For example: M(out var |
                if (CodeAnalysis.CSharpExtensions.IsKind(token, SyntaxKind.IdentifierToken) &&
                    token.Parent.IsKind(SyntaxKind.IdentifierName))
                {
                    if (token.Parent.IsParentKind(SyntaxKind.Argument) &&
                        CodeAnalysis.CSharpExtensions.IsKind(((ArgumentSyntax)token.Parent.Parent).RefOrOutKeyword, SyntaxKind.OutKeyword))
                    {
                        return false;
                    }
                }

                if (token.Text == SyntaxFacts.GetText(SyntaxKind.AsyncKeyword))
                {
                    // async $$
                    //
                    // 'async' will look like a normal identifier.  But we don't want to follow it
                    // with 'is' or 'as' if it's actually the start of a lambda.
                    var delegateType = CSharpTypeInferenceService.Instance.InferDelegateType(
                        semanticModel, token.SpanStart, cancellationToken);
                    if (delegateType != null)
                    {
                        return false;
                    }
                }

                // Now, make sure the name was actually in a location valid for
                // an expression.  If so, then we know we can follow it.
                if (syntaxTree.IsExpressionContext(nameExpr.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(nameExpr.SpanStart, cancellationToken), attributes: false, cancellationToken: cancellationToken))
                {
                    return true;
                }

                return false;
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

                if (block != null && token == block.GetLastToken(includeSkipped: true))
                {
                    if (block.IsParentKind(SyntaxKind.TryStatement) ||
                        block.IsParentKind(SyntaxKind.CatchClause))
                    {
                        return true;
                    }
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

        public static bool IsEnumBaseListContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            // Options:
            //  enum E : |
            //  enum E : i|

            return
                token.IsKind(SyntaxKind.ColonToken) &&
                token.Parent.IsKind(SyntaxKind.BaseList) &&
                token.Parent.IsParentKind(SyntaxKind.EnumDeclaration);
        }

        public static bool IsEnumTypeMemberAccessContext(this SyntaxTree syntaxTree, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree
                .FindTokenOnLeftOfPosition(position, cancellationToken)
                .GetPreviousTokenIfTouchingWord(position);

            if (!token.IsKind(SyntaxKind.DotToken) ||
                !token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            var memberAccess = (MemberAccessExpressionSyntax)token.Parent;
            var leftHandBinding = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken);
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
    }
}
