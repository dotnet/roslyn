// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;

using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using static SyntaxFactory;

internal sealed class NameSimplifier : AbstractCSharpSimplifier<NameSyntax, TypeSyntax>
{
    public static readonly NameSimplifier Instance = new();

    private NameSimplifier()
    {
    }

    public override bool TrySimplify(
        NameSyntax name,
        SemanticModel semanticModel,
        CSharpSimplifierOptions options,
        out TypeSyntax replacementNode,
        out TextSpan issueSpan,
        CancellationToken cancellationToken)
    {
        replacementNode = null;
        issueSpan = default;

        if (name.IsVar || name.IsNint || name.IsNuint)
        {
            return false;
        }

        // we should not simplify a name of a namespace declaration
        if (IsPartOfNamespaceDeclarationName(name))
        {
            return false;
        }

        // We can simplify Qualified names and AliasQualifiedNames. Generally, if we have 
        // something like "A.B.C.D", we only consider the full thing something we can simplify.
        // However, in the case of "A.B.C<>.D", then we'll only consider simplifying up to the 
        // first open name.  This is because if we remove the open name, we'll often change 
        // meaning as "D" will bind to C<T>.D which is different than C<>.D!
        if (name is QualifiedNameSyntax qualifiedName)
        {
            var left = qualifiedName.Left;
            if (ContainsOpenName(left))
            {
                // Don't simplify A.B<>.C
                return false;
            }
        }

        // 1. see whether binding the name binds to a symbol/type. if not, it is ambiguous and
        //    nothing we can do here.
        var symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, name);
        if (symbol == null)
        {
            return false;
        }

        // treat constructor names as types
        var method = symbol as IMethodSymbol;
        if (method.IsConstructor())
        {
            symbol = method.ContainingType;
        }

        if (symbol.Kind == SymbolKind.Method && name.Kind() == SyntaxKind.GenericName)
        {
            var genericName = (GenericNameSyntax)name;
            replacementNode = IdentifierName(genericName.Identifier)
                .WithLeadingTrivia(genericName.GetLeadingTrivia())
                .WithTrailingTrivia(genericName.GetTrailingTrivia());

            issueSpan = genericName.TypeArgumentList.Span;
            return CanReplaceWithReducedName(
                name, replacementNode, semanticModel, cancellationToken);
        }

        if (symbol is not INamespaceOrTypeSymbol)
        {
            return false;
        }

        if (name.HasAnnotations(SpecialTypeAnnotation.Kind))
        {
            var keywordToken = TryGetPredefinedKeywordToken(semanticModel, SpecialTypeAnnotation.GetSpecialType(name.GetAnnotations(SpecialTypeAnnotation.Kind).First()));
            if (keywordToken != null)
            {
                replacementNode = CreatePredefinedTypeSyntax(name, keywordToken.Value);
                issueSpan = name.Span;

                return CanReplaceWithReducedNameInContext(name, replacementNode, semanticModel);
            }
        }

        if (!name.IsRightSideOfDotOrColonColon())
        {
            if (TryReplaceExpressionWithAlias(name, semanticModel, symbol, cancellationToken, out var aliasReplacement))
            {
                // get the token text as it appears in source code to preserve e.g. Unicode character escaping
                var text = aliasReplacement.Name;
                var syntaxRef = aliasReplacement.DeclaringSyntaxReferences.FirstOrDefault();

                if (syntaxRef != null)
                {
                    var declIdentifier = ((UsingDirectiveSyntax)syntaxRef.GetSyntax(cancellationToken)).Alias.Name.Identifier;
                    text = declIdentifier.IsVerbatimIdentifier() ? declIdentifier.ToString()[1..] : declIdentifier.ToString();
                }

                var identifierToken = Identifier(
                        name.GetLeadingTrivia(),
                        SyntaxKind.IdentifierToken,
                        text,
                        aliasReplacement.Name,
                        name.GetTrailingTrivia());

                identifierToken = CSharpSimplificationService.TryEscapeIdentifierToken(identifierToken, name);
                replacementNode = IdentifierName(identifierToken);

                // Merge annotation to new syntax node
                var annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(RenameAnnotation.Kind);
                foreach (var annotatedNodeOrToken in annotatedNodesOrTokens)
                {
                    if (annotatedNodeOrToken.IsToken)
                    {
                        identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken);
                    }
                    else
                    {
                        replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode);
                    }
                }

                annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(AliasAnnotation.Kind);
                foreach (var annotatedNodeOrToken in annotatedNodesOrTokens)
                {
                    if (annotatedNodeOrToken.IsToken)
                    {
                        identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken);
                    }
                    else
                    {
                        replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode);
                    }
                }

                replacementNode = ((SimpleNameSyntax)replacementNode).WithIdentifier(identifierToken);
                issueSpan = name.Span;

                // In case the alias name is the same as the last name of the alias target, we only include 
                // the left part of the name in the unnecessary span to Not confuse uses.
                if (name.Kind() == SyntaxKind.QualifiedName)
                {
                    var qualifiedName3 = (QualifiedNameSyntax)name;

                    if (qualifiedName3.Right.Identifier.ValueText == identifierToken.ValueText)
                    {
                        issueSpan = qualifiedName3.Left.Span;
                    }
                }

                // first check if this would be a valid reduction
                if (CanReplaceWithReducedNameInContext(name, replacementNode, semanticModel))
                {
                    // in case this alias name ends with "Attribute", we're going to see if we can also 
                    // remove that suffix.
                    if (TryReduceAttributeSuffix(
                            name,
                            identifierToken,
                            out var replacementNodeWithoutAttributeSuffix,
                            out var issueSpanWithoutAttributeSuffix))
                    {
                        if (CanReplaceWithReducedName(name, replacementNodeWithoutAttributeSuffix, semanticModel, cancellationToken))
                        {
                            replacementNode = replacementNode.CopyAnnotationsTo(replacementNodeWithoutAttributeSuffix);
                            issueSpan = issueSpanWithoutAttributeSuffix;
                        }
                    }

                    return true;
                }

                return false;
            }

            var nameHasNoAlias = false;

            if (name is SimpleNameSyntax simpleName)
            {
                if (!simpleName.Identifier.HasAnnotations(AliasAnnotation.Kind))
                {
                    nameHasNoAlias = true;
                }
            }

            if (name is QualifiedNameSyntax qualifiedName2)
            {
                if (!qualifiedName2.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                {
                    nameHasNoAlias = true;
                }
            }

            if (name is AliasQualifiedNameSyntax aliasQualifiedName)
            {
                if (aliasQualifiedName.Name is SimpleNameSyntax &&
                    !aliasQualifiedName.Name.Identifier.HasAnnotations(AliasAnnotation.Kind) &&
                    !aliasQualifiedName.Name.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                {
                    nameHasNoAlias = true;
                }
            }

            var aliasInfo = semanticModel.GetAliasInfo(name, cancellationToken);
            if (nameHasNoAlias && aliasInfo == null)
            {
                // Don't simplify to predefined type if name is part of a QualifiedName.
                // QualifiedNames can't contain PredefinedTypeNames (although MemberAccessExpressions can).
                // In other words, the left side of a QualifiedName can't be a PredefinedTypeName.
                var inDeclarationContext = PreferPredefinedTypeKeywordInDeclarations(name, options, semanticModel);
                var inMemberAccessContext = PreferPredefinedTypeKeywordInMemberAccess(name, options, semanticModel);

                if (!name.Parent.IsKind(SyntaxKind.QualifiedName) && (inDeclarationContext || inMemberAccessContext))
                {
                    // See if we can simplify this name (like System.Int32) to a built-in type (like 'int').
                    // If not, we'll still fall through and see if we can convert it to Int32.

                    var codeStyleOptionName = inDeclarationContext
                        ? nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
                        : nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess);

                    var type = semanticModel.GetTypeInfo(name, cancellationToken).Type;
                    if (type != null)
                    {
                        var keywordToken = TryGetPredefinedKeywordToken(semanticModel, type.SpecialType);
                        if (CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordToken, codeStyleOptionName))
                            return true;
                    }
                    else
                    {
                        var typeSymbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;
                        if (typeSymbol is INamedTypeSymbol namedType)
                        {
                            var keywordToken = TryGetPredefinedKeywordToken(semanticModel, namedType.SpecialType);
                            if (CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordToken, codeStyleOptionName))
                                return true;
                        }
                    }
                }
            }

            // Nullable rewrite: Nullable<int> -> int?
            // Don't rewrite in the case where Nullable<int> is part of some qualified name like Nullable<int>.Something
            if (!name.IsVar && symbol.Kind == SymbolKind.NamedType && !name.IsLeftSideOfQualifiedName())
            {
                var type = (INamedTypeSymbol)symbol;
                if (aliasInfo == null && CanSimplifyNullable(type, name, semanticModel))
                {
                    GenericNameSyntax genericName;
                    if (name.Kind() == SyntaxKind.QualifiedName)
                    {
                        genericName = (GenericNameSyntax)((QualifiedNameSyntax)name).Right;
                    }
                    else
                    {
                        genericName = (GenericNameSyntax)name;
                    }

                    var oldType = genericName.TypeArgumentList.Arguments.First();
                    if (oldType.Kind() == SyntaxKind.OmittedTypeArgument)
                    {
                        return false;
                    }

                    replacementNode = NullableType(oldType)
                        .WithLeadingTrivia(name.GetLeadingTrivia())
                            .WithTrailingTrivia(name.GetTrailingTrivia());
                    issueSpan = name.Span;

                    // we need to simplify the whole qualified name at once, because replacing the identifier on the left in
                    // System.Nullable<int> alone would be illegal.
                    // If this fails we want to continue to try at least to remove the System if possible.
                    if (CanReplaceWithReducedNameInContext(name, replacementNode, semanticModel))
                    {
                        return true;
                    }
                }
            }
        }

        switch (name.Kind())
        {
            case SyntaxKind.AliasQualifiedName:
                var simpleName = ((AliasQualifiedNameSyntax)name).Name
                    .WithLeadingTrivia(name.GetLeadingTrivia());

                simpleName = simpleName.ReplaceToken(simpleName.Identifier,
                    ((AliasQualifiedNameSyntax)name).Name.Identifier.CopyAnnotationsTo(
                        simpleName.Identifier.WithLeadingTrivia(
                            ((AliasQualifiedNameSyntax)name).Alias.Identifier.LeadingTrivia)));

                replacementNode = simpleName;

                issueSpan = ((AliasQualifiedNameSyntax)name).Alias.Span;

                break;

            case SyntaxKind.QualifiedName:
                replacementNode = ((QualifiedNameSyntax)name).Right.WithLeadingTrivia(name.GetLeadingTrivia());
                issueSpan = ((QualifiedNameSyntax)name).Left.Span;

                break;

            case SyntaxKind.IdentifierName:
                {
                    var identifier = ((IdentifierNameSyntax)name).Identifier;

                    // we can try to remove the Attribute suffix if this is the attribute name
                    TryReduceAttributeSuffix(name, identifier, out replacementNode, out issueSpan);
                    break;
                }

            case SyntaxKind.GenericName:
                {
                    var identifier = ((GenericNameSyntax)name).Identifier;

                    // we can try to remove the Attribute suffix if this is the attribute name
                    TryReduceAttributeSuffix(name, identifier, out replacementNode, out issueSpan);
                    break;
                }
        }

        if (replacementNode == null)
        {
            return false;
        }

        // We may be looking at a name `X.Y` seeing if we can replace it with `Y`.  However, in
        // order to know for sure, we actually have to look slightly higher at `X.Y.Z` to see if
        // it can simplify to `Y.Z`.  This is because in the `Color Color` case we can only tell
        // if we can reduce by looking by also looking at what comes next to see if it will
        // cause the simplified name to bind to the instance or static side.
        if (TryReduceCrefColorColor(name, replacementNode, semanticModel, cancellationToken))
        {
            return true;
        }

        return CanReplaceWithReducedName(name, replacementNode, semanticModel, cancellationToken);
    }

    private static bool TryReduceCrefColorColor(
        NameSyntax name, TypeSyntax replacement,
        SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (!name.InsideCrefReference())
            return false;

        if (name.Parent is QualifiedCrefSyntax qualifiedCrefParent && qualifiedCrefParent.Container == name)
        {
            // we have <see cref="A.B.C.D"/> and we're trying to see if we can replace 
            // A.B.C with C.  In this case the parent of A.B.C is A.B.C.D which is a 
            // QualifiedCrefSyntax

            var qualifiedReplacement = QualifiedCref(replacement, qualifiedCrefParent.Member);
            if (QualifiedCrefSimplifier.CanSimplifyWithReplacement(qualifiedCrefParent, semanticModel, qualifiedReplacement, cancellationToken))
                return true;
        }
        else if (name.Parent is QualifiedNameSyntax qualifiedParent && qualifiedParent.Left == name &&
                 replacement is NameSyntax replacementName)
        {
            // we have <see cref="A.B.C.D"/> and we're trying to see if we can replace 
            // A.B with B.  In this case the parent of A.B is A.B.C which is a 
            // QualifiedNameSyntax

            var qualifiedReplacement = QualifiedName(replacementName, qualifiedParent.Right);
            return CanReplaceWithReducedName(
                qualifiedParent, qualifiedReplacement, semanticModel, cancellationToken);
        }

        return false;
    }

    private static bool CanSimplifyNullable(INamedTypeSymbol type, NameSyntax name, SemanticModel semanticModel)
    {
        if (!type.IsNullable())
        {
            return false;
        }

        if (type.IsUnboundGenericType)
        {
            // Don't simplify unbound generic type "Nullable<>".
            return false;
        }

        if (InsideNameOfExpression(name, semanticModel))
        {
            // Nullable<T> can't be simplified to T? in nameof expressions.
            return false;
        }

        if (!name.InsideCrefReference())
        {
            // Nullable<T> can always be simplified to T? outside crefs.
            return true;
        }

        if (name.Parent is NameMemberCrefSyntax)
            return false;

        // Inside crefs, if the T in this Nullable{T} is being declared right here
        // then this Nullable{T} is not a constructed generic type and we should
        // not offer to simplify this to T?.
        //
        // For example, we should not offer the simplification in the following cases where
        // T does not bind to an existing type / type parameter in the user's code.
        // - <see cref="Nullable{T}"/>
        // - <see cref="System.Nullable{T}.Value"/>
        //
        // And we should offer the simplification in the following cases where SomeType and
        // SomeMethod bind to a type and method declared elsewhere in the users code.
        // - <see cref="SomeType.SomeMethod(Nullable{SomeType})"/>

        var argument = type.TypeArguments.SingleOrDefault();
        if (argument == null || argument.IsErrorType())
        {
            return false;
        }

        var argumentDecl = argument.DeclaringSyntaxReferences.FirstOrDefault();
        if (argumentDecl == null)
        {
            // The type argument is a type from metadata - so this is a constructed generic nullable type that can be simplified (e.g. Nullable(Of Integer)).
            return true;
        }

        return !name.Span.Contains(argumentDecl.Span);
    }

    private static bool CanReplaceWithPredefinedTypeKeywordInContext(
        NameSyntax name,
        SemanticModel semanticModel,
        out TypeSyntax replacementNode,
        ref TextSpan issueSpan,
        SyntaxToken? keywordToken,
        string codeStyleOptionName)
    {
        replacementNode = null;
        if (keywordToken == null)
            return false;

        replacementNode = CreatePredefinedTypeSyntax(name, keywordToken.Value);

        issueSpan = name.Span; // we want to show the whole name expression as unnecessary

        var canReduce = CanReplaceWithReducedNameInContext(name, replacementNode, semanticModel);

        if (canReduce)
        {
            replacementNode = replacementNode.WithAdditionalAnnotations(new SyntaxAnnotation(codeStyleOptionName));
        }

        return canReduce;
    }

    private static bool TryReduceAttributeSuffix(
        NameSyntax name,
        SyntaxToken identifierToken,
        out TypeSyntax replacementNode,
        out TextSpan issueSpan)
    {
        issueSpan = default;
        replacementNode = null;

        // we can try to remove the Attribute suffix if this is the attribute name
        if (SyntaxFacts.IsAttributeName(name))
        {
            if (name.Parent.Kind() == SyntaxKind.Attribute || name.IsRightSideOfDotOrColonColon())
            {
                const string AttributeName = "Attribute";

                // an attribute that should keep it (unnecessary "Attribute" suffix should be annotated with a DoNotSimplifyAnnotation
                if (identifierToken.ValueText != AttributeName && identifierToken.ValueText.EndsWith(AttributeName, StringComparison.Ordinal) && !identifierToken.HasAnnotation(SimplificationHelpers.DoNotSimplifyAnnotation))
                {
                    // weird. the semantic model is able to bind attribute syntax like "[as()]" although it's not valid code.
                    // so we need another check for keywords manually.
                    var newAttributeName = identifierToken.ValueText[..^9];
                    if (SyntaxFacts.GetKeywordKind(newAttributeName) != SyntaxKind.None)
                    {
                        return false;
                    }

                    // if this attribute name in source contained Unicode escaping, we will loose it now
                    // because there is no easy way to determine the substring from identifier->ToString() 
                    // which would be needed to pass to Identifier
                    // The result is an unescaped Unicode character in source.

                    // once we remove the Attribute suffix, we can't use an escaped identifier
                    var newIdentifierToken = identifierToken.CopyAnnotationsTo(
                        Identifier(
                            identifierToken.LeadingTrivia,
                            newAttributeName,
                            identifierToken.TrailingTrivia));

                    switch (name)
                    {
                        case GenericNameSyntax generic:
                            replacementNode = GenericName(newIdentifierToken, generic.TypeArgumentList)
                                .WithLeadingTrivia(name.GetLeadingTrivia());
                            break;

                        default:
                            replacementNode = IdentifierName(newIdentifierToken)
                                .WithLeadingTrivia(name.GetLeadingTrivia());
                            break;
                    }
                    issueSpan = new TextSpan(identifierToken.Span.End - 9, 9);

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the SyntaxNode is a name of a namespace declaration. To be a namespace name, the syntax
    /// must be parented by an namespace declaration and the node itself must be equal to the declaration's Name
    /// property.
    /// </summary>
    /// <param name="node"></param>
    private static bool IsPartOfNamespaceDeclarationName(SyntaxNode node)
    {
        var parent = node;

        while (parent != null)
        {
            switch (parent.Kind())
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.QualifiedName:
                    node = parent;
                    parent = parent.Parent;
                    break;

                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)parent;
                    return object.Equals(namespaceDeclaration.Name, node);

                default:
                    return false;
            }
        }

        return false;
    }

    public static bool CanReplaceWithReducedNameInContext(
        NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel)
    {
        // Check for certain things that would prevent us from reducing this name in this context.
        // For example, you can simplify "using a = System.Int32" to "using a = int" as it's simply
        // not allowed in the C# grammar.

        if (IsNonNameSyntaxInUsingDirective(name, reducedName) ||
            WillConflictWithExistingLocal(name, reducedName, semanticModel) ||
            IsAmbiguousCast(name, reducedName) ||
            IsNullableTypeInPointerExpression(reducedName) ||
            IsNotNullableReplaceable(name, reducedName) ||
            IsNonReducableQualifiedNameInUsingDirective(semanticModel, name))
        {
            return false;
        }

        return true;
    }

    private static bool ContainsOpenName(NameSyntax name)
    {
        if (name is QualifiedNameSyntax qualifiedName)
        {
            return ContainsOpenName(qualifiedName.Left) || ContainsOpenName(qualifiedName.Right);
        }
        else if (name is GenericNameSyntax genericName)
        {
            return genericName.IsUnboundGenericName;
        }
        else
        {
            return false;
        }
    }

    private static bool CanReplaceWithReducedName(NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var speculationAnalyzer = new SpeculationAnalyzer(name, reducedName, semanticModel, cancellationToken);
        if (speculationAnalyzer.ReplacementChangesSemantics())
        {
            return false;
        }

        return NameSimplifier.CanReplaceWithReducedNameInContext(name, reducedName, semanticModel);
    }

    private static bool IsNotNullableReplaceable(NameSyntax name, TypeSyntax reducedName)
    {
        if (reducedName is NullableTypeSyntax nullableType)
        {
            if (nullableType.ElementType.Kind() == SyntaxKind.OmittedTypeArgument)
                return true;

            return name.IsLeftSideOfDot() || name.IsRightSideOfDot();
        }

        return false;
    }

    private static bool IsNullableTypeInPointerExpression(ExpressionSyntax simplifiedNode)
    {
        // Note: nullable type syntax is not allowed in pointer type syntax
        if (simplifiedNode.Kind() == SyntaxKind.NullableType &&
            simplifiedNode.DescendantNodes().Any(n => n is PointerTypeSyntax))
        {
            return true;
        }

        return false;
    }

    private static bool IsNonNameSyntaxInUsingDirective(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
    {
        return
            expression.IsParentKind(SyntaxKind.UsingDirective) &&
            !(simplifiedNode is NameSyntax);
    }

    private static bool IsAmbiguousCast(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
    {
        // Can't simplify a type name in a cast expression if it would then cause the cast to be
        // parsed differently.  For example:  (Goo::Bar)+1  is a cast.  But if that simplifies to
        // (Bar)+1  then that's an arithmetic expression.
        if (expression?.Parent is CastExpressionSyntax castExpression &&
            castExpression.Type == expression)
        {
            var newCastExpression = castExpression.ReplaceNode(castExpression.Type, simplifiedNode);
            var reparsedCastExpression = ParseExpression(newCastExpression.ToString());

            if (!reparsedCastExpression.IsKind(SyntaxKind.CastExpression))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNonReducableQualifiedNameInUsingDirective(SemanticModel model, NameSyntax name)
    {
        // Whereas most of the time we do not want to reduce namespace names, We will
        // make an exception for namespaces with the global:: alias.
        return IsQualifiedNameInUsingDirective(model, name) &&
            !IsGlobalAliasQualifiedName(name);
    }

    private static bool IsQualifiedNameInUsingDirective(SemanticModel model, NameSyntax name)
    {
        while (name.IsLeftSideOfQualifiedName())
        {
            name = (NameSyntax)name.Parent;
        }

        if (name?.Parent is UsingDirectiveSyntax usingDirective &&
            usingDirective.Alias == null)
        {
            // We're a qualified name in a using.  We don't want to reduce this name as people like
            // fully qualified names in usings so they can properly tell what the name is resolving
            // to.
            // However, if this name is actually referencing the special Script class, then we do
            // want to allow that to be reduced.

            return !IsInScriptClass(model, name);
        }

        return false;
    }

    private static bool IsGlobalAliasQualifiedName(NameSyntax name)
    {
        // Checks whether the `global::` alias is applied to the name
        return name is AliasQualifiedNameSyntax aliasName &&
            aliasName.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword);
    }

    private static bool IsInScriptClass(SemanticModel model, NameSyntax name)
    {
        var symbol = model.GetSymbolInfo(name).Symbol as INamedTypeSymbol;
        while (symbol != null)
        {
            if (symbol.IsScriptClass)
            {
                return true;
            }

            symbol = symbol.ContainingType;
        }

        return false;
    }

    private static bool PreferPredefinedTypeKeywordInDeclarations(NameSyntax name, CSharpSimplifierOptions options, SemanticModel semanticModel)
    {
        return !name.IsDirectChildOfMemberAccessExpression() &&
               !name.InsideCrefReference() &&
               !InsideNameOfExpression(name, semanticModel) &&
               options.PreferPredefinedTypeKeywordInDeclaration.Value;
    }
}
