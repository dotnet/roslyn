// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class ExtractTypeHelpers
    {
        public static async Task<(Document containingDocument, SyntaxAnnotation typeAnnotation)> AddTypeToExistingFileAsync(Document document, INamedTypeSymbol newType, AnnotatedSymbolMapping symbolMapping, CancellationToken cancellationToken)
        {
            var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclaration = originalRoot.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).Single();
            var editor = new SyntaxEditor(originalRoot, symbolMapping.AnnotatedSolution.Workspace);

            var options = new CodeGenerationOptions(
                generateMethodBodies: true,
                options: await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false));
            var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var newTypeNode = codeGenService.CreateNamedTypeDeclaration(newType, options: options, cancellationToken: cancellationToken)
                .WithAdditionalAnnotations(SimplificationHelpers.SimplifyModuleNameAnnotation);

            var typeAnnotation = new SyntaxAnnotation();
            newTypeNode = newTypeNode.WithAdditionalAnnotations(typeAnnotation);

            editor.InsertBefore(typeDeclaration, newTypeNode);

            var newDocument = document.WithSyntaxRoot(editor.GetChangedRoot());
            return (newDocument, typeAnnotation);
        }

        public static async Task<(Document containingDocument, SyntaxAnnotation typeAnnotation)> AddTypeToNewFileAsync(
            Solution solution,
            string containingNamespaceDisplay,
            string fileName,
            ProjectId projectId,
            IEnumerable<string> folders,
            INamedTypeSymbol newSymbol,
            ImmutableArray<SyntaxTrivia> fileBanner,
            CancellationToken cancellationToken)
        {
            var newDocumentId = DocumentId.CreateNewId(projectId, debugName: fileName);
            var solutionWithInterfaceDocument = solution.AddDocument(newDocumentId, fileName, text: "", folders: folders);
            var newDocument = solutionWithInterfaceDocument.GetRequiredDocument(newDocumentId);
            var newSemanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var options = new CodeGenerationOptions(
                contextLocation: newSemanticModel.SyntaxTree.GetLocation(new TextSpan()),
                generateMethodBodies: true,
                options: await newDocument.GetOptionsAsync(cancellationToken).ConfigureAwait(false));

            var namespaceParts = containingNamespaceDisplay.Split('.').Where(s => !string.IsNullOrEmpty(s));
            var newTypeDocument = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                newDocument.Project.Solution,
                newSemanticModel.GetEnclosingNamespace(0, cancellationToken),
                newSymbol.GenerateRootNamespaceOrType(namespaceParts.ToArray()),
                options: options,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var syntaxRoot = await newTypeDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rootWithBanner = syntaxRoot.WithPrependedLeadingTrivia(fileBanner);

            var typeAnnotation = new SyntaxAnnotation();
            var syntaxFacts = newTypeDocument.GetRequiredLanguageService<ISyntaxFactsService>();

            var declarationNode = rootWithBanner.DescendantNodes().First(syntaxFacts.IsTypeDeclaration);
            var annotatedRoot = rootWithBanner.ReplaceNode(declarationNode, declarationNode.WithAdditionalAnnotations(typeAnnotation));

            newTypeDocument = newTypeDocument.WithSyntaxRoot(annotatedRoot);

            var simplified = await Simplifier.ReduceAsync(newTypeDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            var formattedDocument = await Formatter.FormatAsync(simplified).ConfigureAwait(false);

            return (formattedDocument, typeAnnotation);
        }

        public static string GetTypeParameterSuffix(Document document, INamedTypeSymbol type, IEnumerable<ISymbol> extractableMembers)
        {
            var typeParameters = GetRequiredTypeParametersForMembers(type, extractableMembers);

            if (type.TypeParameters.Length == 0)
            {
                return string.Empty;
            }

            var typeParameterNames = typeParameters.SelectAsArray(p => p.Name);
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);

            return Formatter.Format(syntaxGenerator.SyntaxGeneratorInternal.TypeParameterList(typeParameterNames), document.Project.Solution.Workspace).ToString();
        }

        public static ImmutableArray<ITypeParameterSymbol> GetRequiredTypeParametersForMembers(INamedTypeSymbol type, IEnumerable<ISymbol> includedMembers)
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

            return potentialTypeParameters.WhereAsArray(allReferencedTypeParameters.Contains);
        }

        private static ImmutableArray<ITypeParameterSymbol> GetPotentialTypeParameters(INamedTypeSymbol type)
        {
            using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var typeParameters);

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

            return typeParameters.ToImmutable();
        }

        private static ImmutableArray<ITypeParameterSymbol> GetDirectlyReferencedTypeParameters(IEnumerable<ITypeParameterSymbol> potentialTypeParameters, IEnumerable<ISymbol> includedMembers)
        {
            using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var directlyReferencedTypeParameters);
            foreach (var typeParameter in potentialTypeParameters)
            {
                if (includedMembers.Any(m => DoesMemberReferenceTypeParameter(m, typeParameter, new HashSet<ITypeSymbol>())))
                {
                    directlyReferencedTypeParameters.Add(typeParameter);
                }
            }

            return directlyReferencedTypeParameters.ToImmutable();
        }

        private static bool DoesMemberReferenceTypeParameter(ISymbol member, ITypeParameterSymbol typeParameter, HashSet<ITypeSymbol> checkedTypes)
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

        private static bool DoesTypeReferenceTypeParameter(ITypeSymbol type, ITypeParameterSymbol typeParameter, HashSet<ITypeSymbol> checkedTypes)
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
