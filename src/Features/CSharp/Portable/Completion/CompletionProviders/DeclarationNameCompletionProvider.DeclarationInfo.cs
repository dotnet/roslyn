// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class DeclarationNameCompletionProvider
    {
        internal struct NameDeclarationInfo
        {
            private static readonly ImmutableArray<SymbolKindOrTypeKind> s_parameterSyntaxKind =
                ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Parameter));

            public NameDeclarationInfo(
                ImmutableArray<SymbolKindOrTypeKind> possibleSymbolKinds,
                Accessibility? accessibility,
                DeclarationModifiers declarationModifiers,
                ITypeSymbol type,
                IAliasSymbol alias)
            {
                PossibleSymbolKinds = possibleSymbolKinds;
                DeclaredAccessibility = accessibility;
                Modifiers = declarationModifiers;
                Type = type;
                Alias = alias;
            }

            public ImmutableArray<SymbolKindOrTypeKind> PossibleSymbolKinds { get; }
            public DeclarationModifiers Modifiers { get; }
            public ITypeSymbol Type { get; }
            public IAliasSymbol Alias { get; }
            public Accessibility? DeclaredAccessibility { get; }

            internal static async Task<NameDeclarationInfo> GetDeclarationInfo(Document document, int position, CancellationToken cancellationToken)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position);
                var semanticModel = await document.GetSemanticModelForSpanAsync(new Text.TextSpan(token.SpanStart, 0), cancellationToken).ConfigureAwait(false);
                var typeInferenceService = document.GetLanguageService<ITypeInferenceService>();

                if (IsTupleTypeElement(token, semanticModel, position, cancellationToken, out var result)
                    || IsParameterDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsTypeParameterDeclaration(token, semanticModel, position, out result)
                    || IsLocalFunctionDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsLocalVariableDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsEmbeddedVariableDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsForEachVariableDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsIncompleteMemberDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsFieldDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsMethodDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsPropertyDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsPossibleOutVariableDeclaration(token, semanticModel, position, typeInferenceService, cancellationToken, out result)
                    || IsTupleLiteralElement(token, semanticModel, position, cancellationToken, out result)
                    || IsPossibleVariableOrLocalMethodDeclaration(token, semanticModel, position, cancellationToken, out result)
                    || IsPatternMatching(token, semanticModel, position, cancellationToken, out result))
                {
                    return result;
                }

                return default;
            }

            private static bool IsTupleTypeElement(
                SyntaxToken token, SemanticModel semanticModel, int position,
                CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsFollowingTypeOrComma<TupleElementSyntax>(
                    token,
                    semanticModel,
                    tupleElement => tupleElement.Type,
                    _ => default(SyntaxTokenList),
                    _ => ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)), cancellationToken);

                return result.Type != null;
            }

            private static bool IsTupleLiteralElement(
                SyntaxToken token, SemanticModel semanticModel, int position,
                CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                // Incomplete code like
                // void Do()
                // {
                //    (System.Array array, System.Action $$ 
                // gets parsed as a tuple expression. We can figure out the type in such cases.
                // For a legit tuple expression we can't provide any completion.
                if (token.GetAncestor(node => node.IsKind(SyntaxKind.TupleExpression)) != null)
                {
                    result = IsFollowingTypeOrComma<ArgumentSyntax>(
                        token,
                        semanticModel,
                        GetNodeDenotingTheTypeOfTupleArgument,
                        _ => default(SyntaxTokenList),
                        _ => ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)), cancellationToken);
                    return result.Type != null;
                }

                result = default;
                return false;
            }

            private static bool IsPossibleOutVariableDeclaration(SyntaxToken token, SemanticModel semanticModel, int position,
                ITypeInferenceService typeInferenceService, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                if (!token.IsKind(SyntaxKind.IdentifierToken) || !(token.Parent.IsKind(SyntaxKind.IdentifierName)))
                {
                    result = default;
                    return false;
                }

                var argument = token.Parent.Parent as ArgumentSyntax // var is child of ArgumentSyntax, eg. Goo(out var $$
                    ?? token.Parent.Parent.Parent as ArgumentSyntax; // var is child of DeclarationExpression 
                                                                     // under ArgumentSyntax, eg. Goo(out var a$$

                if (argument == null || !argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    result = default;
                    return false;
                }

                var type = typeInferenceService.InferType(semanticModel, argument.SpanStart, objectAsDefault: false, cancellationToken: cancellationToken);
                if (type != null)
                {
                    result = new NameDeclarationInfo(
                        ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)),
                        Accessibility.NotApplicable,
                        new DeclarationModifiers(),
                        type,
                        alias: null);
                    return true;
                }

                result = default;
                return false;
            }

            private static bool IsPossibleVariableOrLocalMethodDeclaration(
                SyntaxToken token, SemanticModel semanticModel, int position,
                CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsLastTokenOfType<ExpressionStatementSyntax>(
                    token, semanticModel,
                    e => e.Expression,
                    _ => default,
                    _ => ImmutableArray.Create(
                        new SymbolKindOrTypeKind(SymbolKind.Local),
                        new SymbolKindOrTypeKind(MethodKind.LocalFunction)),
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsPropertyDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsLastTokenOfType<PropertyDeclarationSyntax>(
                    token,
                    semanticModel,
                    m => m.Type,
                    m => m.Modifiers,
                    GetPossibleMemberDeclarations,
                    cancellationToken);

                return result.Type != null;
            }

            private static bool IsMethodDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsLastTokenOfType<MethodDeclarationSyntax>(
                    token,
                    semanticModel,
                    m => m.ReturnType,
                    m => m.Modifiers,
                    GetPossibleMemberDeclarations,
                    cancellationToken);

                return result.Type != null;
            }

            private static NameDeclarationInfo IsFollowingTypeOrComma<TSyntaxNode>(SyntaxToken token,
                SemanticModel semanticModel,
                Func<TSyntaxNode, SyntaxNode> typeSyntaxGetter,
                Func<TSyntaxNode, SyntaxTokenList?> modifierGetter,
                Func<DeclarationModifiers, ImmutableArray<SymbolKindOrTypeKind>> possibleDeclarationComputer,
                CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
            {
                if (!IsPossibleTypeToken(token) && !token.IsKind(SyntaxKind.CommaToken))
                {
                    return default;
                }

                var target = token.GetAncestor<TSyntaxNode>();
                if (target == null)
                {
                    return default;
                }

                if (token.IsKind(SyntaxKind.CommaToken) && token.Parent != target)
                {
                    return default;
                }

                var typeSyntax = typeSyntaxGetter(target);
                if (typeSyntax == null)
                {
                    return default;
                }

                if (!token.IsKind(SyntaxKind.CommaToken) && token != typeSyntax.GetLastToken())
                {
                    return default;
                }

                var modifiers = modifierGetter(target);

                if (modifiers == null)
                {
                    return default;
                }

                var alias = semanticModel.GetAliasInfo(typeSyntax, cancellationToken);
                var type = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;

                return new NameDeclarationInfo(
                    possibleDeclarationComputer(GetDeclarationModifiers(modifiers.Value)),
                    GetAccessibility(modifiers.Value),
                    GetDeclarationModifiers(modifiers.Value),
                    type,
                    alias);
            }

            private static NameDeclarationInfo IsLastTokenOfType<TSyntaxNode>(
                SyntaxToken token,
                SemanticModel semanticModel,
                Func<TSyntaxNode, SyntaxNode> typeSyntaxGetter,
                Func<TSyntaxNode, SyntaxTokenList> modifierGetter,
                Func<DeclarationModifiers, ImmutableArray<SymbolKindOrTypeKind>> possibleDeclarationComputer,
                CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
            {
                if (!IsPossibleTypeToken(token))
                {
                    return default;
                }

                var target = token.GetAncestor<TSyntaxNode>();
                if (target == null)
                {
                    return default;
                }

                var typeSyntax = typeSyntaxGetter(target);
                if (typeSyntax == null || token != typeSyntax.GetLastToken())
                {
                    return default;
                }

                var modifiers = modifierGetter(target);

                return new NameDeclarationInfo(
                    possibleDeclarationComputer(GetDeclarationModifiers(modifiers)),
                    GetAccessibility(modifiers),
                    GetDeclarationModifiers(modifiers),
                    semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type,
                    semanticModel.GetAliasInfo(typeSyntax, cancellationToken));
            }

            private static bool IsFieldDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsFollowingTypeOrComma<VariableDeclarationSyntax>(token, semanticModel,
                    v => v.Type,
                    v => v.Parent is FieldDeclarationSyntax f ? f.Modifiers : default(SyntaxTokenList?),
                    GetPossibleMemberDeclarations,
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsIncompleteMemberDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsLastTokenOfType<IncompleteMemberSyntax>(token, semanticModel,
                    i => i.Type,
                    i => i.Modifiers,
                    GetPossibleMemberDeclarations,
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsLocalFunctionDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsLastTokenOfType<LocalFunctionStatementSyntax>(token, semanticModel,
                    typeSyntaxGetter: f => f.ReturnType,
                    modifierGetter: f => f.Modifiers,
                    possibleDeclarationComputer: GetPossibleLocalDeclarations,
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsLocalVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                // If we only have a type, this can still end up being a local function (depending on the modifiers).
                var possibleDeclarationComputer = token.IsKind(SyntaxKind.CommaToken)
                    ? (Func<DeclarationModifiers, ImmutableArray<SymbolKindOrTypeKind>>)
                        (_ => ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)))
                    : GetPossibleLocalDeclarations;

                result = IsFollowingTypeOrComma<VariableDeclarationSyntax>(token, semanticModel,
                     typeSyntaxGetter: v => v.Type,
                     modifierGetter: v => v.Parent is LocalDeclarationStatementSyntax localDeclaration
                        ? localDeclaration.Modifiers
                        : default(SyntaxTokenList?), // Return null to bail out.
                     possibleDeclarationComputer,
                     cancellationToken);
                return result.Type != null;
            }

            private static bool IsEmbeddedVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsFollowingTypeOrComma<VariableDeclarationSyntax>(token, semanticModel,
                    typeSyntaxGetter: v => v.Type,
                    modifierGetter: v => v.Parent is UsingStatementSyntax || v.Parent is ForStatementSyntax
                        ? default(SyntaxTokenList)
                        : default(SyntaxTokenList?), // Return null to bail out.
                    possibleDeclarationComputer: d => ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)),
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsForEachVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                // This is parsed as ForEachVariableStatementSyntax:
                // foreach (int $$
                result = IsLastTokenOfType<CommonForEachStatementSyntax>(token, semanticModel,
                    typeSyntaxGetter: f =>
                        f is ForEachStatementSyntax forEachStatement ? forEachStatement.Type :
                        f is ForEachVariableStatementSyntax forEachVariableStatement ? forEachVariableStatement.Variable :
                        null, // Return null to bail out.
                    modifierGetter: f => default,
                    possibleDeclarationComputer: d => ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)),
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsTypeParameterDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, out NameDeclarationInfo result)
            {
                if (token.IsKind(SyntaxKind.LessThanToken, SyntaxKind.CommaToken) &&
                    token.Parent.IsKind(SyntaxKind.TypeParameterList))
                {
                    result = new NameDeclarationInfo(
                        ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.TypeParameter)),
                        Accessibility.NotApplicable,
                        new DeclarationModifiers(),
                        type: null,
                        alias: null);

                    return true;
                }

                result = default;
                return false;
            }

            private static bool IsParameterDeclaration(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = IsLastTokenOfType<ParameterSyntax>(
                    token, semanticModel,
                    p => p.Type,
                    _ => default,
                    _ => s_parameterSyntaxKind,
                    cancellationToken);
                return result.Type != null;
            }

            private static bool IsPatternMatching(SyntaxToken token, SemanticModel semanticModel,
                int position, CancellationToken cancellationToken, out NameDeclarationInfo result)
            {
                result = default;
                if (token.Parent.IsParentKind(SyntaxKind.IsExpression))
                {
                    result = IsLastTokenOfType<BinaryExpressionSyntax>(
                        token, semanticModel,
                        b => b.Right,
                        _ => default,
                        _ => s_parameterSyntaxKind,
                        cancellationToken);
                }
                else if (token.Parent.IsParentKind(SyntaxKind.CaseSwitchLabel))
                {
                    result = IsLastTokenOfType<CaseSwitchLabelSyntax>(
                        token, semanticModel,
                        b => b.Value,
                        _ => default,
                        _ => s_parameterSyntaxKind,
                        cancellationToken);
                }
                else if (token.Parent.IsParentKind(SyntaxKind.DeclarationPattern))
                {
                    result = IsLastTokenOfType<DeclarationPatternSyntax>(
                        token, semanticModel,
                        b => b.Type,
                        _ => default,
                        _ => s_parameterSyntaxKind,
                        cancellationToken);
                }

                return result.Type != null;
            }

            private static bool IsPossibleTypeToken(SyntaxToken token) =>
                token.IsKind(
                    SyntaxKind.IdentifierToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.CloseBracketToken)
                || token.Parent.IsKind(SyntaxKind.PredefinedType);

            private static ImmutableArray<SymbolKindOrTypeKind> GetPossibleMemberDeclarations(DeclarationModifiers modifiers)
            {
                if (modifiers.IsConst || modifiers.IsReadOnly)
                {
                    return ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field));
                }

                var possibleTypes = ImmutableArray.Create(
                    new SymbolKindOrTypeKind(SymbolKind.Field),
                    new SymbolKindOrTypeKind(SymbolKind.Property),
                    new SymbolKindOrTypeKind(MethodKind.Ordinary));

                if (modifiers.IsAbstract || modifiers.IsVirtual || modifiers.IsSealed || modifiers.IsOverride)
                {
                    possibleTypes = possibleTypes.Remove(new SymbolKindOrTypeKind(SymbolKind.Field));
                }

                if (modifiers.IsAsync || modifiers.IsPartial)
                {
                    // Fields and properties cannot be async or partial.
                    possibleTypes = possibleTypes.Remove(new SymbolKindOrTypeKind(SymbolKind.Field));
                    possibleTypes = possibleTypes.Remove(new SymbolKindOrTypeKind(SymbolKind.Property));
                }

                return possibleTypes;
            }

            private static ImmutableArray<SymbolKindOrTypeKind> GetPossibleLocalDeclarations(DeclarationModifiers modifiers)
            {
                return
                    modifiers.IsConst
                        ? ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Local)) :
                    modifiers.IsAsync || modifiers.IsUnsafe
                        ? ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.LocalFunction)) :
                    ImmutableArray.Create(
                        new SymbolKindOrTypeKind(SymbolKind.Local),
                        new SymbolKindOrTypeKind(MethodKind.LocalFunction));
            }

            private static DeclarationModifiers GetDeclarationModifiers(SyntaxTokenList modifiers)
            {
                var declarationModifiers = new DeclarationModifiers();
                foreach (var modifer in modifiers)
                {
                    switch (modifer.Kind())
                    {
                        case SyntaxKind.StaticKeyword:
                            declarationModifiers = declarationModifiers.WithIsStatic(true);
                            continue;
                        case SyntaxKind.AbstractKeyword:
                            declarationModifiers = declarationModifiers.WithIsAbstract(true);
                            continue;
                        case SyntaxKind.NewKeyword:
                            declarationModifiers = declarationModifiers.WithIsNew(true);
                            continue;
                        case SyntaxKind.UnsafeKeyword:
                            declarationModifiers = declarationModifiers.WithIsUnsafe(true);
                            continue;
                        case SyntaxKind.ReadOnlyKeyword:
                            declarationModifiers = declarationModifiers.WithIsReadOnly(true);
                            continue;
                        case SyntaxKind.VirtualKeyword:
                            declarationModifiers = declarationModifiers.WithIsVirtual(true);
                            continue;
                        case SyntaxKind.OverrideKeyword:
                            declarationModifiers = declarationModifiers.WithIsOverride(true);
                            continue;
                        case SyntaxKind.SealedKeyword:
                            declarationModifiers = declarationModifiers.WithIsSealed(true);
                            continue;
                        case SyntaxKind.ConstKeyword:
                            declarationModifiers = declarationModifiers.WithIsConst(true);
                            continue;
                        case SyntaxKind.AsyncKeyword:
                            declarationModifiers = declarationModifiers.WithAsync(true);
                            continue;
                        case SyntaxKind.PartialKeyword:
                            declarationModifiers = declarationModifiers.WithPartial(true);
                            continue;
                    }
                }

                return declarationModifiers;
            }

            private static Accessibility? GetAccessibility(SyntaxTokenList modifiers)
            {
                for (var i = modifiers.Count - 1; i >= 0; i--)
                {
                    var modifier = modifiers[i];
                    switch (modifier.Kind())
                    {
                        case SyntaxKind.PrivateKeyword:
                            return Accessibility.Private;
                        case SyntaxKind.PublicKeyword:
                            return Accessibility.Public;
                        case SyntaxKind.ProtectedKeyword:
                            return Accessibility.Protected;
                        case SyntaxKind.InternalKeyword:
                            return Accessibility.Internal;
                    }
                }

                return null;
            }

            private static SyntaxNode GetNodeDenotingTheTypeOfTupleArgument(ArgumentSyntax argumentSyntax)
            {
                switch (argumentSyntax.Expression?.Kind())
                {
                    case SyntaxKind.DeclarationExpression:
                        // The parser found a declaration as in (System.Action action, System.Array a$$)
                        // we need the type part of the declaration expression.
                        return ((DeclarationExpressionSyntax)argumentSyntax.Expression).Type;
                    default:
                        // We assume the parser found something that represents something named,
                        // e.g. a MemberAccessExpression as in (System.Action action, System.Array $$)
                        // We also assume that this name could be resolved to a type.
                        return argumentSyntax.Expression;
                }
            }
        }
    }
}
