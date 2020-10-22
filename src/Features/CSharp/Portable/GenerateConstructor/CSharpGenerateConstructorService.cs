// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructor
{
    [ExportLanguageService(typeof(IGenerateConstructorService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateConstructorService : AbstractGenerateConstructorService<CSharpGenerateConstructorService, ArgumentSyntax, AttributeArgumentSyntax>
    {
        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateConstructorService()
        {
        }

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
           => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            => node is SimpleNameSyntax;

        protected override bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            => node is ConstructorInitializerSyntax;

        protected override bool IsImplicitObjectCreation(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            => node is ImplicitObjectCreationExpressionSyntax;

        protected override bool TryInitializeConstructorInitializerGeneration(
            SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken,
            out SyntaxToken token, out ImmutableArray<ArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn)
        {
            var constructorInitializer = (ConstructorInitializerSyntax)node;

            if (!constructorInitializer.ArgumentList.CloseParenToken.IsMissing)
            {
                token = constructorInitializer.ThisOrBaseKeyword;
                arguments = constructorInitializer.ArgumentList.Arguments.ToImmutableArray();

                var semanticModel = document.SemanticModel;
                var currentType = semanticModel.GetEnclosingNamedType(constructorInitializer.SpanStart, cancellationToken);
                typeToGenerateIn = constructorInitializer.IsKind(SyntaxKind.ThisConstructorInitializer)
                    ? currentType
                    : currentType.BaseType.OriginalDefinition;
                return typeToGenerateIn != null;
            }

            token = default;
            arguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeSimpleNameGenerationState(
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out ImmutableArray<ArgumentSyntax> arguments,
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
                    arguments = objectCreationExpression.ArgumentList.Arguments.ToImmutableArray();
                    typeToGenerateIn = symbolInfo.GetAnySymbol() as INamedTypeSymbol;
                    return typeToGenerateIn != null;
                }
            }

            token = default;
            arguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeSimpleAttributeNameGenerationState(
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out ImmutableArray<ArgumentSyntax> arguments,
            out ImmutableArray<AttributeArgumentSyntax> attributeArguments,
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
                        attributeArguments = attribute.ArgumentList.Arguments.ToImmutableArray();
                        arguments = attributeArguments.Select(
                            x => SyntaxFactory.Argument(
                                x.NameColon ?? (x.NameEquals != null ? SyntaxFactory.NameColon(x.NameEquals.Name) : null),
                                default, x.Expression)).ToImmutableArray();

                        typeToGenerateIn = symbolInfo.CandidateSymbols.FirstOrDefault().ContainingSymbol as INamedTypeSymbol;
                        return typeToGenerateIn != null;
                    }
                }
            }

            token = default;
            arguments = default;
            attributeArguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeImplicitObjectCreation(SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out ImmutableArray<ArgumentSyntax> arguments,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var implicitObjectCreation = (ImplicitObjectCreationExpressionSyntax)node;
            if (implicitObjectCreation.ArgumentList != null &&
                !implicitObjectCreation.ArgumentList.CloseParenToken.IsMissing)
            {
                var typeInfo = document.SemanticModel.GetTypeInfo(implicitObjectCreation, cancellationToken);
                if (typeInfo.Type is INamedTypeSymbol typeSymbol)
                {
                    token = implicitObjectCreation.NewKeyword;
                    arguments = implicitObjectCreation.ArgumentList.Arguments.ToImmutableArray();
                    typeToGenerateIn = typeSymbol;
                    return true;
                }
            }

            token = default;
            arguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override ImmutableArray<ParameterName> GenerateParameterNames(
            SemanticModel semanticModel, IEnumerable<ArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken)
            => semanticModel.GenerateParameterNames(arguments, reservedNames, parameterNamingRule, cancellationToken);

        protected override ImmutableArray<ParameterName> GenerateParameterNames(
            SemanticModel semanticModel, IEnumerable<AttributeArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken)
            => semanticModel.GenerateParameterNames(arguments, reservedNames, parameterNamingRule, cancellationToken);

        protected override string GenerateNameForArgument(SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
            => semanticModel.GenerateNameForArgument(argument, cancellationToken);

        protected override string GenerateNameForArgument(SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
            => semanticModel.GenerateNameForArgument(argument, cancellationToken);

        protected override RefKind GetRefKind(ArgumentSyntax argument)
            => argument.GetRefKind();

        protected override bool IsNamedArgument(ArgumentSyntax argument)
            => argument.NameColon != null;

        protected override ITypeSymbol GetArgumentType(
            SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            return argument.DetermineParameterType(semanticModel, cancellationToken);
        }

        protected override ITypeSymbol GetAttributeArgumentType(
            SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
        {
            return semanticModel.GetType(argument.Expression, cancellationToken);
        }

        protected override bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
            => compilation.ClassifyConversion(sourceType, targetType).IsImplicit;

        /// <summary>
        /// Find the constructor that our newly generated constructor should delegate to, if any, instead of creating members.
        /// </summary>
        /// <returns>
        /// <para>
        /// This method is called multiple times, for different numbers of arguments, and different types to create, until
        /// it finds a valid match for an existing constructor that can be delegated to. For example given:
        /// </para>
        /// <code>
        /// class Base
        /// {
        ///     Base(int x) { }
        /// }
        /// 
        /// class Derived : Base
        /// {
        /// }
        /// </code>
        /// <para>
        /// If the user types <c>new Derived(1, 2)</c> then this method will be called 4 times, to try to find a constructor
        /// on Derived that takes two ints, then that takes one int, then a constructor on Base that takes two ints, then that
        /// takes one int.
        /// </para>
        /// <para>
        /// This class takes the original syntax node that the user typed and creates a new node, for whichever form is being
        /// tried, places that in the surrounding context, and then uses the speculative semantic model to see if it can be bound.
        /// </para>
        /// </returns>
        protected override IMethodSymbol GetDelegatingConstructor(
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
                if (tokenKind != SyntaxKind.BaseKeyword && Equals(state.TypeToGenerateIn, namedType))
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
                if (document.SemanticModel.TryGetSpeculativeSemanticModel(ctorInitializer.Span.Start, newCtorInitializer, out var speculativeModel))
                {
                    var symbolInfo = speculativeModel.GetSymbolInfo(newCtorInitializer, cancellationToken);
                    var delegatingConstructor = GenerateConstructorHelpers.GetDelegatingConstructor(
                        document, symbolInfo, candidates, namedType, state.ParameterTypes);

                    if (delegatingConstructor == null || thisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword))
                    {
                        return delegatingConstructor;
                    }

                    return CanDelegeteThisConstructor(state, document, delegatingConstructor, cancellationToken) ? delegatingConstructor : null;
                }
            }
            else
            {
                var oldNode = oldToken.Parent
                    .AncestorsAndSelf(ascendOutOfTrivia: false)
                    .Where(node => SpeculationAnalyzer.CanSpeculateOnNode(node))
                    .LastOrDefault();

                // Get the new constructor call for the desired named type
                var newConstructorCall = GetConstructorInvocationWithCorrectTypeName(state.TypeToGenerateIn, namedType, oldToken, out var nodeToReplace);

                var newNode = oldNode.ReplaceNode(nodeToReplace, newConstructorCall);
                var newTypeName = (TypeSyntax)newNode.GetAnnotatedNodes(s_annotation).Single();

                // Now trim the argument list as appropriate
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

                // Try to find the symbol info to see if this is a valid call, which means we'll delegate to it
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

        private static SyntaxNode GetConstructorInvocationWithCorrectTypeName(INamedTypeSymbol typeToGenerateIn, INamedTypeSymbol desiredTypeName, SyntaxToken oldToken, out SyntaxNode nodeToReplace)
            => oldToken.Parent is ImplicitObjectCreationExpressionSyntax implicitObjectCreation
                ? GetConstructorInvocationWithCorrectTypeName(desiredTypeName, implicitObjectCreation, out nodeToReplace)
                : GetConstructorInvocationWithCorrectTypeName(typeToGenerateIn, desiredTypeName, (TypeSyntax)oldToken.Parent, out nodeToReplace);

        /// <summary>
        /// We might be trying to create a base class of the original type name the user entered, and for
        /// an implicit object creation expression, we never have a type name from the user, so just always
        /// change to a normal creation expression to force the type we want.
        /// </summary>
        private static SyntaxNode GetConstructorInvocationWithCorrectTypeName(INamedTypeSymbol desiredTypeName, ImplicitObjectCreationExpressionSyntax implicitObjectCreation, out SyntaxNode nodeToReplace)
        {
            nodeToReplace = implicitObjectCreation;

            var typeToCreate = desiredTypeName.GenerateTypeSyntax().WithAdditionalAnnotations(s_annotation);

            return SyntaxFactory.ObjectCreationExpression(implicitObjectCreation.NewKeyword, typeToCreate, implicitObjectCreation.ArgumentList, implicitObjectCreation.Initializer);
        }

        /// <summary>
        /// We might be trying to create a base class of the original type name the user entered, so for
        /// a normal creation expression we just find the typename part of the expression and
        /// make sure its the type we want.
        /// </summary>
        private static SyntaxNode GetConstructorInvocationWithCorrectTypeName(INamedTypeSymbol typeToGenerateIn, INamedTypeSymbol desiredTypeName, TypeSyntax existingTypeName, out SyntaxNode nodeToReplace)
        {
            nodeToReplace = existingTypeName;

            TypeSyntax newTypeName;
            if (!Equals(desiredTypeName, typeToGenerateIn))
            {
                while (true)
                {
                    if (nodeToReplace.Parent is not TypeSyntax parentType)
                    {
                        break;
                    }

                    nodeToReplace = parentType;
                }

                newTypeName = desiredTypeName.GenerateTypeSyntax().WithAdditionalAnnotations(s_annotation);
            }
            else
            {
                newTypeName = existingTypeName.WithAdditionalAnnotations(s_annotation);
            }
            return newTypeName;
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

        protected override IMethodSymbol GetCurrentConstructor(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
            => semanticModel.GetDeclaredSymbol(token.GetAncestor<ConstructorDeclarationSyntax>(), cancellationToken);

        protected override IMethodSymbol GetDelegatedConstructor(SemanticModel semanticModel, IMethodSymbol constructor, CancellationToken cancellationToken)
        {
            if (constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is ConstructorDeclarationSyntax constructorDeclarationSyntax &&
                constructorDeclarationSyntax.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
            {
                return semanticModel.GetSymbolInfo(constructorDeclarationSyntax.Initializer, cancellationToken).Symbol as IMethodSymbol;
            }

            return null;
        }
    }
}
