// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;

internal sealed class NameSyntaxClassifier : AbstractNameSyntaxClassifier
{
    public override void AddClassifications(
        SyntaxNode syntax,
        TextSpan textSpan,
        SemanticModel semanticModel,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        if (syntax is SimpleNameSyntax name)
            ClassifyTypeSyntax(name, semanticModel, result, cancellationToken);
    }

    public override ImmutableArray<Type> SyntaxNodeTypes { get; } =
    [
        typeof(IdentifierNameSyntax),
        typeof(GenericNameSyntax),
    ];

    protected override bool IsParentAnAttribute(SyntaxNode node)
        => node.IsParentKind(SyntaxKind.Attribute);

    private void ClassifyTypeSyntax(
        SimpleNameSyntax name,
        SemanticModel semanticModel,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);

        var _ =
            TryClassifySymbol(name, symbolInfo, result) ||
            TryClassifyFromIdentifier(name, symbolInfo, result) ||
            TryClassifyValueIdentifier(name, symbolInfo, result) ||
            TryClassifySomeContextualKeywordIdentifiersAsKeywords(name, symbolInfo, result);
    }

    private bool TryClassifySymbol(
        SimpleNameSyntax name,
        SymbolInfo symbolInfo,
        SegmentedList<ClassifiedSpan> result)
    {
        if (symbolInfo.CandidateReason is
            CandidateReason.Ambiguous or
            CandidateReason.MemberGroup)
        {
            return TryClassifyAmbiguousSymbol(name, symbolInfo, result);
        }

        // Only classify if we get one good symbol back, or if it bound to a constructor symbol with
        // overload resolution/accessibility errors, or bound to type/constructor and type wasn't creatable.
        var symbol = TryGetSymbol(name, symbolInfo);
        if (TryClassifySymbol(name, symbol, out var classifiedSpan))
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

    private static bool TryClassifyAmbiguousSymbol(
        SimpleNameSyntax name,
        SymbolInfo symbolInfo,
        SegmentedList<ClassifiedSpan> result)
    {
        // If everything classifies the same way, then just pick that classification.
        using var _ = PooledHashSet<ClassifiedSpan>.GetInstance(out var set);
        var isStatic = false;

        foreach (var symbol in symbolInfo.CandidateSymbols)
        {
            if (TryClassifySymbol(name, symbol, out var classifiedSpan))
            {
                // If one symbol resolves to static, then just make it bold
                isStatic = isStatic || IsStaticSymbol(symbol);
                set.Add(classifiedSpan);
            }
        }

        if (set.Count == 1)
        {
            // If any of the symbols are static, add the static classification and the regular symbol classification
            if (isStatic)
            {
                result.Add(new ClassifiedSpan(set.First().TextSpan, ClassificationTypeNames.StaticSymbol));
            }

            result.Add(set.First());
            return true;
        }

        return false;
    }

    private static bool TryClassifySymbol(
        SimpleNameSyntax name,
        [NotNullWhen(returnValue: true)] ISymbol? symbol,
        out ClassifiedSpan classifiedSpan)
    {
        // For Namespace parts, we want don't want to classify the QualifiedNameSyntax
        // nodes, we instead wait for the each IdentifierNameSyntax node to avoid
        // creating overlapping ClassifiedSpans.
        if (symbol is INamespaceSymbol namespaceSymbol &&
            name is IdentifierNameSyntax)
        {
            // Do not classify the global:: namespace. It is already syntactically classified as a keyword.
            var isGlobalNamespace = namespaceSymbol.IsGlobalNamespace &&
                name.Identifier.IsKind(SyntaxKind.GlobalKeyword);
            if (isGlobalNamespace)
            {
                classifiedSpan = default;
                return false;
            }

            // Classifies both extern aliases and namespaces.
            classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.NamespaceName);
            return true;
        }

        if (name.IsVar && IsInVarContext(name))
        {
            // Don't do anything further to check if `var` is the contextual keyword here. We do not support code
            // squatting on typenames that are all lowercase.
            classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.Keyword);
            return true;
        }

        if (name is IdentifierNameSyntax { Identifier.Text: "args" } &&
            symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName } })
        {
            classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.Keyword);
            return true;
        }

        if (name.IsNint || name.IsNuint)
        {
            if (symbol is ITypeSymbol type && type.IsNativeIntegerType)
            {
                classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.Keyword);
                return true;
            }
        }

        if ((name.IsUnmanaged || name.IsNotNull) && name.Parent.IsKind(SyntaxKind.TypeConstraint))
        {
            classifiedSpan = new ClassifiedSpan(name.Span, ClassificationTypeNames.Keyword);
            return true;
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
            case IPropertySymbol:
                token = name.GetNameToken();
                classifiedSpan = new ClassifiedSpan(token.Span, ClassificationTypeNames.PropertyName);
                return true;
            case IEventSymbol:
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
            case ILabelSymbol:
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
            return methodSymbol.ContainingType?.GetClassification() ?? ClassificationTypeNames.MethodName;

        // Note: We only classify an extension method if it is in reduced form.
        // If an extension method is called as a static method invocation (e.g. Enumerable.Select(...)),
        // it is classified as an ordinary method.
        if (methodSymbol.MethodKind == MethodKind.ReducedExtension)
            return ClassificationTypeNames.ExtensionMethodName;

        // If calling an extension method through the extension container (not the static class container) then it's
        // definitely an extension method.
        if (methodSymbol.ContainingType.IsExtension)
            return ClassificationTypeNames.ExtensionMethodName;

        return ClassificationTypeNames.MethodName;
    }

    private static bool IsInVarContext(SimpleNameSyntax name)
    {
        return
            name.CheckParent<RefTypeSyntax>(v => v.Type == name) ||
            name.CheckParent<ScopedTypeSyntax>(v => v.Type == name) ||
            name.CheckParent<ForEachStatementSyntax>(f => f.Type == name) ||
            name.CheckParent<DeclarationPatternSyntax>(v => v.Type == name) ||
            name.CheckParent<VariableDeclarationSyntax>(v => v.Type == name) ||
            name.CheckParent<DeclarationExpressionSyntax>(f => f.Type == name);
    }

    private static bool TryClassifyFromIdentifier(
        SimpleNameSyntax name,
        SymbolInfo symbolInfo,
        SegmentedList<ClassifiedSpan> result)
    {
        // Okay - it wasn't a type. If the syntax matches "var q = from" or "q = from", and from
        // doesn't bind to anything then optimistically color from as a keyword.
        if (name is IdentifierNameSyntax &&
            name.Identifier.HasMatchingText(SyntaxKind.FromKeyword) &&
            symbolInfo.Symbol == null)
        {
            var token = name.Identifier;
            if (name.IsRightSideOfAnyAssignExpression() || name.IsVariableDeclaratorValue())
            {
                result.Add(new ClassifiedSpan(token.Span, ClassificationTypeNames.Keyword));
                return true;
            }
        }

        return false;
    }

    private static bool TryClassifyValueIdentifier(
        SimpleNameSyntax name,
        SymbolInfo symbolInfo,
        SegmentedList<ClassifiedSpan> result)
    {
        if (symbolInfo.Symbol.IsImplicitValueParameter())
        {
            result.Add(new ClassifiedSpan(name.Identifier.Span, ClassificationTypeNames.Keyword));
            return true;
        }

        return false;
    }

    private static bool TryClassifySomeContextualKeywordIdentifiersAsKeywords(SimpleNameSyntax name, SymbolInfo symbolInfo, SegmentedList<ClassifiedSpan> result)
    {
        // Simple approach, if the user ever types one of identifiers from the list and it doesn't actually bind to anything, presume that
        // they intend to use it as a keyword. This works for all error
        // cases, while not conflicting with the extremely rare case where such identifiers might actually be used to
        // reference actual symbols with that names.
        if (symbolInfo.GetAnySymbol() is null &&
            name is IdentifierNameSyntax { Identifier.Text: "async" or "nameof" or "partial" })
        {
            result.Add(new(name.Span, ClassificationTypeNames.Keyword));
            return true;
        }

        return false;
    }
}
