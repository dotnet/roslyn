// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
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
        TLiteralExpressionSyntax,
        TObjectCreationExpressionSyntax,
        TTupleExpressionSyntax,
        TArgumentSyntax,
        TTupleTypeSyntax,
        TTypeBlockSyntax,
        TNamespaceDeclarationSyntax>
        : CodeRefactoringProvider, IConvertTupleToStructCodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TNameSyntax : TExpressionSyntax
        where TIdentifierNameSyntax : TNameSyntax
        where TLiteralExpressionSyntax : TExpressionSyntax
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TTupleExpressionSyntax : TExpressionSyntax
        where TArgumentSyntax : SyntaxNode
        where TTupleTypeSyntax : SyntaxNode
        where TTypeBlockSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
    {
        protected abstract TArgumentSyntax GetArgumentWithChangedName(TArgumentSyntax argument, string name);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var (tupleExprOrTypeNode, tupleType) = await TryGetTupleInfoAsync(
                document, textSpan, cancellationToken).ConfigureAwait(false);

            if (tupleExprOrTypeNode == null || tupleType == null)
            {
                return;
            }

            // Check if the tuple type actually references another anonymous type inside of it.
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
                      .SelectMany<ITypeSymbol, ITypeParameterSymbol>(t => t.GetReferencedTypeParameters())
                      .Distinct()
                      .ToImmutableArray();

            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var scopes);
            scopes.Add(CreateAction(context, Scope.ContainingMember));

            // If we captured any Method type-parameters, we can only replace the tuple types we
            // find in the containing method.  No other tuple types in other members would be able
            // to reference this type parameter.
            if (!capturedTypeParameters.Any(tp => tp.TypeParameterKind == TypeParameterKind.Method))
            {
                var containingType = tupleExprOrTypeNode.GetAncestor<TTypeBlockSyntax>();
                if (containingType != null)
                    scopes.Add(CreateAction(context, Scope.ContainingType));

                // If we captured any Type type-parameters, we can only replace the tuple
                // types we find in the containing type.  No other tuple types in other
                // types would be able to reference this type parameter.
                if (!capturedTypeParameters.Any(tp => tp.TypeParameterKind == TypeParameterKind.Type))
                {
                    // To do a global find/replace of matching tuples, we need to search for documents
                    // containing tuples *and* which have the names of the tuple fields in them.  That means
                    // the tuple field name must exist in the document.
                    //
                    // this means we can only find tuples like ```(x: 1, ...)``` but not ```(1, 2)```.  The
                    // latter has members called Item1 and Item2, but those names don't show up in source.
                    if (fields.All(f => f.CorrespondingTupleField != f))
                    {
                        scopes.Add(CreateAction(context, Scope.ContainingProject));
                        scopes.Add(CreateAction(context, Scope.DependentProjects));
                    }
                }
            }

            context.RegisterRefactoring(
                new CodeAction.CodeActionWithNestedActions(
                    FeaturesResources.Convert_to_struct,
                    scopes.ToImmutable(),
                    isInlinable: false),
                tupleExprOrTypeNode.Span);
        }

        private CodeAction CreateAction(CodeRefactoringContext context, Scope scope)
            => new MyCodeAction(GetTitle(scope), c => ConvertToStructAsync(context.Document, context.Span, scope, c));

        private static string GetTitle(Scope scope)
            => scope switch
            {
                Scope.ContainingMember => FeaturesResources.updating_usages_in_containing_member,
                Scope.ContainingType => FeaturesResources.updating_usages_in_containing_type,
                Scope.ContainingProject => FeaturesResources.updating_usages_in_containing_project,
                Scope.DependentProjects => FeaturesResources.updating_usages_in_dependent_projects,
                _ => throw ExceptionUtilities.UnexpectedValue(scope),
            };

        private static async Task<(SyntaxNode, INamedTypeSymbol)> TryGetTupleInfoAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            // Enable refactoring either for TupleExpression or TupleType
            var expressionOrType =
                await document.TryGetRelevantNodeAsync<TTupleTypeSyntax>(span, cancellationToken).ConfigureAwait(false) as SyntaxNode ??
                await document.TryGetRelevantNodeAsync<TTupleExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
            if (expressionOrType == null)
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

        public async Task<Solution> ConvertToStructAsync(
            Document document, TextSpan span, Scope scope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.AbstractConvertTupleToStructCodeRefactoringProvider_ConvertToStructAsync, cancellationToken))
            {
                var solution = document.Project.Solution;
                var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.RunRemoteAsync<SerializableConvertTupleToStructResult>(
                        WellKnownServiceHubService.CodeAnalysis,
                        nameof(IRemoteConvertTupleToStructCodeRefactoringProvider.ConvertToStructAsync),
                        solution,
                        new object[]
                        {
                            document.Id,
                            span,
                            scope,
                        },
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);

                    var resultSolution = await RemoteUtilities.UpdateSolutionAsync(
                        solution, result.DocumentTextChanges, cancellationToken).ConfigureAwait(false);

                    return await AddRenameTokenAsync(
                        resultSolution, result.RenamedToken, cancellationToken).ConfigureAwait(false);
                }
            }

            return await ConvertToStructInCurrentProcessAsync(
                document, span, scope, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Solution> AddRenameTokenAsync(
            Solution solution,
            (DocumentId documentId, TextSpan span) renamedToken,
            CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(renamedToken.documentId);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(renamedToken.span.Start);
            var newRoot = root.ReplaceToken(token, token.WithAdditionalAnnotations(RenameAnnotation.Create()));

            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private async Task<Solution> ConvertToStructInCurrentProcessAsync(
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

            var capturedTypeParameters =
                tupleType.TupleElements.Select(p => p.Type)
                                       .SelectMany<ITypeSymbol, ITypeParameterSymbol>(t => t.GetReferencedTypeParameters())
                                       .Distinct()
                                       .ToImmutableArray();

            // Get the rule that will name the parameter according to the users preferences for this document
            // (and importantly not any of the documents where we change the call sites, below)
            var parameterNamingRule = await document.GetApplicableNamingRuleAsync(SymbolKind.Parameter, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false);

            // Next, generate the full struct that will be used to replace all instances of this
            // tuple type.
            var namedTypeSymbol = await GenerateFinalNamedTypeAsync(
                document, scope, structName, capturedTypeParameters, tupleType, parameterNamingRule, cancellationToken).ConfigureAwait(false);

            var documentToEditorMap = new Dictionary<Document, SyntaxEditor>();
            var documentsToUpdate = await GetDocumentsToUpdateAsync(
                document, tupleExprOrTypeNode, tupleType, scope, cancellationToken).ConfigureAwait(false);

            // Next, go through and replace all matching tuple expressions and types in the appropriate
            // scope with the new named type we've generated.  
            await ReplaceExpressionAndTypesInScopeAsync(
                documentToEditorMap, documentsToUpdate,
                tupleExprOrTypeNode, tupleType,
                structName, capturedTypeParameters,
                containingNamespace, parameterNamingRule, cancellationToken).ConfigureAwait(false);

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
            string structName, ImmutableArray<ITypeParameterSymbol> typeParameters,
            INamespaceSymbol containingNamespace, NamingRule parameterNamingRule, CancellationToken cancellationToken)
        {
            // Process the documents one project at a time.
            foreach (var group in documentsToUpdate.GroupBy(d => d.Document.Project))
            {
                // grab the compilation and keep it around as long as we're processing
                // the project so we don't clean things up in the middle.  To do this
                // we use a GC.KeepAlive below so that we can mark that this compilation
                // should stay around (even though we don't reference is directly in 
                // any other way here).
                var project = group.Key;
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var generator = project.LanguageServices.GetService<SyntaxGenerator>();

                // Get the fully qualified name for the new type we're creating.  We'll use this
                // at replacement points so that we can find the right type even if we're in a 
                // different namespace.

                // If the struct is being injected into the global namespace, then reference it with
                // "global::NewStruct",  Otherwise, get the full name to the namespace, and append
                // the NewStruct name to it.
                var structNameNode = CreateStructNameNode(
                    generator, structName, typeParameters, addRenameAnnotation: false);

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

                    // We should only ever get a default array (meaning, update the root), or a
                    // non-empty array.  We should never be asked to update exactly '0' nodes.
                    Debug.Assert(documentToUpdate.NodesToUpdate.IsDefault ||
                                 !documentToUpdate.NodesToUpdate.IsEmpty);

                    // If we were given specific nodes to update, only update those.  Otherwise
                    // updated everything from the root down.
                    var nodesToUpdate = documentToUpdate.NodesToUpdate.IsDefault
                        ? ImmutableArray.Create(syntaxRoot)
                        : documentToUpdate.NodesToUpdate;

                    var editor = new SyntaxEditor(syntaxRoot, generator);

                    var replaced = false;

                    foreach (var container in nodesToUpdate)
                    {
                        replaced |= await ReplaceTupleExpressionsAndTypesInDocumentAsync(
                            document, parameterNamingRule, editor, tupleExprOrTypeNode, tupleType,
                            fullTypeName, structName, typeParameters,
                            container, cancellationToken).ConfigureAwait(false);
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

        private static TNameSyntax CreateStructNameNode(
            SyntaxGenerator generator, string structName,
            ImmutableArray<ITypeParameterSymbol> typeParameters, bool addRenameAnnotation)
        {
            var structNameToken = generator.Identifier(structName);
            if (addRenameAnnotation)
            {
                structNameToken = structNameToken.WithAdditionalAnnotations(RenameAnnotation.Create());
            }

            return typeParameters.IsEmpty
                ? (TNameSyntax)generator.IdentifierName(structNameToken)
                : (TNameSyntax)generator.GenericName(structNameToken, typeParameters.Select(tp => generator.IdentifierName(tp.Name)));
        }

        private static async Task<ImmutableArray<DocumentToUpdate>> GetDocumentsToUpdateAsync(
            Document document, SyntaxNode tupleExprOrTypeNode,
            INamedTypeSymbol tupleType, Scope scope, CancellationToken cancellationToken)
        {
            return scope switch
            {
                Scope.ContainingMember => GetDocumentsToUpdateForContainingMember(document, tupleExprOrTypeNode),
                Scope.ContainingType => await GetDocumentsToUpdateForContainingTypeAsync(
                    document, tupleExprOrTypeNode, cancellationToken).ConfigureAwait(false),
                Scope.ContainingProject => await GetDocumentsToUpdateForContainingProjectAsync(
                    document.Project, tupleType, cancellationToken).ConfigureAwait(false),
                Scope.DependentProjects => await GetDocumentsToUpdateForDependentProjectAsync(
                    document.Project, tupleType, cancellationToken).ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(scope),
            };
        }

        private static async Task<ImmutableArray<DocumentToUpdate>> GetDocumentsToUpdateForDependentProjectAsync(
            Project startingProject, INamedTypeSymbol tupleType, CancellationToken cancellationToken)
        {
            var solution = startingProject.Solution;
            var graph = solution.GetProjectDependencyGraph();

            // Note: there are a couple of approaches we can take here.  Processing 'direct'
            // dependencies, or processing 'transitive' dependencies.  Both have pros/cons:
            //
            // Direct Dependencies:
            //  Pros:
            //      All updated projects are able to see the newly added type.
            //      Transitive deps won't be updated to use a type they can't actually use.
            //  Cons:
            //      If that project then exports that new type, then transitive deps will
            //      break if they use those exported APIs since they won't know about the
            //      type.
            //
            // Transitive Dependencies:
            //  Pros:
            //      All affected code is updated.
            //  Cons: 
            //      Non-direct deps will not compile unless the take a reference on the
            //      starting project.

            var dependentProjects = graph.GetProjectsThatDirectlyDependOnThisProject(startingProject.Id);
            var allProjects = dependentProjects.Select<ProjectId, Project>(solution.GetProject)
                                               .Where(p => p.SupportsCompilation)
                                               .Concat(startingProject).ToSet();

            using var _ = ArrayBuilder<DocumentToUpdate>.GetInstance(out var result);
            var tupleFieldNames = tupleType.TupleElements.SelectAsArray<IFieldSymbol, string>(f => f.Name);

            foreach (var project in allProjects)
            {
                await AddDocumentsToUpdateForProjectAsync(
                    project, result, tupleFieldNames, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutable();
        }

        private static async Task<ImmutableArray<DocumentToUpdate>> GetDocumentsToUpdateForContainingProjectAsync(
            Project project, INamedTypeSymbol tupleType, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DocumentToUpdate>.GetInstance(out var result);
            var tupleFieldNames = tupleType.TupleElements.SelectAsArray<IFieldSymbol, string>(f => f.Name);

            await AddDocumentsToUpdateForProjectAsync(
                project, result, tupleFieldNames, cancellationToken).ConfigureAwait(false);

            return result.ToImmutable();
        }

        private static async Task AddDocumentsToUpdateForProjectAsync(Project project, ArrayBuilder<DocumentToUpdate> result, ImmutableArray<string> tupleFieldNames, CancellationToken cancellationToken)
        {
            foreach (var document in project.Documents)
            {
                var info = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
                if (info.ContainsTupleExpressionOrTupleType &&
                    InfoProbablyContainsTupleFieldNames(info, tupleFieldNames))
                {
                    // Use 'default' for nodesToUpdate so we walk the entire document
                    result.Add(new DocumentToUpdate(document, nodesToUpdate: default));
                }
            }
        }

        private static bool InfoProbablyContainsTupleFieldNames(SyntaxTreeIndex info, ImmutableArray<string> tupleFieldNames)
        {
            foreach (var name in tupleFieldNames)
            {
                if (!info.ProbablyContainsIdentifier(name))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<ImmutableArray<DocumentToUpdate>> GetDocumentsToUpdateForContainingTypeAsync(
            Document startingDocument, SyntaxNode tupleExprOrTypeNode, CancellationToken cancellationToken)
        {
            var containingType = tupleExprOrTypeNode.GetAncestor<TTypeBlockSyntax>();
            Debug.Assert(containingType != null,
                "We should always get a containing scope since we already checked for that to support Scope.ContainingType.");

            var solution = startingDocument.Project.Solution;
            var semanticModel = await startingDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(containingType, cancellationToken);

            using var _ = ArrayBuilder<DocumentToUpdate>.GetInstance(out var result);

            var declarationService = startingDocument.GetLanguageService<ISymbolDeclarationService>();
            foreach (var group in declarationService.GetDeclarations(typeSymbol).GroupBy(r => r.SyntaxTree))
            {
                var document = solution.GetDocument(group.Key);
                var nodes = group.SelectAsArray<SyntaxReference, SyntaxNode>(r => r.GetSyntax(cancellationToken));

                result.Add(new DocumentToUpdate(document, nodes));
            }

            return result.ToImmutable();
        }

        private static ImmutableArray<DocumentToUpdate> GetDocumentsToUpdateForContainingMember(
            Document document, SyntaxNode tupleExprOrTypeNode)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var containingMember = tupleExprOrTypeNode.FirstAncestorOrSelf<SyntaxNode, ISyntaxFactsService>((node, syntaxFacts) => syntaxFacts.IsMethodLevelMember(node), syntaxFacts) ?? tupleExprOrTypeNode;

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
                    autoInsertionLocation: false,
                    parseOptions: root.SyntaxTree.Options);

                return codeGenService.AddNamedType(
                    currentContainer, namedTypeSymbol, options, cancellationToken);
            });
        }

        private static async Task<Solution> ApplyChangesAsync(
            Document startingDocument, Dictionary<Document, SyntaxEditor> documentToEditorMap, CancellationToken cancellationToken)
        {
            var currentSolution = startingDocument.Project.Solution;

            foreach (var (currentDoc, editor) in documentToEditorMap)
            {
                var docId = currentDoc.Id;
                var newRoot = editor.GetChangedRoot();
                var updatedDocument = currentSolution.WithDocumentSyntaxRoot(docId, newRoot, PreservationMode.PreserveIdentity)
                                                     .GetDocument(docId);

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
            Document document, NamingRule parameterNamingRule, SyntaxEditor editor, SyntaxNode startingNode,
            INamedTypeSymbol tupleType, TNameSyntax fullyQualifiedStructName,
            string structName, ImmutableArray<ITypeParameterSymbol> typeParameters,
            SyntaxNode containerToUpdate, CancellationToken cancellationToken)
        {
            var changed = false;
            changed |= await ReplaceMatchingTupleExpressionsAsync(
                document, parameterNamingRule, editor, startingNode, tupleType,
                fullyQualifiedStructName, structName, typeParameters,
                containerToUpdate, cancellationToken).ConfigureAwait(false);

            changed |= await ReplaceMatchingTupleTypesAsync(
                document, editor, startingNode, tupleType,
                fullyQualifiedStructName, structName, typeParameters,
                containerToUpdate, cancellationToken).ConfigureAwait(false);

            return changed;
        }

        private async Task<bool> ReplaceMatchingTupleExpressionsAsync(
            Document document, NamingRule parameterNamingRule, SyntaxEditor editor, SyntaxNode startingNode,
            INamedTypeSymbol tupleType, TNameSyntax qualifiedTypeName,
            string typeName, ImmutableArray<ITypeParameterSymbol> typeParameters,
            SyntaxNode containingMember, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var comparer = syntaxFacts.StringComparer;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var childCreationNodes = containingMember.DescendantNodesAndSelf()
                                                     .OfType<TTupleExpressionSyntax>();

            var changed = false;
            foreach (var childCreation in childCreationNodes)
            {
                if (!(semanticModel.GetTypeInfo(childCreation, cancellationToken).Type is INamedTypeSymbol childType))
                {
                    Debug.Fail("We should always be able to get an tuple type for any tuple expression node.");
                    continue;
                }

                if (AreEquivalent(comparer, tupleType, childType))
                {
                    changed = true;
                    ReplaceWithObjectCreation(
                        editor, typeName, typeParameters, qualifiedTypeName, startingNode, childCreation, parameterNamingRule);
                }
            }

            return changed;
        }

        private static bool AreEquivalent(StringComparer comparer, INamedTypeSymbol tupleType, INamedTypeSymbol childType)
            => SymbolEquivalenceComparer.Instance.Equals(tupleType, childType) &&
               NamesMatch(comparer, tupleType.TupleElements, childType.TupleElements);

        private static bool NamesMatch(
            StringComparer comparer, ImmutableArray<IFieldSymbol> fields1, ImmutableArray<IFieldSymbol> fields2)
        {
            if (fields1.Length != fields2.Length)
            {
                return false;
            }

            for (var i = 0; i < fields1.Length; i++)
            {
                if (!comparer.Equals(fields1[i].Name, fields2[i].Name))
                {
                    return false;
                }
            }

            return true;
        }

        private void ReplaceWithObjectCreation(
            SyntaxEditor editor, string typeName, ImmutableArray<ITypeParameterSymbol> typeParameters,
            TNameSyntax qualifiedTypeName, SyntaxNode startingCreationNode, TTupleExpressionSyntax childCreation, NamingRule parameterNamingRule)
        {
            // Use the callback form as tuples types may be nested, and we want to
            // properly replace them even in that case.
            editor.ReplaceNode(
                childCreation,
                (currentNode, g) =>
                {
                    var currentTupleExpr = (TTupleExpressionSyntax)currentNode;

                    // If we hit the node the user started on, then add the rename annotation here.
                    var typeNameNode = startingCreationNode == childCreation
                        ? CreateStructNameNode(g, typeName, typeParameters, addRenameAnnotation: true)
                        : qualifiedTypeName;

                    var syntaxFacts = g.SyntaxFacts;
                    syntaxFacts.GetPartsOfTupleExpression<TArgumentSyntax>(
                        currentTupleExpr, out var openParen, out var arguments, out var closeParen);
                    arguments = ConvertArguments(g, parameterNamingRule, arguments);

                    return g.ObjectCreationExpression(typeNameNode, openParen, arguments, closeParen)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                });
        }

        private SeparatedSyntaxList<TArgumentSyntax> ConvertArguments(SyntaxGenerator generator, NamingRule parameterNamingRule, SeparatedSyntaxList<TArgumentSyntax> arguments)
            => generator.SeparatedList<TArgumentSyntax>(ConvertArguments(generator, parameterNamingRule, arguments.GetWithSeparators()));

        private SyntaxNodeOrTokenList ConvertArguments(SyntaxGenerator generator, NamingRule parameterNamingRule, SyntaxNodeOrTokenList list)
            => new SyntaxNodeOrTokenList(list.Select(v => ConvertArgumentOrToken(generator, parameterNamingRule, v)));

        private SyntaxNodeOrToken ConvertArgumentOrToken(SyntaxGenerator generator, NamingRule parameterNamingRule, SyntaxNodeOrToken arg)
            => arg.IsToken
                ? arg
                : ConvertArgument(generator, parameterNamingRule, (TArgumentSyntax)arg.AsNode());

        private TArgumentSyntax ConvertArgument(
            SyntaxGenerator generator, NamingRule parameterNamingRule, TArgumentSyntax argument)
        {
            // If the original arguments had names then we keep them, but convert the case to match the
            // the constructor parameters they now refer to. It helps keep the code self-documenting.
            // Remove for complex args as it's most likely just clutter a person doesn't need
            // when instantiating their new type.
            var expr = generator.SyntaxFacts.GetExpressionOfArgument(argument);
            if (expr is TLiteralExpressionSyntax)
            {
                var argumentName = generator.SyntaxFacts.GetNameForArgument(argument);
                var newArgumentName = GetConstructorParameterName(parameterNamingRule, argumentName);

                return GetArgumentWithChangedName(argument, newArgumentName);
            }

            return (TArgumentSyntax)generator.Argument(expr).WithTriviaFrom(argument);
        }

        private static async Task<bool> ReplaceMatchingTupleTypesAsync(
            Document document, SyntaxEditor editor, SyntaxNode startingNode,
            INamedTypeSymbol tupleType, TNameSyntax qualifiedTypeName,
            string typeName, ImmutableArray<ITypeParameterSymbol> typeParameters,
            SyntaxNode containingMember, CancellationToken cancellationToken)
        {
            var comparer = document.GetLanguageService<ISyntaxFactsService>().StringComparer;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var childTupleNodes = containingMember.DescendantNodesAndSelf()
                                                  .OfType<TTupleTypeSyntax>();

            var changed = false;
            foreach (var childTupleType in childTupleNodes)
            {
                if (!(semanticModel.GetTypeInfo(childTupleType, cancellationToken).Type is INamedTypeSymbol childType))
                {
                    Debug.Fail("We should always be able to get an tuple type for any tuple type syntax node.");
                    continue;
                }

                if (AreEquivalent(comparer, tupleType, childType))
                {
                    changed = true;
                    ReplaceWithTypeNode(
                        editor, typeName, typeParameters, qualifiedTypeName, startingNode, childTupleType);
                }
            }

            return changed;
        }

        private static void ReplaceWithTypeNode(
            SyntaxEditor editor, string typeName, ImmutableArray<ITypeParameterSymbol> typeParameters,
            TNameSyntax qualifiedTypeName, SyntaxNode startingNode, TTupleTypeSyntax childTupleType)
        {
            // Use the callback form as tuple types may be nested, and we want to
            // properly replace them even in that case.
            editor.ReplaceNode(
                childTupleType,
                (currentNode, g) =>
                {
                    // If we hit the node the user started on, then add the rename annotation here.
                    var typeNameNode = startingNode == childTupleType
                        ? CreateStructNameNode(g, typeName, typeParameters, addRenameAnnotation: true)
                        : qualifiedTypeName;

                    return typeNameNode.WithTriviaFrom(currentNode);
                });
        }

        private static async Task<INamedTypeSymbol> GenerateFinalNamedTypeAsync(
            Document document, Scope scope, string structName,
            ImmutableArray<ITypeParameterSymbol> typeParameters,
            INamedTypeSymbol tupleType, NamingRule parameterNamingRule, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var fields = tupleType.TupleElements;

            // Now try to generate all the members that will go in the new class. This is a bit
            // circular.  In order to generate some of the members, we need to know about the type.
            // But in order to create the type, we need the members.  To address this we do two
            // passes. First, we create an empty version of the class.  This can then be used to
            // help create members like Equals/GetHashCode.  Then, once we have all the members we
            // create the final type.
            var namedTypeWithoutMembers = CreateNamedType(
                semanticModel.Compilation.Assembly, scope, structName, typeParameters, members: default);

            var generator = SyntaxGenerator.GetGenerator(document);

            var constructor = CreateConstructor(semanticModel, structName, fields, generator, parameterNamingRule);

            // Generate Equals/GetHashCode.  We can defer to our existing language service for this
            // so that we generate the same Equals/GetHashCode that our other IDE features generate.
            var equalsAndGetHashCodeService = document.GetLanguageService<IGenerateEqualsAndGetHashCodeService>();

            var equalsMethod = await equalsAndGetHashCodeService.GenerateEqualsMethodAsync(
                document, namedTypeWithoutMembers, ImmutableArray<ISymbol>.CastUp(fields),
                localNameOpt: SyntaxGeneratorExtensions.OtherName, cancellationToken).ConfigureAwait(false);
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

            var namedTypeSymbol = CreateNamedType(semanticModel.Compilation.Assembly, scope, structName, typeParameters, members.ToImmutableAndFree());
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
            const string ValueName = "value";

            var valueNode = generator.IdentifierName(ValueName);
            var arguments = tupleType.TupleElements.SelectAsArray<IFieldSymbol, SyntaxNode>(
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
                CodeGenerationSymbolFactory.CreateParameterSymbol(structType, ValueName),
                isImplicit: true,
                ImmutableArray.Create(convertToTupleStatement)));
            members.Add(CodeGenerationSymbolFactory.CreateConversionSymbol(
                attributes: default,
                Accessibility.Public,
                DeclarationModifiers.Static,
                structType,
                CodeGenerationSymbolFactory.CreateParameterSymbol(tupleType, ValueName),
                isImplicit: true,
                ImmutableArray.Create(convertToStructStatement)));
        }

        private static INamedTypeSymbol CreateNamedType(
            IAssemblySymbol containingAssembly,
            Scope scope, string structName,
            ImmutableArray<ITypeParameterSymbol> typeParameters, ImmutableArray<ISymbol> members)
        {
            var accessibility = scope == Scope.DependentProjects
                ? Accessibility.Public
                : Accessibility.Internal;
            return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default, accessibility, modifiers: default,
                TypeKind.Struct, structName, typeParameters, members: members, containingAssembly: containingAssembly);
        }

        private static IMethodSymbol CreateConstructor(
            SemanticModel semanticModel, string className,
            ImmutableArray<IFieldSymbol> fields, SyntaxGenerator generator,
            NamingRule parameterNamingRule)
        {
            // For every property, create a corresponding parameter, as well as an assignment
            // statement from that parameter to the property.
            var parameterToPropMap = new Dictionary<string, ISymbol>();
            var parameters = fields.SelectAsArray<IFieldSymbol, IParameterSymbol>(field =>
            {
                var parameter = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    field.Type, GetConstructorParameterName(parameterNamingRule, field.Name));

                parameterToPropMap[parameter.Name] = field;

                return parameter;
            });

            var assignmentStatements = generator.CreateAssignmentStatements(
                semanticModel, parameters, parameterToPropMap, ImmutableDictionary<string, string>.Empty,
                addNullChecks: false, preferThrowExpression: false);

            var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default, Accessibility.Public, modifiers: default,
                className, parameters, assignmentStatements);

            return constructor;
        }

        private static string GetConstructorParameterName(NamingRule parameterNamingRule, string name)
            => parameterNamingRule.NamingStyle.MakeCompliant(name).First();

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
