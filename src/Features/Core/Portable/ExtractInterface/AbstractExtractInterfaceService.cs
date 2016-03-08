// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal abstract class AbstractExtractInterfaceService : ILanguageService
    {
        internal abstract Task<SyntaxNode> GetTypeDeclarationAsync(
            Document document,
            int position,
            TypeDiscoveryRule typeDiscoveryRule,
            CancellationToken cancellationToken);

        internal abstract Solution GetSolutionWithUpdatedOriginalType(
            Solution solutionWithFormattedInterfaceDocument,
            INamedTypeSymbol extractedInterfaceSymbol,
            IEnumerable<ISymbol> includedMembers,
            Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            List<DocumentId> documentIds,
            SyntaxAnnotation typeNodeAnnotation,
            DocumentId documentIdWithTypeNode,
            CancellationToken cancellationToken);

        internal abstract string GetGeneratedNameTypeParameterSuffix(IList<ITypeParameterSymbol> typeParameters, Workspace workspace);

        internal abstract string GetContainingNamespaceDisplay(INamedTypeSymbol typeSymbol, CompilationOptions compilationOptions);

        internal abstract bool ShouldIncludeAccessibilityModifier(SyntaxNode typeNode);

        public async Task<IEnumerable<ExtractInterfaceCodeAction>> GetExtractInterfaceCodeActionAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(document, span.Start, TypeDiscoveryRule.TypeNameOnly, cancellationToken).ConfigureAwait(false);

            return typeAnalysisResult.CanExtractInterface
                ? SpecializedCollections.SingletonEnumerable(new ExtractInterfaceCodeAction(this, typeAnalysisResult))
                : SpecializedCollections.EmptyEnumerable<ExtractInterfaceCodeAction>();
        }

        public ExtractInterfaceResult ExtractInterface(
            Document documentWithTypeToExtractFrom,
            int position,
            Action<string, NotificationSeverity> errorHandler,
            CancellationToken cancellationToken)
        {
            var typeAnalysisResult = AnalyzeTypeAtPositionAsync(documentWithTypeToExtractFrom, position, TypeDiscoveryRule.TypeDeclaration, cancellationToken).WaitAndGetResult(cancellationToken);

            if (!typeAnalysisResult.CanExtractInterface)
            {
                errorHandler(typeAnalysisResult.ErrorMessage, NotificationSeverity.Error);
                return new ExtractInterfaceResult(succeeded: false);
            }

            return ExtractInterfaceFromAnalyzedType(typeAnalysisResult, cancellationToken);
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
                var errorMessage = FeaturesResources.CouldNotExtractInterfaceSelection;
                return new ExtractInterfaceTypeAnalysisResult(errorMessage);
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetDeclaredSymbol(typeNode, cancellationToken);
            if (type == null || type.Kind != SymbolKind.NamedType)
            {
                var errorMessage = FeaturesResources.CouldNotExtractInterfaceSelection;
                return new ExtractInterfaceTypeAnalysisResult(errorMessage);
            }

            var typeToExtractFrom = type as INamedTypeSymbol;
            var extractableMembers = typeToExtractFrom.GetMembers().Where(IsExtractableMember);
            if (!extractableMembers.Any())
            {
                var errorMessage = FeaturesResources.CouldNotExtractInterfaceTypeMember;
                return new ExtractInterfaceTypeAnalysisResult(errorMessage);
            }

            return new ExtractInterfaceTypeAnalysisResult(this, document, typeNode, typeToExtractFrom, extractableMembers);
        }

        public ExtractInterfaceResult ExtractInterfaceFromAnalyzedType(ExtractInterfaceTypeAnalysisResult refactoringResult, CancellationToken cancellationToken)
        {
            var containingNamespaceDisplay = refactoringResult.TypeToExtractFrom.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : refactoringResult.TypeToExtractFrom.ContainingNamespace.ToDisplayString();

            var extractInterfaceOptions = GetExtractInterfaceOptions(
                refactoringResult.DocumentToExtractFrom,
                refactoringResult.TypeToExtractFrom,
                refactoringResult.ExtractableMembers,
                containingNamespaceDisplay,
                cancellationToken);

            if (extractInterfaceOptions.IsCancelled)
            {
                return new ExtractInterfaceResult(succeeded: false);
            }

            return ExtractInterfaceFromAnalyzedType(refactoringResult, extractInterfaceOptions, cancellationToken);
        }

        public ExtractInterfaceResult ExtractInterfaceFromAnalyzedType(ExtractInterfaceTypeAnalysisResult refactoringResult, ExtractInterfaceOptionsResult extractInterfaceOptions, CancellationToken cancellationToken)
        {
            var solution = refactoringResult.DocumentToExtractFrom.Project.Solution;
            List<DocumentId> documentIds;
            SyntaxAnnotation typeNodeSyntaxAnnotation;

            var containingNamespaceDisplay = GetContainingNamespaceDisplay(refactoringResult.TypeToExtractFrom, refactoringResult.DocumentToExtractFrom.Project.CompilationOptions);

            var symbolToDeclarationAnnotationMap = CreateSymbolToDeclarationAnnotationMap(
                extractInterfaceOptions.IncludedMembers,
                ref solution,
                out documentIds,
                refactoringResult.TypeNode,
                out typeNodeSyntaxAnnotation,
                cancellationToken);

            var extractedInterfaceSymbol = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: null,
                accessibility: ShouldIncludeAccessibilityModifier(refactoringResult.TypeNode) ? refactoringResult.TypeToExtractFrom.DeclaredAccessibility : Accessibility.NotApplicable,
                modifiers: new DeclarationModifiers(),
                typeKind: TypeKind.Interface,
                name: extractInterfaceOptions.InterfaceName,
                typeParameters: GetTypeParameters(refactoringResult.TypeToExtractFrom, extractInterfaceOptions.IncludedMembers),
                members: CreateInterfaceMembers(extractInterfaceOptions.IncludedMembers));

            var interfaceDocumentId = DocumentId.CreateNewId(refactoringResult.DocumentToExtractFrom.Project.Id, debugName: extractInterfaceOptions.FileName);

            var unformattedInterfaceDocument = GetUnformattedInterfaceDocument(
                solution,
                containingNamespaceDisplay,
                extractInterfaceOptions.FileName,
                refactoringResult.DocumentToExtractFrom.Folders,
                extractedInterfaceSymbol,
                interfaceDocumentId,
                cancellationToken);

            var solutionWithFormattedInterfaceDocument = GetSolutionWithFormattedInterfaceDocument(unformattedInterfaceDocument, cancellationToken);

            var completedSolution = GetSolutionWithOriginalTypeUpdated(
                solutionWithFormattedInterfaceDocument,
                documentIds,
                refactoringResult.DocumentToExtractFrom.Id,
                typeNodeSyntaxAnnotation,
                refactoringResult.TypeToExtractFrom,
                extractedInterfaceSymbol,
                extractInterfaceOptions.IncludedMembers,
                symbolToDeclarationAnnotationMap,
                cancellationToken);

            return new ExtractInterfaceResult(
                succeeded: true,
                updatedSolution: completedSolution,
                navigationDocumentId: interfaceDocumentId);
        }

        private Dictionary<ISymbol, SyntaxAnnotation> CreateSymbolToDeclarationAnnotationMap(
            IEnumerable<ISymbol> includedMembers,
            ref Solution solution,
            out List<DocumentId> documentIds,
            SyntaxNode typeNode,
            out SyntaxAnnotation typeNodeAnnotation,
            CancellationToken cancellationToken)
        {
            var symbolToDeclarationAnnotationMap = new Dictionary<ISymbol, SyntaxAnnotation>();
            var currentRoots = new Dictionary<SyntaxTree, SyntaxNode>();
            documentIds = new List<DocumentId>();

            var typeNodeRoot = typeNode.SyntaxTree.GetRoot(CancellationToken.None);
            typeNodeAnnotation = new SyntaxAnnotation();
            currentRoots[typeNode.SyntaxTree] = typeNodeRoot.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(typeNodeAnnotation));
            documentIds.Add(solution.GetDocument(typeNode.SyntaxTree).Id);

            foreach (var includedMember in includedMembers)
            {
                var location = includedMember.Locations.Single();
                var tree = location.SourceTree;

                SyntaxNode root;
                if (!currentRoots.TryGetValue(tree, out root))
                {
                    root = tree.GetRoot(cancellationToken);
                    documentIds.Add(solution.GetDocument(tree).Id);
                }

                var token = root.FindToken(location.SourceSpan.Start);

                var annotation = new SyntaxAnnotation();
                symbolToDeclarationAnnotationMap.Add(includedMember, annotation);
                currentRoots[tree] = root.ReplaceToken(token, token.WithAdditionalAnnotations(annotation));
            }

            foreach (var root in currentRoots)
            {
                var document = solution.GetDocument(root.Key);
                solution = solution.WithDocumentSyntaxRoot(document.Id, root.Value, PreservationMode.PreserveIdentity);
            }

            return symbolToDeclarationAnnotationMap;
        }

        internal ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
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
            return service.GetExtractInterfaceOptions(
                syntaxFactsService,
                notificationService,
                extractableMembers.ToList(),
                defaultInterfaceName,
                conflictingTypeNames.ToList(),
                containingNamespace,
                generatedNameTypeParameterSuffix,
                document.Project.Language);
        }

        private Document GetUnformattedInterfaceDocument(
            Solution solution,
            string containingNamespaceDisplay,
            string name,
            IEnumerable<string> folders,
            INamedTypeSymbol extractedInterfaceSymbol,
            DocumentId interfaceDocumentId,
            CancellationToken cancellationToken)
        {
            var solutionWithInterfaceDocument = solution.AddDocument(interfaceDocumentId, name, text: "", folders: folders);
            var interfaceDocument = solutionWithInterfaceDocument.GetDocument(interfaceDocumentId);
            var interfaceDocumentSemanticModel = interfaceDocument.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var namespaceParts = containingNamespaceDisplay.Split('.').Where(s => !string.IsNullOrEmpty(s));
            var unformattedInterfaceDocument = CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                interfaceDocument.Project.Solution,
                interfaceDocumentSemanticModel.GetEnclosingNamespace(0, cancellationToken),
                extractedInterfaceSymbol.GenerateRootNamespaceOrType(namespaceParts.ToArray()),
                options: new CodeGenerationOptions(interfaceDocumentSemanticModel.SyntaxTree.GetLocation(new TextSpan())),
                cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

            return unformattedInterfaceDocument;
        }

        private static Solution GetSolutionWithFormattedInterfaceDocument(Document unformattedInterfaceDocument, CancellationToken cancellationToken)
        {
            Solution solutionWithInterfaceDocument;
            var formattedRoot = Formatter.Format(unformattedInterfaceDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken), unformattedInterfaceDocument.Project.Solution.Workspace, cancellationToken: cancellationToken);
            var rootToSimplify = formattedRoot.WithAdditionalAnnotations(Simplifier.Annotation);
            var finalInterfaceDocument = Simplifier.ReduceAsync(unformattedInterfaceDocument.WithSyntaxRoot(rootToSimplify), cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

            solutionWithInterfaceDocument = finalInterfaceDocument.Project.Solution;
            return solutionWithInterfaceDocument;
        }

        private Solution GetSolutionWithOriginalTypeUpdated(
            Solution solutionWithFormattedInterfaceDocument,
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
                return solutionWithFormattedInterfaceDocument;
            }

            var formattedSolution = GetSolutionWithUpdatedOriginalType(
                solutionWithFormattedInterfaceDocument,
                extractedInterfaceSymbol,
                includedMembers,
                symbolToDeclarationAnnotationMap,
                documentIds,
                typeNodeAnnotation,
                invocationLocationDocumentId,
                cancellationToken);

            foreach (var docId in documentIds)
            {
                var formattedDoc = Formatter.FormatAsync(
                    formattedSolution.GetDocument(docId),
                    Formatter.Annotation,
                    cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

                formattedSolution = formattedDoc.Project.Solution;
            }

            return formattedSolution;
        }

        private IList<ISymbol> CreateInterfaceMembers(IEnumerable<ISymbol> includedMembers)
        {
            var interfaceMembers = new List<ISymbol>();

            foreach (var member in includedMembers)
            {
                switch (member.Kind)
                {
                    case SymbolKind.Event:
                        var @event = member as IEventSymbol;
                        interfaceMembers.Add(CodeGenerationSymbolFactory.CreateEventSymbol(
                            attributes: SpecializedCollections.EmptyList<AttributeData>(),
                            accessibility: Accessibility.Public,
                            modifiers: new DeclarationModifiers(isAbstract: true),
                            type: @event.Type,
                            explicitInterfaceSymbol: null,
                            name: @event.Name));
                        break;
                    case SymbolKind.Method:
                        var method = member as IMethodSymbol;
                        interfaceMembers.Add(CodeGenerationSymbolFactory.CreateMethodSymbol(
                            attributes: SpecializedCollections.EmptyList<AttributeData>(),
                            accessibility: Accessibility.Public,
                            modifiers: new DeclarationModifiers(isAbstract: true, isUnsafe: method.IsUnsafe()),
                            returnType: method.ReturnType,
                            explicitInterfaceSymbol: null,
                            name: method.Name,
                            typeParameters: method.TypeParameters,
                            parameters: method.Parameters));
                        break;
                    case SymbolKind.Property:
                        var property = member as IPropertySymbol;
                        interfaceMembers.Add(CodeGenerationSymbolFactory.CreatePropertySymbol(
                            attributes: SpecializedCollections.EmptyList<AttributeData>(),
                            accessibility: Accessibility.Public,
                            modifiers: new DeclarationModifiers(isAbstract: true, isUnsafe: property.IsUnsafe()),
                            type: property.Type,
                            explicitInterfaceSymbol: null,
                            name: property.Name,
                            parameters: property.Parameters,
                            getMethod: property.GetMethod == null ? null : (property.GetMethod.DeclaredAccessibility == Accessibility.Public ? property.GetMethod : null),
                            setMethod: property.SetMethod == null ? null : (property.SetMethod.DeclaredAccessibility == Accessibility.Public ? property.SetMethod : null),
                            isIndexer: property.IsIndexer));
                        break;
                    default:
                        Debug.Assert(false, string.Format(FeaturesResources.UnexpectedInterfaceMemberKind, member.Kind.ToString()));
                        break;
                }
            }

            return interfaceMembers;
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
                return (prop.GetMethod != null && prop.GetMethod.DeclaredAccessibility == Accessibility.Public) ||
                    (prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public);
            }

            return false;
        }

        private IList<ITypeParameterSymbol> GetTypeParameters(INamedTypeSymbol type, IEnumerable<ISymbol> includedMembers)
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

            return potentialTypeParameters.Where(p => allReferencedTypeParameters.Contains(p)).ToList();
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
                    Debug.Assert(false, string.Format(FeaturesResources.UnexpectedInterfaceMemberKind, member.Kind.ToString()));
                    return false;
            }
        }

        private bool DoesTypeReferenceTypeParameter(ITypeSymbol type, ITypeParameterSymbol typeParameter, HashSet<ITypeSymbol> checkedTypes)
        {
            if (!checkedTypes.Add(type))
            {
                return false;
            }

            if (type == typeParameter ||
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
