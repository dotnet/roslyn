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
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class CSharpCodeGenerationHelpers
    {
        public static (ImmutableArray<AttributeData>, NullableAnnotation) AdjustNullableAnnotationByAttributes(ITypeSymbol type, RefKind refKind, ImmutableArray<AttributeData> attributes, NullableAnnotation nullableAnnotation, bool isParameter)
        {
            if (type.IsReferenceType)
            {
                // The compiler does not allow 'T?' where 'T : class?'.
                if (!(type is ITypeParameterSymbol typeParameter) ||
                    typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    if (refKind == RefKind.Ref)
                    {
                        if (isParameter)
                        {
                            // For "ref" parameters with both [AllowNull] and [MaybeNull], add the nullable annotation.
                            //
                            // before                               -> after
                            // ------------------------------------    -------------
                            // ([AllowNull][MaybeNull] ref string?) -> (ref string?)
                            // ([AllowNull][MaybeNull] ref string)  -> (ref string?)
                            if (attributes.Contains(IsAllowNull) && attributes.Contains(IsMaybeNull))
                            {
                                attributes = attributes.RemoveAll(attr => IsAllowNull(attr) || IsMaybeNull(attr));
                                if (nullableAnnotation == NullableAnnotation.NotAnnotated)
                                {
                                    nullableAnnotation = NullableAnnotation.Annotated;
                                }
                            }

                            // For not annotated "ref" parameters with both [DisallowNull] and [NotNull], remove the nullable annotation.
                            // The compiler does not currently allow parameter nullabilities to differ.
                            //
                            //           before                                -> after
                            //           -------------------------------------    -------------------------------------
                            // -desired: ([DisallowNull][NotNull] ref string?) -> (ref string)
                            // +current: ([DisallowNull][NotNull] ref string?) -> (ref [DisallowNull][NotNull] string?)
                            //           ([DisallowNull][NotNull] ref string)  -> (ref string)
                            if (nullableAnnotation == NullableAnnotation.NotAnnotated)
                            {
                                if (attributes.Contains(IsDisallowNull) && attributes.Contains(IsNotNull))
                                {
                                    attributes = attributes.RemoveAll(attr => IsDisallowNull(attr) || IsNotNull(attr));
                                }
                            }

                            static bool IsAllowNull(AttributeData attribute)
                                => IsCodeAnalysisAttribute(attribute) &&
                                   attribute.AttributeClass.Name == nameof(AllowNullAttribute);

                            static bool IsDisallowNull(AttributeData attribute)
                                => IsCodeAnalysisAttribute(attribute) &&
                                   attribute.AttributeClass.Name == nameof(DisallowNullAttribute);

                            static bool IsMaybeNull(AttributeData attribute)
                                => IsCodeAnalysisAttribute(attribute) &&
                                   attribute.AttributeClass.Name == nameof(MaybeNullAttribute);

                            static bool IsNotNull(AttributeData attribute)
                                => IsCodeAnalysisAttribute(attribute) &&
                                   attribute.AttributeClass.Name == nameof(NotNullAttribute);
                        }

                        // For "ref readonly" return types, nothing to do.
                    }
                    else
                    {
                        if (refKind == RefKind.In && isParameter)
                        {
                            // For "in" parameters, ignore output attributes.
                            //
                            // before                     -> after
                            // --------------------------    --------------
                            // ([MaybeNull] ref readonly) -> (ref readonly)
                            // ([NotNull] ref readonly)   -> (ref readonly)
                            attributes = attributes.RemoveAll(IsOutputNullableFlowAnalysisAttribute);
                        }

                        // For [AllowNull]/[MaybeNull], add the nullable annotation.
                        //
                        // before                -> after
                        // ---------------------    --------------------
                        // ([AllowNull] string?) -> (string?)
                        // ([AllowNull] string)  -> (string?)
                        // ([AllowNull] T)       -> (T?) where T : class
                        var newAttributes = attributes.RemoveAll(IsAllowNullOrMaybeNullAttribute);
                        if (newAttributes.Length != attributes.Length)
                        {
                            attributes = newAttributes;
                            if (nullableAnnotation == NullableAnnotation.NotAnnotated)
                            {
                                nullableAnnotation = NullableAnnotation.Annotated;
                            }
                        }

                        if (isParameter)
                        {
                            // The compiler does not currently allow parameter nullabilities to differ.
                            //
                            //           before                   -> after
                            //           ------------------------    ------------------------
                            // -desired: ([DisallowNull] string?) -> (string)
                            // +current: ([DisallowNull] string?) -> ([DisallowNull] string?)
                            //           ([DisallowNull] string)  -> (string)
                            if (nullableAnnotation == NullableAnnotation.NotAnnotated)
                            {
                                attributes = attributes.RemoveAll(IsDisallowNullOrNotNullAttribute);
                            }
                        }
                        else
                        {
                            // For [DisallowNull]/[NotNull], remove the nullable annotation.
                            //
                            // before                   -> after
                            // ------------------------    --------
                            // ([DisallowNull] string?) -> (string)
                            // ([DisallowNull] string)  -> (string)
                            newAttributes = attributes.RemoveAll(IsDisallowNullOrNotNullAttribute);
                            if (newAttributes.Length != attributes.Length)
                            {
                                attributes = newAttributes;
                                if (nullableAnnotation == NullableAnnotation.Annotated)
                                {
                                    nullableAnnotation = NullableAnnotation.NotAnnotated;
                                }
                            }
                        }
                    }
                }
            }
            else if (type.IsValueType)
            {
                // Nullable<T> and T are incompatible in signature. Don't change the nullable annotation.
                //
                // before                -> after
                // ---------------------    ---------------------
                // ([AllowNull] int?)    -> (int?)
                // ([DisallowNull] int?) -> ([DisallowNull] int?)
                // ([AllowNull] int)     -> (int)
                // ([DisallowNull] int)  -> (int)
                attributes = nullableAnnotation == NullableAnnotation.Annotated
                    ? attributes.RemoveAll(IsAllowNullOrMaybeNullAttribute)
                    : attributes.RemoveAll(IsNullableFlowAnalysisAttribute);
            }

            return (attributes, nullableAnnotation);
        }

        public static TDeclarationSyntax ConditionallyAddFormattingAnnotationTo<TDeclarationSyntax>(
            TDeclarationSyntax result,
            SyntaxList<MemberDeclarationSyntax> members) where TDeclarationSyntax : MemberDeclarationSyntax
        {
            return members.Count == 1
                ? result.WithAdditionalAnnotations(Formatter.Annotation)
                : result;
        }

        internal static void AddAccessibilityModifiers(
            Accessibility accessibility,
            ArrayBuilder<SyntaxToken> tokens,
            CodeGenerationOptions options,
            Accessibility defaultAccessibility)
        {
            options ??= CodeGenerationOptions.Default;
            if (!options.GenerateDefaultAccessibility && accessibility == defaultAccessibility)
            {
                return;
            }

            switch (accessibility)
            {
                case Accessibility.Public:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    break;
                case Accessibility.Protected:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Private:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    break;
                case Accessibility.ProtectedAndInternal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Internal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
            }
        }

        public static TypeDeclarationSyntax AddMembersTo(
            TypeDeclarationSyntax destination, SyntaxList<MemberDeclarationSyntax> members)
        {
            destination = ReplaceUnterminatedConstructs(destination);

            return ConditionallyAddFormattingAnnotationTo(
                destination.EnsureOpenAndCloseBraceTokens().WithMembers(members),
                members);
        }

        private static TypeDeclarationSyntax ReplaceUnterminatedConstructs(TypeDeclarationSyntax destination)
        {
            const string MultiLineCommentTerminator = "*/";
            var lastToken = destination.GetLastToken();
            var updatedToken = lastToken.ReplaceTrivia(lastToken.TrailingTrivia,
                (t1, t2) =>
                {
                    if (t1.Kind() == SyntaxKind.MultiLineCommentTrivia)
                    {
                        var text = t1.ToString();
                        if (!text.EndsWith(MultiLineCommentTerminator, StringComparison.Ordinal))
                        {
                            return SyntaxFactory.SyntaxTrivia(SyntaxKind.MultiLineCommentTrivia, text + MultiLineCommentTerminator);
                        }
                    }
                    else if (t1.Kind() == SyntaxKind.SkippedTokensTrivia)
                    {
                        return ReplaceUnterminatedConstructs(t1);
                    }

                    return t1;
                });

            return destination.ReplaceToken(lastToken, updatedToken);
        }

        private static SyntaxTrivia ReplaceUnterminatedConstructs(SyntaxTrivia skippedTokensTrivia)
        {
            var syntax = (SkippedTokensTriviaSyntax)skippedTokensTrivia.GetStructure();
            var tokens = syntax.Tokens;

            var updatedTokens = SyntaxFactory.TokenList(tokens.Select(ReplaceUnterminatedConstruct));
            var updatedSyntax = syntax.WithTokens(updatedTokens);

            return SyntaxFactory.Trivia(updatedSyntax);
        }

        private static SyntaxToken ReplaceUnterminatedConstruct(SyntaxToken token)
        {
            if (token.IsVerbatimStringLiteral())
            {
                var tokenText = token.ToString();
                if (tokenText.Length <= 2 || tokenText.Last() != '"')
                {
                    tokenText += '"';
                    return SyntaxFactory.Literal(token.LeadingTrivia, tokenText, token.ValueText, token.TrailingTrivia);
                }
            }
            else if (token.IsRegularStringLiteral())
            {
                var tokenText = token.ToString();
                if (tokenText.Length <= 1 || tokenText.Last() != '"')
                {
                    tokenText += '"';
                    return SyntaxFactory.Literal(token.LeadingTrivia, tokenText, token.ValueText, token.TrailingTrivia);
                }
            }

            return token;
        }

        public static MemberDeclarationSyntax FirstMember(SyntaxList<MemberDeclarationSyntax> members)
            => members.FirstOrDefault();

        public static MemberDeclarationSyntax FirstMethod(SyntaxList<MemberDeclarationSyntax> members)
            => members.FirstOrDefault(m => m is MethodDeclarationSyntax);

        public static MemberDeclarationSyntax LastField(SyntaxList<MemberDeclarationSyntax> members)
            => members.LastOrDefault(m => m is FieldDeclarationSyntax);

        public static MemberDeclarationSyntax LastConstructor(SyntaxList<MemberDeclarationSyntax> members)
            => members.LastOrDefault(m => m is ConstructorDeclarationSyntax);

        public static MemberDeclarationSyntax LastMethod(SyntaxList<MemberDeclarationSyntax> members)
            => members.LastOrDefault(m => m is MethodDeclarationSyntax);

        public static MemberDeclarationSyntax LastOperator(SyntaxList<MemberDeclarationSyntax> members)
            => members.LastOrDefault(m => m is OperatorDeclarationSyntax || m is ConversionOperatorDeclarationSyntax);

        public static SyntaxList<TDeclaration> Insert<TDeclaration>(
            SyntaxList<TDeclaration> declarationList,
            TDeclaration declaration,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            Func<SyntaxList<TDeclaration>, TDeclaration> after = null,
            Func<SyntaxList<TDeclaration>, TDeclaration> before = null)
            where TDeclaration : SyntaxNode
        {
            var index = GetInsertionIndex(
                declarationList, declaration, options, availableIndices,
                CSharpDeclarationComparer.WithoutNamesInstance,
                CSharpDeclarationComparer.WithNamesInstance,
                after, before);

            if (availableIndices != null)
            {
                availableIndices.Insert(index, true);
            }

            if (index != 0 && declarationList[index - 1].ContainsDiagnostics && AreBracesMissing(declarationList[index - 1]))
            {
                return declarationList.Insert(index, declaration.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
            }

            return declarationList.Insert(index, declaration);
        }

        private static bool AreBracesMissing<TDeclaration>(TDeclaration declaration) where TDeclaration : SyntaxNode
            => declaration.ChildTokens().Where(t => t.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken) && t.IsMissing).Any();

        public static SyntaxNode GetContextNode(
            Location location, CancellationToken cancellationToken)
        {
            var contextLocation = location as Location;

            var contextTree = contextLocation != null && contextLocation.IsInSource
                ? contextLocation.SourceTree
                : null;

            return contextTree?.GetRoot(cancellationToken).FindToken(contextLocation.SourceSpan.Start).Parent;
        }

        public static ExplicitInterfaceSpecifierSyntax GenerateExplicitInterfaceSpecifier(
            IEnumerable<ISymbol> implementations)
        {
            var implementation = implementations.FirstOrDefault();
            if (implementation == null)
            {
                return null;
            }

            if (!(implementation.ContainingType.GenerateTypeSyntax() is NameSyntax name))
            {
                return null;
            }

            return SyntaxFactory.ExplicitInterfaceSpecifier(name);
        }

        public static CodeGenerationDestination GetDestination(SyntaxNode destination)
        {
            if (destination != null)
            {
                return destination.Kind() switch
                {
                    SyntaxKind.ClassDeclaration => CodeGenerationDestination.ClassType,
                    SyntaxKind.CompilationUnit => CodeGenerationDestination.CompilationUnit,
                    SyntaxKind.EnumDeclaration => CodeGenerationDestination.EnumType,
                    SyntaxKind.InterfaceDeclaration => CodeGenerationDestination.InterfaceType,
                    SyntaxKind.NamespaceDeclaration => CodeGenerationDestination.Namespace,
                    SyntaxKind.StructDeclaration => CodeGenerationDestination.StructType,
                    _ => CodeGenerationDestination.Unspecified,
                };
            }

            return CodeGenerationDestination.Unspecified;
        }

        public static TSyntaxNode ConditionallyAddDocumentationCommentTo<TSyntaxNode>(
            TSyntaxNode node,
            ISymbol symbol,
            CodeGenerationOptions options,
            CancellationToken cancellationToken = default)
            where TSyntaxNode : SyntaxNode
        {
            if (!options.GenerateDocumentationComments || node.GetLeadingTrivia().Any(t => t.IsDocComment()))
            {
                return node;
            }

            var result = TryGetDocumentationComment(symbol, "///", out var comment, cancellationToken)
                ? node.WithPrependedLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment))
                      .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)
                : node;
            return result;
        }
    }
}
