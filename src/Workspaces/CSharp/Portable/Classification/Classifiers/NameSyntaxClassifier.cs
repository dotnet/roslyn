// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class NameSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override IEnumerable<ClassifiedSpan> ClassifyNode(
            SyntaxNode syntax,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var name = syntax as NameSyntax;
            if (name != null)
            {
                return ClassifyTypeSyntax(name, semanticModel, cancellationToken);
            }

            return null;
        }

        public override IEnumerable<Type> SyntaxNodeTypes
        {
            get
            {
                yield return typeof(NameSyntax);
            }
        }

        private IEnumerable<ClassifiedSpan> ClassifyTypeSyntax(
            NameSyntax name,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (!IsNamespaceName(name))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);

                IEnumerable<ClassifiedSpan> result;
                if (TryClassifySymbol(name, symbolInfo, semanticModel, cancellationToken, out result) ||
                    TryClassifyFromIdentifier(name, symbolInfo, out result) ||
                    TryClassifyValueIdentifier(name, symbolInfo, out result) ||
                    TryClassifyNameOfIdentifier(name, symbolInfo, out result))
                {
                    return result;
                }
            }

            return null;
        }

        private static bool IsNamespaceName(NameSyntax name)
        {
            while (name.Parent is NameSyntax)
            {
                name = (NameSyntax)name.Parent;
            }

            return name.IsParentKind(SyntaxKind.NamespaceDeclaration);
        }

        private bool TryClassifySymbol(
            NameSyntax name,
            SymbolInfo symbolInfo,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IEnumerable<ClassifiedSpan> result)
        {
            if (symbolInfo.CandidateReason == CandidateReason.Ambiguous)
            {
                return TryClassifyAmbiguousSymbol(name, symbolInfo, semanticModel, cancellationToken, out result);
            }

            // Only classify if we get one good symbol back, or if it bound to a constructor symbol with
            // overload resolution/accessibility errors, or bound to type/constructor and type wasn't creatable.
            var symbol = TryGetSymbol(name, symbolInfo, semanticModel);

            ClassifiedSpan classifiedSpan;
            if (TryClassifySymbol(name, symbol, semanticModel, cancellationToken, out classifiedSpan))
            {
                result = SpecializedCollections.SingletonEnumerable(classifiedSpan);
                return true;
            }

            result = null;
            return false;
        }

        private bool TryClassifyAmbiguousSymbol(
            NameSyntax name,
            SymbolInfo symbolInfo,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IEnumerable<ClassifiedSpan> result)
        {
            // If everything classifies the same way, then just pick that classification.
            var set = new HashSet<ClassifiedSpan>();
            foreach (var symbol in symbolInfo.CandidateSymbols)
            {
                ClassifiedSpan classifiedSpan;
                if (TryClassifySymbol(name, symbol, semanticModel, cancellationToken, out classifiedSpan))
                {
                    set.Add(classifiedSpan);
                }
            }

            if (set.Count == 1)
            {
                result = SpecializedCollections.SingletonEnumerable(set.First());
                return true;
            }

            result = null;
            return false;
        }

        private bool TryClassifySymbol(
            NameSyntax name,
            ISymbol symbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ClassifiedSpan classifiedSpan)
        {
            if (symbol != null)
            {
                // see through using aliases
                if (symbol.Kind == SymbolKind.Alias)
                {
                    symbol = (symbol as IAliasSymbol).Target;
                }
                else if (symbol.IsConstructor() && name.IsParentKind(SyntaxKind.Attribute))
                {
                    symbol = symbol.ContainingType;
                }
            }

            if (name.IsVar &&
                IsInVarContext(name))
            {
                var alias = semanticModel.GetAliasInfo(name, cancellationToken);
                if (alias == null || alias.Name != "var")
                {
                    if (!IsSymbolCalledVar(symbol))
                    {
                        // We bound to a symbol.  If we bound to a symbol called "var" then we want to
                        // classify this appropriately as a type.  Otherwise, we want to classify this as
                        // a keyword.
                        classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.Keyword);
                        return true;
                    }
                }
            }

            if (symbol != null)
            {
                // Use .Equals since we can't rely on object identity for constructed types.
                if (symbol is ITypeSymbol)
                {
                    var classification = GetClassificationForType((ITypeSymbol)symbol);
                    if (classification != null)
                    {
                        var token = name.GetNameToken();
                        classifiedSpan = new ClassifiedSpan(token.Span, classification);
                        return true;
                    }
                }
            }

            classifiedSpan = default(ClassifiedSpan);
            return false;
        }

        private bool IsInVarContext(NameSyntax name)
        {
            return
                name.CheckParent<VariableDeclarationSyntax>(v => v.Type == name) ||
                name.CheckParent<ForEachStatementSyntax>(f => f.Type == name) ||
                name.CheckParent<TypedVariableComponentSyntax>(f => f.Type == name);
        }

        private static ISymbol TryGetSymbol(NameSyntax name, SymbolInfo symbolInfo, SemanticModel semanticModel)
        {
            if (symbolInfo.Symbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                var firstSymbol = symbolInfo.CandidateSymbols[0];

                switch (symbolInfo.CandidateReason)
                {
                    case CandidateReason.NotAValue:
                        return firstSymbol;

                    case CandidateReason.NotCreatable:
                        // We want to color types even if they can't be constructed.
                        if (firstSymbol.IsConstructor() || firstSymbol is ITypeSymbol)
                        {
                            return firstSymbol;
                        }

                        break;

                    case CandidateReason.OverloadResolutionFailure:
                        // If we couldn't bind to a constructor, still classify the type.
                        if (firstSymbol.IsConstructor())
                        {
                            return firstSymbol;
                        }

                        break;

                    case CandidateReason.Inaccessible:
                        // If a constructor wasn't accessible, still classify the type if it's accessible.
                        if (firstSymbol.IsConstructor() && semanticModel.IsAccessible(name.SpanStart, firstSymbol.ContainingType))
                        {
                            return firstSymbol;
                        }

                        break;
                }
            }

            return symbolInfo.Symbol;
        }

        private bool TryClassifyFromIdentifier(
            NameSyntax name,
            SymbolInfo symbolInfo,
            out IEnumerable<ClassifiedSpan> result)
        {
            // Okay - it wasn't a type. If the syntax matches "var q = from" or "q = from", and from
            // doesn't bind to anything then optimistically color from as a keyword.
            var identifierName = name as IdentifierNameSyntax;
            if (identifierName != null &&
                identifierName.Identifier.HasMatchingText(SyntaxKind.FromKeyword) &&
                symbolInfo.Symbol == null)
            {
                var token = identifierName.Identifier;
                if (identifierName.IsRightSideOfAnyAssignExpression() || identifierName.IsVariableDeclaratorValue())
                {
                    result = SpecializedCollections.SingletonEnumerable(
                        new ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword));
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool TryClassifyValueIdentifier(
            NameSyntax name,
            SymbolInfo symbolInfo,
            out IEnumerable<ClassifiedSpan> result)
        {
            var identifierName = name as IdentifierNameSyntax;
            if (symbolInfo.Symbol.IsValueParameter())
            {
                result = SpecializedCollections.SingletonEnumerable(
                    new ClassifiedSpan(identifierName.Identifier.Span, ClassificationTypeNames.Keyword));
                return true;
            }

            result = null;
            return false;
        }

        private bool TryClassifyNameOfIdentifier(NameSyntax name, SymbolInfo symbolInfo, out IEnumerable<ClassifiedSpan> result)
        {
            var identifierName = name as IdentifierNameSyntax;
            if (identifierName != null &&
                identifierName.Identifier.IsKindOrHasMatchingText(SyntaxKind.NameOfKeyword) &&
                symbolInfo.Symbol == null &&
                !symbolInfo.CandidateSymbols.Any())
            {
                result = SpecializedCollections.SingletonEnumerable(new ClassifiedSpan(identifierName.Identifier.Span, ClassificationTypeNames.Keyword));
                return true;
            }

            result = null;
            return false;
        }

        private bool IsSymbolCalledVar(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol)
            {
                return ((INamedTypeSymbol)symbol).Arity == 0 && symbol.Name == "var";
            }

            return symbol != null && symbol.Name == "var";
        }
    }
}
