﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToClass
{
    internal abstract class AbstractConvertAnonymousTypeToClassCodeRefactoringProvider<
        TExpressionSyntax,
        TNameSyntax,
        TIdentifierNameSyntax,
        TObjectCreationExpressionSyntax,
        TAnonymousObjectCreationExpressionSyntax,
        TNamespaceDeclarationSyntax>
        : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TNameSyntax : TExpressionSyntax
        where TIdentifierNameSyntax : TNameSyntax
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TAnonymousObjectCreationExpressionSyntax : TExpressionSyntax
        where TNamespaceDeclarationSyntax : SyntaxNode
    {
        protected abstract TObjectCreationExpressionSyntax CreateObjectCreationExpression(TNameSyntax nameNode, TAnonymousObjectCreationExpressionSyntax currentAnonymousObject);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var (anonymousObject, anonymousType) = await TryGetAnonymousObjectAsync(
                document, context.Span, cancellationToken).ConfigureAwait(false);

            if (anonymousObject == null || anonymousType == null)
            {
                return;
            }

            // Check if the anonymous type actually references another anonymous type inside of it.
            // If it does, we can't convert this.  There is no way to describe this anonymous type
            // in the concrete type we create.
            var containsAnonymousType = anonymousType.GetMembers()
                                                     .OfType<IPropertySymbol>()
                                                     .Any(p => p.Type.ContainsAnonymousType());
            if (containsAnonymousType)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                c => ConvertToClassAsync(document, context.Span, c)));
        }

        private async Task<(TAnonymousObjectCreationExpressionSyntax, INamedTypeSymbol)> TryGetAnonymousObjectAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var position = span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            // Span actually has to be within the token (i.e. not in trivia around it).
            if (!token.Span.IntersectsWith(position))
            {
                return default;
            }

            if (!span.IsEmpty && span != token.Span)
            {
                // if there is a selection, it has to be of the whole token.
                return default;
            }

            var anonymousObject = token.Parent as TAnonymousObjectCreationExpressionSyntax;
            if (anonymousObject == null)
            {
                return default;
            }

            // The position/selection must be of the 'new' token of the anonymous object.
            if (anonymousObject.GetFirstToken() != token)
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var anonymousType = semanticModel.GetTypeInfo(anonymousObject, cancellationToken).Type as INamedTypeSymbol;

            return (anonymousObject, anonymousType);
        }

        private async Task<Document> ConvertToClassAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var (anonymousObject, anonymousType) = await TryGetAnonymousObjectAsync(
                document, span, cancellationToken).ConfigureAwait(false);

            Debug.Assert(anonymousObject != null);
            Debug.Assert(anonymousType != null);

            var position = span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Generate a unique name for the class we're creating.  We'll also add a rename
            // annotation so the user can pick the right name for the type afterwards.
            var className = NameGenerator.GenerateUniqueName(
                "NewClass", n => semanticModel.LookupSymbols(position, name: n).IsEmpty);

            // First, create the set of properties this class will have based on the properties the
            // anonymous type has itself.  Also, get a mapping of the original anonymous type's
            // properties to the new name we generated for it (if we converted camelCase to
            // PascalCase).
            var (properties, propertyMap) = GenerateProperties(document, anonymousType);

            // Next, generate the full class that will be used to replace all instances of this
            // anonymous type.
            var namedTypeSymbol = await GenerateFinalNamedTypeAsync(
                document, className, properties, cancellationToken).ConfigureAwait(false);

            var generator = SyntaxGenerator.GetGenerator(document);
            var editor = new SyntaxEditor(root, generator);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var containingMember = anonymousObject.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsMethodLevelMember) ?? anonymousObject;

            // Next, go and update any references to these anonymous type properties to match
            // the new PascalCased name we've picked for the new properties that will go in
            // the named type.
            await ReplacePropertyReferencesAsync(
                document, editor, containingMember,
                propertyMap, cancellationToken).ConfigureAwait(false);

            // Next, go through and replace all matching anonymous types in this method with a call
            // to construct the new named type we've generated.  
            await ReplaceMatchingAnonymousTypesAsync(
                document, editor, namedTypeSymbol,
                containingMember, anonymousObject,
                anonymousType, cancellationToken).ConfigureAwait(false);

            // Then, actually insert the new class in the appropriate container.
            var container = anonymousObject.GetAncestor<TNamespaceDeclarationSyntax>() ?? root;
            editor.ReplaceNode(container, (currentContainer, _) =>
            {
                var codeGenService = document.GetLanguageService<ICodeGenerationService>();
                var options = new CodeGenerationOptions(
                    generateMembers: true,
                    sortMembers: false,
                    autoInsertionLocation: false);

                return codeGenService.AddNamedType(
                    currentContainer, namedTypeSymbol, options, cancellationToken);
            });

            var updatedDocument = document.WithSyntaxRoot(editor.GetChangedRoot());

            // Finally, format using the equals+getHashCode service so that our generated methods
            // follow any special formatting rules specific to them.
            var equalsAndGetHashCodeService = document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
            return await equalsAndGetHashCodeService.FormatDocumentAsync(
                updatedDocument, cancellationToken).ConfigureAwait(false);
        }

        private async Task ReplacePropertyReferencesAsync(
            Document document, SyntaxEditor editor, SyntaxNode containingMember,
            ImmutableDictionary<IPropertySymbol, string> propertyMap, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var identifiers = containingMember.DescendantNodes().OfType<TIdentifierNameSyntax>();

            foreach (var identifier in identifiers)
            {
                if (!syntaxFacts.IsNameOfMemberAccessExpression(identifier))
                {
                    continue;
                }

                var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).GetAnySymbol() as IPropertySymbol;
                if (symbol == null)
                {
                    continue;
                }

                if (propertyMap.TryGetValue(symbol, out var newName))
                {
                    editor.ReplaceNode(
                        identifier,
                        (currentId, g) => g.IdentifierName(newName).WithTriviaFrom(currentId));
                }
            }
        }

        private async Task ReplaceMatchingAnonymousTypesAsync(
            Document document, SyntaxEditor editor, INamedTypeSymbol classSymbol,
            SyntaxNode containingMember, TAnonymousObjectCreationExpressionSyntax creationNode,
            INamedTypeSymbol anonymousType, CancellationToken cancellationToken)
        {
            // When invoked we want to fixup all creations of the "same" anonymous type within the
            // containing method.  We define same-ness as meaning "they have the type symbol".  this
            // means both have the same member names, in the same order, with the same member types.
            // We fix all these up in the method because the user may be creating several instances
            // of this anonymous type in that method and then combining them in interesting ways
            // (i.e. checking them for equality, using them in collections, etc.).  The language
            // guarantees within a method boundary that these will be the same type and can be used
            // together in this fashion.
            //
            // Note: we could consider expanding this in the future (potentially with another
            // lightbulb action).  Specifically, we could look in the containing type and replace
            // any matches in any methods.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var childCreationNodes = containingMember.DescendantNodesAndSelf()
                                                     .OfType<TAnonymousObjectCreationExpressionSyntax>();

            foreach (var childCreation in childCreationNodes)
            {
                var childType = semanticModel.GetTypeInfo(childCreation, cancellationToken).Type;
                if (childType == null)
                {
                    Debug.Fail("We should always be able to get an anonymous type for any anonymous creation node.");
                    continue;
                }

                if (anonymousType.Equals(childType))
                {
                    ReplaceWithObjectCreation(editor, classSymbol, creationNode, childCreation);
                }
            }
        }

        private void ReplaceWithObjectCreation(
            SyntaxEditor editor, INamedTypeSymbol classSymbol,
            TAnonymousObjectCreationExpressionSyntax startingCreationNode,
            TAnonymousObjectCreationExpressionSyntax childCreation)
        {
            // Use the callback form as anonymous types may be nested, and we want to
            // properly replace them even in that case.
            editor.ReplaceNode(
                childCreation,
                (currentNode, g) =>
                {
                    var currentAnonymousObject = (TAnonymousObjectCreationExpressionSyntax)currentNode;

                    // If we hit the node the user started on, then add the rename annotation here.
                    var className = classSymbol.Name;
                    var classNameToken = startingCreationNode == childCreation
                        ? g.Identifier(className).WithAdditionalAnnotations(RenameAnnotation.Create())
                        : g.Identifier(className);

                    var classNameNode = classSymbol.TypeParameters.Length == 0
                        ? (TNameSyntax)g.IdentifierName(classNameToken)
                        : (TNameSyntax)g.GenericName(classNameToken,
                            classSymbol.TypeParameters.Select(tp => g.IdentifierName(tp.Name)));

                    return CreateObjectCreationExpression(classNameNode, currentAnonymousObject)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                });
        }

        private static async Task<INamedTypeSymbol> GenerateFinalNamedTypeAsync(
            Document document, string className,
            ImmutableArray<IPropertySymbol> properties, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Next, see if any of the properties ended up using any type parameters from the
            // containing method/named-type.  If so, we'll need to generate a generic type so we can
            // properly pass these along.
            var capturedTypeParameters =
                properties.Select(p => p.Type)
                          .SelectMany(t => t.GetReferencedTypeParameters())
                          .Distinct()
                          .ToImmutableArray();

            // Now try to generate all the members that will go in the new class. This is a bit
            // circular.  In order to generate some of the members, we need to know about the type.
            // But in order to create the type, we need the members.  To address this we do two
            // passes. First, we create an empty version of the class.  This can then be used to
            // help create members like Equals/GetHashCode.  Then, once we have all the members we
            // create the final type.
            var namedTypeWithoutMembers = CreateNamedType(className, capturedTypeParameters, members: default);

            var generator = SyntaxGenerator.GetGenerator(document);
            var constructor = CreateConstructor(compilation, className, properties, generator);

            // Generate Equals/GetHashCode.  Only readonly properties are suitable for these
            // methods.  We can defer to our existing language service for this so that we
            // generate the same Equals/GetHashCode that our other IDE features generate.
            var readonlyProperties = ImmutableArray<ISymbol>.CastUp(
                properties.WhereAsArray(p => p.SetMethod == null));

            var equalsAndGetHashCodeService = document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();

            var equalsMethod = await equalsAndGetHashCodeService.GenerateEqualsMethodAsync(
                document, namedTypeWithoutMembers, readonlyProperties,
                localNameOpt: SyntaxGeneratorExtensions.OtherName, cancellationToken).ConfigureAwait(false);
            var getHashCodeMethod = await equalsAndGetHashCodeService.GenerateGetHashCodeMethodAsync(
                document, namedTypeWithoutMembers,
                readonlyProperties, cancellationToken).ConfigureAwait(false);

            var members = ArrayBuilder<ISymbol>.GetInstance();
            members.AddRange(properties);
            members.Add(constructor);
            members.Add(equalsMethod);
            members.Add(getHashCodeMethod);

            var namedTypeSymbol = CreateNamedType(className, capturedTypeParameters, members.ToImmutableAndFree());
            return namedTypeSymbol;
        }

        private static INamedTypeSymbol CreateNamedType(
            string className, ImmutableArray<ITypeParameterSymbol> capturedTypeParameters, ImmutableArray<ISymbol> members)
        {
            return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default, Accessibility.Internal, modifiers: default,
                TypeKind.Class, className, capturedTypeParameters, members: members);
        }

        private static (ImmutableArray<IPropertySymbol> properties, ImmutableDictionary<IPropertySymbol, string> propertyMap) GenerateProperties(
            Document document, INamedTypeSymbol anonymousType)
        {
            var originalProperties = anonymousType.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
            var newProperties = originalProperties.SelectAsArray(p => GenerateProperty(document, p));

            // If we changed the names of any properties, record that name mapping.  We'll
            // use this to update reference to the old anonymous-type properties to the new
            // names.
            var builder = ImmutableDictionary.CreateBuilder<IPropertySymbol, string>();
            for (var i = 0; i < originalProperties.Length; i++)
            {
                var originalProperty = originalProperties[i];
                var newProperty = newProperties[i];

                if (originalProperty.Name != newProperty.Name)
                {
                    builder[originalProperty] = newProperty.Name;
                }
            }

            return (newProperties, builder.ToImmutable());
        }

        private static IPropertySymbol GenerateProperty(Document document, IPropertySymbol prop)
        {
            // The actual properties generated by C#/VB are not what we want.  For example, they
            // think of themselves as having real accessors that will read/write into a real field
            // in the type.  Instead, we just want to generate auto-props. So we effectively clone
            // the property, just throwing aways anything we don't need for that purpose.
            // 
            // We also want to follow general .net naming.  So that means converting to pascal
            // case from camel-case.

            var getMethod = prop.GetMethod != null ? CreateAccessorSymbol(prop, MethodKind.PropertyGet) : null;
            var setMethod = prop.SetMethod != null ? CreateAccessorSymbol(prop, MethodKind.PropertySet) : null;

            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                attributes: default, Accessibility.Public, modifiers: default,
                prop.Type, refKind: default, explicitInterfaceImplementations: default,
                GetLegalName(prop.Name.ToPascalCase(trimLeadingTypePrefix: false), document),
                parameters: default, getMethod: getMethod, setMethod: setMethod);
        }

        private static string GetLegalName(string name, Document document)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return syntaxFacts.IsLegalIdentifier(name)
                ? name
                : "Item"; // Just a dummy name for the property.  Does not need to be localized.
        }

        private static IMethodSymbol CreateAccessorSymbol(IPropertySymbol prop, MethodKind kind)
            => CodeGenerationSymbolFactory.CreateMethodSymbol(
                   attributes: default, Accessibility.Public, DeclarationModifiers.Abstract,
                   prop.Type, refKind: default, explicitInterfaceImplementations: default,
                   name: "", typeParameters: default, parameters: default, methodKind: kind);

        private static IMethodSymbol CreateConstructor(
            Compilation compilation, string className,
            ImmutableArray<IPropertySymbol> properties, SyntaxGenerator generator)
        {
            // For every property, create a corresponding parameter, as well as an assignment
            // statement from that parameter to the property.
            var parameterToPropMap = new Dictionary<string, ISymbol>();
            var parameters = properties.SelectAsArray(prop =>
            {
                var parameter = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    prop.Type, prop.Name.ToCamelCase(trimLeadingTypePrefix: false));

                parameterToPropMap[parameter.Name] = prop;

                return parameter;
            });

            var assignmentStatements = generator.CreateAssignmentStatements(
                compilation, parameters, parameterToPropMap, ImmutableDictionary<string, string>.Empty,
                addNullChecks: false, preferThrowExpression: false);

            var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default, Accessibility.Public, modifiers: default,
                className, parameters, assignmentStatements);

            return constructor;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_to_class, createChangedDocument)
            {
            }
        }
    }
}
