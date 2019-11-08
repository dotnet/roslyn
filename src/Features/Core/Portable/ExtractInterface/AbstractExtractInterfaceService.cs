// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal abstract partial class AbstractExtractInterfaceService : ILanguageService
    {
        protected abstract Task<SyntaxNode> GetTypeDeclarationAsync(
            Document document,
            int position,
            TypeDiscoveryRule typeDiscoveryRule,
            CancellationToken cancellationToken);

        protected abstract Task<Solution> UpdateMembersWithExplicitImplementationsAsync(
            Solution unformattedSolution,
            IReadOnlyList<DocumentId> documentId,
            INamedTypeSymbol extractedInterfaceSymbol,
            INamedTypeSymbol typeToExtractFrom,
            IEnumerable<ISymbol> includedMembers,
            Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            CancellationToken cancellationToken);

        internal abstract string GetGeneratedNameTypeParameterSuffix(IList<ITypeParameterSymbol> typeParameters, Workspace workspace);

        internal abstract string GetContainingNamespaceDisplay(INamedTypeSymbol typeSymbol, CompilationOptions compilationOptions);

        internal abstract bool ShouldIncludeAccessibilityModifier(SyntaxNode typeNode);

        public async Task<ImmutableArray<ExtractInterfaceCodeAction>> GetExtractInterfaceCodeActionAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(document, span.Start, TypeDiscoveryRule.TypeNameOnly, cancellationToken).ConfigureAwait(false);

            return typeAnalysisResult.CanExtractInterface
                ? ImmutableArray.Create(new ExtractInterfaceCodeAction(this, typeAnalysisResult))
                : ImmutableArray<ExtractInterfaceCodeAction>.Empty;
        }

        public async Task<ExtractInterfaceResult> ExtractInterfaceAsync(
            Document documentWithTypeToExtractFrom,
            int position,
            Action<string, NotificationSeverity> errorHandler,
            CancellationToken cancellationToken)
        {
            var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(
                documentWithTypeToExtractFrom,
                position,
                TypeDiscoveryRule.TypeDeclaration,
                cancellationToken).ConfigureAwait(false);

            if (!typeAnalysisResult.CanExtractInterface)
            {
                errorHandler(typeAnalysisResult.ErrorMessage, NotificationSeverity.Error);
                return new ExtractInterfaceResult(succeeded: false);
            }

            return await ExtractInterfaceFromAnalyzedTypeAsync(typeAnalysisResult, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ExtractInterfaceTypeAnalysisResult> AnalyzeTypeAtPositionAsync(
            Document document,
            int position,
            TypeDiscoveryRule typeDiscoveryRule,
            CancellationToken cancellationToken)
        {
            var typeNode = await GetTypeDeclarationAsync(document, position, typeDiscoveryRule, cancellationToken).ConfigureAwait(false);
            if (typeNode == null)
            {
                var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_selection_is_not_inside_a_class_interface_struct;
                return new ExtractInterfaceTypeAnalysisResult(errorMessage);
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetDeclaredSymbol(typeNode, cancellationToken);
            if (type == null || type.Kind != SymbolKind.NamedType)
            {
                var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_selection_is_not_inside_a_class_interface_struct;
                return new ExtractInterfaceTypeAnalysisResult(errorMessage);
            }

            var typeToExtractFrom = type as INamedTypeSymbol;
            var extractableMembers = typeToExtractFrom.GetMembers().Where(IsExtractableMember);
            if (!extractableMembers.Any())
            {
                var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_type_does_not_contain_any_member_that_can_be_extracted_to_an_interface;
                return new ExtractInterfaceTypeAnalysisResult(errorMessage);
            }

            return new ExtractInterfaceTypeAnalysisResult(this, document, typeNode, typeToExtractFrom, extractableMembers);
        }

        public async Task<ExtractInterfaceResult> ExtractInterfaceFromAnalyzedTypeAsync(ExtractInterfaceTypeAnalysisResult refactoringResult, CancellationToken cancellationToken)
        {
            var containingNamespaceDisplay = refactoringResult.TypeToExtractFrom.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : refactoringResult.TypeToExtractFrom.ContainingNamespace.ToDisplayString();

            var extractInterfaceOptions = await GetExtractInterfaceOptionsAsync(
                refactoringResult.DocumentToExtractFrom,
                refactoringResult.TypeToExtractFrom,
                refactoringResult.ExtractableMembers,
                containingNamespaceDisplay,
                cancellationToken).ConfigureAwait(false);

            if (extractInterfaceOptions.IsCancelled)
            {
                return new ExtractInterfaceResult(succeeded: false);
            }

            return await ExtractInterfaceFromAnalyzedTypeAsync(refactoringResult, extractInterfaceOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ExtractInterfaceResult> ExtractInterfaceFromAnalyzedTypeAsync(
            ExtractInterfaceTypeAnalysisResult refactoringResult,
            ExtractInterfaceOptionsResult extractInterfaceOptions,
            CancellationToken cancellationToken)
        {
            var solution = refactoringResult.DocumentToExtractFrom.Project.Solution;

            var extractedInterfaceSymbol = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default,
                accessibility: ShouldIncludeAccessibilityModifier(refactoringResult.TypeNode) ? refactoringResult.TypeToExtractFrom.DeclaredAccessibility : Accessibility.NotApplicable,
                modifiers: new DeclarationModifiers(),
                typeKind: TypeKind.Interface,
                name: extractInterfaceOptions.InterfaceName,
                typeParameters: GetTypeParameters(refactoringResult.TypeToExtractFrom, extractInterfaceOptions.IncludedMembers),
                members: CreateInterfaceMembers(extractInterfaceOptions.IncludedMembers));

            switch (extractInterfaceOptions.Location)
            {
                case ExtractInterfaceOptionsResult.ExtractLocation.NewFile:
                    var containingNamespaceDisplay = GetContainingNamespaceDisplay(refactoringResult.TypeToExtractFrom, refactoringResult.DocumentToExtractFrom.Project.CompilationOptions);
                    return await ExtractInterfaceToNewFileAsync(
                        solution,
                        containingNamespaceDisplay,
                        extractedInterfaceSymbol,
                        refactoringResult,
                        extractInterfaceOptions,
                        cancellationToken).ConfigureAwait(false);

                case ExtractInterfaceOptionsResult.ExtractLocation.SameFile:
                    return await ExtractInterfaceToSameFileAsync(
                        solution,
                        refactoringResult,
                        extractedInterfaceSymbol,
                        extractInterfaceOptions,
                        cancellationToken).ConfigureAwait(false);

                default: throw new InvalidOperationException($"Unable to extract interface for operation of type {extractInterfaceOptions.GetType()}");
            }
        }

        private async Task<ExtractInterfaceResult> ExtractInterfaceToNewFileAsync(
            Solution solution, string containingNamespaceDisplay, INamedTypeSymbol extractedInterfaceSymbol,
            ExtractInterfaceTypeAnalysisResult refactoringResult, ExtractInterfaceOptionsResult extractInterfaceOptions, CancellationToken cancellationToken)
        {
            var symbolMapping = await CreateSymbolMappingAsync(
                extractInterfaceOptions.IncludedMembers,
                solution,
                refactoringResult.TypeNode,
                cancellationToken).ConfigureAwait(false);

            var syntaxFactsService = refactoringResult.DocumentToExtractFrom.GetLanguageService<ISyntaxFactsService>();
            var originalDocumentSyntaxRoot = await refactoringResult.DocumentToExtractFrom.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var fileBanner = syntaxFactsService.GetFileBanner(originalDocumentSyntaxRoot);

            var interfaceDocumentId = DocumentId.CreateNewId(refactoringResult.DocumentToExtractFrom.Project.Id, debugName: extractInterfaceOptions.FileName);

            var unformattedInterfaceDocument = await GetUnformattedInterfaceDocumentAsync(
                symbolMapping.AnnotatedSolution,
                containingNamespaceDisplay,
                extractInterfaceOptions.FileName,
                refactoringResult.DocumentToExtractFrom.Folders,
                extractedInterfaceSymbol,
                interfaceDocumentId,
                fileBanner,
                cancellationToken).ConfigureAwait(false);

            var completedUnformattedSolution = await GetSolutionWithOriginalTypeUpdatedAsync(
                unformattedInterfaceDocument.Project.Solution,
                symbolMapping.DocumentIds,
                refactoringResult.DocumentToExtractFrom.Id,
                symbolMapping.TypeNodeAnnotation,
                refactoringResult.TypeToExtractFrom,
                extractedInterfaceSymbol,
                extractInterfaceOptions.IncludedMembers,
                symbolMapping.SymbolToDeclarationAnnotationMap,
                cancellationToken).ConfigureAwait(false);

            var completedSolution = await GetFormattedSolutionAsync(
                completedUnformattedSolution,
                symbolMapping.DocumentIds.Concat(unformattedInterfaceDocument.Id),
                cancellationToken).ConfigureAwait(false);

            return new ExtractInterfaceResult(
                succeeded: true,
                updatedSolution: completedSolution,
                navigationDocumentId: interfaceDocumentId);
        }

        private async Task<ExtractInterfaceResult> ExtractInterfaceToSameFileAsync(
            Solution solution, ExtractInterfaceTypeAnalysisResult refactoringResult, INamedTypeSymbol extractedInterfaceSymbol,
            ExtractInterfaceOptionsResult extractInterfaceOptions, CancellationToken cancellationToken)
        {
            // Track all of the symbols we need to modify, which includes the original type declaration being modified
            var symbolMapping = await CreateSymbolMappingAsync(
                extractInterfaceOptions.IncludedMembers,
                solution,
                refactoringResult.TypeNode,
                cancellationToken).ConfigureAwait(false);

            var document = symbolMapping.AnnotatedSolution.GetDocument(refactoringResult.DocumentToExtractFrom.Id);
            var originalRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclaration = originalRoot.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).Single();

            var trackedDocument = document.WithSyntaxRoot(originalRoot.TrackNodes(typeDeclaration));

            var currentRoot = await trackedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(currentRoot, symbolMapping.AnnotatedSolution.Workspace);

            // Generate the interface syntax node, which will be inserted above the type it's extracted from
            var codeGenService = trackedDocument.GetLanguageService<ICodeGenerationService>();
            var interfaceNode = codeGenService.CreateNamedTypeDeclaration(extractedInterfaceSymbol)
                .WithAdditionalAnnotations(SimplificationHelpers.SimplifyModuleNameAnnotation);

            typeDeclaration = currentRoot.GetCurrentNode(typeDeclaration);
            editor.InsertBefore(typeDeclaration, interfaceNode);

            var unformattedSolution = document.WithSyntaxRoot(editor.GetChangedRoot()).Project.Solution;

            // After the interface is inserted, update the original type to show it implements the new interface
            var unformattedSolutionWithUpdatedType = await GetSolutionWithOriginalTypeUpdatedAsync(
                unformattedSolution, symbolMapping.DocumentIds,
                refactoringResult.DocumentToExtractFrom.Id, symbolMapping.TypeNodeAnnotation,
                refactoringResult.TypeToExtractFrom, extractedInterfaceSymbol,
                extractInterfaceOptions.IncludedMembers, symbolMapping.SymbolToDeclarationAnnotationMap, cancellationToken).ConfigureAwait(false);

            var completedSolution = await GetFormattedSolutionAsync(
                unformattedSolutionWithUpdatedType,
                symbolMapping.DocumentIds.Concat(refactoringResult.DocumentToExtractFrom.Id),
                cancellationToken).ConfigureAwait(false);

            return new ExtractInterfaceResult(
                succeeded: true,
                updatedSolution: completedSolution,
                navigationDocumentId: refactoringResult.DocumentToExtractFrom.Id);
        }

        private async Task<SymbolMapping> CreateSymbolMappingAsync(
                IEnumerable<ISymbol> includedMembers,
                Solution solution,
                SyntaxNode typeNode,
                CancellationToken cancellationToken)
        {
            var symbolToDeclarationAnnotationMap = new Dictionary<ISymbol, SyntaxAnnotation>();
            var currentRoots = new Dictionary<SyntaxTree, SyntaxNode>();
            var documentIds = new List<DocumentId>();

            var typeNodeRoot = await typeNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var typeNodeAnnotation = new SyntaxAnnotation();
            currentRoots[typeNode.SyntaxTree] = typeNodeRoot.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(typeNodeAnnotation));
            documentIds.Add(solution.GetDocument(typeNode.SyntaxTree).Id);

            foreach (var includedMember in includedMembers)
            {
                var location = includedMember.Locations.Single();
                var tree = location.SourceTree;
                if (!currentRoots.TryGetValue(tree, out var root))
                {
                    root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    documentIds.Add(solution.GetDocument(tree).Id);
                }

                var token = root.FindToken(location.SourceSpan.Start);

                var annotation = new SyntaxAnnotation();
                symbolToDeclarationAnnotationMap.Add(includedMember, annotation);
                currentRoots[tree] = root.ReplaceToken(token, token.WithAdditionalAnnotations(annotation));
            }

            var annotatedSolution = solution;
            foreach (var root in currentRoots)
            {
                var document = annotatedSolution.GetDocument(root.Key);
                annotatedSolution = document.WithSyntaxRoot(root.Value).Project.Solution;
            }

            return new SymbolMapping(symbolToDeclarationAnnotationMap, annotatedSolution, documentIds, typeNodeAnnotation);
        }

        internal Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            Document document,
            INamedTypeSymbol type,
            IEnumerable<ISymbol> extractableMembers,
            string containingNamespace,
            CancellationToken cancellationToken)
        {
            var conflictingTypeNames = type.ContainingNamespace.GetAllTypes(cancellationToken).Select(t => t.Name);
            var candidateInterfaceName = type.TypeKind == TypeKind.Interface ? type.Name : "I" + type.Name;
            var defaultInterfaceName = NameGenerator.GenerateUniqueName(candidateInterfaceName, name => !conflictingTypeNames.Contains(name));
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
            var generatedNameTypeParameterSuffix = GetGeneratedNameTypeParameterSuffix(GetTypeParameters(type, extractableMembers), document.Project.Solution.Workspace);

            var service = document.Project.Solution.Workspace.Services.GetService<IExtractInterfaceOptionsService>();
            return service.GetExtractInterfaceOptionsAsync(
                syntaxFactsService,
                notificationService,
                extractableMembers.ToList(),
                defaultInterfaceName,
                conflictingTypeNames.ToList(),
                containingNamespace,
                generatedNameTypeParameterSuffix,
                document.Project.Language);
        }

        private async Task<Document> GetUnformattedInterfaceDocumentAsync(
            Solution solution,
            string containingNamespaceDisplay,
            string name,
            IEnumerable<string> folders,
            INamedTypeSymbol extractedInterfaceSymbol,
            DocumentId interfaceDocumentId,
            ImmutableArray<SyntaxTrivia> fileBanner,
            CancellationToken cancellationToken)
        {
            var solutionWithInterfaceDocument = solution.AddDocument(interfaceDocumentId, name, text: "", folders: folders);
            var interfaceDocument = solutionWithInterfaceDocument.GetDocument(interfaceDocumentId);
            var interfaceDocumentSemanticModel = await interfaceDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var namespaceParts = containingNamespaceDisplay.Split('.').Where(s => !string.IsNullOrEmpty(s));
            var unformattedInterfaceDocument = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                interfaceDocument.Project.Solution,
                interfaceDocumentSemanticModel.GetEnclosingNamespace(0, cancellationToken),
                extractedInterfaceSymbol.GenerateRootNamespaceOrType(namespaceParts.ToArray()),
                options: new CodeGenerationOptions(contextLocation: interfaceDocumentSemanticModel.SyntaxTree.GetLocation(new TextSpan()), generateMethodBodies: false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var syntaxRoot = await unformattedInterfaceDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return unformattedInterfaceDocument.WithSyntaxRoot(syntaxRoot.WithPrependedLeadingTrivia(fileBanner));
        }

        private async Task<Solution> GetFormattedSolutionAsync(Solution unformattedSolution, IEnumerable<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            // Since code action performs formatting and simplification on a single document, 
            // this ensures that anything marked with formatter or simplifier annotations gets 
            // correctly handled as long as it it's in the listed documents
            var formattedSolution = unformattedSolution;
            foreach (var documentId in documentIds)
            {
                var document = formattedSolution.GetDocument(documentId);
                var formattedDocument = await Formatter.FormatAsync(
                    document,
                    Formatter.Annotation,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var simplifiedDocument = await Simplifier.ReduceAsync(
                    formattedDocument,
                    Simplifier.Annotation,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                formattedSolution = simplifiedDocument.Project.Solution;
            }

            return formattedSolution;
        }

        private async Task<Solution> GetSolutionWithOriginalTypeUpdatedAsync(
            Solution solution,
            List<DocumentId> documentIds,
            DocumentId invocationLocationDocumentId,
            SyntaxAnnotation typeNodeAnnotation,
            INamedTypeSymbol typeToExtractFrom,
            INamedTypeSymbol extractedInterfaceSymbol,
            IEnumerable<ISymbol> includedMembers,
            Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            CancellationToken cancellationToken)
        {
            // If an interface "INewInterface" is extracted from an interface "IExistingInterface",
            // then "INewInterface" is not marked as implementing "IExistingInterface" and its 
            // extracted members are also not updated.
            if (typeToExtractFrom.TypeKind == TypeKind.Interface)
            {
                return solution;
            }

            var unformattedSolution = solution;
            foreach (var documentId in documentIds)
            {
                var document = solution.GetDocument(documentId);
                var currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(currentRoot, solution.Workspace);

                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var typeReference = syntaxGenerator.TypeExpression(extractedInterfaceSymbol);

                var typeDeclaration = currentRoot.GetAnnotatedNodes(typeNodeAnnotation).SingleOrDefault();

                if (typeDeclaration == null)
                {
                    continue;
                }

                var unformattedTypeDeclaration = syntaxGenerator.AddInterfaceType(typeDeclaration, typeReference).WithAdditionalAnnotations(Formatter.Annotation);
                editor.ReplaceNode(typeDeclaration, unformattedTypeDeclaration);

                unformattedSolution = document.WithSyntaxRoot(editor.GetChangedRoot()).Project.Solution;

                // Only update the first instance of the typedeclaration,
                // since it's not needed in all declarations
                break;
            }

            var updatedUnformattedSolution = await UpdateMembersWithExplicitImplementationsAsync(
                unformattedSolution,
                documentIds,
                extractedInterfaceSymbol,
                typeToExtractFrom,
                includedMembers,
                symbolToDeclarationAnnotationMap,
                cancellationToken).ConfigureAwait(false);

            return updatedUnformattedSolution;
        }

        private ImmutableArray<ISymbol> CreateInterfaceMembers(IEnumerable<ISymbol> includedMembers)
        {
            var interfaceMembers = ArrayBuilder<ISymbol>.GetInstance();

            foreach (var member in includedMembers)
            {
                switch (member.Kind)
                {
                    case SymbolKind.Event:
                        var @event = member as IEventSymbol;
                        interfaceMembers.Add(CodeGenerationSymbolFactory.CreateEventSymbol(
                            attributes: ImmutableArray<AttributeData>.Empty,
                            accessibility: Accessibility.Public,
                            modifiers: new DeclarationModifiers(isAbstract: true),
                            type: @event.Type,
                            explicitInterfaceImplementations: default,
                            name: @event.Name));
                        break;
                    case SymbolKind.Method:
                        var method = member as IMethodSymbol;
                        interfaceMembers.Add(CodeGenerationSymbolFactory.CreateMethodSymbol(
                            attributes: ImmutableArray<AttributeData>.Empty,
                            accessibility: Accessibility.Public,
                            modifiers: new DeclarationModifiers(isAbstract: true, isUnsafe: method.IsUnsafe()),
                            returnType: method.ReturnType,
                            refKind: method.RefKind,
                            explicitInterfaceImplementations: default,
                            name: method.Name,
                            typeParameters: method.TypeParameters,
                            parameters: method.Parameters));
                        break;
                    case SymbolKind.Property:
                        var property = member as IPropertySymbol;
                        interfaceMembers.Add(CodeGenerationSymbolFactory.CreatePropertySymbol(
                            attributes: ImmutableArray<AttributeData>.Empty,
                            accessibility: Accessibility.Public,
                            modifiers: new DeclarationModifiers(isAbstract: true, isUnsafe: property.IsUnsafe()),
                            type: property.Type,
                            refKind: property.RefKind,
                            explicitInterfaceImplementations: default,
                            name: property.Name,
                            parameters: property.Parameters,
                            getMethod: property.GetMethod == null ? null : (property.GetMethod.DeclaredAccessibility == Accessibility.Public ? property.GetMethod : null),
                            setMethod: property.SetMethod == null ? null : (property.SetMethod.DeclaredAccessibility == Accessibility.Public ? property.SetMethod : null),
                            isIndexer: property.IsIndexer));
                        break;
                    default:
                        Debug.Assert(false, string.Format(FeaturesResources.Unexpected_interface_member_kind_colon_0, member.Kind.ToString()));
                        break;
                }
            }

            return interfaceMembers.ToImmutableAndFree();
        }

        internal virtual bool IsExtractableMember(ISymbol m)
        {
            if (m.IsStatic ||
                m.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            if (m.Kind == SymbolKind.Event || m.IsOrdinaryMethod())
            {
                return true;
            }

            if (m.Kind == SymbolKind.Property)
            {
                var prop = m as IPropertySymbol;
                return (prop is { GetMethod: { DeclaredAccessibility: Accessibility.Public } }) ||
                    (prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public);
            }

            return false;
        }

        private ImmutableArray<ITypeParameterSymbol> GetTypeParameters(INamedTypeSymbol type, IEnumerable<ISymbol> includedMembers)
        {
            var potentialTypeParameters = GetPotentialTypeParameters(type);

            var directlyReferencedTypeParameters = GetDirectlyReferencedTypeParameters(potentialTypeParameters, includedMembers);

            // The directly referenced TypeParameters may have constraints that reference other 
            // type parameters.

            var allReferencedTypeParameters = new HashSet<ITypeParameterSymbol>(directlyReferencedTypeParameters);
            var unanalyzedTypeParameters = new Queue<ITypeParameterSymbol>(directlyReferencedTypeParameters);

            while (!unanalyzedTypeParameters.IsEmpty())
            {
                var typeParameter = unanalyzedTypeParameters.Dequeue();

                foreach (var constraint in typeParameter.ConstraintTypes)
                {
                    foreach (var originalTypeParameter in potentialTypeParameters)
                    {
                        if (!allReferencedTypeParameters.Contains(originalTypeParameter) &&
                            DoesTypeReferenceTypeParameter(constraint, originalTypeParameter, new HashSet<ITypeSymbol>()))
                        {
                            allReferencedTypeParameters.Add(originalTypeParameter);
                            unanalyzedTypeParameters.Enqueue(originalTypeParameter);
                        }
                    }
                }
            }

            return potentialTypeParameters.Where(allReferencedTypeParameters.Contains).ToImmutableArray();
        }

        private List<ITypeParameterSymbol> GetPotentialTypeParameters(INamedTypeSymbol type)
        {
            var typeParameters = new List<ITypeParameterSymbol>();

            var typesToVisit = new Stack<INamedTypeSymbol>();

            var currentType = type;
            while (currentType != null)
            {
                typesToVisit.Push(currentType);
                currentType = currentType.ContainingType;
            }

            while (typesToVisit.Any())
            {
                typeParameters.AddRange(typesToVisit.Pop().TypeParameters);
            }

            return typeParameters;
        }

        private IList<ITypeParameterSymbol> GetDirectlyReferencedTypeParameters(IEnumerable<ITypeParameterSymbol> potentialTypeParameters, IEnumerable<ISymbol> includedMembers)
        {
            var directlyReferencedTypeParameters = new List<ITypeParameterSymbol>();
            foreach (var typeParameter in potentialTypeParameters)
            {
                if (includedMembers.Any(m => DoesMemberReferenceTypeParameter(m, typeParameter, new HashSet<ITypeSymbol>())))
                {
                    directlyReferencedTypeParameters.Add(typeParameter);
                }
            }

            return directlyReferencedTypeParameters;
        }

        private bool DoesMemberReferenceTypeParameter(ISymbol member, ITypeParameterSymbol typeParameter, HashSet<ITypeSymbol> checkedTypes)
        {
            switch (member.Kind)
            {
                case SymbolKind.Event:
                    var @event = member as IEventSymbol;
                    return DoesTypeReferenceTypeParameter(@event.Type, typeParameter, checkedTypes);
                case SymbolKind.Method:
                    var method = member as IMethodSymbol;
                    return method.Parameters.Any(t => DoesTypeReferenceTypeParameter(t.Type, typeParameter, checkedTypes)) ||
                        method.TypeParameters.Any(t => t.ConstraintTypes.Any(c => DoesTypeReferenceTypeParameter(c, typeParameter, checkedTypes))) ||
                        DoesTypeReferenceTypeParameter(method.ReturnType, typeParameter, checkedTypes);
                case SymbolKind.Property:
                    var property = member as IPropertySymbol;
                    return property.Parameters.Any(t => DoesTypeReferenceTypeParameter(t.Type, typeParameter, checkedTypes)) ||
                        DoesTypeReferenceTypeParameter(property.Type, typeParameter, checkedTypes);
                default:
                    Debug.Assert(false, string.Format(FeaturesResources.Unexpected_interface_member_kind_colon_0, member.Kind.ToString()));
                    return false;
            }
        }

        private bool DoesTypeReferenceTypeParameter(ITypeSymbol type, ITypeParameterSymbol typeParameter, HashSet<ITypeSymbol> checkedTypes)
        {
            if (!checkedTypes.Add(type))
            {
                return false;
            }

            // We want to ignore nullability when comparing as T and T? both are references to the type parameter
            if (type.Equals(typeParameter, SymbolEqualityComparer.Default) ||
                type.GetTypeArguments().Any(t => DoesTypeReferenceTypeParameter(t, typeParameter, checkedTypes)))
            {
                return true;
            }

            if (type.ContainingType != null &&
                type.Kind != SymbolKind.TypeParameter &&
                DoesTypeReferenceTypeParameter(type.ContainingType, typeParameter, checkedTypes))
            {
                return true;
            }

            return false;
        }
    }
}
