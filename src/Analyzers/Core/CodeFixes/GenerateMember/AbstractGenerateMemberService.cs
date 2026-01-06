// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember;

internal abstract partial class AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
    where TSimpleNameSyntax : TExpressionSyntax
    where TExpressionSyntax : SyntaxNode
{
    protected AbstractGenerateMemberService()
    {
    }

    protected static readonly ISet<TypeKind> EnumType = new HashSet<TypeKind> { TypeKind.Enum };
    protected static readonly ISet<TypeKind> ClassInterfaceModuleStructTypes = new HashSet<TypeKind>
    {
        TypeKind.Class,
        TypeKind.Module,
        TypeKind.Struct,
        TypeKind.Interface
    };

    protected static bool ValidateTypeToGenerateIn(
        [NotNullWhen(true)] INamedTypeSymbol? typeToGenerateIn,
        bool isStatic,
        ISet<TypeKind> typeKinds)
    {
        if (typeToGenerateIn == null)
            return false;

        if (typeToGenerateIn.IsAnonymousType)
            return false;

        if (!typeKinds.Contains(typeToGenerateIn.TypeKind))
            return false;

        if (typeToGenerateIn.TypeKind == TypeKind.Interface && isStatic)
            return false;

        // TODO(cyrusn): Make sure that there is a totally visible part somewhere (i.e.
        // venus) that we can generate into.
        var locations = typeToGenerateIn.Locations;
        return locations.Any(static loc => loc.IsInSource);
    }

    protected static bool TryDetermineTypeToGenerateIn(
        SemanticDocument document,
        INamedTypeSymbol containingType,
        TExpressionSyntax simpleNameOrMemberAccessExpression,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn,
        out bool isStatic,
        out bool isColorColorCase)
    {
        TryDetermineTypeToGenerateInWorker(
            document, containingType, simpleNameOrMemberAccessExpression, cancellationToken, out typeToGenerateIn, out isStatic, out isColorColorCase);

        if (typeToGenerateIn.IsNullable(out var underlyingType) &&
            underlyingType is INamedTypeSymbol underlyingNamedType)
        {
            typeToGenerateIn = underlyingNamedType;
        }

        typeToGenerateIn = typeToGenerateIn?.OriginalDefinition;
        return typeToGenerateIn != null;
    }

    private static void TryDetermineTypeToGenerateInWorker(
        SemanticDocument semanticDocument,
        INamedTypeSymbol containingType,
        TExpressionSyntax expression,
        CancellationToken cancellationToken,
        out INamedTypeSymbol? typeToGenerateIn,
        out bool isStatic,
        out bool isColorColorCase)
    {
        typeToGenerateIn = null;
        isStatic = false;
        isColorColorCase = false;

        var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = semanticDocument.SemanticModel;
        if (syntaxFacts.IsSimpleMemberAccessExpression(expression))
        {
            // Figure out what's before the dot.  For VB, that also means finding out 
            // what ".X" might mean, even when there's nothing before the dot itself.
            var beforeDotExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(
                expression, allowImplicitTarget: true);

            if (beforeDotExpression != null)
            {
                DetermineTypeToGenerateInWorker(
                    semanticModel, beforeDotExpression, out typeToGenerateIn, out isStatic, out isColorColorCase, cancellationToken);
            }
        }
        else if (syntaxFacts.IsConditionalAccessExpression(expression))
        {
            var beforeDotExpression = syntaxFacts.GetExpressionOfConditionalAccessExpression(expression);

            if (beforeDotExpression != null)
            {
                DetermineTypeToGenerateInWorker(
                    semanticModel, beforeDotExpression, out typeToGenerateIn, out isStatic, out isColorColorCase, cancellationToken);
            }
        }
        else if (syntaxFacts.IsPointerMemberAccessExpression(expression))
        {
            var beforeArrowExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(expression);
            if (beforeArrowExpression != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(beforeArrowExpression, cancellationToken);

                if (typeInfo.Type is IPointerTypeSymbol pointerType)
                {
                    typeToGenerateIn = pointerType.PointedAtType as INamedTypeSymbol;
                }
            }
        }
        else if (syntaxFacts.IsAttributeNamedArgumentIdentifier(expression))
        {
            var attributeNode = expression.GetAncestors().FirstOrDefault(syntaxFacts.IsAttribute);
            Contract.ThrowIfNull(attributeNode);

            var attributeName = syntaxFacts.GetNameOfAttribute(attributeNode);
            var attributeType = semanticModel.GetTypeInfo(attributeName, cancellationToken);

            typeToGenerateIn = attributeType.Type as INamedTypeSymbol;
        }
        else if (syntaxFacts.IsMemberInitializerNamedAssignmentIdentifier(
                expression, out var initializedObject))
        {
            typeToGenerateIn = semanticModel.GetTypeInfo(initializedObject, cancellationToken).Type as INamedTypeSymbol;
        }
        else if (syntaxFacts.IsNameOfSubpattern(expression))
        {
            var propertyPatternClause = expression.Ancestors().FirstOrDefault(syntaxFacts.IsPropertyPatternClause);

            if (propertyPatternClause != null)
            {
                // something like: { [|X|]: int i } or like: Blah { [|X|]: int i }
                var inferenceService = semanticDocument.Document.GetRequiredLanguageService<ITypeInferenceService>();
                typeToGenerateIn = inferenceService.InferType(semanticModel, propertyPatternClause, objectAsDefault: true, cancellationToken) as INamedTypeSymbol;
            }
        }
        else if (syntaxFacts.IsMemberBindingExpression(expression))
        {
            var target = syntaxFacts.GetTargetOfMemberBinding(expression);

            if (target != null)
            {
                typeToGenerateIn = semanticModel.GetTypeInfo(target, cancellationToken).Type as INamedTypeSymbol;
            }
        }
        else
        {
            // Generating into the containing type.
            typeToGenerateIn = containingType;
            isStatic = syntaxFacts.IsInStaticContext(expression);
        }
    }

    private static void DetermineTypeToGenerateInWorker(
        SemanticModel semanticModel,
        SyntaxNode expression,
        out INamedTypeSymbol? typeToGenerateIn,
        out bool isStatic,
        out bool isColorColorCase,
        CancellationToken cancellationToken)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
        var semanticInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

        typeToGenerateIn = typeInfo.Type is ITypeParameterSymbol typeParameter
            ? typeParameter.GetNamedTypeSymbolConstraint()
            : typeInfo.Type as INamedTypeSymbol;

        isStatic = semanticInfo.Symbol is INamedTypeSymbol;
        isColorColorCase = typeInfo.Type != null && semanticInfo.Symbol != null && semanticInfo.Symbol.Name == typeInfo.Type.Name;
    }
}
