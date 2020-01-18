// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal class NameSyntaxSimplifier : AbstractCSharpSimplifier<NameSyntax, TypeSyntax>
    {
        public static readonly NameSyntaxSimplifier Instance = new NameSyntaxSimplifier();

        private NameSyntaxSimplifier()
        {
        }


        private static bool TryReduceName(
            NameSyntax name,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            if (name.IsVar)
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
                replacementNode = SyntaxFactory.IdentifierName(genericName.Identifier)
                    .WithLeadingTrivia(genericName.GetLeadingTrivia())
                    .WithTrailingTrivia(genericName.GetTrailingTrivia());

                issueSpan = genericName.TypeArgumentList.Span;
                return CanReplaceWithReducedName(
                    name, replacementNode, semanticModel, cancellationToken);
            }

            if (!(symbol is INamespaceOrTypeSymbol))
            {
                return false;
            }

            if (name.HasAnnotations(SpecialTypeAnnotation.Kind))
            {
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        name.GetLeadingTrivia(),
                        GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(name.GetAnnotations(SpecialTypeAnnotation.Kind).First())),
                        name.GetTrailingTrivia()));

                issueSpan = name.Span;

                return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel);
            }
            else
            {
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
                            text = declIdentifier.IsVerbatimIdentifier() ? declIdentifier.ToString().Substring(1) : declIdentifier.ToString();
                        }

                        var identifierToken = SyntaxFactory.Identifier(
                                name.GetLeadingTrivia(),
                                SyntaxKind.IdentifierToken,
                                text,
                                aliasReplacement.Name,
                                name.GetTrailingTrivia());

                        identifierToken = CSharpSimplificationService.TryEscapeIdentifierToken(identifierToken, name, semanticModel);
                        replacementNode = SyntaxFactory.IdentifierName(identifierToken);

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
                        if (name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel))
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
                        var inDeclarationContext = PreferPredefinedTypeKeywordInDeclarations(name, optionSet, semanticModel);
                        var inMemberAccessContext = PreferPredefinedTypeKeywordInMemberAccess(name, optionSet, semanticModel);

                        if (!name.Parent.IsKind(SyntaxKind.QualifiedName) && (inDeclarationContext || inMemberAccessContext))
                        {
                            // See if we can simplify this name (like System.Int32) to a built-in type (like 'int').
                            // If not, we'll still fall through and see if we can convert it to Int32.

                            var codeStyleOptionName = inDeclarationContext
                                ? nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
                                : nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess);

                            var type = semanticModel.GetTypeInfo(name, cancellationToken).Type;
                            if (type != null)
                            {
                                var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                                if (keywordKind != SyntaxKind.None &&
                                    CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordKind, codeStyleOptionName))
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                var typeSymbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;
                                if (typeSymbol.IsKind(SymbolKind.NamedType))
                                {
                                    var keywordKind = GetPredefinedKeywordKind(((INamedTypeSymbol)typeSymbol).SpecialType);
                                    if (keywordKind != SyntaxKind.None &&
                                        CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordKind, codeStyleOptionName))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }

                    // Nullable rewrite: Nullable<int> -> int?
                    // Don't rewrite in the case where Nullable<int> is part of some qualified name like Nullable<int>.Something
                    if (!name.IsVar && (symbol.Kind == SymbolKind.NamedType) && !name.IsLeftSideOfQualifiedName())
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

                            replacementNode = SyntaxFactory.NullableType(oldType)
                                .WithLeadingTrivia(name.GetLeadingTrivia())
                                    .WithTrailingTrivia(name.GetTrailingTrivia());
                            issueSpan = name.Span;

                            // we need to simplify the whole qualified name at once, because replacing the identifier on the left in
                            // System.Nullable<int> alone would be illegal.
                            // If this fails we want to continue to try at least to remove the System if possible.
                            if (name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel))
                            {
                                return true;
                            }
                        }
                    }
                }

                SyntaxToken identifier;
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
                        identifier = ((IdentifierNameSyntax)name).Identifier;

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
            if (!InsideCrefReference(name))
                return false;

            if (name.Parent is QualifiedCrefSyntax qualifiedCrefParent && qualifiedCrefParent.Container == name)
            {
                // we have <see cref="A.B.C.D"/> and we're trying to see if we can replace 
                // A.B.C with C.  In this case the parent of A.B.C is A.B.C.D which is a 
                // QualifiedCrefSyntax

                var qualifiedReplacement = SyntaxFactory.QualifiedCref(replacement, qualifiedCrefParent.Member);
                if (QualifiedCrefSimplifier.CanSimplifyWithReplacement(qualifiedCrefParent, semanticModel, qualifiedReplacement, cancellationToken))
                    return true;
            }
            else if (name.Parent is QualifiedNameSyntax qualifiedParent && qualifiedParent.Left == name &&
                     replacement is NameSyntax replacementName)
            {
                // we have <see cref="A.B.C.D"/> and we're trying to see if we can replace 
                // A.B with B.  In this case the parent of A.B is A.B.C which is a 
                // QualifiedNameSyntax

                var qualifiedReplacement = SyntaxFactory.QualifiedName(replacementName, qualifiedParent.Right);
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

            if (!InsideCrefReference(name))
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
            SyntaxKind keywordKind,
            string codeStyleOptionName)
        {
            replacementNode = CreatePredefinedTypeSyntax(name, keywordKind);

            issueSpan = name.Span; // we want to show the whole name expression as unnecessary

            var canReduce = name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel);

            if (canReduce)
            {
                replacementNode = replacementNode.WithAdditionalAnnotations(new SyntaxAnnotation(codeStyleOptionName));
            }

            return canReduce;
        }

        private static TypeSyntax CreatePredefinedTypeSyntax(ExpressionSyntax expression, SyntaxKind keywordKind)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(expression.GetLeadingTrivia(), keywordKind, expression.GetTrailingTrivia()));
        }

        private static bool TryReduceAttributeSuffix(
            NameSyntax name,
            SyntaxToken identifierToken,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan)
        {
            issueSpan = default;
            replacementNode = default;

            // we can try to remove the Attribute suffix if this is the attribute name
            if (SyntaxFacts.IsAttributeName(name))
            {
                if (name.Parent.Kind() == SyntaxKind.Attribute || name.IsRightSideOfDotOrColonColon())
                {
                    const string AttributeName = "Attribute";

                    // an attribute that should keep it (unnecessary "Attribute" suffix should be annotated with a DontSimplifyAnnotation
                    if (identifierToken.ValueText != AttributeName && identifierToken.ValueText.EndsWith(AttributeName, StringComparison.Ordinal) && !identifierToken.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation))
                    {
                        // weird. the semantic model is able to bind attribute syntax like "[as()]" although it's not valid code.
                        // so we need another check for keywords manually.
                        var newAttributeName = identifierToken.ValueText.Substring(0, identifierToken.ValueText.Length - 9);
                        if (SyntaxFacts.GetKeywordKind(newAttributeName) != SyntaxKind.None)
                        {
                            return false;
                        }

                        // if this attribute name in source contained Unicode escaping, we will loose it now
                        // because there is no easy way to determine the substring from identifier->ToString() 
                        // which would be needed to pass to SyntaxFactory.Identifier
                        // The result is an unescaped Unicode character in source.

                        // once we remove the Attribute suffix, we can't use an escaped identifier
                        var newIdentifierToken = identifierToken.CopyAnnotationsTo(
                            SyntaxFactory.Identifier(
                                identifierToken.LeadingTrivia,
                                newAttributeName,
                                identifierToken.TrailingTrivia));

                        replacementNode = SyntaxFactory.IdentifierName(newIdentifierToken)
                            .WithLeadingTrivia(name.GetLeadingTrivia());
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
        /// <returns></returns>
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
                        var namespaceDeclaration = (NamespaceDeclarationSyntax)parent;
                        return object.Equals(namespaceDeclaration.Name, node);

                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool TrySimplify(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan)
        {
            replacementNode = null;
            issueSpan = default;

            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        if (IsMemberAccessADynamicInvocation(memberAccess, semanticModel))
                        {
                            return false;
                        }

                        if (TrySimplifyMemberAccessOrQualifiedName(memberAccess.Expression, memberAccess.Name, semanticModel, out var newLeft, out issueSpan))
                        {
                            // replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = memberAccess.Update(newLeft, memberAccess.OperatorToken, memberAccess.Name)
                                .WithAdditionalAnnotations(Simplifier.Annotation);

                            // Ensure that replacement doesn't change semantics.
                            return !ReplacementChangesSemantics(memberAccess, replacementNode, semanticModel);
                        }

                        return false;
                    }

                case SyntaxKind.QualifiedName:
                    {
                        var qualifiedName = (QualifiedNameSyntax)expression;
                        if (TrySimplifyMemberAccessOrQualifiedName(qualifiedName.Left, qualifiedName.Right, semanticModel, out var newLeft, out issueSpan))
                        {
                            // replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = qualifiedName.Update((NameSyntax)newLeft, qualifiedName.DotToken, qualifiedName.Right)
                                .WithAdditionalAnnotations(Simplifier.Annotation);

                            // Ensure that replacement doesn't change semantics.
                            return !ReplacementChangesSemantics(qualifiedName, replacementNode, semanticModel);
                        }

                        return false;
                    }
            }

            return false;
        }

        private static bool ReplacementChangesSemantics(ExpressionSyntax originalExpression, ExpressionSyntax replacedExpression, SemanticModel semanticModel)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(originalExpression, replacedExpression, semanticModel, CancellationToken.None);
            return speculationAnalyzer.ReplacementChangesSemantics();
        }

        // Note: The caller needs to verify that replacement doesn't change semantics of the original expression.
        private static bool TrySimplifyMemberAccessOrQualifiedName(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan)
        {
            replacementNode = null;
            issueSpan = default;

            if (left != null && right != null)
            {
                var leftSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, left);
                if (leftSymbol != null && (leftSymbol.Kind == SymbolKind.NamedType))
                {
                    var rightSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, right);
                    if (rightSymbol != null && (rightSymbol.IsStatic || rightSymbol.Kind == SymbolKind.NamedType))
                    {
                        // Static member access or nested type member access.
                        var containingType = rightSymbol.ContainingType;

                        var enclosingSymbol = semanticModel.GetEnclosingSymbol(left.SpanStart);
                        var enclosingTypeParametersInsideOut = new List<ISymbol>();

                        while (enclosingSymbol != null)
                        {
                            if (enclosingSymbol is IMethodSymbol methodSymbol)
                            {
                                if (methodSymbol.TypeArguments.Length != 0)
                                {
                                    enclosingTypeParametersInsideOut.AddRange(methodSymbol.TypeArguments);
                                }
                            }

                            if (enclosingSymbol is INamedTypeSymbol namedTypeSymbol)
                            {
                                if (namedTypeSymbol.TypeArguments.Length != 0)
                                {
                                    enclosingTypeParametersInsideOut.AddRange(namedTypeSymbol.TypeArguments);
                                }
                            }

                            enclosingSymbol = enclosingSymbol.ContainingSymbol;
                        }

                        if (containingType != null && !containingType.Equals(leftSymbol))
                        {
                            if (leftSymbol is INamedTypeSymbol namedType &&
                                containingType.TypeArguments.Length != 0)
                            {
                                return false;
                            }

                            // We have a static member access or a nested type member access using a more derived type.
                            // Simplify syntax so as to use accessed member's most immediate containing type instead of the derived type.
                            replacementNode = containingType.GenerateTypeSyntax()
                                .WithLeadingTrivia(left.GetLeadingTrivia())
                                .WithTrailingTrivia(left.GetTrailingTrivia());
                            issueSpan = left.Span;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool CanReplaceWithReducedName(
            MemberAccessExpressionSyntax memberAccess,
            ExpressionSyntax reducedName,
            SemanticModel semanticModel,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            if (!IsThisOrTypeOrNamespace(memberAccess, semanticModel))
            {
                return false;
            }

            var speculationAnalyzer = new SpeculationAnalyzer(memberAccess, reducedName, semanticModel, cancellationToken);
            if (!speculationAnalyzer.SymbolsForOriginalAndReplacedNodesAreCompatible() ||
                speculationAnalyzer.ReplacementChangesSemantics())
            {
                return false;
            }

            if (WillConflictWithExistingLocal(memberAccess, reducedName, semanticModel))
            {
                return false;
            }

            if (IsMemberAccessADynamicInvocation(memberAccess, semanticModel))
            {
                return false;
            }

            if (AccessMethodWithDynamicArgumentInsideStructConstructor(memberAccess, semanticModel))
            {
                return false;
            }

            if (memberAccess.Expression.Kind() == SyntaxKind.BaseExpression)
            {
                var enclosingNamedType = semanticModel.GetEnclosingNamedType(memberAccess.SpanStart, cancellationToken);
                if (enclosingNamedType != null &&
                    !enclosingNamedType.IsSealed &&
                    symbol != null &&
                    symbol.IsOverridable())
                {
                    return false;
                }
            }

            var invalidTransformation1 = ParserWouldTreatExpressionAsCast(reducedName, memberAccess);

            return !invalidTransformation1;
        }
    }
}
