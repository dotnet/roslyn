// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Provides information about the way a particular symbol is being used at a symbol reference node.
/// For namespaces and types, this corresponds to values from <see cref="TypeOrNamespaceUsageInfo"/>.
/// For methods, fields, properties, events, locals and parameters, this corresponds to values from <see cref="ValueUsageInfo"/>.
/// </summary>
[DataContract]
internal readonly record struct SymbolUsageInfo
{
    public static readonly SymbolUsageInfo None = Create(ValueUsageInfo.None);

    [DataMember(Order = 0)]
    public ValueUsageInfo? ValueUsageInfoOpt { get; }

    [DataMember(Order = 1)]
    public TypeOrNamespaceUsageInfo? TypeOrNamespaceUsageInfoOpt { get; }

    // Must be public since it's used for deserialization.
    public SymbolUsageInfo(ValueUsageInfo? valueUsageInfoOpt, TypeOrNamespaceUsageInfo? typeOrNamespaceUsageInfoOpt)
    {
        Debug.Assert(valueUsageInfoOpt.HasValue ^ typeOrNamespaceUsageInfoOpt.HasValue);

        ValueUsageInfoOpt = valueUsageInfoOpt;
        TypeOrNamespaceUsageInfoOpt = typeOrNamespaceUsageInfoOpt;
    }

    public static SymbolUsageInfo Create(ValueUsageInfo valueUsageInfo)
        => new(valueUsageInfo, typeOrNamespaceUsageInfoOpt: null);

    public static SymbolUsageInfo Create(TypeOrNamespaceUsageInfo typeOrNamespaceUsageInfo)
        => new(valueUsageInfoOpt: null, typeOrNamespaceUsageInfo);

    public bool IsReadFrom()
        => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsReadFrom();

    public bool IsWrittenTo()
        => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsWrittenTo();

    public static SymbolUsageInfo GetSymbolUsageInfo(
        ISemanticFacts semanticFacts,
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = semanticFacts.SyntaxFacts;

        var topNameNode = node;
        while (syntaxFacts.IsQualifiedName(topNameNode.Parent))
            topNameNode = topNameNode.Parent;

        var parent = topNameNode?.Parent;

        // typeof/sizeof are a special case where we don't want to return a TypeOrNamespaceUsageInfo, but rather a ValueUsageInfo.Name.
        // This brings it in line with nameof(...), making all those operators appear in a similar fashion.
        if (parent?.RawKind == syntaxFacts.SyntaxKinds.TypeOfExpression ||
            parent?.RawKind == syntaxFacts.SyntaxKinds.SizeOfExpression)
        {
            return new(ValueUsageInfo.Name, typeOrNamespaceUsageInfoOpt: null);
        }

        var isInNamespaceNameContext = syntaxFacts.IsBaseNamespaceDeclaration(parent);
        return syntaxFacts.IsInNamespaceOrTypeContext(topNameNode)
            ? Create(GetTypeOrNamespaceUsageInfo())
            : GetSymbolUsageInfoCommon();

        // Local functions.
        TypeOrNamespaceUsageInfo GetTypeOrNamespaceUsageInfo()
        {
            var usageInfo = IsNodeOrAnyAncestorLeftSideOfDot(node, syntaxFacts) || syntaxFacts.IsLeftSideOfExplicitInterfaceSpecifier(node)
                ? TypeOrNamespaceUsageInfo.Qualified
                : TypeOrNamespaceUsageInfo.None;

            if (isInNamespaceNameContext)
            {
                usageInfo |= TypeOrNamespaceUsageInfo.NamespaceDeclaration;
            }
            else if (node.FirstAncestorOrSelf<SyntaxNode, ISyntaxFacts>((node, syntaxFacts) => syntaxFacts.IsUsingOrExternOrImport(node), syntaxFacts) != null)
            {
                usageInfo |= TypeOrNamespaceUsageInfo.Import;
            }

            while (syntaxFacts.IsQualifiedName(node.Parent))
                node = node.Parent;

            if (syntaxFacts.IsTypeArgumentList(node.Parent))
            {
                usageInfo |= TypeOrNamespaceUsageInfo.TypeArgument;
            }
            else if (syntaxFacts.IsTypeConstraint(node.Parent))
            {
                usageInfo |= TypeOrNamespaceUsageInfo.TypeConstraint;
            }
            else if (syntaxFacts.IsBaseTypeList(node.Parent) ||
                syntaxFacts.IsBaseTypeList(node.Parent?.Parent))
            {
                usageInfo |= TypeOrNamespaceUsageInfo.Base;
            }
            else if (syntaxFacts.IsTypeOfObjectCreationExpression(node))
            {
                usageInfo |= TypeOrNamespaceUsageInfo.ObjectCreation;
            }

            return usageInfo;
        }

        SymbolUsageInfo GetSymbolUsageInfoCommon()
        {
            if (semanticFacts.IsInOutContext(semanticModel, node, cancellationToken))
            {
                return Create(ValueUsageInfo.WritableReference);
            }
            else if (semanticFacts.IsInRefContext(semanticModel, node, cancellationToken))
            {
                return Create(ValueUsageInfo.ReadableWritableReference);
            }
            else if (semanticFacts.IsInInContext(semanticModel, node, cancellationToken))
            {
                return Create(ValueUsageInfo.ReadableReference);
            }
            else if (semanticFacts.IsOnlyWrittenTo(semanticModel, node, cancellationToken))
            {
                return Create(ValueUsageInfo.Write);
            }
            else
            {
                var operation = semanticModel.GetOperation(node, cancellationToken);
                if (operation is IObjectCreationOperation)
                    return Create(TypeOrNamespaceUsageInfo.ObjectCreation);

                // Note: sizeof/typeof also return 'name', but are handled above in GetSymbolUsageInfo.
                if (IsInNameOfOperation(node))
                    return Create(ValueUsageInfo.Name);

                if (node.IsPartOfStructuredTrivia())
                    return Create(ValueUsageInfo.Name);

                var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
                if (symbolInfo.Symbol != null)
                {
                    switch (symbolInfo.Symbol.Kind)
                    {
                        case SymbolKind.Namespace:
                            var namespaceUsageInfo = TypeOrNamespaceUsageInfo.None;
                            if (isInNamespaceNameContext)
                                namespaceUsageInfo |= TypeOrNamespaceUsageInfo.NamespaceDeclaration;

                            if (IsNodeOrAnyAncestorLeftSideOfDot(node, syntaxFacts))
                                namespaceUsageInfo |= TypeOrNamespaceUsageInfo.Qualified;

                            return Create(namespaceUsageInfo);

                        case SymbolKind.NamedType:
                            var typeUsageInfo = TypeOrNamespaceUsageInfo.None;
                            if (IsNodeOrAnyAncestorLeftSideOfDot(node, syntaxFacts))
                                typeUsageInfo |= TypeOrNamespaceUsageInfo.Qualified;

                            return Create(typeUsageInfo);

                        case SymbolKind.Method:
                        case SymbolKind.Property:
                        case SymbolKind.Field:
                        case SymbolKind.Event:
                        case SymbolKind.Parameter:
                        case SymbolKind.Local:
                            var valueUsageInfo = ValueUsageInfo.Read;
                            if (semanticFacts.IsWrittenTo(semanticModel, node, cancellationToken))
                                valueUsageInfo |= ValueUsageInfo.Write;

                            return Create(valueUsageInfo);
                    }
                }

                return SymbolUsageInfo.None;
            }
        }

        bool IsInNameOfOperation(SyntaxNode node)
        {
            // Walk up out of the member access expression. This way if we have something like
            // nameof(C.Goo()), we ensure that operation.Parent is the INameOfOperation. 

            while (syntaxFacts.IsMemberAccessExpression(node?.Parent))
                node = node.Parent;

            if (node is null)
                return false;

            var operation = semanticModel.GetOperation(node, cancellationToken);

            // Note: sizeof/typeof also return 'name', but are handled in GetSymbolUsageInfo.
            if (operation?.Parent is INameOfOperation)
                return true;

            return false;
        }
    }

    private static bool IsNodeOrAnyAncestorLeftSideOfDot(SyntaxNode node, ISyntaxFacts syntaxFacts)
    {
        if (syntaxFacts.IsLeftSideOfDot(node))
        {
            return true;
        }

        if (syntaxFacts.IsRightOfQualifiedName(node) ||
            syntaxFacts.IsNameOfSimpleMemberAccessExpression(node) ||
            syntaxFacts.IsNameOfMemberBindingExpression(node))
        {
            return syntaxFacts.IsLeftSideOfDot(node.Parent);
        }

        return false;
    }
}
