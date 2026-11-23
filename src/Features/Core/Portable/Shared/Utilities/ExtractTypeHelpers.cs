// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static class ExtractTypeHelpers
{
    public static async Task<(Document containingDocument, SyntaxAnnotation typeAnnotation)> AddTypeToExistingFileAsync(Document document, INamedTypeSymbol newType, AnnotatedSymbolMapping symbolMapping, CancellationToken cancellationToken)
    {
        var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var typeDeclaration = originalRoot.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).Single();
        var editor = new SyntaxEditor(originalRoot, symbolMapping.AnnotatedSolution.Services);

        var context = new CodeGenerationContext(generateMethodBodies: true);
        var info = await document.GetCodeGenerationInfoAsync(context, cancellationToken).ConfigureAwait(false);

        var newTypeNode = info.Service.CreateNamedTypeDeclaration(newType, CodeGenerationDestination.Unspecified, info, cancellationToken)
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
        Document hintDocument,
        CancellationToken cancellationToken)
    {
        var newDocumentId = DocumentId.CreateNewId(projectId, debugName: fileName);

        // The file name suggested for the new type may already exist in the project
        // (for example, the user previously created a file with that name). Adding a
        // second document with the same path would silently overwrite the existing
        // file's contents when the refactoring is applied, so pick a file name that
        // isn't in use instead. See https://github.com/dotnet/roslyn/issues/61471.
        fileName = GetUniqueFileName(solution, projectId, hintDocument.FilePath, fileName);
        var newDocumentPath = PathUtilities.CombinePaths(PathUtilities.GetDirectoryName(hintDocument.FilePath), fileName);

        var solutionWithNewDocument = solution.AddDocument(newDocumentId, fileName, text: "", folders: folders, filePath: newDocumentPath);
        var newDocument = solutionWithNewDocument.GetRequiredDocument(newDocumentId);

        var newSemanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var context = new CodeGenerationContext(
            contextLocation: newSemanticModel.SyntaxTree.GetLocation(new TextSpan()),
            generateMethodBodies: true);

        // need to remove the root namespace from the containing namespace display because it is implied
        // For C# this does nothing as there is no root namespace (root namespace is empty string)
        var generateTypeService = newDocument.GetRequiredLanguageService<IGenerateTypeService>();
        var rootNamespace = generateTypeService.GetRootNamespace(newDocument.Project.CompilationOptions);
        var index = rootNamespace.IsEmpty() ? -1 : containingNamespaceDisplay.IndexOf(rootNamespace);
        // if we did find the root namespace as the first element, then we remove it
        // this may leave us with an extra "." character at the start, but when we split it shouldn't matter
        var namespaceWithoutRoot = index == 0
            ? containingNamespaceDisplay.Remove(index, rootNamespace.Length)
            : containingNamespaceDisplay;

        var namespaceParts = namespaceWithoutRoot.Split('.').Where(s => !string.IsNullOrEmpty(s));
        var newTypeDocument = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
            new CodeGenerationSolutionContext(
                newDocument.Project.Solution,
                context),
            newSemanticModel.GetEnclosingNamespace(0, cancellationToken),
            newSymbol.GenerateRootNamespaceOrType([.. namespaceParts]),
            cancellationToken).ConfigureAwait(false);

        var newCleanupOptions = await newTypeDocument.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);

        var formattingService = newTypeDocument.GetLanguageService<INewDocumentFormattingService>();
        if (formattingService is not null)
        {
            newTypeDocument = await formattingService.FormatNewDocumentAsync(newTypeDocument, hintDocument, newCleanupOptions, cancellationToken).ConfigureAwait(false);
        }

        var syntaxRoot = await newTypeDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var typeAnnotation = new SyntaxAnnotation();
        var syntaxFacts = newTypeDocument.GetRequiredLanguageService<ISyntaxFactsService>();

        var declarationNode = syntaxRoot.DescendantNodes().First(syntaxFacts.IsTypeDeclaration);
        var annotatedRoot = syntaxRoot.ReplaceNode(declarationNode, declarationNode.WithAdditionalAnnotations(typeAnnotation));

        newTypeDocument = newTypeDocument.WithSyntaxRoot(annotatedRoot);

        var simplified = await Simplifier.ReduceAsync(newTypeDocument, newCleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var formattedDocument = await Formatter.FormatAsync(simplified, newCleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

        return (formattedDocument, typeAnnotation);
    }

    /// <summary>
    /// Returns <paramref name="fileName"/> when no document in the project already uses
    /// that file name in the directory of <paramref name="hintFilePath"/>; otherwise
    /// returns the first unused name of the form <c>Name1.cs</c>, <c>Name2.cs</c>, ...
    /// </summary>
    private static string GetUniqueFileName(Solution solution, ProjectId projectId, string hintFilePath, string fileName)
    {
        var directoryName = PathUtilities.GetDirectoryName(hintFilePath);

        // Collect the file names already used in this folder once. The new name
        // just has to avoid being one of those names.
        var fileNamesInFolder = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var project = solution.GetProject(projectId);
        if (project is not null)
        {
            foreach (var document in project.Documents)
            {
                var filePath = document.FilePath;
                if (filePath is not null &&
                    string.Equals(PathUtilities.GetDirectoryName(filePath), directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    fileNamesInFolder.Add(PathUtilities.GetFileName(filePath));
                }
            }
        }

        var extensionIndex = fileName.LastIndexOf('.');
        var nameWithoutExtension = extensionIndex >= 0 ? fileName[..extensionIndex] : fileName;
        var extension = extensionIndex >= 0 ? fileName[extensionIndex..] : string.Empty;

        return NameGenerator.GenerateUniqueName(nameWithoutExtension, extension, candidate => !fileNamesInFolder.Contains(candidate));
    }

    public static string GetTypeParameterSuffix(
        Document document, SyntaxFormattingOptions formattingOptions, INamedTypeSymbol type, ImmutableArray<ISymbol> extractableMembers, CancellationToken cancellationToken)
    {
        var typeParameters = GetRequiredTypeParametersForMembers(type, extractableMembers);

        if (type.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        var typeParameterNames = typeParameters.SelectAsArray(p => p.Name);
        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);

        return Formatter.Format(syntaxGenerator.SyntaxGeneratorInternal.TypeParameterList(typeParameterNames), document.Project.Solution.Services, formattingOptions, cancellationToken).ToString();
    }

    public static ImmutableArray<ITypeParameterSymbol> GetRequiredTypeParametersForMembers(
        INamedTypeSymbol type, ImmutableArray<ISymbol> includedMembers)
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
                        DoesTypeReferenceTypeParameter(constraint, originalTypeParameter, []))
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

        return typeParameters.ToImmutableAndClear();
    }

    private static ImmutableArray<ITypeParameterSymbol> GetDirectlyReferencedTypeParameters(
        ImmutableArray<ITypeParameterSymbol> potentialTypeParameters, ImmutableArray<ISymbol> includedMembers)
    {
        using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var directlyReferencedTypeParameters);
        foreach (var typeParameter in potentialTypeParameters)
        {
            if (includedMembers.Any(m => DoesMemberReferenceTypeParameter(m, typeParameter, [])))
            {
                directlyReferencedTypeParameters.Add(typeParameter);
            }
        }

        return directlyReferencedTypeParameters.ToImmutableAndClear();
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
                return method.Parameters.Any(static (t, arg) => DoesTypeReferenceTypeParameter(t.Type, arg.typeParameter, arg.checkedTypes), (typeParameter, checkedTypes)) ||
                    method.TypeParameters.Any(static (t, arg) => t.ConstraintTypes.Any(static (c, arg) => DoesTypeReferenceTypeParameter(c, arg.typeParameter, arg.checkedTypes), (arg.typeParameter, arg.checkedTypes)), (typeParameter, checkedTypes)) ||
                    DoesTypeReferenceTypeParameter(method.ReturnType, typeParameter, checkedTypes);
            case SymbolKind.Property:
                var property = member as IPropertySymbol;
                return property.Parameters.Any(static (t, arg) => DoesTypeReferenceTypeParameter(t.Type, arg.typeParameter, arg.checkedTypes), (typeParameter, checkedTypes)) ||
                    DoesTypeReferenceTypeParameter(property.Type, typeParameter, checkedTypes);
            case SymbolKind.Field:
                var field = member as IFieldSymbol;
                return DoesTypeReferenceTypeParameter(field.Type, typeParameter, checkedTypes);
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
            type.GetTypeArguments().Any(static (t, arg) => DoesTypeReferenceTypeParameter(t, arg.typeParameter, arg.checkedTypes), (typeParameter, checkedTypes)))
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
