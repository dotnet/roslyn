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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
    {
        protected abstract bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

        private partial class Editor
        {
            private readonly TService _service;
            private TargetProjectChangeInLanguage _targetProjectChangeInLanguage = TargetProjectChangeInLanguage.NoChange;
            private IGenerateTypeService _targetLanguageService;

            private readonly SemanticDocument _semanticDocument;
            private readonly State _state;
            private readonly bool _intoNamespace;
            private readonly bool _inNewFile;
            private readonly bool _fromDialog;
            private readonly GenerateTypeOptionsResult _generateTypeOptionsResult;
            private readonly CancellationToken _cancellationToken;
            private readonly CodeAndImportGenerationOptionsProvider _fallbackOptions;

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                CodeAndImportGenerationOptionsProvider fallbackOptions,
                bool intoNamespace,
                bool inNewFile,
                CancellationToken cancellationToken)
            {
                _service = service;
                _semanticDocument = document;
                _state = state;
                _fallbackOptions = fallbackOptions;
                _intoNamespace = intoNamespace;
                _inNewFile = inNewFile;
                _cancellationToken = cancellationToken;
            }

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                CodeAndImportGenerationOptionsProvider fallbackOptions,
                bool fromDialog,
                GenerateTypeOptionsResult generateTypeOptionsResult,
                CancellationToken cancellationToken)
            {
                // the document comes from the same snapshot as the project
                Contract.ThrowIfFalse(document.Project.Solution == generateTypeOptionsResult.Project.Solution);

                _service = service;
                _semanticDocument = document;
                _state = state;
                _fallbackOptions = fallbackOptions;
                _fromDialog = fromDialog;
                _generateTypeOptionsResult = generateTypeOptionsResult;
                _cancellationToken = cancellationToken;
            }

            private enum TargetProjectChangeInLanguage
            {
                NoChange,
                CSharpToVisualBasic,
                VisualBasicToCSharp
            }

            internal async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync()
            {
                // Check to see if it is from GFU Dialog
                if (!_fromDialog)
                {
                    // Generate the actual type declaration.
                    var namedType = await GenerateNamedTypeAsync().ConfigureAwait(false);

                    if (_intoNamespace)
                    {
                        if (_inNewFile)
                        {
                            // Generating into a new file is somewhat complicated.
                            var documentName = GetTypeName(_state) + _service.DefaultFileExtension;

                            return await GetGenerateInNewFileOperationsAsync(
                                namedType,
                                documentName,
                                null,
                                true,
                                null,
                                _semanticDocument.Project,
                                _semanticDocument.Project,
                                isDialog: false).ConfigureAwait(false);
                        }
                        else
                        {
                            return await GetGenerateIntoContainingNamespaceOperationsAsync(namedType).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        return await GetGenerateIntoTypeOperationsAsync(namedType).ConfigureAwait(false);
                    }
                }
                else
                {
                    var namedType = await GenerateNamedTypeAsync(_generateTypeOptionsResult).ConfigureAwait(false);

                    // Honor the options from the dialog
                    // Check to see if the type is requested to be generated in cross language Project
                    // e.g.: C# -> VB or VB -> C#
                    if (_semanticDocument.Project.Language != _generateTypeOptionsResult.Project.Language)
                    {
                        _targetProjectChangeInLanguage =
                            _generateTypeOptionsResult.Project.Language == LanguageNames.CSharp
                            ? TargetProjectChangeInLanguage.VisualBasicToCSharp
                            : TargetProjectChangeInLanguage.CSharpToVisualBasic;

                        // Get the cross language service
                        _targetLanguageService = _generateTypeOptionsResult.Project.LanguageServices.GetService<IGenerateTypeService>();
                    }

                    if (_generateTypeOptionsResult.IsNewFile)
                    {
                        return await GetGenerateInNewFileOperationsAsync(
                            namedType,
                            _generateTypeOptionsResult.NewFileName,
                            _generateTypeOptionsResult.Folders,
                            _generateTypeOptionsResult.AreFoldersValidIdentifiers,
                            _generateTypeOptionsResult.FullFilePath,
                            _generateTypeOptionsResult.Project,
                            _semanticDocument.Project,
                            isDialog: true).ConfigureAwait(false);
                    }
                    else
                    {
                        return await GetGenerateIntoExistingDocumentAsync(
                            namedType,
                            _semanticDocument.Project,
                            _generateTypeOptionsResult,
                            isDialog: true).ConfigureAwait(false);
                    }
                }
            }

            private string GetNamespaceToGenerateInto()
            {
                var namespaceToGenerateInto = _state.NamespaceToGenerateInOpt.Trim();
                var rootNamespace = _service.GetRootNamespace(_semanticDocument.SemanticModel.Compilation.Options).Trim();
                if (!string.IsNullOrWhiteSpace(rootNamespace))
                {
                    if (namespaceToGenerateInto == rootNamespace ||
                        namespaceToGenerateInto.StartsWith(rootNamespace + ".", StringComparison.Ordinal))
                    {
                        namespaceToGenerateInto = namespaceToGenerateInto[rootNamespace.Length..];
                    }
                }

                return namespaceToGenerateInto;
            }

            private string GetNamespaceToGenerateIntoForUsageWithNamespace(Project targetProject, Project triggeringProject)
            {
                var namespaceToGenerateInto = _state.NamespaceToGenerateInOpt.Trim();

                if (targetProject.Language == LanguageNames.CSharp ||
                    targetProject == triggeringProject)
                {
                    // If the target project is C# project then we don't have to make any modification to the namespace
                    // or
                    // This is a VB project generation into itself which requires no change as well
                    return namespaceToGenerateInto;
                }

                // If the target Project is VB then we have to check if the RootNamespace of the VB project is the parent most namespace of the type being generated
                // True, Remove the RootNamespace
                // False, Add Global to the Namespace
                Debug.Assert(targetProject.Language == LanguageNames.VisualBasic);
                IGenerateTypeService targetLanguageService;
                if (_semanticDocument.Project.Language == LanguageNames.VisualBasic)
                {
                    targetLanguageService = _service;
                }
                else
                {
                    Debug.Assert(_targetLanguageService != null);
                    targetLanguageService = _targetLanguageService;
                }

                var rootNamespace = targetLanguageService.GetRootNamespace(targetProject.CompilationOptions).Trim();
                if (!string.IsNullOrWhiteSpace(rootNamespace))
                {
                    var rootNamespaceLength = CheckIfRootNamespacePresentInNamespace(namespaceToGenerateInto, rootNamespace);
                    if (rootNamespaceLength > -1)
                    {
                        // True, Remove the RootNamespace
                        namespaceToGenerateInto = namespaceToGenerateInto[rootNamespaceLength..];
                    }
                    else
                    {
                        // False, Add Global to the Namespace
                        namespaceToGenerateInto = AddGlobalDotToTheNamespace(namespaceToGenerateInto);
                    }
                }
                else
                {
                    // False, Add Global to the Namespace
                    namespaceToGenerateInto = AddGlobalDotToTheNamespace(namespaceToGenerateInto);
                }

                return namespaceToGenerateInto;
            }

            private static string AddGlobalDotToTheNamespace(string namespaceToBeGenerated)
                => "Global." + namespaceToBeGenerated;

            // Returns the length of the meaningful rootNamespace substring part of namespaceToGenerateInto
            private static int CheckIfRootNamespacePresentInNamespace(string namespaceToGenerateInto, string rootNamespace)
            {
                if (namespaceToGenerateInto == rootNamespace)
                {
                    return rootNamespace.Length;
                }

                if (namespaceToGenerateInto.StartsWith(rootNamespace + ".", StringComparison.Ordinal))
                {
                    return rootNamespace.Length + 1;
                }

                return -1;
            }

            private static void AddFoldersToNamespaceContainers(List<string> container, IList<string> folders)
            {
                // Add the folder as part of the namespace if there are not empty
                if (folders != null && folders.Count != 0)
                {
                    // Remove the empty entries and replace the spaces in the folder name to '_'
                    var refinedFolders = folders.Where(n => n != null && !n.IsEmpty()).Select(n => n.Replace(' ', '_')).ToArray();
                    container.AddRange(refinedFolders);
                }
            }

            private async Task<IEnumerable<CodeActionOperation>> GetGenerateInNewFileOperationsAsync(
                INamedTypeSymbol namedType,
                string documentName,
                IList<string> folders,
                bool areFoldersValidIdentifiers,
                string fullFilePath,
                Project projectToBeUpdated,
                Project triggeringProject,
                bool isDialog)
            {
                // First, we fork the solution with a new, empty, file in it.  
                var newDocumentId = DocumentId.CreateNewId(projectToBeUpdated.Id, debugName: documentName);
                var newSolution = projectToBeUpdated.Solution.AddDocument(newDocumentId, documentName, string.Empty, folders, fullFilePath);

                // Now we get the semantic model for that file we just added.  We do that to get the
                // root namespace in that new document, along with location for that new namespace.
                // That way, when we use the code gen service we can say "add this symbol to the
                // root namespace" and it will pick the one in the new file.
                var newDocument = newSolution.GetDocument(newDocumentId);
                var newSemanticModel = await newDocument.GetSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                var enclosingNamespace = newSemanticModel.GetEnclosingNamespace(0, _cancellationToken);

                var namespaceContainersAndUsings = GetNamespaceContainersAndAddUsingsOrImport(
                    isDialog, folders, areFoldersValidIdentifiers, projectToBeUpdated, triggeringProject);

                var containers = namespaceContainersAndUsings.containers;
                var includeUsingsOrImports = namespaceContainersAndUsings.usingOrImport;

                var rootNamespaceOrType = namedType.GenerateRootNamespaceOrType(containers);

                // Now, actually ask the code gen service to add this namespace or type to the root
                // namespace in the new file.  This will properly generate the code, and add any
                // additional niceties like imports/usings.
                var codeGenResult = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                    new CodeGenerationSolutionContext(
                        newSolution,
                        new CodeGenerationContext(newSemanticModel.SyntaxTree.GetLocation(new TextSpan())),
                        _fallbackOptions),
                    enclosingNamespace,
                    rootNamespaceOrType,
                    _cancellationToken).ConfigureAwait(false);

                // containers is determined to be
                // 1: folders -> if triggered from Dialog
                // 2: containers -> if triggered not from a Dialog but from QualifiedName
                // 3: triggering document folder structure -> if triggered not from a Dialog and a SimpleName
                var adjustedContainer = isDialog
                    ? folders
                    : _state.SimpleName != _state.NameOrMemberAccessExpression
                        ? containers.ToList()
                        : _semanticDocument.Document.Folders.ToList();

                if (newDocument.Project.Language == _semanticDocument.Document.Project.Language)
                {
                    var formattingService = newDocument.GetLanguageService<INewDocumentFormattingService>();
                    if (formattingService is not null)
                    {
                        // TODO: fallback options: https://github.com/dotnet/roslyn/issues/60794
                        var cleanupOptions = await codeGenResult.GetCodeCleanupOptionsAsync(fallbackOptions: null, _cancellationToken).ConfigureAwait(false);
                        codeGenResult = await formattingService.FormatNewDocumentAsync(codeGenResult, _semanticDocument.Document, cleanupOptions, _cancellationToken).ConfigureAwait(false);
                    }
                }

                // Now, take the code that would be generated and actually create an edit that would
                // produce a document with that code in it.
                var newRoot = await codeGenResult.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);

                return await CreateAddDocumentAndUpdateUsingsOrImportsOperationsAsync(
                    projectToBeUpdated,
                    triggeringProject,
                    documentName,
                    newRoot,
                    includeUsingsOrImports,
                    adjustedContainer,
                    SourceCodeKind.Regular,
                    _cancellationToken).ConfigureAwait(false);
            }

            private async Task<IEnumerable<CodeActionOperation>> CreateAddDocumentAndUpdateUsingsOrImportsOperationsAsync(
                Project projectToBeUpdated,
                Project triggeringProject,
                string documentName,
                SyntaxNode root,
                string includeUsingsOrImports,
                IList<string> containers,
                SourceCodeKind sourceCodeKind,
                CancellationToken cancellationToken)
            {
                // TODO(cyrusn): make sure documentId is unique.
                var documentId = DocumentId.CreateNewId(projectToBeUpdated.Id, documentName);

                var updatedSolution = projectToBeUpdated.Solution.AddDocument(DocumentInfo.Create(
                    documentId,
                    documentName,
                    containers,
                    sourceCodeKind));

                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(documentId, root, PreservationMode.PreserveIdentity);

                // Update the Generating Document with a using if required
                if (includeUsingsOrImports != null)
                {
                    updatedSolution = await _service.TryAddUsingsOrImportToDocumentAsync(updatedSolution, null, _semanticDocument.Document, _state.SimpleName, includeUsingsOrImports, cancellationToken).ConfigureAwait(false);
                }

                // Add reference of the updated project to the triggering Project if they are 2 different projects
                updatedSolution = AddProjectReference(projectToBeUpdated, triggeringProject, updatedSolution);

                return new CodeActionOperation[] { new ApplyChangesOperation(updatedSolution), new OpenDocumentOperation(documentId) };
            }

            private static Solution AddProjectReference(Project projectToBeUpdated, Project triggeringProject, Solution updatedSolution)
            {
                if (projectToBeUpdated != triggeringProject)
                {
                    if (!triggeringProject.ProjectReferences.Any(pr => pr.ProjectId == projectToBeUpdated.Id))
                    {
                        updatedSolution = updatedSolution.AddProjectReference(triggeringProject.Id, new ProjectReference(projectToBeUpdated.Id));
                    }
                }

                return updatedSolution;
            }

            private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoContainingNamespaceOperationsAsync(INamedTypeSymbol namedType)
            {
                var enclosingNamespace = _semanticDocument.SemanticModel.GetEnclosingNamespace(
                    _state.SimpleName.SpanStart, _cancellationToken);

                var solution = _semanticDocument.Project.Solution;
                var codeGenResult = await CodeGenerator.AddNamedTypeDeclarationAsync(
                    new CodeGenerationSolutionContext(
                        solution,
                        new CodeGenerationContext(afterThisLocation: _semanticDocument.SyntaxTree.GetLocation(_state.SimpleName.Span)),
                        _fallbackOptions),
                    enclosingNamespace,
                    namedType,
                    _cancellationToken).ConfigureAwait(false);

                return new CodeActionOperation[] { new ApplyChangesOperation(codeGenResult.Project.Solution) };
            }

            private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoExistingDocumentAsync(
                INamedTypeSymbol namedType,
                Project triggeringProject,
                GenerateTypeOptionsResult generateTypeOptionsResult,
                bool isDialog)
            {
                var root = await generateTypeOptionsResult.ExistingDocument.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                var folders = generateTypeOptionsResult.ExistingDocument.Folders;

                var namespaceContainersAndUsings = GetNamespaceContainersAndAddUsingsOrImport(isDialog, new List<string>(folders), generateTypeOptionsResult.AreFoldersValidIdentifiers, generateTypeOptionsResult.Project, triggeringProject);

                var containers = namespaceContainersAndUsings.containers;
                var includeUsingsOrImports = namespaceContainersAndUsings.usingOrImport;

                (INamespaceSymbol, INamespaceOrTypeSymbol, Location) enclosingNamespaceGeneratedTypeToAddAndLocation;
                if (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange)
                {
                    enclosingNamespaceGeneratedTypeToAddAndLocation = await _service.GetOrGenerateEnclosingNamespaceSymbolAsync(
                        namedType,
                        containers,
                        generateTypeOptionsResult.ExistingDocument,
                        root,
                        _cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    enclosingNamespaceGeneratedTypeToAddAndLocation = await _targetLanguageService.GetOrGenerateEnclosingNamespaceSymbolAsync(
                        namedType,
                        containers,
                        generateTypeOptionsResult.ExistingDocument,
                        root,
                        _cancellationToken).ConfigureAwait(false);
                }

                var solution = _semanticDocument.Project.Solution;
                var codeGenResult = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                    new CodeGenerationSolutionContext(
                        solution,
                        new CodeGenerationContext(afterThisLocation: enclosingNamespaceGeneratedTypeToAddAndLocation.Item3),
                        _fallbackOptions),
                    enclosingNamespaceGeneratedTypeToAddAndLocation.Item1,
                    enclosingNamespaceGeneratedTypeToAddAndLocation.Item2,
                    _cancellationToken).ConfigureAwait(false);
                var newRoot = await codeGenResult.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                var updatedSolution = solution.WithDocumentSyntaxRoot(generateTypeOptionsResult.ExistingDocument.Id, newRoot, PreservationMode.PreserveIdentity);

                // Update the Generating Document with a using if required
                if (includeUsingsOrImports != null)
                {
                    updatedSolution = await _service.TryAddUsingsOrImportToDocumentAsync(
                                        updatedSolution,
                                        generateTypeOptionsResult.ExistingDocument.Id == _semanticDocument.Document.Id ? newRoot : null,
                                        _semanticDocument.Document,
                                        _state.SimpleName,
                                        includeUsingsOrImports,
                                        _cancellationToken).ConfigureAwait(false);
                }

                updatedSolution = AddProjectReference(generateTypeOptionsResult.Project, triggeringProject, updatedSolution);

                return new CodeActionOperation[] { new ApplyChangesOperation(updatedSolution) };
            }

            private (string[] containers, string usingOrImport) GetNamespaceContainersAndAddUsingsOrImport(
                bool isDialog,
                IList<string> folders,
                bool areFoldersValidIdentifiers,
                Project targetProject,
                Project triggeringProject)
            {
                string includeUsingsOrImports = null;
                if (!areFoldersValidIdentifiers)
                {
                    folders = SpecializedCollections.EmptyList<string>();
                }

                // Now actually create the symbol that we want to add to the root namespace.  The
                // symbol may either be a named type (if we're not generating into a namespace) or
                // it may be a namespace symbol.
                string[] containers = null;
                if (!isDialog)
                {
                    // Not generated from the Dialog 
                    containers = GetNamespaceToGenerateInto().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (!_service.IsSimpleName(_state.NameOrMemberAccessExpression))
                {
                    // If the usage was with a namespace
                    containers = GetNamespaceToGenerateIntoForUsageWithNamespace(targetProject, triggeringProject).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    // Generated from the Dialog
                    var containerList = new List<string>();

                    var rootNamespaceOfTheProjectGeneratedInto =
                        _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange
                            ? _service.GetRootNamespace(_generateTypeOptionsResult.Project.CompilationOptions).Trim()
                            : _targetLanguageService.GetRootNamespace(_generateTypeOptionsResult.Project.CompilationOptions).Trim();

                    var defaultNamespace = _generateTypeOptionsResult.DefaultNamespace;

                    // Case 1 : If the type is generated into the same C# project or
                    // Case 2 : If the type is generated from a C# project to a C# Project
                    // Case 3 : If the Type is generated from a VB Project to a C# Project
                    // Using and Namespace will be the DefaultNamespace + Folder Structure
                    if ((_semanticDocument.Project == _generateTypeOptionsResult.Project && _semanticDocument.Project.Language == LanguageNames.CSharp) ||
                        (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange && _generateTypeOptionsResult.Project.Language == LanguageNames.CSharp) ||
                        _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.VisualBasicToCSharp)
                    {
                        if (!string.IsNullOrWhiteSpace(defaultNamespace))
                        {
                            containerList.Add(defaultNamespace);
                        }

                        // Populate the ContainerList
                        AddFoldersToNamespaceContainers(containerList, folders);

                        containers = containerList.ToArray();
                        includeUsingsOrImports = string.Join(".", containerList.ToArray());
                    }

                    // Case 4 : If the type is generated into the same VB project or
                    // Case 5 : If Type is generated from a VB Project to VB Project
                    // Case 6 : If Type is generated from a C# Project to VB Project 
                    // Namespace will be Folder Structure and Import will have the RootNamespace of the project generated into as part of the Imports
                    if ((_semanticDocument.Project == _generateTypeOptionsResult.Project && _semanticDocument.Project.Language == LanguageNames.VisualBasic) ||
                        (_semanticDocument.Project != _generateTypeOptionsResult.Project && _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange && _generateTypeOptionsResult.Project.Language == LanguageNames.VisualBasic) ||
                        _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.CSharpToVisualBasic)
                    {
                        // Populate the ContainerList
                        AddFoldersToNamespaceContainers(containerList, folders);
                        containers = containerList.ToArray();
                        includeUsingsOrImports = string.Join(".", containerList.ToArray());
                        if (!string.IsNullOrWhiteSpace(rootNamespaceOfTheProjectGeneratedInto))
                        {
                            includeUsingsOrImports = string.IsNullOrEmpty(includeUsingsOrImports) ?
                                                     rootNamespaceOfTheProjectGeneratedInto :
                                                     rootNamespaceOfTheProjectGeneratedInto + "." + includeUsingsOrImports;
                        }
                    }

                    Debug.Assert(includeUsingsOrImports != null);
                }

                return (containers, includeUsingsOrImports);
            }

            private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoTypeOperationsAsync(INamedTypeSymbol namedType)
            {
                var solution = _semanticDocument.Project.Solution;
                var codeGenResult = await CodeGenerator.AddNamedTypeDeclarationAsync(
                    new CodeGenerationSolutionContext(
                        solution,
                        new CodeGenerationContext(contextLocation: _state.SimpleName.GetLocation()),
                        _fallbackOptions),
                    _state.TypeToGenerateInOpt,
                    namedType,
                    _cancellationToken)
                    .ConfigureAwait(false);

                return new CodeActionOperation[] { new ApplyChangesOperation(codeGenResult.Project.Solution) };
            }

            private ImmutableArray<ITypeSymbol> GetArgumentTypes(IList<TArgumentSyntax> argumentList)
            {
                var types = argumentList.Select(a => _service.DetermineArgumentType(_semanticDocument.SemanticModel, a, _cancellationToken));
                return types.SelectAsArray(FixType);
            }

            private ImmutableArray<TExpressionSyntax> GetArgumentExpressions(IList<TArgumentSyntax> argumentList)
            {
                var syntaxFacts = _semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                return argumentList.SelectAsArray(a => (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(a));
            }

            private ITypeSymbol FixType(
                ITypeSymbol typeSymbol)
            {
                var compilation = _semanticDocument.SemanticModel.Compilation;
                return typeSymbol.RemoveUnnamedErrorTypes(compilation);
            }

            private async Task<bool> FindExistingOrCreateNewMemberAsync(
                ParameterName parameterName,
                ITypeSymbol parameterType,
                ImmutableDictionary<string, ISymbol>.Builder parameterToFieldMap,
                ImmutableDictionary<string, string>.Builder parameterToNewFieldMap)
            {
                // If the base types have an accessible field or property with the same name and
                // an acceptable type, then we should just defer to that.
                if (_state.BaseTypeOrInterfaceOpt != null)
                {
                    var expectedFieldName = parameterName.NameBasedOnArgument;
                    var members = from t in _state.BaseTypeOrInterfaceOpt.GetBaseTypesAndThis()
                                  from m in t.GetMembers()
                                  where m.Name.Equals(expectedFieldName, StringComparison.OrdinalIgnoreCase)
                                  where IsSymbolAccessible(m)
                                  where IsViableFieldOrProperty(parameterType, m)
                                  select m;

                    var membersArray = members.ToImmutableArray();
                    var symbol = membersArray.FirstOrDefault(m => m.Name.Equals(expectedFieldName, StringComparison.Ordinal)) ?? membersArray.FirstOrDefault();
                    if (symbol != null)
                    {
                        parameterToFieldMap[parameterName.BestNameForParameter] = symbol;
                        return true;
                    }
                }

                var fieldNamingRule = await _semanticDocument.Document.GetApplicableNamingRuleAsync(SymbolKind.Field, Accessibility.Private, _cancellationToken).ConfigureAwait(false);
                var nameToUse = fieldNamingRule.NamingStyle.MakeCompliant(parameterName.NameBasedOnArgument).First();
                parameterToNewFieldMap[parameterName.BestNameForParameter] = nameToUse;
                return false;
            }

            private bool IsViableFieldOrProperty(
                ITypeSymbol parameterType,
                ISymbol symbol)
            {
                if (symbol != null && !symbol.IsStatic && parameterType.Language == symbol.Language)
                {
                    if (symbol is IFieldSymbol field)
                    {
                        return
                            !field.IsReadOnly &&
                            _service.IsConversionImplicit(_semanticDocument.SemanticModel.Compilation, parameterType, field.Type);
                    }
                    else if (symbol is IPropertySymbol property)
                    {
                        return
                            property.Parameters.Length == 0 &&
                            property.SetMethod != null &&
                            IsSymbolAccessible(property.SetMethod) &&
                            _service.IsConversionImplicit(_semanticDocument.SemanticModel.Compilation, parameterType, property.Type);
                    }
                }

                return false;
            }

            private bool IsSymbolAccessible(ISymbol symbol)
            {
                // Public and protected constructors are accessible.  Internal constructors are
                // accessible if we have friend access.  We can't call the normal accessibility
                // checkers since they will think that a protected constructor isn't accessible
                // (since we don't have the destination type that would have access to them yet).
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.Protected:
                    case Accessibility.Public:
                        return true;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                        // TODO: Code coverage
                        return _semanticDocument.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(
                            symbol.ContainingAssembly);

                    default:
                        return false;
                }
            }
        }
    }
}
