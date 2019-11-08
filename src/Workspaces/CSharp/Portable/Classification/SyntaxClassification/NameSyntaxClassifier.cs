// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class NameSyntaxClassifier : AbstractNameSyntaxClassifier
    {
        public override void AddClassifications(
            Workspace workspace,
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

        protected override int? GetRightmostNameArity(SyntaxNode node)
        {
            if (node is ExpressionSyntax expressionSyntax)
            {
                return expressionSyntax.GetRightmostName()?.Arity;
            }

            return null;
        }

        protected override bool IsParentAnAttribute(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.Attribute);
        }

        private void ClassifyTypeSyntax(
            NameSyntax name,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);

            var _ =
                TryClassifySymbol(name, symbolInfo, semanticModel, result, cancellationToken) ||
                TryClassifyFromIdentifier(name, symbolInfo, result) ||
                TryClassifyValueIdentifier(name, symbolInfo, result) ||
                TryClassifyNameOfIdentifier(name, symbolInfo, result);
        }

        private bool TryClassifySymbol(
            NameSyntax name,
            SymbolInfo symbolInfo,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (symbolInfo.CandidateReason == CandidateReason.Ambiguous ||
                symbolInfo.CandidateReason == CandidateReason.MemberGroup)
            {
                return TryClassifyAmbiguousSymbol(name, symbolInfo, semanticModel, result, cancellationToken);
            }

            // Only classify if we get one good symbol back, or if it bound to a constructor symbol with
            // overload resolution/accessibility errors, or bound to type/constructor and type wasn't creatable.
            var symbol = TryGetSymbol(name, symbolInfo, semanticModel);
            if (TryClassifySymbol(name, symbol, semanticModel, cancellationToken, out var classifiedSpan))
            {
                result.Add(classifiedSpan);

                if (classifiedSpan.ClassificationType != ClassificationTypeNames.Keyword)
                {
                    // Additionally classify static symbols
                    TryClassifyStaticSymbol(symbol, classifiedSpan.TextSpan, result);
                }

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
            [NotNullWhen(returnValue: true)] ISymbol? symbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ClassifiedSpan classifiedSpan)
        {
            // For Namespace parts, we want don't want to classify the QualifiedNameSyntax
            // nodes, we instead wait for the each IdentifierNameSyntax node to avoid
            // creating overlapping ClassifiedSpans.
            if (symbol is INamespaceSymbol namespaceSymbol &&
                name is IdentifierNameSyntax identifierNameSyntax)
            {
                // Do not classify the global:: namespace. It is already syntactically classified as a keyword.
                var isGlobalNamespace = namespaceSymbol.IsGlobalNamespace &&
                    identifierNameSyntax.Identifier.IsKind(SyntaxKind.GlobalKeyword);
                if (isGlobalNamespace)
                {
                    classifiedSpan = default;
                    return false;
                }

                // Classifies both extern aliases and namespaces.
                classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.NamespaceName);
                return true;
            }

            if (name.IsVar &&
                IsInVarContext(name))
            {
                var alias = semanticModel.GetAliasInfo(name, cancellationToken);
                if (alias == null || alias.Name != "var")
                {
                    if (!IsSymbolWithName(symbol, "var"))
                    {
                        // We bound to a symbol.  If we bound to a symbol called "var" then we want to
                        // classify this appropriately as a type.  Otherwise, we want to classify this as
                        // a keyword.
                        classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.Keyword);
                        return true;
                    }
                }
            }

            if ((name.IsUnmanaged || name.IsNotNull) && name.Parent.IsKind(SyntaxKind.TypeConstraint))
            {
                var nameToCheck = name.IsUnmanaged ? "unmanaged" : "notnull";
                var alias = semanticModel.GetAliasInfo(name, cancellationToken);
                if (alias == null || alias.Name != nameToCheck)
                {
                    if (!IsSymbolWithName(symbol, nameToCheck))
                    {
                        // We bound to a symbol.  If we bound to a symbol called "unmanaged"/"notnull" then we want to
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
                    if (parameterSymbol is { IsImplicitlyDeclared: true, Name: "value" })
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
                case ILabelSymbol labelSymbol:
                    token = name.GetNameToken();
                    classifiedSpan = new ClassifiedSpan(token.Span, ClassificationTypeNames.LabelName);
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
            // Classify constructors by their containing type. We do not need to worry about
            // destructors because their declaration is handled by syntactic classification
            // and they cannot be invoked, so their is no usage to semantically classify.
            if (methodSymbol.MethodKind == MethodKind.Constructor)
            {
                return methodSymbol.ContainingType?.GetClassification() ?? ClassificationTypeNames.MethodName;
            }

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
#nullable disable // Can 'identifierName' be null here?
                result.Add(new ClassifiedSpan(identifierName.Identifier.Span, ClassificationTypeNames.Keyword));
#nullable enable
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

        private bool IsSymbolWithName([NotNullWhen(returnValue: true)] ISymbol? symbol, string name)
        {
            if (symbol is null || symbol.Name != name)
            {
                return false;
            }

            if (symbol is INamedTypeSymbol namedType)
            {
                return namedType.Arity == 0;
            }

            return true;
        }
    }
}
