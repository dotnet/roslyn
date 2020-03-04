' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A BinderBuilder builds a linked list of Binder objects for various typical binders.
    ''' 
    ''' Typically the binder chain looks something like this:
    '''    BackstopBinder
    '''    SourceModuleBinder
    '''    TypesOfImportedNamespacesMembersBinder (for modules of project-level imported namespaces)
    '''    ImportedTypesAndNamespacesMembersBinder (for project-level imported namespaces and types)
    '''    ImportAliasesBinder (for project-level import aliases)
    '''    SourceFileBinder
    '''    TypesOfImportedNamespacesMembersBinder (for modules of file-level imported namespaces)
    '''    ImportedTypesAndNamespacesMembersBinder (for file-level imported namespaces and types)
    '''    ImportAliasesBinder (for file-level import aliases)
    '''    NamespaceBinder... (for each namespace, starting at the global namespace)
    '''    TypeBinder... (for each type, and nested type)
    '''  (maybe more)
    '''    DiagnosticBagBinder 
    ''' 
    '''  Note: Binders are also built by the BinderCache class. Changes to how namespace and type Binders
    '''  are built may need changes there also.
    ''' </summary>
    Friend Class BinderBuilder
        ''' <summary>
        ''' Creates a binder for a binding global imports in a source file. This includes the following binders:
        '''    BackstopBinder
        '''    SourceModuleBinder
        '''    SourceFileBinder
        '''    NamespaceBinder (for the global namespace)
        '''    IgnoreBaseClassesBinder (so that base classes are ignore during binding)
        ''' </summary>
        Public Shared Function CreateBinderForSourceFileImports(moduleSymbol As SourceModuleSymbol,
                                                                tree As SyntaxTree) As Binder
            Dim sourceModuleBinder As Binder = CreateSourceModuleBinder(moduleSymbol)
            Dim sourceFileBinder As Binder = New SourceFileBinder(sourceModuleBinder, moduleSymbol.TryGetSourceFile(tree), tree)
            Dim namespaceBinder As Binder = New NamespaceBinder(sourceFileBinder, moduleSymbol.ContainingSourceAssembly.DeclaringCompilation.GlobalNamespace)
            Dim ignoreBasesBinder As Binder = New IgnoreBaseClassesBinder(namespaceBinder)

            Return New LocationSpecificBinder(BindingLocation.SourceFileImportsDeclaration, ignoreBasesBinder)
        End Function

        ''' <summary>
        ''' Creates a binder for a binding project-level imports. This includes the following binders:
        '''    BackstopBinder
        '''    SourceModuleBinder
        '''    ProjectImportsBinder
        '''    NamespaceBinder (for the global namespace)
        '''    IgnoreBaseClassesBinder (so that base classes are ignore during binding)
        ''' </summary>
        Public Shared Function CreateBinderForProjectImports(moduleSymbol As SourceModuleSymbol,
                                                                 tree As SyntaxTree) As Binder
            Dim sourceModuleBinder As Binder = CreateSourceModuleBinder(moduleSymbol)
            Dim projectImportsBinder As Binder = New ProjectImportsBinder(sourceModuleBinder, tree)
            Dim namespaceBinder As Binder = New NamespaceBinder(projectImportsBinder, moduleSymbol.ContainingSourceAssembly.DeclaringCompilation.GlobalNamespace)
            Dim ignoreBasesBinder As Binder = New IgnoreBaseClassesBinder(namespaceBinder)

            Return New LocationSpecificBinder(BindingLocation.ProjectImportsDeclaration, ignoreBasesBinder)
        End Function

        ''' <summary>
        ''' Creates a binder for a source file. This includes the following binders:
        '''    BackstopBinder
        '''    SourceModuleBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of project-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for project-level imported namespaces and types)
        '''    ImportAliasesBinder (for project-level import aliases)
        '''    SourceFileBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of file-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for file-level imported namespaces and types)
        '''    ImportAliasesBinder (for file-level import aliases)
        ''' </summary>
        Private Shared Function CreateBinderForSourceFile(moduleSymbol As SourceModuleSymbol,
                                                          tree As SyntaxTree) As Binder
            Dim moduleBinder As Binder = CreateSourceModuleBinder(moduleSymbol)

            ' Add project level member imports.
            Dim projectMemberImports = moduleSymbol.MemberImports
            If projectMemberImports.Length > 0 Then
                moduleBinder = New TypesOfImportedNamespacesMembersBinder(moduleBinder, projectMemberImports)
                moduleBinder = New ImportedTypesAndNamespacesMembersBinder(moduleBinder, projectMemberImports)
            End If

            ' Add project level alias imports.
            Dim projectAliasImports = moduleSymbol.AliasImportsMap
            If projectAliasImports IsNot Nothing Then
                moduleBinder = New ImportAliasesBinder(moduleBinder, projectAliasImports)
            End If

            ' Add project level xmlns imports.
            Dim projectXmlNamespaces = moduleSymbol.XmlNamespaces
            If projectXmlNamespaces IsNot Nothing Then
                moduleBinder = New XmlNamespaceImportsBinder(moduleBinder, projectXmlNamespaces)
            End If

            Dim sourceFile = moduleSymbol.TryGetSourceFile(tree)

            If sourceFile Is Nothing Then
                Return moduleBinder
            End If

            Dim sourceFileBinder As Binder = New SourceFileBinder(moduleBinder, sourceFile, tree)

            ' Add file-level member imports.
            Dim memberImports = sourceFile.MemberImports
            If Not memberImports.IsEmpty Then
                sourceFileBinder = New TypesOfImportedNamespacesMembersBinder(sourceFileBinder, memberImports)
                sourceFileBinder = New ImportedTypesAndNamespacesMembersBinder(sourceFileBinder, memberImports)
            End If

            'Add file-level alias imports.
            Dim aliasImports = sourceFile.AliasImportsOpt
            If aliasImports IsNot Nothing Then
                sourceFileBinder = New ImportAliasesBinder(sourceFileBinder, aliasImports)
            End If

            ' Add file-level xmlns imports.
            Dim xmlNamespaces = sourceFile.XmlNamespacesOpt
            If xmlNamespaces IsNot Nothing Then
                sourceFileBinder = New XmlNamespaceImportsBinder(sourceFileBinder, xmlNamespaces)
            End If

            Return sourceFileBinder
        End Function

        ''' <summary>
        ''' Creates a binder for a project level namespace declaration 
        ''' This includes the following binders:
        '''    BackstopBinder
        '''    SourceModuleBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of project-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for project-level imported namespaces and types)
        '''    SourceFileBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of file-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for file-level imported namespaces and types)
        '''    ImportAliasesBinder (for file-level import aliases)
        '''    NamespaceBinder... (for each namespace, starting at the global namespace)
        ''' </summary>
        Public Shared Function CreateBinderForProjectLevelNamespace(moduleSymbol As SourceModuleSymbol,
                                                                    tree As SyntaxTree) As Binder
            ' Get the binder associated with the default project namespace
            Dim namespaceSymbol As NamespaceSymbol = moduleSymbol.RootNamespace
            Debug.Assert(namespaceSymbol IsNot Nothing, "Something is deeply wrong with the declaration table or the symbol table")
            Return BinderBuilder.CreateBinderForNamespace(moduleSymbol, tree, namespaceSymbol)
        End Function

        ''' <summary>
        ''' Creates a binder for a source namespace declaration (the part of a namespace
        ''' in a single namespace declaration). This includes the following binders:
        '''    BackstopBinder
        '''    SourceModuleBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of project-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for project-level imported namespaces and types)
        '''    SourceFileBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of file-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for file-level imported namespaces and types)
        '''    ImportAliasesBinder (for file-level import aliases)
        '''    NamespaceBinder... (for each namespace, starting at the global namespace)
        ''' </summary>
        Public Shared Function CreateBinderForNamespace(moduleSymbol As SourceModuleSymbol,
                                                        tree As SyntaxTree,
                                                        nsSymbol As NamespaceSymbol) As NamespaceBinder

            Dim containingNamespace As NamespaceSymbol = nsSymbol.ContainingNamespace
            If containingNamespace Is Nothing Then
                ' At the root namespace. Need to use the root namespace from the compilation in order to bind
                ' symbol from referenced assemblies.
                Dim containingBinder = CreateBinderForSourceFile(moduleSymbol, tree)
                Return New NamespaceBinder(containingBinder, moduleSymbol.ContainingSourceAssembly.DeclaringCompilation.GlobalNamespace)
            End If

            Dim namespaces = ArrayBuilder(Of NamespaceSymbol).GetInstance()

            While containingNamespace IsNot Nothing
                namespaces.Push(nsSymbol)
                nsSymbol = containingNamespace
                containingNamespace = nsSymbol.ContainingNamespace
            End While

            Debug.Assert(containingNamespace Is Nothing)
            Dim binder As NamespaceBinder = CreateBinderForNamespace(moduleSymbol, tree, nsSymbol)

            While namespaces.Count > 0
                nsSymbol = namespaces.Pop()
                containingNamespace = nsSymbol.ContainingNamespace

                If binder.NamespaceSymbol.Extent.Kind <> nsSymbol.Extent.Kind Then
                    ' Need to get the namespace symbol from the containing binder so we correctly work in referenced assemblies
                    ' (i.e., we need a namespace symbol potentially with larger extent).
                    nsSymbol = DirectCast(binder.NamespaceSymbol.GetMembers(nsSymbol.Name).First(Function(s) s.Kind = SymbolKind.Namespace), NamespaceSymbol)
                End If

                binder = New NamespaceBinder(binder, nsSymbol)
            End While

            namespaces.Free()

            Return binder
        End Function

        ''' <summary>
        ''' Creates a binder for a source type declaration (the part of a type in a single
        ''' type declaration. For partial types this include just one part). This includes the following binders:
        '''    BackstopBinder
        '''    SourceModuleBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of project-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for project-level imported namespaces and types)
        '''    SourceFileBinder
        '''    TypesOfImportedNamespacesMembersBinder (for modules of file-level imported namespaces)
        '''    ImportedTypesAndNamespacesMembersBinder (for file-level imported namespaces and types)
        '''    ImportAliasesBinder (for file-level import aliases)
        '''    NamespaceBinder... (for each namespace, starting at the global namespace)
        '''    NamedTypeBinder... (for each type, and nested type)
        ''' </summary>
        Public Shared Function CreateBinderForType(moduleSymbol As SourceModuleSymbol,
                                                   tree As SyntaxTree,
                                                   typeSymbol As NamedTypeSymbol) As Binder
            Dim containingSymbol = typeSymbol.ContainingSymbol

            If containingSymbol.Kind = SymbolKind.Namespace Then
                Return New NamedTypeBinder(CreateBinderForNamespace(moduleSymbol, tree, DirectCast(containingSymbol, NamespaceSymbol)), typeSymbol)
            End If

            Debug.Assert(TypeOf containingSymbol Is NamedTypeSymbol)
            Debug.Assert(containingSymbol.IsFromCompilation(moduleSymbol.DeclaringCompilation))

            Dim types = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
            types.Push(typeSymbol)

            While containingSymbol.Kind <> SymbolKind.Namespace
                typeSymbol = DirectCast(containingSymbol, NamedTypeSymbol)
                containingSymbol = typeSymbol.ContainingSymbol
                types.Push(typeSymbol)
            End While

            Debug.Assert(containingSymbol IsNot Nothing AndAlso containingSymbol.Kind = SymbolKind.Namespace)

            Dim binder As Binder = CreateBinderForNamespace(moduleSymbol, tree, DirectCast(containingSymbol, NamespaceSymbol))
            While types.Count > 0
                typeSymbol = types.Pop()
                binder = New NamedTypeBinder(binder, typeSymbol)
            End While

            types.Free()

            Return binder
        End Function

        ''' <summary>
        ''' Creates a binder for a source attribute block from the containing type or containing namespace.
        ''' This binder is used by the normal compilation code path for source attributes. In this case, no
        ''' containing binder exists.
        ''' </summary>
        ''' <param name="moduleSymbol"></param>
        ''' <param name="tree"></param>
        ''' <param name="target">The symbol which is the target of the attribute.</param>
        Public Shared Function CreateBinderForAttribute(moduleSymbol As SourceModuleSymbol,
                                                        tree As SyntaxTree,
                                                        target As Symbol) As AttributeBinder
            Debug.Assert(target IsNot Nothing)

            Dim containingType As NamedTypeSymbol
            Select Case target.Kind
                Case SymbolKind.Parameter
                    containingType = target.ContainingSymbol.ContainingType

                Case Else
                    containingType = target.ContainingType
            End Select

            Dim containingBinder As Binder

            If containingType IsNot Nothing Then
                containingBinder = BinderBuilder.CreateBinderForType(
                    moduleSymbol, tree, containingType)
            Else
                containingBinder = BinderBuilder.CreateBinderForNamespace(
                    moduleSymbol, tree, target.ContainingNamespace)
            End If

            Dim sourceMethod = TryCast(target, SourceMethodSymbol)
            If sourceMethod IsNot Nothing Then
                containingBinder = BinderBuilder.CreateBinderForMethodDeclaration(sourceMethod, containingBinder)
            End If

            Return New AttributeBinder(containingBinder, tree)
        End Function

        ''' <summary>
        ''' Creates a binder for a source attribute block when a containing binder is available. Used by semantic model.
        ''' </summary>
        Public Shared Function CreateBinderForAttribute(tree As SyntaxTree, containingBinder As Binder, node As VisualBasicSyntaxNode) As AttributeBinder
            Return New AttributeBinder(containingBinder, tree, node)
        End Function

        Public Shared Function CreateBinderForParameterDefaultValue(moduleSymbol As SourceModuleSymbol,
                                                                    tree As SyntaxTree,
                                                                    parameterSymbol As ParameterSymbol,
                                                                    node As VisualBasicSyntaxNode) As Binder
            Dim containingBinder As Binder
            Dim containingSymbol = parameterSymbol.ContainingSymbol
            Dim methodSymbol = TryCast(containingSymbol, SourceMethodSymbol)

            If methodSymbol IsNot Nothing Then
                containingBinder = BinderBuilder.CreateBinderForMethodDeclaration(moduleSymbol,
                                                                        tree,
                                                                        methodSymbol)
            Else
                Dim containingType = containingSymbol.ContainingType()
                containingBinder = BinderBuilder.CreateBinderForType(moduleSymbol,
                                                               tree,
                                                               containingType)
            End If

            Return New DeclarationInitializerBinder(parameterSymbol, ImmutableArray(Of Symbol).Empty, containingBinder, node)
        End Function

        ''' <summary>
        ''' Creates a binder for binding a source parameter's default value.
        ''' </summary>
        Public Shared Function CreateBinderForParameterDefaultValue(parameterSymbol As ParameterSymbol, containingBinder As Binder, node As VisualBasicSyntaxNode) As Binder
            Dim methodSymbol = TryCast(parameterSymbol.ContainingSymbol, SourceMethodSymbol)

            If methodSymbol IsNot Nothing Then
                containingBinder = BinderBuilder.CreateBinderForMethodDeclaration(methodSymbol, containingBinder)
            End If

            Return New DeclarationInitializerBinder(parameterSymbol, ImmutableArray(Of Symbol).Empty, containingBinder, node)
        End Function

        ''' <summary>
        ''' Creates a binder for binding for binding inside the interior of documentation comment 
        ''' </summary>
        Public Shared Function CreateBinderForDocumentationComment(containingBinder As Binder, commentedSymbol As Symbol, binderType As DocumentationCommentBinder.BinderType) As Binder
            Select Case binderType
                Case DocumentationCommentBinder.BinderType.Cref
                    Return New DocumentationCommentCrefBinder(containingBinder, commentedSymbol)

                Case DocumentationCommentBinder.BinderType.NameInParamOrParamRef
                    Return New DocumentationCommentParamBinder(containingBinder, commentedSymbol)

                Case DocumentationCommentBinder.BinderType.NameInTypeParam
                    Return New DocumentationCommentTypeParamBinder(containingBinder, commentedSymbol)

                Case DocumentationCommentBinder.BinderType.NameInTypeParamRef
                    Return New DocumentationCommentTypeParamRefBinder(containingBinder, commentedSymbol)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(binderType)
            End Select
        End Function

        ' Create a binder for the given method declaration. Method type parameters are in scope, but
        ' parameters are not. Note that unlike C#, all declarations of partial methods must have
        ' the same type parameter names, so a methodSymbol is enough disambiguation.
        Public Shared Function CreateBinderForMethodDeclaration(methodSymbol As MethodSymbol, containingBinder As Binder) As Binder
            Debug.Assert(containingBinder.SourceModule Is methodSymbol.ContainingModule)

            If methodSymbol.IsGenericMethod Then
                Return New MethodTypeParametersBinder(containingBinder, methodSymbol.TypeParameters)
            Else
                Return containingBinder
            End If
        End Function

        ' Create a binder for the given generic method declaration. This is equivalent to
        ' GetBinderForMethodDeclaration but does not check that the method is in fact
        ' generic since calling MethodSymbol.IsGenericMethod may involving binding
        ' which would result in a recursive call to this method.
        Public Shared Function CreateBinderForGenericMethodDeclaration(methodSymbol As SourceMethodSymbol, containingBinder As Binder) As Binder
            Return New MethodTypeParametersBinder(containingBinder, methodSymbol.TypeParameters)
        End Function

        ' Create a binder for the given method declaration. Method type parameters are in scope, but
        ' parameters are not. Note that unlike C#, all declarations of partial methods must have
        ' the same type parameter names, so a methodSymbol is enough disambiguation.
        Public Shared Function CreateBinderForMethodDeclaration(moduleSymbol As SourceModuleSymbol,
                                                                 tree As SyntaxTree,
                                                                 methodSymbol As SourceMethodSymbol) As Binder
            Return CreateBinderForMethodDeclaration(methodSymbol,
                        CreateBinderForType(moduleSymbol, tree, methodSymbol.ContainingType))
        End Function

        ' Create a binder for the given method body, possibly with an ImplicitVariableBinder right
        ' before it. Method type parameters and parameters are in scope.
        ' If Option Explicit Off is in effect, an ImplicitVariableBinder
        ' is created also.
        Public Shared Function CreateBinderForMethodBody(methodSymbol As MethodSymbol, root As SyntaxNode, containingBinder As Binder) As Binder
            Debug.Assert(TypeOf VBSemanticModel.StripSemanticModelBinder(containingBinder) Is NamedTypeBinder)

            Dim methodDeclBinder As Binder = CreateBinderForMethodDeclaration(methodSymbol, containingBinder)

            If methodDeclBinder.OptionExplicit = False Then
                methodDeclBinder = New ImplicitVariableBinder(methodDeclBinder, methodSymbol)
            End If

            Return New MethodBodyBinder(methodSymbol, root, methodDeclBinder)
        End Function

        ' Create a binder for the given method body. Method type parameters and parameters are in scope.
        Public Shared Function CreateBinderForMethodBody(moduleSymbol As SourceModuleSymbol,
                                                         tree As SyntaxTree,
                                                         methodSymbol As SourceMethodSymbol) As Binder
            Return CreateBinderForMethodBody(methodSymbol, methodSymbol.Syntax,
                        CreateBinderForType(moduleSymbol, tree, methodSymbol.ContainingType))
        End Function

        Public Shared Function CreateBinderForInitializer(containingBinder As Binder,
                                                          fieldOrProperty As Symbol,
                                                          additionalFieldsOrProperties As ImmutableArray(Of Symbol)) As Binder

            Debug.Assert((fieldOrProperty.Kind = SymbolKind.Field) OrElse (fieldOrProperty.Kind = SymbolKind.Property))
            Debug.Assert(additionalFieldsOrProperties.All(Function(s) s.Kind = SymbolKind.Field OrElse s.Kind = SymbolKind.Property))
            Debug.Assert(containingBinder IsNot Nothing)

            Dim declarationSyntax As VisualBasicSyntaxNode

            If fieldOrProperty.Kind = SymbolKind.Field Then
                declarationSyntax = DirectCast(fieldOrProperty, SourceFieldSymbol).DeclarationSyntax
            Else
                declarationSyntax = DirectCast(fieldOrProperty, SourcePropertySymbol).DeclarationSyntax
            End If

            Return New DeclarationInitializerBinder(fieldOrProperty, additionalFieldsOrProperties, containingBinder, declarationSyntax)
        End Function

        ''' <summary>
        ''' Create a binder for the source module. Includes the following:
        '''    BackstopBinder
        '''    SourceModuleBinder
        ''' </summary>
        Public Shared Function CreateSourceModuleBinder(moduleSymbol As SourceModuleSymbol) As Binder
            Dim backstop As Binder = New BackstopBinder()
            Return New SourceModuleBinder(backstop, moduleSymbol)
        End Function

    End Class

End Namespace
