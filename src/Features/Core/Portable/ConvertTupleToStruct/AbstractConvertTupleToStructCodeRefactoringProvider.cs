// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertTupleToStruct
{
    internal abstract partial class AbstractConvertTupleToStructCodeRefactoringProvider<
        TExpressionSyntax,
        TNameSyntax,
        TIdentifierNameSyntax,
        TObjectCreationExpressionSyntax,
        TTupleExpressionSyntax,
        TTupleTypeSyntax,
        TTypeBlockSyntax,
        TNamespaceDeclarationSyntax>
        : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TNameSyntax : TExpressionSyntax
        where TIdentifierNameSyntax : TNameSyntax
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TTupleExpressionSyntax : TExpressionSyntax
        where TTupleTypeSyntax : SyntaxNode
        where TTypeBlockSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
    {
        private enum Scope
        {
            ContainingMember,
            ContainingType,
            ContainingProject,
            DependentProjects
        }

        protected abstract TObjectCreationExpressionSyntax CreateObjectCreationExpression(
            TNameSyntax nameNode, TTupleExpressionSyntax tupleExpression);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var (tupleExprOrTypeNode, tupleType) = await TryGetTupleInfoAsync(
                document, context.Span, cancellationToken).ConfigureAwait(false);

            if (tupleExprOrTypeNode == null || tupleType == null)
            {
                return;
            }

            // Check if the anonymous type actually references another anonymous type inside of it.
            // If it does, we can't convert this.  There is no way to describe this anonymous type
            // in the concrete type we create.
            var fields = tupleType.TupleElements;
            var containsAnonymousType = fields.Any(p => p.Type.ContainsAnonymousType());
            if (containsAnonymousType)
            {
                return;
            }

            var capturedTypeParameters =
                fields.Select(p => p.Type)
                      .SelectMany(t => t.GetReferencedTypeParameters())
                      .Distinct()
                      .ToImmutableArray();

            if (capturedTypeParameters.Length > 0)
            {
                return;
            }

            var scopes = ArrayBuilder<CodeAction>.GetInstance();
            scopes.Add(CreateAction(context, Scope.ContainingMember));

            var containingType = tupleExprOrTypeNode.GetAncestor<TTypeBlockSyntax>();
            if (containingType != null)
            {
                scopes.Add(CreateAction(context, Scope.ContainingType));
            }

            scopes.Add(CreateAction(context, Scope.ContainingProject));
            scopes.Add(CreateAction(context, Scope.DependentProjects));

            context.RegisterRefactoring(new CodeAction.CodeActionWithNestedActions(
                FeaturesResources.Convert_to_struct,
                scopes.ToImmutableAndFree(),
                isInlinable: false));
        }

        private CodeAction CreateAction(CodeRefactoringContext context, Scope scope)
            => new MyCodeAction(GetTitle(scope), c => ConvertToStructAsync(context.Document, context.Span, scope, c));

        private string GetTitle(Scope scope)
        {
            switch (scope)
            {
                case Scope.ContainingMember: return FeaturesResources.and_update_usages_in_containing_member;
                case Scope.ContainingType: return FeaturesResources.and_update_usages_in_containing_type;
                case Scope.ContainingProject: return FeaturesResources.and_update_usages_in_containing_project;
                case Scope.DependentProjects: return FeaturesResources.and_update_usages_in_dependent_projects;
                default:
                    throw ExceptionUtilities.UnexpectedValue(scope);
            }
        }

        private async Task<(SyntaxNode, INamedTypeSymbol)> TryGetTupleInfoAsync(
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

            var tupleExprNode = token.Parent as TTupleExpressionSyntax;
            var tupleTypeNode = token.Parent as TTupleTypeSyntax;
            if (tupleExprNode == null && tupleTypeNode == null)
            {
                return default;
            }

            var expressionOrType = tupleExprNode ?? (SyntaxNode)tupleTypeNode;

            // The position/selection must be of the open paren for the tuple, or the entire tuple.
            if (expressionOrType.GetFirstToken() != token)
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var tupleType = semanticModel.GetTypeInfo(expressionOrType, cancellationToken).Type as INamedTypeSymbol;
            if (tupleType?.IsTupleType != true)
            {
                return default;
            }

            return (expressionOrType, tupleType);
        }

        private async Task<Solution> ConvertToStructAsync(
            Document document, TextSpan span, Scope scope, CancellationToken cancellationToken)
        {
            var (tupleExprOrTypeNode, tupleType) = await TryGetTupleInfoAsync(
                document, span, cancellationToken).ConfigureAwait(false);

            Debug.Assert(tupleExprOrTypeNode != null);
            Debug.Assert(tupleType != null);

            var position = span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var container = tupleExprOrTypeNode.GetAncestor<TNamespaceDeclarationSyntax>() ?? root;
            var containingNamespace = container is TNamespaceDeclarationSyntax namespaceDecl
                ? (INamespaceSymbol)semanticModel.GetDeclaredSymbol(namespaceDecl, cancellationToken)
                : semanticModel.Compilation.GlobalNamespace;

            // Generate a unique name for the struct we're creating.  We'll also add a rename
            // annotation so the user can pick the right name for the type afterwards.
            var structName = NameGenerator.GenerateUniqueName(
                "NewStruct", n => semanticModel.LookupSymbols(position, name: n).IsEmpty);

            // Next, generate the full struct that will be used to replace all instances of this
            // anonymous type.
            var namedTypeSymbol = await GenerateFinalNamedTypeAsync(
                document, structName, tupleType, cancellationToken).ConfigureAwait(false);

            var documentToEditorMap = new Dictionary<Document, SyntaxEditor>();
            var documentsToUpdate = await GetDocumentsToUpdateAsync(
                document, tupleExprOrTypeNode, tupleType, scope, cancellationToken).ConfigureAwait(false);

            // Next, go through and replace all matching tuple expressions and types in the appropriate
            // scope with the new named type we've generated.  
            await ReplaceExpressionAndTypesInScopeAsync(
                documentToEditorMap, documentsToUpdate, 
                tupleExprOrTypeNode, tupleType,
                structName, containingNamespace, 
                cancellationToken).ConfigureAwait(false);

            await GenerateStructIntoContainingNamespaceAsync(
                document, tupleExprOrTypeNode, namedTypeSymbol,
                documentToEditorMap, cancellationToken).ConfigureAwait(false);

            var updatedSolution = await ApplyChangesAsync(
                document, documentToEditorMap, cancellationToken).ConfigureAwait(false);

            return updatedSolution;
        }

        private async Task ReplaceExpressionAndTypesInScopeAsync(
            Dictionary<Document, SyntaxEditor> documentToEditorMap, 
            ImmutableArray<DocumentToUpdate> documentsToUpdate,
            SyntaxNode tupleExprOrTypeNode, INamedTypeSymbol tupleType, 
            string structName, INamespaceSymbol containingNamespace, 
            CancellationToken cancellationToken)
        {
            // Process the documents one project at a time.
            foreach (var group in documentsToUpdate.GroupBy(d => d.Document.Project))
            {
                // grab the compilation and keep it around as long as we're processing
                // the project so we don't clean things up in the middle.
                var project = group.Key;
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var generator = project.LanguageServices.GetService<SyntaxGenerator>();

                // Get the fully qualified name for the new type we're creating.  We'll use this
                // at replacement points so that we can find the right type even if we're in a 
                // different namespace.

                // If the struct is being injected into the global namespace, then reference it with
                // "global::NewStruct",  Otherwise, get the full name to the namespace, and append
                // the NewStruct name to it.
                var structNameNode = generator.IdentifierName(structName);
                var fullTypeName = containingNamespace.IsGlobalNamespace
                    ? (TNameSyntax)generator.GlobalAliasedName(structNameNode)
                    : (TNameSyntax)generator.QualifiedName(generator.NameExpression(containingNamespace), structNameNode);

                fullTypeName = fullTypeName.WithAdditionalAnnotations(Simplifier.Annotation)
                                           .WithAdditionalAnnotations(DoNotAllowVarAnnotation.Annotation);

                foreach (var documentToUpdate in group)
                {
                    var document = documentToUpdate.Document;
                    var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    // If we were given specific nodes to update, only update those.  Otherwise
                    // updated everything from the root down.
                    var nodesToUpdate = documentToUpdate.NodesToUpdate.IsDefaultOrEmpty
                        ? ImmutableArray.Create(syntaxRoot)
                        : documentToUpdate.NodesToUpdate;

                    var editor = new SyntaxEditor(syntaxRoot, generator);

                    var replaced = false;

                    foreach (var container in nodesToUpdate)
                    {
                        replaced |= await ReplaceTupleExpressionsAndTypesInDocumentAsync(
                            document, editor, tupleExprOrTypeNode, tupleType,
                            fullTypeName, structName, container, cancellationToken).ConfigureAwait(false);
                    }

                    if (replaced)
                    {
                        // We made a replacement.  Keep track of this so we can update our solution
                        // later.
                        documentToEditorMap.Add(document, editor);
                    }
                }

                GC.KeepAlive(compilation);
            }
        }

        private async Task<ImmutableArray<DocumentToUpdate>> GetDocumentsToUpdateAsync(
            Document document, SyntaxNode tupleExprOrTypeNode,
            INamedTypeSymbol tupleType, Scope scope, CancellationToken cancellationToken)
        {
            switch (scope)
            {
                case Scope.ContainingMember:
                    return GetDocumentsToUpdateForContainingMember(document, tupleExprOrTypeNode);
                case Scope.ContainingType:
                    return await GetDocumentsToUpdateForContainingTypeAsync(
                        document, tupleExprOrTypeNode, cancellationToken).ConfigureAwait(false);
                case Scope.ContainingProject:
                    break;
                case Scope.DependentProjects:
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }

        private async Task<ImmutableArray<DocumentToUpdate>> GetDocumentsToUpdateForContainingTypeAsync(
            Document startingDocument, SyntaxNode tupleExprOrTypeNode, CancellationToken cancellationToken)
        {
            var containingType = tupleExprOrTypeNode.GetAncestor<TTypeBlockSyntax>();
            Debug.Assert(containingType != null,
                "We should always get a containing scope since we already checked for that to support Scope.ContainingType.");

            var solution = startingDocument.Project.Solution;
            var semanticModel = await startingDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(containingType, cancellationToken);

            var result = ArrayBuilder<DocumentToUpdate>.GetInstance();

            foreach (var group in typeSymbol.DeclaringSyntaxReferences.GroupBy(r => r.SyntaxTree))
            {
                var document = solution.GetDocument(group.Key);
                var nodes = group.SelectAsArray(r => r.GetSyntax(cancellationToken));

                result.Add(new DocumentToUpdate(document, nodes));
            }

            return result.ToImmutableAndFree();
        }

        private ImmutableArray<DocumentToUpdate> GetDocumentsToUpdateForContainingMember(
            Document document, SyntaxNode tupleExprOrTypeNode)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var containingMember = tupleExprOrTypeNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsMethodLevelMember) ?? tupleExprOrTypeNode;

            return ImmutableArray.Create(new DocumentToUpdate(
                document, ImmutableArray.Create(containingMember)));
        }

        private static async Task GenerateStructIntoContainingNamespaceAsync(
            Document document, SyntaxNode tupleExprOrTypeNode, INamedTypeSymbol namedTypeSymbol, 
            Dictionary<Document, SyntaxEditor> documentToEditorMap, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // If we don't already have an editor for the containing document, then make one.
            if (!documentToEditorMap.TryGetValue(document, out var editor))
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                editor = new SyntaxEditor(root, generator);

                documentToEditorMap.Add(document, editor);
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var container = tupleExprOrTypeNode.GetAncestor<TNamespaceDeclarationSyntax>() ?? root;

            // Then, actually insert the new class in the appropriate container.
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
        }

        private async Task<Solution> ApplyChangesAsync(
            Document startingDocument, Dictionary<Document, SyntaxEditor> documentToEditorMap, CancellationToken cancellationToken)
        {
            var currentSolution = startingDocument.Project.Solution;

            foreach (var (currentDoc, editor) in documentToEditorMap)
            {
                var updatedDocument = currentDoc.WithSyntaxRoot(editor.GetChangedRoot());

                if (currentDoc == startingDocument)
                {
                    // If this is the starting document, format using the equals+getHashCode service
                    // so that our generated methods follow any special formatting rules specific to
                    // them.
                    var equalsAndGetHashCodeService = startingDocument.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
                    updatedDocument = await equalsAndGetHashCodeService.FormatDocumentAsync(
                        updatedDocument, cancellationToken).ConfigureAwait(false);
                }

                currentSolution = updatedDocument.Project.Solution;
            }

            return currentSolution;
        }

        private async Task<bool> ReplaceTupleExpressionsAndTypesInDocumentAsync(
            Document document, SyntaxEditor editor,
            SyntaxNode startingNode, INamedTypeSymbol tupleType,
            TNameSyntax fullyQualifiedStructName, string structName,
            SyntaxNode containerToUpdate, CancellationToken cancellationToken)
        {
            var changed = false;
            changed |= await ReplaceMatchingTupleExpressionsAsync(
                document, editor, startingNode, tupleType,
                fullyQualifiedStructName, structName, 
                containerToUpdate, cancellationToken).ConfigureAwait(false);

            changed |= await ReplaceMatchingTupleTypesAsync(
                document, editor, startingNode, tupleType,
                fullyQualifiedStructName, structName,
                containerToUpdate, cancellationToken).ConfigureAwait(false);

            return changed;
        }

        private async Task<bool> ReplaceMatchingTupleExpressionsAsync(
            Document document, SyntaxEditor editor,
            SyntaxNode startingNode, INamedTypeSymbol tupleType, 
            TNameSyntax qualifiedTypeName, string typeName, 
            SyntaxNode containingMember,  CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var childCreationNodes = containingMember.DescendantNodesAndSelf()
                                                     .OfType<TTupleExpressionSyntax>();

            var changed = false;
            foreach (var childCreation in childCreationNodes)
            {
                var childType = semanticModel.GetTypeInfo(childCreation, cancellationToken).Type;
                if (childType == null)
                {
                    Debug.Fail("We should always be able to get an anonymous type for any anonymous creation node.");
                    continue;
                }

                if (tupleType.Equals(childType))
                {
                    changed = true;
                    ReplaceWithObjectCreation(
                        editor, typeName, qualifiedTypeName, startingNode, childCreation);
                }
            }

            return changed;
        }

        private void ReplaceWithObjectCreation(
            SyntaxEditor editor, string typeName, TNameSyntax qualifiedTypeName,
            SyntaxNode startingCreationNode, TTupleExpressionSyntax childCreation)
        {
            // Use the callback form as anonymous types may be nested, and we want to
            // properly replace them even in that case.
            editor.ReplaceNode(
                childCreation,
                (currentNode, g) =>
                {
                    var currentTupleExpr = (TTupleExpressionSyntax)currentNode;

                    // If we hit the node the user started on, then add the rename annotation here.
                    var typeNameNode = startingCreationNode == childCreation
                        ? (TIdentifierNameSyntax)g.IdentifierName(g.Identifier(typeName).WithAdditionalAnnotations(RenameAnnotation.Create()))
                        : qualifiedTypeName;

                    return CreateObjectCreationExpression(typeNameNode, currentTupleExpr)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                });
        }

        private async Task<bool> ReplaceMatchingTupleTypesAsync(
            Document document, SyntaxEditor editor,
            SyntaxNode startingNode, INamedTypeSymbol tupleType,
            TNameSyntax qualifiedTypeName, string typeName,
            SyntaxNode containingMember, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var childTupleNodes = containingMember.DescendantNodesAndSelf()
                                                  .OfType<TTupleTypeSyntax>();

            var changed = false;
            foreach (var childTupleType in childTupleNodes)
            {
                var childType = semanticModel.GetTypeInfo(childTupleType, cancellationToken).Type;
                if (childType == null)
                {
                    Debug.Fail("We should always be able to get an anonymous type for any anonymous creation node.");
                    continue;
                }

                if (tupleType.Equals(childType))
                {
                    changed = true;
                    ReplaceWithTypeNode(
                        editor, typeName, qualifiedTypeName, startingNode, childTupleType);
                }
            }

            return changed;
        }

        private void ReplaceWithTypeNode(
            SyntaxEditor editor, string typeName, TNameSyntax qualifiedTypeName,
            SyntaxNode startingNode, TTupleTypeSyntax childTupleType)
        {
            // Use the callback form as anonymous types may be nested, and we want to
            // properly replace them even in that case.
            editor.ReplaceNode(
                childTupleType,
                (currentNode, g) =>
                {
                    // If we hit the node the user started on, then add the rename annotation here.
                    var typeNameNode = startingNode == childTupleType
                        ? (TIdentifierNameSyntax)g.IdentifierName(g.Identifier(typeName).WithAdditionalAnnotations(RenameAnnotation.Create()))
                        : qualifiedTypeName;

                    return typeNameNode.WithTriviaFrom(currentNode);
                });
        }

        private static async Task<INamedTypeSymbol> GenerateFinalNamedTypeAsync(
            Document document, string structName, INamedTypeSymbol tupleType, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            var fields = tupleType.TupleElements;

            // Next, see if any of the properties ended up using any type parameters from the
            // containing method/named-type.  If so, we'll need to generate a generic type so we can
            // properly pass these along.
            var capturedTypeParameters =
                fields.Select(p => p.Type)
                      .SelectMany(t => t.GetReferencedTypeParameters())
                      .Distinct()
                      .ToImmutableArray();

            // Now try to generate all the members that will go in the new class. This is a bit
            // circular.  In order to generate some of the members, we need to know about the type.
            // But in order to create the type, we need the members.  To address this we do two
            // passes. First, we create an empty version of the class.  This can then be used to
            // help create members like Equals/GetHashCode.  Then, once we have all the members we
            // create the final type.
            var namedTypeWithoutMembers = CreateNamedType(structName, capturedTypeParameters, members: default);

            var generator = SyntaxGenerator.GetGenerator(document);
            var constructor = CreateConstructor(compilation, structName, fields, generator);

            // Generate Equals/GetHashCode.  We can defer to our existing language service for this
            // so that we generate the same Equals/GetHashCode that our other IDE features generate.
            var equalsAndGetHashCodeService = document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();
                        
            var equalsMethod = await equalsAndGetHashCodeService.GenerateEqualsMethodAsync(
                document, namedTypeWithoutMembers, ImmutableArray<ISymbol>.CastUp(fields), 
                localNameOpt: ICodeDefinitionFactoryExtensions.OtherName, cancellationToken).ConfigureAwait(false);
            var getHashCodeMethod = await equalsAndGetHashCodeService.GenerateGetHashCodeMethodAsync(
                document, namedTypeWithoutMembers,
                ImmutableArray<ISymbol>.CastUp(fields), cancellationToken).ConfigureAwait(false);

            var members = ArrayBuilder<ISymbol>.GetInstance();
            members.AddRange(fields);
            members.Add(constructor);
            members.Add(equalsMethod);
            members.Add(getHashCodeMethod);
            members.Add(GenerateDeconstructMethod(semanticModel, generator, tupleType, constructor));
            AddConversions(generator, members, tupleType, namedTypeWithoutMembers);

            var namedTypeSymbol = CreateNamedType(structName, capturedTypeParameters, members.ToImmutableAndFree());
            return namedTypeSymbol;
        }

        private static IMethodSymbol GenerateDeconstructMethod(
            SemanticModel model, SyntaxGenerator generator, 
            INamedTypeSymbol tupleType, IMethodSymbol constructor)
        {
            var assignments = tupleType.TupleElements.Select(
                (field, index) => generator.ExpressionStatement(
                    generator.AssignmentStatement(
                        generator.IdentifierName(constructor.Parameters[index].Name),
                        generator.MemberAccessExpression(
                            generator.ThisExpression(),
                            field.Name)))).ToImmutableArray();

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                Accessibility.Public,
                modifiers: default,
                model.Compilation.GetSpecialType(SpecialType.System_Void),
                RefKind.None,
                explicitInterfaceImplementations: default,
                WellKnownMemberNames.DeconstructMethodName,
                typeParameters: default,
                constructor.Parameters.SelectAsArray(p => 
                    CodeGenerationSymbolFactory.CreateParameterSymbol(RefKind.Out, p.Type, p.Name)),
                assignments);
        }

        private static void AddConversions(
            SyntaxGenerator generator, ArrayBuilder<ISymbol> members,
            INamedTypeSymbol tupleType, INamedTypeSymbol structType)
        {
            const string valueName = "value";

            var valueNode = generator.IdentifierName(valueName);
            var arguments = tupleType.TupleElements.SelectAsArray(
                field => generator.Argument(
                    generator.MemberAccessExpression(valueNode, field.Name)));

            var convertToTupleStatement = generator.ReturnStatement(
                generator.TupleExpression(arguments));

            var convertToStructStatement = generator.ReturnStatement(
                generator.ObjectCreationExpression(structType, arguments));

            members.Add(CodeGenerationSymbolFactory.CreateConversionSymbol(
                attributes: default,
                Accessibility.Public,
                DeclarationModifiers.Static,
                tupleType,
                CodeGenerationSymbolFactory.CreateParameterSymbol(structType, valueName),
                isImplicit: true,
                ImmutableArray.Create(convertToTupleStatement)));
            members.Add(CodeGenerationSymbolFactory.CreateConversionSymbol(
                attributes: default,
                Accessibility.Public,
                DeclarationModifiers.Static,
                structType,
                CodeGenerationSymbolFactory.CreateParameterSymbol(tupleType, valueName),
                isImplicit: true,
                ImmutableArray.Create(convertToStructStatement)));
        }

        private static INamedTypeSymbol CreateNamedType(
            string structName, ImmutableArray<ITypeParameterSymbol> capturedTypeParameters, ImmutableArray<ISymbol> members)
        {
            return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default, Accessibility.Internal, modifiers: default, 
                TypeKind.Struct, structName, capturedTypeParameters, members: members);
        }

        private static IMethodSymbol CreateConstructor(
            Compilation compilation, string className,
            ImmutableArray<IFieldSymbol> fields, SyntaxGenerator generator)
        {
            // For every property, create a corresponding parameter, as well as an assignment
            // statement from that parameter to the property.
            var parameterToPropMap = new Dictionary<string, ISymbol>();
            var parameters = fields.SelectAsArray(field =>
            {
                var parameter = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    field.Type, field.Name.ToCamelCase(trimLeadingTypePrefix: false));

                parameterToPropMap[parameter.Name] = field;

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

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
