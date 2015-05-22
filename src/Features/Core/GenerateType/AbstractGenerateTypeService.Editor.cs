// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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

            private readonly SemanticDocument _document;
            private readonly State _state;
            private readonly bool _intoNamespace;
            private readonly bool _inNewFile;
            private readonly bool _fromDialog;
            private readonly GenerateTypeOptionsResult _generateTypeOptionsResult;
            private readonly CancellationToken _cancellationToken;

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                bool intoNamespace,
                bool inNewFile,
                CancellationToken cancellationToken)
            {
                _service = service;
                _document = document;
                _state = state;
                _intoNamespace = intoNamespace;
                _inNewFile = inNewFile;
                _cancellationToken = cancellationToken;
            }

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                bool fromDialog,
                GenerateTypeOptionsResult generateTypeOptionsResult,
                CancellationToken cancellationToken)
            {
                _service = service;
                _document = document;
                _state = state;
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
                    var namedType = GenerateNamedType();

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
                                _document.Project,
                                _document.Project,
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
                    var namedType = GenerateNamedType(_generateTypeOptionsResult);

                    // Honor the options from the dialog
                    // Check to see if the type is requested to be generated in cross language Project
                    // e.g.: C# -> VB or VB -> C#
                    if (_document.Project.Language != _generateTypeOptionsResult.Project.Language)
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
                            _document.Project,
                            isDialog: true).ConfigureAwait(false);
                    }
                    else
                    {
                        return await GetGenerateIntoExistingDocumentAsync(
                            namedType,
                            _document.Project,
                            _generateTypeOptionsResult,
                            isDialog: true).ConfigureAwait(false);
                    }
                }
            }

            private string GetNamespaceToGenerateInto()
            {
                var namespaceToGenerateInto = _state.NamespaceToGenerateInOpt.Trim();
                var rootNamespace = _service.GetRootNamespace(_document.SemanticModel.Compilation.Options).Trim();
                if (!string.IsNullOrWhiteSpace(rootNamespace))
                {
                    if (namespaceToGenerateInto == rootNamespace ||
                        namespaceToGenerateInto.StartsWith(rootNamespace + ".", StringComparison.Ordinal))
                    {
                        namespaceToGenerateInto = namespaceToGenerateInto.Substring(rootNamespace.Length);
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
                Contract.Assert(targetProject.Language == LanguageNames.VisualBasic);
                IGenerateTypeService targetLanguageService = null;
                if (_document.Project.Language == LanguageNames.VisualBasic)
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
                        namespaceToGenerateInto = namespaceToGenerateInto.Substring(rootNamespaceLength);
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

            private string AddGlobalDotToTheNamespace(string namespaceToBeGenerated)
            {
                return "Global." + namespaceToBeGenerated;
            }

            // Returns the length of the meaningful rootNamespace substring part of namespaceToGenerateInto
            private int CheckIfRootNamespacePresentInNamespace(string namespaceToGenerateInto, string rootNamespace)
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

            private void AddFoldersToNamespaceContainers(List<string> container, IList<string> folders)
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

                var namespaceContainersAndUsings = GetNamespaceContainersAndAddUsingsOrImport(isDialog, folders, areFoldersValidIdentifiers, projectToBeUpdated, triggeringProject);

                var containers = namespaceContainersAndUsings.Item1;
                var includeUsingsOrImports = namespaceContainersAndUsings.Item2;

                var rootNamespaceOrType = namedType.GenerateRootNamespaceOrType(containers);

                // Now, actually ask the code gen service to add this namespace or type to the root
                // namespace in the new file.  This will properly generate the code, and add any
                // additional niceties like imports/usings.
                var codeGenResult = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                    newSolution,
                    enclosingNamespace,
                    rootNamespaceOrType,
                    new CodeGenerationOptions(newSemanticModel.SyntaxTree.GetLocation(new TextSpan())),
                    _cancellationToken).ConfigureAwait(false);

                // containers is determined to be
                // 1: folders -> if triggered from Dialog
                // 2: containers -> if triggered not from a Dialog but from QualifiedName
                // 3: triggering document folder structure -> if triggered not from a Dialog and a SimpleName
                var adjustedContainer = isDialog ? folders :
                        _state.SimpleName != _state.NameOrMemberAccessExpression ? containers.ToList() : _document.Document.Folders.ToList();

                // Now, take the code that would be generated and actually create an edit that would
                // produce a document with that code in it.

                return CreateAddDocumentAndUpdateUsingsOrImportsOperations(
                    projectToBeUpdated,
                    triggeringProject,
                    documentName,
                    await codeGenResult.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false),
                    _document.Document,
                    includeUsingsOrImports,
                    adjustedContainer,
                    SourceCodeKind.Regular,
                    _cancellationToken);
            }

            private IEnumerable<CodeActionOperation> CreateAddDocumentAndUpdateUsingsOrImportsOperations(
                Project projectToBeUpdated,
                Project triggeringProject,
                string documentName,
                SyntaxNode root,
                Document generatingDocument,
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
                    updatedSolution = _service.TryAddUsingsOrImportToDocument(updatedSolution, null, _document.Document, _state.SimpleName, includeUsingsOrImports, cancellationToken);
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
                var enclosingNamespace = _document.SemanticModel.GetEnclosingNamespace(
                    _state.SimpleName.SpanStart, _cancellationToken);

                var solution = _document.Project.Solution;
                var codeGenResult = await CodeGenerator.AddNamedTypeDeclarationAsync(
                    solution,
                    enclosingNamespace,
                    namedType,
                    new CodeGenerationOptions(afterThisLocation: _document.SyntaxTree.GetLocation(_state.SimpleName.Span)),
                    _cancellationToken)
                    .ConfigureAwait(false);

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

                var containers = namespaceContainersAndUsings.Item1;
                var includeUsingsOrImports = namespaceContainersAndUsings.Item2;

                Tuple<INamespaceSymbol, INamespaceOrTypeSymbol, Location> enclosingNamespaceGeneratedTypeToAddAndLocation = null;
                if (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange)
                {
                    enclosingNamespaceGeneratedTypeToAddAndLocation = _service.GetOrGenerateEnclosingNamespaceSymbol(
                     namedType,
                     containers,
                     generateTypeOptionsResult.ExistingDocument,
                     root,
                     _cancellationToken).WaitAndGetResult(_cancellationToken);
                }
                else
                {
                    enclosingNamespaceGeneratedTypeToAddAndLocation = _targetLanguageService.GetOrGenerateEnclosingNamespaceSymbol(
                     namedType,
                     containers,
                     generateTypeOptionsResult.ExistingDocument,
                     root,
                     _cancellationToken).WaitAndGetResult(_cancellationToken);
                }

                var solution = _document.Project.Solution;
                var codeGenResult = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                    solution,
                    enclosingNamespaceGeneratedTypeToAddAndLocation.Item1,
                    enclosingNamespaceGeneratedTypeToAddAndLocation.Item2,
                    new CodeGenerationOptions(afterThisLocation: enclosingNamespaceGeneratedTypeToAddAndLocation.Item3),
                    _cancellationToken)
                    .ConfigureAwait(false);
                var newRoot = await codeGenResult.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                var updatedSolution = solution.WithDocumentSyntaxRoot(generateTypeOptionsResult.ExistingDocument.Id, newRoot, PreservationMode.PreserveIdentity);

                // Update the Generating Document with a using if required
                if (includeUsingsOrImports != null)
                {
                    updatedSolution = _service.TryAddUsingsOrImportToDocument(
                                        updatedSolution,
                                        generateTypeOptionsResult.ExistingDocument.Id == _document.Document.Id ? newRoot : null,
                                        _document.Document,
                                        _state.SimpleName,
                                        includeUsingsOrImports,
                                        _cancellationToken);
                }

                updatedSolution = AddProjectReference(generateTypeOptionsResult.Project, triggeringProject, updatedSolution);

                return new CodeActionOperation[] { new ApplyChangesOperation(updatedSolution) };
            }

            private Tuple<string[], string> GetNamespaceContainersAndAddUsingsOrImport(
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
                    List<string> containerList = new List<string>();

                    string rootNamespaceOfTheProjectGeneratedInto;

                    if (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange)
                    {
                        rootNamespaceOfTheProjectGeneratedInto = _service.GetRootNamespace(_generateTypeOptionsResult.Project.CompilationOptions).Trim();
                    }
                    else
                    {
                        rootNamespaceOfTheProjectGeneratedInto = _targetLanguageService.GetRootNamespace(_generateTypeOptionsResult.Project.CompilationOptions).Trim();
                    }

                    var projectManagementService = _document.Project.Solution.Workspace.Services.GetService<IProjectManagementService>();
                    var defaultNamespace = _generateTypeOptionsResult.DefaultNamespace;

                    // Case 1 : If the type is generated into the same C# project or
                    // Case 2 : If the type is generated from a C# project to a C# Project
                    // Case 3 : If the Type is generated from a VB Project to a C# Project
                    // Using and Namespace will be the DefaultNamespace + Folder Structure
                    if ((_document.Project == _generateTypeOptionsResult.Project && _document.Project.Language == LanguageNames.CSharp) ||
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
                    if ((_document.Project == _generateTypeOptionsResult.Project && _document.Project.Language == LanguageNames.VisualBasic) ||
                        (_document.Project != _generateTypeOptionsResult.Project && _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange && _generateTypeOptionsResult.Project.Language == LanguageNames.VisualBasic) ||
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

                    Contract.Assert(includeUsingsOrImports != null);
                }

                return Tuple.Create(containers, includeUsingsOrImports);
            }

            private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoTypeOperationsAsync(INamedTypeSymbol namedType)
            {
                var codeGenService = GetCodeGenerationService();
                var solution = _document.Project.Solution;
                var codeGenResult = await CodeGenerator.AddNamedTypeDeclarationAsync(
                    solution,
                    _state.TypeToGenerateInOpt,
                    namedType,
                    new CodeGenerationOptions(contextLocation: _state.SimpleName.GetLocation()),
                    _cancellationToken)
                    .ConfigureAwait(false);

                return new CodeActionOperation[] { new ApplyChangesOperation(codeGenResult.Project.Solution) };
            }

            private IList<ITypeSymbol> GetArgumentTypes(IList<TArgumentSyntax> argumentList)
            {
                var types = argumentList.Select(a => _service.DetermineArgumentType(_document.SemanticModel, a, _cancellationToken));
                return types.Select(FixType).ToList();
            }

            private ITypeSymbol FixType(
                ITypeSymbol typeSymbol)
            {
                var compilation = _document.SemanticModel.Compilation;
                return typeSymbol.RemoveUnnamedErrorTypes(compilation);
            }

            private ICodeGenerationService GetCodeGenerationService()
            {
                var language = _state.TypeToGenerateInOpt == null
                    ? _state.SimpleName.Language
                    : _state.TypeToGenerateInOpt.Language;
                return _document.Project.Solution.Workspace.Services.GetLanguageServices(language).GetService<ICodeGenerationService>();
            }

            private bool TryFindMatchingField(
                string parameterName,
                ITypeSymbol parameterType,
                Dictionary<string, ISymbol> parameterToFieldMap,
                bool caseSensitive)
            {
                // If the base types have an accessible field or property with the same name and
                // an acceptable type, then we should just defer to that.
                if (_state.BaseTypeOrInterfaceOpt != null)
                {
                    var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    var query =
                        _state.BaseTypeOrInterfaceOpt
                            .GetBaseTypesAndThis()
                            .SelectMany(t => t.GetMembers())
                            .Where(s => s.Name.Equals(parameterName, comparison));
                    var symbol = query.FirstOrDefault(IsSymbolAccessible);

                    if (IsViableFieldOrProperty(parameterType, symbol))
                    {
                        parameterToFieldMap[parameterName] = symbol;
                        return true;
                    }
                }

                return false;
            }

            private bool IsViableFieldOrProperty(
                ITypeSymbol parameterType,
                ISymbol symbol)
            {
                if (symbol != null && !symbol.IsStatic && parameterType.Language == symbol.Language)
                {
                    if (symbol is IFieldSymbol)
                    {
                        var field = (IFieldSymbol)symbol;
                        return
                            !field.IsReadOnly &&
                            _service.IsConversionImplicit(_document.SemanticModel.Compilation, parameterType, field.Type);
                    }
                    else if (symbol is IPropertySymbol)
                    {
                        var property = (IPropertySymbol)symbol;
                        return
                            property.Parameters.Length == 0 &&
                            property.SetMethod != null &&
                            IsSymbolAccessible(property.SetMethod) &&
                            _service.IsConversionImplicit(_document.SemanticModel.Compilation, parameterType, property.Type);
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
                        return _document.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(
                            symbol.ContainingAssembly);

                    default:
                        return false;
                }
            }
        }
    }
}
