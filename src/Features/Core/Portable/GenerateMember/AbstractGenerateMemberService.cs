// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember
{
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

        protected bool ValidateTypeToGenerateIn(
            Solution solution,
            INamedTypeSymbol typeToGenerateIn,
            bool isStatic,
            ISet<TypeKind> typeKinds,
            CancellationToken cancellationToken)
        {
            if (typeToGenerateIn == null)
            {
                return false;
            }

            if (typeToGenerateIn.IsAnonymousType)
            {
                return false;
            }

            if (!typeKinds.Contains(typeToGenerateIn.TypeKind))
            {
                return false;
            }

            if (typeToGenerateIn.TypeKind == TypeKind.Interface && isStatic)
            {
                return false;
            }

            // TODO(cyrusn): Make sure that there is a totally visible part somewhere (i.e.
            // venus) that we can generate into.
            var locations = typeToGenerateIn.Locations;
            return locations.Any(loc => loc.IsInSource);
        }

        protected bool TryDetermineTypeToGenerateIn(
            SemanticDocument document,
            INamedTypeSymbol containingType,
            TExpressionSyntax simpleNameOrMemberAccessExpression,
            CancellationToken cancellationToken,
            out INamedTypeSymbol typeToGenerateIn,
            out bool isStatic)
        {
            TryDetermineTypeToGenerateInWorker(
                document, containingType, simpleNameOrMemberAccessExpression, cancellationToken, out typeToGenerateIn, out isStatic);

            if (typeToGenerateIn != null)
            {
                typeToGenerateIn = typeToGenerateIn.OriginalDefinition;
            }

            return typeToGenerateIn != null;
        }

        private static void TryDetermineTypeToGenerateInWorker(
            SemanticDocument document,
            INamedTypeSymbol containingType,
            TExpressionSyntax simpleNameOrMemberAccessExpression,
            CancellationToken cancellationToken,
            out INamedTypeSymbol typeToGenerateIn,
            out bool isStatic)
        {
            typeToGenerateIn = null;
            isStatic = false;

            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var semanticModel = document.SemanticModel;
            var isMemberAccessExpression = syntaxFacts.IsSimpleMemberAccessExpression(simpleNameOrMemberAccessExpression);
            if (isMemberAccessExpression ||
                syntaxFacts.IsConditionalMemberAccessExpression(simpleNameOrMemberAccessExpression))
            {
                var beforeDotExpression = isMemberAccessExpression ?
                    syntaxFacts.GetExpressionOfMemberAccessExpression(simpleNameOrMemberAccessExpression) :
                    syntaxFacts.GetExpressionOfConditionalMemberAccessExpression(simpleNameOrMemberAccessExpression);
                if (beforeDotExpression != null)
                {
                    var typeInfo = semanticModel.GetTypeInfo(beforeDotExpression, cancellationToken);
                    var semanticInfo = semanticModel.GetSymbolInfo(beforeDotExpression, cancellationToken);

                    typeToGenerateIn = typeInfo.Type is ITypeParameterSymbol
                        ? ((ITypeParameterSymbol)typeInfo.Type).GetNamedTypeSymbolConstraint()
                        : typeInfo.Type as INamedTypeSymbol;

                    isStatic = semanticInfo.Symbol is INamedTypeSymbol;
                }

                return;
            }

            if (syntaxFacts.IsPointerMemberAccessExpression(simpleNameOrMemberAccessExpression))
            {
                var beforeArrowExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(simpleNameOrMemberAccessExpression);
                if (beforeArrowExpression != null)
                {
                    var typeInfo = semanticModel.GetTypeInfo(beforeArrowExpression, cancellationToken);

                    if (typeInfo.Type.IsPointerType())
                    {
                        typeToGenerateIn = ((IPointerTypeSymbol)typeInfo.Type).PointedAtType as INamedTypeSymbol;
                        isStatic = false;
                    }
                }

                return;
            }

            if (syntaxFacts.IsAttributeNamedArgumentIdentifier(simpleNameOrMemberAccessExpression))
            {
                var attributeNode = simpleNameOrMemberAccessExpression.GetAncestors().FirstOrDefault(syntaxFacts.IsAttribute);
                var attributeName = syntaxFacts.GetNameOfAttribute(attributeNode);
                var attributeType = semanticModel.GetTypeInfo(attributeName, cancellationToken);

                typeToGenerateIn = attributeType.Type as INamedTypeSymbol;
                isStatic = false;
                return;
            }

            SyntaxNode initializedObject;
            if (syntaxFacts.IsObjectInitializerNamedAssignmentIdentifier(
                    simpleNameOrMemberAccessExpression, out initializedObject))
            {
                typeToGenerateIn = semanticModel.GetTypeInfo(initializedObject, cancellationToken).Type as INamedTypeSymbol;
                isStatic = false;
                return;
            }

            // Generating into the containing type.
            typeToGenerateIn = containingType;
            isStatic = syntaxFacts.IsInStaticContext(simpleNameOrMemberAccessExpression);
        }
    }
}
