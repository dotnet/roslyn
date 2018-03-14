// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class NameSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override void AddClassifications(
            SyntaxNode syntax,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (syntax is NameSyntax name)
            {
                ClassifyTypeSyntax(name, semanticModel, result, cancellationToken);
            }
        }

        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(typeof(NameSyntax));

        private void ClassifyTypeSyntax(
            NameSyntax name,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (!IsNamespaceName(name))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);

                var _ =
                    TryClassifySymbol(name, symbolInfo, semanticModel, result, cancellationToken) ||
                    TryClassifyFromIdentifier(name, symbolInfo, result) ||
                    TryClassifyValueIdentifier(name, symbolInfo, result) ||
                    TryClassifyNameOfIdentifier(name, symbolInfo, result);
            }
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
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (symbolInfo.CandidateReason == CandidateReason.Ambiguous)
            {
                return TryClassifyAmbiguousSymbol(name, symbolInfo, semanticModel, result, cancellationToken);
            }

            // Only classify if we get one good symbol back, or if it bound to a constructor symbol with
            // overload resolution/accessibility errors, or bound to type/constructor and type wasn't creatable.
            var symbol = TryGetSymbol(name, symbolInfo, semanticModel);
            if (TryClassifySymbol(name, symbol, semanticModel, cancellationToken, out var classifiedSpan))
            {
                result.Add(classifiedSpan);
                return true;
            }

            return false;
        }

        private bool TryClassifyAmbiguousSymbol(
            NameSyntax name,
            SymbolInfo symbolInfo,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            // If everything classifies the same way, then just pick that classification.
            var set = PooledHashSet<ClassifiedSpan>.GetInstance();
            try
            {
                foreach (var symbol in symbolInfo.CandidateSymbols)
                {
                    if (TryClassifySymbol(name, symbol, semanticModel, cancellationToken, out var classifiedSpan))
                    {
                        set.Add(classifiedSpan);
                    }
                }

                if (set.Count == 1)
                {
                    result.Add(set.First());
                    return true;
                }

                return false;
            }
            finally
            {
                set.Free();
            }
        }

        private bool TryClassifySymbol(
            NameSyntax name,
            ISymbol symbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ClassifiedSpan classifiedSpan)
        {
            // Classify a reference to an attribute constructor in an attribute location
            // as if we were classifying the attribute type itself.
            if (symbol.IsConstructor() && name.IsParentKind(SyntaxKind.Attribute))
            {
                symbol = symbol.ContainingType;
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

            // Use .Equals since we can't rely on object identity for constructed types.
            SyntaxToken token;
            switch (symbol)
            {
                case ITypeSymbol typeSymbol:
                    var classification = GetClassificationForType(typeSymbol);
                    if (classification != null)
                    {
                        token = name.GetNameToken();
                        classifiedSpan = new ClassifiedSpan(token.Span, classification);
                        return true;
                    }
                    break;

                case IFieldSymbol fieldSymbol:
                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, GetClassificationForField(fieldSymbol));
                    return true;
                case IMethodSymbol methodSymbol:
                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, GetClassificationForMethod(methodSymbol));
                    return true;
                case IPropertySymbol propertySymbol:
                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, ClassificationTypeNames.PropertyName);
                    return true;
                case IEventSymbol eventSymbol:
                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, ClassificationTypeNames.EventName);
                    return true;
                case IParameterSymbol parameterSymbol:
                    if (parameterSymbol.IsImplicitlyDeclared && parameterSymbol.Name == "value")
                    {
                        break;
                    }

                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, ClassificationTypeNames.ParameterName);
                    return true;
                case ILocalSymbol localSymbol:
                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, GetClassificationForLocal(localSymbol));
                    return true;
            }

            classifiedSpan = default;
            return false;
        }

        private static string GetClassificationForField(IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsConst)
            {
                return fieldSymbol.ContainingType.IsEnumType() ? ClassificationTypeNames.EnumMemberName : ClassificationTypeNames.ConstantName;
            }

            return ClassificationTypeNames.FieldName;
        }

        private static string GetClassificationForLocal(ILocalSymbol localSymbol)
        {
            return localSymbol.IsConst
                ? ClassificationTypeNames.ConstantName
                : ClassificationTypeNames.LocalName;
        }

        private static string GetClassificationForMethod(IMethodSymbol methodSymbol)
        {
            // Note: We only classify an extension method if it is in reduced form.
            // If an extension method is called as a static method invocation (e.g. Enumerable.Select(...)),
            // it is classified as an ordinary method.
            return methodSymbol.MethodKind == MethodKind.ReducedExtension
                ? ClassificationTypeNames.ExtensionMethodName
                : ClassificationTypeNames.MethodName;
        }

        private bool IsInVarContext(NameSyntax name)
        {
            return
                name.CheckParent<RefTypeSyntax>(v => v.Type == name) ||
                name.CheckParent<ForEachStatementSyntax>(f => f.Type == name) ||
                name.CheckParent<DeclarationPatternSyntax>(v => v.Type == name) ||
                name.CheckParent<VariableDeclarationSyntax>(v => v.Type == name) ||
                name.CheckParent<DeclarationExpressionSyntax>(f => f.Type == name);
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

                    case CandidateReason.WrongArity:
                        if (name.GetRightmostName()?.Arity == 0)
                        {
                            // When the user writes something like "IList" we don't want to *not* classify 
                            // just because the type bound to "IList<T>".  This is also important for use
                            // cases like "Add-using" where it can be confusing when the using is added for
                            // "using System.Collection.Generic" but then the type name still does not classify.
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
            ArrayBuilder<ClassifiedSpan> result)
        {
            // Okay - it wasn't a type. If the syntax matches "var q = from" or "q = from", and from
            // doesn't bind to anything then optimistically color from as a keyword.
            if (name is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.HasMatchingText(SyntaxKind.FromKeyword) &&
                symbolInfo.Symbol == null)
            {
                var token = identifierName.Identifier;
                if (identifierName.IsRightSideOfAnyAssignExpression() || identifierName.IsVariableDeclaratorValue())
                {
                    result.Add(new ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword));
                    return true;
                }
            }

            return false;
        }

        private bool TryClassifyValueIdentifier(
            NameSyntax name,
            SymbolInfo symbolInfo,
            ArrayBuilder<ClassifiedSpan> result)
        {
            var identifierName = name as IdentifierNameSyntax;
            if (symbolInfo.Symbol.IsImplicitValueParameter())
            {
                result.Add(new ClassifiedSpan(identifierName.Identifier.Span, ClassificationTypeNames.Keyword));
                return true;
            }

            return false;
        }

        private bool TryClassifyNameOfIdentifier(
            NameSyntax name, SymbolInfo symbolInfo, ArrayBuilder<ClassifiedSpan> result)
        {
            if (name is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.IsKindOrHasMatchingText(SyntaxKind.NameOfKeyword) &&
                symbolInfo.Symbol == null &&
                !symbolInfo.CandidateSymbols.Any())
            {
                result.Add(new ClassifiedSpan(identifierName.Identifier.Span, ClassificationTypeNames.Keyword));
                return true;
            }

            return false;
        }

        private bool IsSymbolCalledVar(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return namedType.Arity == 0 && symbol.Name == "var";
            }

            return symbol != null && symbol.Name == "var";
        }
    }
}
