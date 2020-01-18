// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal class ExpressionSyntaxSimplifier : AbstractCSharpSimplifier<ExpressionSyntax, ExpressionSyntax>
    {
        public static readonly ExpressionSyntaxSimplifier Instance = new ExpressionSyntaxSimplifier();

        private ExpressionSyntaxSimplifier()
        {
        }



        public static bool TryReduceOrSimplifyExplicitName(
            this ExpressionSyntax expression,
            SemanticModel semanticModel,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            if (TryReduceExplicitName(expression, semanticModel, out var replacementTypeNode, out issueSpan, optionSet, cancellationToken))
            {
                replacementNode = replacementTypeNode;
                return true;
            }

            return TrySimplify(expression, semanticModel, out replacementNode, out issueSpan);
        }

        private static bool TryReduceExplicitName(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            if (expression.ContainsInterleavedDirective(cancellationToken))
                return false;

            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax memberAccess))
                return TryReduceMemberAccessExpression(memberAccess, semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);

            if (expression is NameSyntax name)
                return TryReduceName(name, semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);

            return false;
        }

        private static bool TryReduceMemberAccessExpression(
            MemberAccessExpressionSyntax memberAccess,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            if (memberAccess.Name == null || memberAccess.Expression == null)
                return false;

            // if this node is annotated as being a SpecialType, let's use this information.
            if (memberAccess.HasAnnotations(SpecialTypeAnnotation.Kind))
            {
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        memberAccess.GetLeadingTrivia(),
                        GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(memberAccess.GetAnnotations(SpecialTypeAnnotation.Kind).First())),
                        memberAccess.GetTrailingTrivia()));

                issueSpan = memberAccess.Span;
                return true;
            }

            // See https://github.com/dotnet/roslyn/issues/40974
            //
            // To be very safe, we only support simplifying code that bound to a symbol without any
            // sort of problems.  We could potentially relax this in the future.  However, we would
            // need to be very careful about the implications of us offering to fixup 'broken' code 
            // in a manner that might end up making things worse or confusing the user.
            var symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, memberAccess);
            if (symbol == null)
                return false;

            if (memberAccess.Expression.IsKind(SyntaxKind.ThisExpression) &&
                !SimplificationHelpers.ShouldSimplifyThisOrMeMemberAccessExpression(semanticModel, optionSet, symbol))
            {
                return false;
            }

            // if this node is on the left side, we could simplify to aliases
            if (!memberAccess.IsRightSideOfDot())
            {
                // Check if we need to replace this syntax with an alias identifier
                if (TryReplaceExpressionWithAlias(
                        memberAccess, semanticModel, symbol,
                        cancellationToken, out var aliasReplacement))
                {
                    // get the token text as it appears in source code to preserve e.g. unicode character escaping
                    var text = aliasReplacement.Name;
                    var syntaxRef = aliasReplacement.DeclaringSyntaxReferences.FirstOrDefault();

                    if (syntaxRef != null)
                    {
                        var declIdentifier = ((UsingDirectiveSyntax)syntaxRef.GetSyntax(cancellationToken)).Alias.Name.Identifier;
                        text = declIdentifier.IsVerbatimIdentifier() ? declIdentifier.ToString().Substring(1) : declIdentifier.ToString();
                    }

                    replacementNode = SyntaxFactory.IdentifierName(
                                        memberAccess.Name.Identifier.CopyAnnotationsTo(SyntaxFactory.Identifier(
                                            memberAccess.GetLeadingTrivia(),
                                            SyntaxKind.IdentifierToken,
                                            text,
                                            aliasReplacement.Name,
                                            memberAccess.GetTrailingTrivia())));

                    replacementNode = memberAccess.CopyAnnotationsTo(replacementNode);
                    replacementNode = memberAccess.Name.CopyAnnotationsTo(replacementNode);

                    issueSpan = memberAccess.Span;

                    // In case the alias name is the same as the last name of the alias target, we only include 
                    // the left part of the name in the unnecessary span to Not confuse uses.
                    if (memberAccess.Name.Identifier.ValueText == ((IdentifierNameSyntax)replacementNode).Identifier.ValueText)
                    {
                        issueSpan = memberAccess.Expression.Span;
                    }

                    return true;
                }

                // Check if the Expression can be replaced by Predefined Type keyword
                if (PreferPredefinedTypeKeywordInMemberAccess(memberAccess, optionSet, semanticModel))
                {
                    if (symbol != null && symbol.IsKind(SymbolKind.NamedType))
                    {
                        var keywordKind = GetPredefinedKeywordKind(((INamedTypeSymbol)symbol).SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            replacementNode = CreatePredefinedTypeSyntax(memberAccess, keywordKind);

                            replacementNode = replacementNode
                                .WithAdditionalAnnotations(new SyntaxAnnotation(
                                    nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)));

                            issueSpan = memberAccess.Span; // we want to show the whole expression as unnecessary

                            return true;
                        }
                    }
                }
            }

            // Try to eliminate cases without actually calling CanReplaceWithReducedName. For expressions of the form
            // 'this.Name' or 'base.Name', no additional check here is required.
            if (!memberAccess.Expression.IsKind(SyntaxKind.ThisExpression, SyntaxKind.BaseExpression))
            {
                GetReplacementCandidates(
                    semanticModel,
                    memberAccess,
                    symbol,
                    out var speculativeSymbols,
                    out var speculativeNamespacesAndTypes);

                if (!IsReplacementCandidate(symbol, speculativeSymbols, speculativeNamespacesAndTypes))
                {
                    return false;
                }
            }

            replacementNode = memberAccess.GetNameWithTriviaMoved();
            issueSpan = memberAccess.Expression.Span;

            return CanReplaceWithReducedName(
                memberAccess, replacementNode, semanticModel, symbol, cancellationToken);
        }

        public static SimpleNameSyntax GetNameWithTriviaMoved(this MemberAccessExpressionSyntax memberAccess)
            => memberAccess.Name
                .WithLeadingTrivia(GetLeadingTriviaForSimplifiedMemberAccess(memberAccess))
                .WithTrailingTrivia(memberAccess.GetTrailingTrivia());

        private static void GetReplacementCandidates(
            SemanticModel semanticModel,
            MemberAccessExpressionSyntax memberAccess,
            ISymbol actualSymbol,
            out ImmutableArray<ISymbol> speculativeSymbols,
            out ImmutableArray<ISymbol> speculativeNamespacesAndTypes)
        {
            var containsNamespaceOrTypeSymbol = actualSymbol is INamespaceOrTypeSymbol;
            var containsOtherSymbol = !containsNamespaceOrTypeSymbol;

            speculativeSymbols = containsOtherSymbol
                ? semanticModel.LookupSymbols(memberAccess.SpanStart, name: memberAccess.Name.Identifier.ValueText)
                : ImmutableArray<ISymbol>.Empty;
            speculativeNamespacesAndTypes = containsNamespaceOrTypeSymbol
                ? semanticModel.LookupNamespacesAndTypes(memberAccess.SpanStart, name: memberAccess.Name.Identifier.ValueText)
                : ImmutableArray<ISymbol>.Empty;
        }

        /// <summary>
        /// Determines if <paramref name="speculativeSymbols"/> and <paramref name="speculativeNamespacesAndTypes"/>
        /// together contain a superset of the symbols in <paramref name="actualSymbol"/>.
        /// </summary>
        private static bool IsReplacementCandidate(ISymbol actualSymbol, ImmutableArray<ISymbol> speculativeSymbols, ImmutableArray<ISymbol> speculativeNamespacesAndTypes)
        {
            if (speculativeSymbols.IsEmpty && speculativeNamespacesAndTypes.IsEmpty)
            {
                return false;
            }

            if (actualSymbol is object)
            {
                return speculativeSymbols.Contains(actualSymbol, CandidateSymbolEqualityComparer.Instance)
                    || speculativeNamespacesAndTypes.Contains(actualSymbol, CandidateSymbolEqualityComparer.Instance);
            }

            return true;
        }

        /// <summary>
        /// Compares symbols by their original definition.
        /// </summary>
        private sealed class CandidateSymbolEqualityComparer : IEqualityComparer<ISymbol>
        {
            public static CandidateSymbolEqualityComparer Instance { get; } = new CandidateSymbolEqualityComparer();

            private CandidateSymbolEqualityComparer()
            {
            }

            public bool Equals(ISymbol x, ISymbol y)
            {
                if (x is null || y is null)
                {
                    return x == y;
                }

                return x.OriginalDefinition.Equals(y.OriginalDefinition);
            }

            public int GetHashCode(ISymbol obj)
            {
                return obj?.OriginalDefinition.GetHashCode() ?? 0;
            }
        }

        private static SyntaxTriviaList GetLeadingTriviaForSimplifiedMemberAccess(MemberAccessExpressionSyntax memberAccess)
        {
            // We want to include any user-typed trivia that may be present between the 'Expression', 'OperatorToken' and 'Identifier' of the MemberAccessExpression.
            // However, we don't want to include any elastic trivia that may have been introduced by the expander in these locations. This is to avoid triggering
            // aggressive formatting. Otherwise, formatter will see this elastic trivia added by the expander and use that as a cue to introduce unnecessary blank lines
            // etc. around the user's original code.
            return memberAccess.GetLeadingTrivia()
                .AddRange(memberAccess.Expression.GetTrailingTrivia().WithoutElasticTrivia())
                .AddRange(memberAccess.OperatorToken.LeadingTrivia.WithoutElasticTrivia())
                .AddRange(memberAccess.OperatorToken.TrailingTrivia.WithoutElasticTrivia())
                .AddRange(memberAccess.Name.GetLeadingTrivia().WithoutElasticTrivia());
        }

        private static IEnumerable<SyntaxTrivia> WithoutElasticTrivia(this IEnumerable<SyntaxTrivia> list)
        {
            return list.Where(t => !t.IsElastic());
        }

        public static bool InsideCrefReference(this ExpressionSyntax expression)
        {
            var crefAttribute = expression.FirstAncestorOrSelf<XmlCrefAttributeSyntax>();
            return crefAttribute != null;
        }

        private static bool InsideNameOfExpression(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var nameOfInvocationExpr = expression.FirstAncestorOrSelf<InvocationExpressionSyntax>(
                invocationExpr =>
                {
                    return (invocationExpr.Expression is IdentifierNameSyntax identifierName) && (identifierName.Identifier.Text == "nameof") &&
                        semanticModel.GetConstantValue(invocationExpr).HasValue &&
                        (semanticModel.GetTypeInfo(invocationExpr).Type.SpecialType == SpecialType.System_String);
                });

            return nameOfInvocationExpr != null;
        }

        private static bool PreferPredefinedTypeKeywordInDeclarations(NameSyntax name, OptionSet optionSet, SemanticModel semanticModel)
        {
            return !IsInMemberAccessContext(name) &&
                   !InsideCrefReference(name) &&
                   !InsideNameOfExpression(name, semanticModel) &&
                   SimplificationHelpers.PreferPredefinedTypeKeywordInDeclarations(optionSet, semanticModel.Language);
        }

        private static bool PreferPredefinedTypeKeywordInMemberAccess(ExpressionSyntax expression, OptionSet optionSet, SemanticModel semanticModel)
        {
            if (!SimplificationHelpers.PreferPredefinedTypeKeywordInMemberAccess(optionSet, semanticModel.Language))
                return false;

            return (IsInMemberAccessContext(expression) || InsideCrefReference(expression)) &&
                   !InsideNameOfExpression(expression, semanticModel);
        }

        public static bool IsInMemberAccessContext(this ExpressionSyntax expression) =>
            expression?.Parent is MemberAccessExpressionSyntax;

        private static bool IsAliasReplaceableExpression(ExpressionSyntax expression)
        {
            var current = expression;
            while (current.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax currentMember))
            {
                current = currentMember.Expression;
                continue;
            }

            return current.IsKind(SyntaxKind.AliasQualifiedName,
                                  SyntaxKind.IdentifierName,
                                  SyntaxKind.GenericName,
                                  SyntaxKind.QualifiedName);
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            Constraint = "Most trees do not have using alias directives, so avoid the expensive " + nameof(CSharpExtensions.GetSymbolInfo) + " call for this case.")]
        private static bool TryReplaceExpressionWithAlias(
            ExpressionSyntax node, SemanticModel semanticModel,
            ISymbol symbol, CancellationToken cancellationToken, out IAliasSymbol aliasReplacement)
        {
            aliasReplacement = null;

            if (!IsAliasReplaceableExpression(node))
                return false;

            // Avoid the TryReplaceWithAlias algorithm if the tree has no using alias directives. Since the input node
            // might be a speculative node (not fully rooted in a tree), we use the original semantic model to find the
            // equivalent node in the original tree, and from there determine if the tree has any using alias
            // directives.
            var originalModel = semanticModel.GetOriginalSemanticModel();

            // Perf: We are only using the syntax tree root in a fast-path syntax check. If the root is not readily
            // available, it is fine to continue through the normal algorithm.
            if (originalModel.SyntaxTree.TryGetRoot(out var root))
            {
                if (!HasUsingAliasDirective(root))
                {
                    return false;
                }
            }

            // If the Symbol is a constructor get its containing type
            if (symbol.IsConstructor())
            {
                symbol = symbol.ContainingType;
            }

            if (node is QualifiedNameSyntax || node is AliasQualifiedNameSyntax)
            {
                SyntaxAnnotation aliasAnnotationInfo = null;

                // The following condition checks if the user has used alias in the original code and
                // if so the expression is replaced with the Alias
                if (node is QualifiedNameSyntax qualifiedNameNode)
                {
                    if (qualifiedNameNode.Right.Identifier.HasAnnotations(AliasAnnotation.Kind))
                    {
                        aliasAnnotationInfo = qualifiedNameNode.Right.Identifier.GetAnnotations(AliasAnnotation.Kind).Single();
                    }
                }

                if (node is AliasQualifiedNameSyntax aliasQualifiedNameNode)
                {
                    if (aliasQualifiedNameNode.Name.Identifier.HasAnnotations(AliasAnnotation.Kind))
                    {
                        aliasAnnotationInfo = aliasQualifiedNameNode.Name.Identifier.GetAnnotations(AliasAnnotation.Kind).Single();
                    }
                }

                if (aliasAnnotationInfo != null)
                {
                    var aliasName = AliasAnnotation.GetAliasName(aliasAnnotationInfo);
                    var aliasIdentifier = SyntaxFactory.IdentifierName(aliasName);

                    var aliasTypeInfo = semanticModel.GetSpeculativeAliasInfo(node.SpanStart, aliasIdentifier, SpeculativeBindingOption.BindAsTypeOrNamespace);

                    if (aliasTypeInfo != null)
                    {
                        aliasReplacement = aliasTypeInfo;
                        return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol);
                    }
                }
            }

            if (node.Kind() == SyntaxKind.IdentifierName &&
                semanticModel.GetAliasInfo((IdentifierNameSyntax)node, cancellationToken) != null)
            {
                return false;
            }

            // an alias can only replace a type or namespace
            if (symbol == null ||
                (symbol.Kind != SymbolKind.Namespace && symbol.Kind != SymbolKind.NamedType))
            {
                return false;
            }

            var preferAliasToQualifiedName = true;
            if (node is QualifiedNameSyntax qualifiedName)
            {
                if (!qualifiedName.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                {
                    var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
                    if (type != null)
                    {
                        var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            preferAliasToQualifiedName = false;
                        }
                    }
                }
            }

            if (node is AliasQualifiedNameSyntax aliasQualifiedNameSyntax)
            {
                if (!aliasQualifiedNameSyntax.Name.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                {
                    var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
                    if (type != null)
                    {
                        var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            preferAliasToQualifiedName = false;
                        }
                    }
                }
            }

            aliasReplacement = GetAliasForSymbol((INamespaceOrTypeSymbol)symbol, node.GetFirstToken(), semanticModel, cancellationToken);
            if (aliasReplacement != null && preferAliasToQualifiedName)
            {
                return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol);
            }

            return false;
        }

        private static bool HasUsingAliasDirective(SyntaxNode syntax)
        {
            SyntaxList<UsingDirectiveSyntax> usings;
            SyntaxList<MemberDeclarationSyntax> members;
            if (syntax.IsKind(SyntaxKind.NamespaceDeclaration, out NamespaceDeclarationSyntax namespaceDeclaration))
            {
                usings = namespaceDeclaration.Usings;
                members = namespaceDeclaration.Members;
            }
            else if (syntax.IsKind(SyntaxKind.CompilationUnit, out CompilationUnitSyntax compilationUnit))
            {
                usings = compilationUnit.Usings;
                members = compilationUnit.Members;
            }
            else
            {
                return false;
            }

            foreach (var usingDirective in usings)
            {
                if (usingDirective.Alias != null)
                {
                    return true;
                }
            }

            foreach (var member in members)
            {
                if (HasUsingAliasDirective(member))
                {
                    return true;
                }
            }

            return false;
        }

        // We must verify that the alias actually binds back to the thing it's aliasing.
        // It's possible there's another symbol with the same name as the alias that binds
        // first
        private static bool ValidateAliasForTarget(IAliasSymbol aliasReplacement, SemanticModel semanticModel, ExpressionSyntax node, ISymbol symbol)
        {
            var aliasName = aliasReplacement.Name;

            // If we're the argument of a nameof(X.Y) call, then we can't simplify to an
            // alias unless the alias has the same name as us (i.e. 'Y').
            if (node.IsNameOfArgumentExpression())
            {
                var nameofValueOpt = semanticModel.GetConstantValue(node.Parent.Parent.Parent);
                if (!nameofValueOpt.HasValue)
                {
                    return false;
                }

                if (nameofValueOpt.Value is string existingVal &&
                    existingVal != aliasName)
                {
                    return false;
                }
            }

            var boundSymbols = semanticModel.LookupNamespacesAndTypes(node.SpanStart, name: aliasName);

            if (boundSymbols.Length == 1)
            {
                if (boundSymbols[0] is IAliasSymbol boundAlias && aliasReplacement.Target.Equals(symbol))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
