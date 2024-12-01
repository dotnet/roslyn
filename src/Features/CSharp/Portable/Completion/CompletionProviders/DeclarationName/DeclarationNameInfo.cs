// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName;

internal readonly struct NameDeclarationInfo(
    ImmutableArray<SymbolKindOrTypeKind> possibleSymbolKinds,
    Accessibility? accessibility,
    DeclarationModifiers declarationModifiers = default,
    ITypeSymbol? type = null,
    IAliasSymbol? alias = null,
    ISymbol? symbol = null)
{
    private static readonly ImmutableArray<SymbolKindOrTypeKind> s_parameterSyntaxKind =
        [new SymbolKindOrTypeKind(SymbolKind.Parameter)];

    private static readonly ImmutableArray<SymbolKindOrTypeKind> s_propertySyntaxKind =
        [new SymbolKindOrTypeKind(SymbolKind.Property)];

    private readonly ImmutableArray<SymbolKindOrTypeKind> _possibleSymbolKinds = possibleSymbolKinds;

    public readonly DeclarationModifiers Modifiers = declarationModifiers;
    public readonly Accessibility? DeclaredAccessibility = accessibility;

    public readonly ITypeSymbol? Type = type;
    public readonly IAliasSymbol? Alias = alias;
    public readonly ISymbol? Symbol = symbol;

    public ImmutableArray<SymbolKindOrTypeKind> PossibleSymbolKinds => _possibleSymbolKinds.NullToEmpty();

    public static async Task<NameDeclarationInfo> GetDeclarationInfoAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var info = await GetDeclarationInfoWorkerAsync(document, position, cancellationToken).ConfigureAwait(false);

        // if we bound to some error type, and that error type itself didn't start with an uppercase letter, then
        // it's almost certainly just an error case where the user was referencing something that was not a type.
        // for example:
        //
        //  goo $$
        //  goo = ...
        //
        // This syntactically looks like a type, but really isn't.  We don't want to offer anything here as it's far
        // more likely to be an error rather than a true new declaration.
        if (info.Type is IErrorTypeSymbol { Name.Length: > 0 } &&
            !char.IsUpper(info.Type.Name[0]))
        {
            return default;
        }

        return info;
    }

    private static async Task<NameDeclarationInfo> GetDeclarationInfoWorkerAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position);
        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(token.SpanStart, cancellationToken).ConfigureAwait(false);
        var typeInferenceService = document.GetRequiredLanguageService<ITypeInferenceService>();

        if (IsTupleTypeElement(token, semanticModel, cancellationToken, out var result)
            || IsPrimaryConstructorParameter(token, semanticModel, cancellationToken, out result)
            || IsParameterDeclaration(token, semanticModel, cancellationToken, out result)
            || IsTypeParameterDeclaration(token, out result)
            || IsLocalFunctionDeclaration(token, semanticModel, cancellationToken, out result)
            || IsLocalVariableDeclaration(token, semanticModel, cancellationToken, out result)
            || IsEmbeddedVariableDeclaration(token, semanticModel, cancellationToken, out result)
            || IsForEachVariableDeclaration(token, semanticModel, cancellationToken, out result)
            || IsIncompleteMemberDeclaration(token, semanticModel, cancellationToken, out result)
            || IsFieldDeclaration(token, semanticModel, cancellationToken, out result)
            || IsMethodDeclaration(token, semanticModel, cancellationToken, out result)
            || IsPropertyDeclaration(token, semanticModel, cancellationToken, out result)
            || IsPossibleOutVariableDeclaration(token, semanticModel, typeInferenceService, cancellationToken, out result)
            || IsTupleLiteralElement(token, semanticModel, cancellationToken, out result)
            || IsPossibleLocalVariableOrFunctionDeclaration(token, semanticModel, cancellationToken, out result)
            || IsPatternMatching(token, semanticModel, cancellationToken, out result))
        {
            return result;
        }

        return default;
    }

    private static bool IsTupleTypeElement(
        SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsFollowingTypeOrComma<TupleElementSyntax>(
            token,
            semanticModel,
            tupleElement => tupleElement.Type,
            _ => default(SyntaxTokenList),
            _ => [new SymbolKindOrTypeKind(SymbolKind.Local)], cancellationToken);

        return result.Type != null;
    }

    private static bool IsTupleLiteralElement(
        SyntaxToken token, SemanticModel semanticModel,
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
                _ => [new SymbolKindOrTypeKind(SymbolKind.Local)], cancellationToken);
            return result.Type != null;
        }

        result = default;
        return false;
    }

    private static bool IsPossibleOutVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
        ITypeInferenceService typeInferenceService, CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken) &&
            token.Parent.IsKind(SyntaxKind.IdentifierName))
        {
            var argument = token.Parent.Parent as ArgumentSyntax  // var is child of ArgumentSyntax, eg. Goo(out var $$
                ?? token.Parent.Parent?.Parent as ArgumentSyntax; // var is child of DeclarationExpression 

            // under ArgumentSyntax, eg. Goo(out var a$$
            if (argument is { RefOrOutKeyword: SyntaxToken(SyntaxKind.OutKeyword) })
            {
                var type = typeInferenceService.InferType(semanticModel, argument.SpanStart, objectAsDefault: false, cancellationToken: cancellationToken);
                if (type != null)
                {
                    var parameter = CSharpSemanticFacts.Instance.FindParameterForArgument(
                        semanticModel, argument, allowUncertainCandidates: true, allowParams: false, cancellationToken);

                    result = new NameDeclarationInfo(
                        [new SymbolKindOrTypeKind(SymbolKind.Local)],
                        Accessibility.NotApplicable,
                        type: type,
                        symbol: parameter);
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    private static bool IsPossibleLocalVariableOrFunctionDeclaration(
        SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsLastTokenOfType<ExpressionStatementSyntax>(
            token, semanticModel,
            e => e.Expression,
            _ => default,
            _ => [new SymbolKindOrTypeKind(SymbolKind.Local), new SymbolKindOrTypeKind(MethodKind.LocalFunction)],
            cancellationToken,
            out var expression);

        if (result.Type is null || expression is null)
            return false;

        // we have something like `x.y $$`.
        //
        // For this to actually be the start of a local declaration or function x.y needs to bind to an actual
        // type symbol, not just any arbitrary expression that might have a type (e.g. `Console.BackgroundColor $$).
        var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).GetAnySymbol();
        return symbol is ITypeSymbol;
    }

    private static bool IsPropertyDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
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
        CancellationToken cancellationToken, out NameDeclarationInfo result)
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

    private static NameDeclarationInfo IsFollowingTypeOrComma<TSyntaxNode>(
        SyntaxToken token,
        SemanticModel semanticModel,
        Func<TSyntaxNode, SyntaxNode?> typeSyntaxGetter,
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

        return new NameDeclarationInfo(
            possibleDeclarationComputer(GetDeclarationModifiers(modifiers.Value)),
            GetAccessibility(modifiers.Value),
            GetDeclarationModifiers(modifiers.Value),
            semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type,
            semanticModel.GetAliasInfo(typeSyntax, cancellationToken));
    }

    private static NameDeclarationInfo IsLastTokenOfType<TSyntaxNode>(
        SyntaxToken token,
        SemanticModel semanticModel,
        Func<TSyntaxNode, SyntaxNode?> typeSyntaxGetter,
        Func<TSyntaxNode, SyntaxTokenList> modifierGetter,
        Func<DeclarationModifiers, ImmutableArray<SymbolKindOrTypeKind>> possibleDeclarationComputer,
        CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        return IsLastTokenOfType(token, semanticModel, typeSyntaxGetter, modifierGetter, possibleDeclarationComputer, cancellationToken, out _);
    }

    private static NameDeclarationInfo IsLastTokenOfType<TSyntaxNode>(
        SyntaxToken token,
        SemanticModel semanticModel,
        Func<TSyntaxNode, SyntaxNode?> typeSyntaxGetter,
        Func<TSyntaxNode, SyntaxTokenList> modifierGetter,
        Func<DeclarationModifiers, ImmutableArray<SymbolKindOrTypeKind>> possibleDeclarationComputer,
        CancellationToken cancellationToken,
        out SyntaxNode? typeSyntax) where TSyntaxNode : SyntaxNode
    {
        typeSyntax = null;
        if (!IsPossibleTypeToken(token))
            return default;

        var target = token.GetAncestor<TSyntaxNode>();
        if (target == null)
            return default;

        typeSyntax = typeSyntaxGetter(target);
        if (typeSyntax == null || token != typeSyntax.GetLastToken())
            return default;

        var modifiers = modifierGetter(target);

        return new NameDeclarationInfo(
            possibleDeclarationComputer(GetDeclarationModifiers(modifiers)),
            GetAccessibility(modifiers),
            GetDeclarationModifiers(modifiers),
            semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type,
            semanticModel.GetAliasInfo(typeSyntax, cancellationToken));
    }

    private static bool IsFieldDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsFollowingTypeOrComma<VariableDeclarationSyntax>(token, semanticModel,
            v => v.Type,
            v => v.Parent is FieldDeclarationSyntax f ? f.Modifiers : null,
            GetPossibleMemberDeclarations,
            cancellationToken);
        return result.Type != null;
    }

    private static bool IsIncompleteMemberDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsLastTokenOfType<IncompleteMemberSyntax>(token, semanticModel,
            i => i.Type,
            i => i.Modifiers,
            GetPossibleMemberDeclarations,
            cancellationToken);
        return result.Type != null;
    }

    private static bool IsLocalFunctionDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsLastTokenOfType<LocalFunctionStatementSyntax>(token, semanticModel,
            typeSyntaxGetter: f => f.ReturnType,
            modifierGetter: f => f.Modifiers,
            possibleDeclarationComputer: GetPossibleLocalDeclarations,
            cancellationToken);
        return result.Type != null;
    }

    private static bool IsLocalVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        // If we only have a type, this can still end up being a local function (depending on the modifiers).
        var possibleDeclarationComputer = token.IsKind(SyntaxKind.CommaToken)
            ? (Func<DeclarationModifiers, ImmutableArray<SymbolKindOrTypeKind>>)
                (_ => [new SymbolKindOrTypeKind(SymbolKind.Local)])
            : GetPossibleLocalDeclarations;

        result = IsFollowingTypeOrComma<VariableDeclarationSyntax>(token, semanticModel,
             typeSyntaxGetter: v => v.Type,
             modifierGetter: v => v.Parent is LocalDeclarationStatementSyntax localDeclaration
                ? localDeclaration.Modifiers
                : null, // Return null to bail out.
             possibleDeclarationComputer,
             cancellationToken);
        if (result.Type != null)
        {
            return true;
        }

        // If the type has a trailing question mark, we may parse it as a conditional access expression.
        // We will use the condition as the type to bind; we won't make the type we bind nullable
        // because we ignore nullability when generating names anyways
        if (token.IsKind(SyntaxKind.QuestionToken) &&
            token.Parent is ConditionalExpressionSyntax conditionalExpressionSyntax)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(conditionalExpressionSyntax.Condition);

            if (symbolInfo.GetAnySymbol() is ITypeSymbol type)
            {
                var alias = semanticModel.GetAliasInfo(conditionalExpressionSyntax.Condition, cancellationToken);

                result = new NameDeclarationInfo(
                    possibleDeclarationComputer(default),
                    accessibility: null,
                    type: type,
                    alias: alias);
                return true;
            }
        }

        return false;
    }

    private static bool IsEmbeddedVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsFollowingTypeOrComma<VariableDeclarationSyntax>(token, semanticModel,
            typeSyntaxGetter: v => v.Type,
            modifierGetter: v => v.Parent is UsingStatementSyntax or ForStatementSyntax
                ? default(SyntaxTokenList)
                : null, // Return null to bail out.
            possibleDeclarationComputer: d => [new SymbolKindOrTypeKind(SymbolKind.Local)],
            cancellationToken);
        return result.Type != null;
    }

    private static bool IsForEachVariableDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        // This is parsed as ForEachVariableStatementSyntax:
        // foreach (int $$
        result = IsLastTokenOfType<CommonForEachStatementSyntax>(token, semanticModel,
            typeSyntaxGetter: f =>
                f is ForEachStatementSyntax forEachStatement ? forEachStatement.Type :
                f is ForEachVariableStatementSyntax forEachVariableStatement ? forEachVariableStatement.Variable :
                null, // Return null to bail out.
            modifierGetter: f => default,
            possibleDeclarationComputer: d => [new SymbolKindOrTypeKind(SymbolKind.Local)],
            cancellationToken);
        return result.Type != null;
    }

    private static bool IsTypeParameterDeclaration(SyntaxToken token, out NameDeclarationInfo result)
    {
        if (token.Kind() is SyntaxKind.LessThanToken or SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.TypeParameterList))
        {
            result = new NameDeclarationInfo(
                [new SymbolKindOrTypeKind(SymbolKind.TypeParameter)],
                Accessibility.NotApplicable);

            return true;
        }

        result = default;
        return false;
    }

    private static bool IsPrimaryConstructorParameter(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
    {
        result = IsLastTokenOfType<ParameterSyntax>(
            token, semanticModel,
            p => p.Type,
            _ => default,
            _ => s_propertySyntaxKind,
            cancellationToken);

        if (result.Type != null &&
            token.GetAncestor<ParameterSyntax>()?.Parent?.Parent is (kind: SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static bool IsParameterDeclaration(SyntaxToken token, SemanticModel semanticModel,
        CancellationToken cancellationToken, out NameDeclarationInfo result)
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
        CancellationToken cancellationToken, out NameDeclarationInfo result)
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

    private static bool IsPossibleTypeToken(SyntaxToken token)
        => token.Kind() is
             SyntaxKind.IdentifierToken or
             SyntaxKind.GreaterThanToken or
             SyntaxKind.CloseBracketToken or
             SyntaxKind.QuestionToken ||
           token.Parent.IsKind(SyntaxKind.PredefinedType);

    private static ImmutableArray<SymbolKindOrTypeKind> GetPossibleMemberDeclarations(DeclarationModifiers modifiers)
    {
        if (modifiers.IsConst || modifiers.IsReadOnly)
        {
            return [new SymbolKindOrTypeKind(SymbolKind.Field)];
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
                ? [new SymbolKindOrTypeKind(SymbolKind.Local)] :
            modifiers.IsAsync || modifiers.IsUnsafe
                ? [new SymbolKindOrTypeKind(MethodKind.LocalFunction)] :
            [new SymbolKindOrTypeKind(SymbolKind.Local), new SymbolKindOrTypeKind(MethodKind.LocalFunction)];
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

    private static SyntaxNode? GetNodeDenotingTheTypeOfTupleArgument(ArgumentSyntax argumentSyntax)
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

    public static Glyph GetGlyph(SymbolKind kind, Accessibility? declaredAccessibility)
    {
        var publicIcon = kind switch
        {
            SymbolKind.Field => Glyph.FieldPublic,
            SymbolKind.Local => Glyph.Local,
            SymbolKind.Method => Glyph.MethodPublic,
            SymbolKind.Parameter => Glyph.Parameter,
            SymbolKind.Property => Glyph.PropertyPublic,
            SymbolKind.RangeVariable => Glyph.RangeVariable,
            SymbolKind.TypeParameter => Glyph.TypeParameter,
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };

        switch (declaredAccessibility)
        {
            case Accessibility.Private:
                publicIcon += Glyph.ClassPrivate - Glyph.ClassPublic;
                break;

            case Accessibility.Protected:
            case Accessibility.ProtectedAndInternal:
            case Accessibility.ProtectedOrInternal:
                publicIcon += Glyph.ClassProtected - Glyph.ClassPublic;
                break;

            case Accessibility.Internal:
                publicIcon += Glyph.ClassInternal - Glyph.ClassPublic;
                break;
        }

        return publicIcon;
    }

    public static SymbolKind GetSymbolKind(SymbolKindOrTypeKind symbolKindOrTypeKind)
    {
        // There's no special glyph for local functions.
        // We don't need to differentiate them at this point.
        return symbolKindOrTypeKind.SymbolKind.HasValue ? symbolKindOrTypeKind.SymbolKind.Value :
            symbolKindOrTypeKind.MethodKind.HasValue ? SymbolKind.Method :
            throw ExceptionUtilities.Unreachable();
    }
}
