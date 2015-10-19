// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateConstructor
{
    [ExportLanguageService(typeof(IGenerateConstructorService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateConstructorService : AbstractGenerateConstructorService<CSharpGenerateConstructorService, ArgumentSyntax, AttributeArgumentSyntax>
    {
        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        protected override bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is SimpleNameSyntax;
        }

        protected override bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is ConstructorInitializerSyntax;
        }

        protected override bool TryInitializeConstructorInitializerGeneration(
            SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken,
            out SyntaxToken token, out IList<ArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn)
        {
            var constructorInitializer = (ConstructorInitializerSyntax)node;

            if (!constructorInitializer.ArgumentList.CloseParenToken.IsMissing)
            {
                token = constructorInitializer.ThisOrBaseKeyword;
                arguments = constructorInitializer.ArgumentList.Arguments.ToList();

                var semanticModel = document.SemanticModel;
                var currentType = semanticModel.GetEnclosingNamedType(constructorInitializer.SpanStart, cancellationToken);
                typeToGenerateIn = constructorInitializer.IsKind(SyntaxKind.ThisConstructorInitializer)
                    ? currentType
                    : currentType.BaseType.OriginalDefinition;
                return typeToGenerateIn != null;
            }

            token = default(SyntaxToken);
            arguments = null;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeSimpleNameGenerationState(
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out IList<ArgumentSyntax> arguments,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var simpleName = (SimpleNameSyntax)node;
            var fullName = simpleName.IsRightSideOfQualifiedName()
                ? (NameSyntax)simpleName.Parent
                : simpleName;

            if (fullName.Parent is ObjectCreationExpressionSyntax)
            {
                var objectCreationExpression = (ObjectCreationExpressionSyntax)fullName.Parent;
                if (objectCreationExpression.ArgumentList != null &&
                    !objectCreationExpression.ArgumentList.CloseParenToken.IsMissing)
                {
                    var symbolInfo = document.SemanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken);
                    token = simpleName.Identifier;
                    arguments = objectCreationExpression.ArgumentList.Arguments.ToList();
                    typeToGenerateIn = symbolInfo.GetAnySymbol() as INamedTypeSymbol;
                    return typeToGenerateIn != null;
                }
            }

            token = default(SyntaxToken);
            arguments = null;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeSimpleAttributeNameGenerationState(
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out IList<ArgumentSyntax> arguments,
            out IList<AttributeArgumentSyntax> attributeArguments,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var simpleName = (SimpleNameSyntax)node;
            var fullName = simpleName.IsRightSideOfQualifiedName()
                ? (NameSyntax)simpleName.Parent
                : simpleName;

            if (fullName.Parent is AttributeSyntax)
            {
                var attribute = (AttributeSyntax)fullName.Parent;
                if (attribute.ArgumentList != null &&
                    !attribute.ArgumentList.CloseParenToken.IsMissing)
                {
                    var symbolInfo = document.SemanticModel.GetSymbolInfo(attribute, cancellationToken);
                    if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !symbolInfo.CandidateSymbols.IsEmpty)
                    {
                        token = simpleName.Identifier;
                        attributeArguments = attribute.ArgumentList.Arguments.ToList();
                        arguments = attributeArguments.Select(x => SyntaxFactory.Argument(x.NameColon ?? ((x.NameEquals != null) ? SyntaxFactory.NameColon(x.NameEquals.Name) : null), default(SyntaxToken), x.Expression)).ToList();

                        typeToGenerateIn = symbolInfo.CandidateSymbols.FirstOrDefault().ContainingSymbol as INamedTypeSymbol;
                        return typeToGenerateIn != null;
                    }
                }
            }

            token = default(SyntaxToken);
            arguments = null;
            attributeArguments = null;
            typeToGenerateIn = null;
            return false;
        }

        protected override IList<string> GenerateParameterNames(
            SemanticModel semanticModel, IEnumerable<ArgumentSyntax> arguments, IList<string> reservedNames)
        {
            return semanticModel.GenerateParameterNames(arguments, reservedNames);
        }

        protected override IList<string> GenerateParameterNames(
            SemanticModel semanticModel, IEnumerable<AttributeArgumentSyntax> arguments, IList<string> reservedNames)
        {
            return semanticModel.GenerateParameterNames(arguments, reservedNames);
        }

        protected override string GenerateNameForArgument(
            SemanticModel semanticModel, ArgumentSyntax argument)
        {
            return semanticModel.GenerateNameForArgument(argument);
        }

        protected override string GenerateNameForArgument(
            SemanticModel semanticModel, AttributeArgumentSyntax argument)
        {
            return semanticModel.GenerateNameForArgument(argument);
        }

        protected override RefKind GetRefKind(ArgumentSyntax argument)
        {
            return argument.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword ? RefKind.Ref :
                   argument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword ? RefKind.Out : RefKind.None;
        }

        protected override bool IsNamedArgument(ArgumentSyntax argument)
        {
            return argument.NameColon != null;
        }

        protected override ITypeSymbol GetArgumentType(
            SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            return semanticModel.GetType(argument.Expression, cancellationToken);
        }

        protected override ITypeSymbol GetAttributeArgumentType(
            SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
        {
            return semanticModel.GetType(argument.Expression, cancellationToken);
        }

        protected override bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            return compilation.ClassifyConversion(sourceType, targetType).IsImplicit;
        }

        internal override IMethodSymbol GetDelegatingConstructor(
            State state,
            SemanticDocument document, 
            int argumentCount, 
            INamedTypeSymbol namedType, 
            ISet<IMethodSymbol> candidates, 
            CancellationToken cancellationToken)
        {
            var oldToken = state.Token;
            var tokenKind = oldToken.Kind();

            if (state.IsConstructorInitializerGeneration)
            {
                SyntaxToken thisOrBaseKeyword;
                SyntaxKind newCtorInitializerKind;
                if (tokenKind != SyntaxKind.BaseKeyword && state.TypeToGenerateIn == namedType)
                {
                    thisOrBaseKeyword = SyntaxFactory.Token(SyntaxKind.ThisKeyword);
                    newCtorInitializerKind = SyntaxKind.ThisConstructorInitializer;
                }
                else
                {
                    thisOrBaseKeyword = SyntaxFactory.Token(SyntaxKind.BaseKeyword);
                    newCtorInitializerKind = SyntaxKind.BaseConstructorInitializer;
                }

                var ctorInitializer = (ConstructorInitializerSyntax)oldToken.Parent;
                var oldArgumentList = ctorInitializer.ArgumentList;
                var newArgumentList = GetNewArgumentList(oldArgumentList, argumentCount);

                var newCtorInitializer = SyntaxFactory.ConstructorInitializer(newCtorInitializerKind, ctorInitializer.ColonToken, thisOrBaseKeyword, newArgumentList);
                SemanticModel speculativeModel;
                if (document.SemanticModel.TryGetSpeculativeSemanticModel(ctorInitializer.Span.Start, newCtorInitializer, out speculativeModel))
                {
                    var symbolInfo = speculativeModel.GetSymbolInfo(newCtorInitializer, cancellationToken);
                    return GenerateConstructorHelpers.GetDelegatingConstructor(
                        document, symbolInfo, candidates, namedType, state.ParameterTypes);
                }
            }
            else
            {
                var oldNode = oldToken.Parent
                    .AncestorsAndSelf(ascendOutOfTrivia: false)
                    .Where(node => SpeculationAnalyzer.CanSpeculateOnNode(node))
                    .LastOrDefault();

                var typeNameToReplace = (TypeSyntax)oldToken.Parent;
                TypeSyntax newTypeName;
                if (namedType != state.TypeToGenerateIn)
                {
                    while (true)
                    {
                        var parentType = typeNameToReplace.Parent as TypeSyntax;
                        if (parentType == null)
                        {
                            break;
                        }

                        typeNameToReplace = parentType;
                    }

                    newTypeName = namedType.GenerateTypeSyntax().WithAdditionalAnnotations(s_annotation);
                }
                else
                {
                    newTypeName = typeNameToReplace.WithAdditionalAnnotations(s_annotation);
                }

                var newNode = oldNode.ReplaceNode(typeNameToReplace, newTypeName);
                newTypeName = (TypeSyntax)newNode.GetAnnotatedNodes(s_annotation).Single();

                var oldArgumentList = (ArgumentListSyntax)newTypeName.Parent.ChildNodes().FirstOrDefault(n => n is ArgumentListSyntax);
                if (oldArgumentList != null)
                {
                    var newArgumentList = GetNewArgumentList(oldArgumentList, argumentCount);
                    if (newArgumentList != oldArgumentList)
                    {
                        newNode = newNode.ReplaceNode(oldArgumentList, newArgumentList);
                        newTypeName = (TypeSyntax)newNode.GetAnnotatedNodes(s_annotation).Single();
                    }
                }

                var speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(oldNode, newNode, document.SemanticModel);
                if (speculativeModel != null)
                {
                    var symbolInfo = speculativeModel.GetSymbolInfo(newTypeName.Parent, cancellationToken);
                    return GenerateConstructorHelpers.GetDelegatingConstructor(
                        document, symbolInfo, candidates, namedType, state.ParameterTypes);
                }
            }

            return null;
        }

        private static ArgumentListSyntax GetNewArgumentList(ArgumentListSyntax oldArgumentList, int argumentCount)
        {
            if (oldArgumentList.IsMissing || oldArgumentList.Arguments.Count == argumentCount)
            {
                return oldArgumentList;
            }

            var newArguments = oldArgumentList.Arguments.Take(argumentCount);
            return SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().AddRange(newArguments));
        }
    }
}
