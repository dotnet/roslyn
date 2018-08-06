' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class DeclarationTreeBuilder
        Inherits VisualBasicSyntaxVisitor(Of SingleNamespaceOrTypeDeclaration)

        ' The root namespace, expressing as an array of strings, one for each component.
        Private ReadOnly _rootNamespace As ImmutableArray(Of String)
        Private ReadOnly _scriptClassName As String
        Private ReadOnly _isSubmission As Boolean
        Private ReadOnly _syntaxTree As SyntaxTree

        Public Shared Function ForTree(tree As SyntaxTree, rootNamespace As ImmutableArray(Of String), scriptClassName As String, isSubmission As Boolean) As RootSingleNamespaceDeclaration
            Dim builder = New DeclarationTreeBuilder(tree, rootNamespace, scriptClassName, isSubmission)
            Dim decl = DirectCast(builder.ForDeclaration(tree.GetRoot()), RootSingleNamespaceDeclaration)
            Return decl
        End Function

        Private Sub New(syntaxTree As SyntaxTree, rootNamespace As ImmutableArray(Of String), scriptClassName As String, isSubmission As Boolean)
            _syntaxTree = syntaxTree
            _rootNamespace = rootNamespace
            _scriptClassName = scriptClassName
            _isSubmission = isSubmission
        End Sub

        Private Function ForDeclaration(node As SyntaxNode) As SingleNamespaceOrTypeDeclaration
            Return Visit(node)
        End Function

        Private Function VisitNamespaceChildren(node As VisualBasicSyntaxNode, members As SyntaxList(Of StatementSyntax)) As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
            Dim implicitClass As SingleNamespaceOrTypeDeclaration = Nothing

            Dim childrenBuilder = VisitNamespaceChildren(node, members, implicitClass)
            If implicitClass IsNot Nothing Then
                childrenBuilder.Add(implicitClass)
            End If

            Return childrenBuilder.ToImmutableAndFree()
        End Function

        Private Function VisitNamespaceChildren(node As VisualBasicSyntaxNode,
                                                members As SyntaxList(Of StatementSyntax),
                                                <Out()> ByRef implicitClass As SingleNamespaceOrTypeDeclaration) As ArrayBuilder(Of SingleNamespaceOrTypeDeclaration)

            Dim children = ArrayBuilder(Of SingleNamespaceOrTypeDeclaration).GetInstance()
            Dim implicitClassTypeChildren = ArrayBuilder(Of SingleTypeDeclaration).GetInstance()

            ' We look for any members that are not allowed in namespace. 
            ' If there are any we create an implicit class to wrap them.
            Dim requiresImplicitClass = False

            For Each member In members
                Dim namespaceOrType As SingleNamespaceOrTypeDeclaration = Visit(member)
                If namespaceOrType IsNot Nothing Then
                    If namespaceOrType.Kind = DeclarationKind.EventSyntheticDelegate Then
                        ' broken code scenario. Event declared in namespace created a delegate declaration which should go into the 
                        ' implicit class
                        implicitClassTypeChildren.Add(DirectCast(namespaceOrType, SingleTypeDeclaration))
                        requiresImplicitClass = True
                    Else
                        children.Add(namespaceOrType)
                    End If
                ElseIf Not requiresImplicitClass Then
                    requiresImplicitClass = member.Kind <> SyntaxKind.IncompleteMember AndAlso member.Kind <> SyntaxKind.EmptyStatement
                End If
            Next

            If requiresImplicitClass Then
                ' The implicit class is not static and has no extensions
                Dim declFlags As SingleTypeDeclaration.TypeDeclarationFlags = SingleTypeDeclaration.TypeDeclarationFlags.None
                Dim memberNames = GetNonTypeMemberNames(members, declFlags)

                implicitClass = CreateImplicitClass(
                    node,
                    memberNames,
                    implicitClassTypeChildren.ToImmutable,
                    declFlags)
            Else
                implicitClass = Nothing
            End If

            implicitClassTypeChildren.Free()

            Return children
        End Function

        Private Shared Function GetReferenceDirectives(compilationUnit As CompilationUnitSyntax) As ImmutableArray(Of ReferenceDirective)
            Dim directiveNodes = compilationUnit.GetReferenceDirectives(
                Function(d) Not d.File.ContainsDiagnostics AndAlso Not String.IsNullOrEmpty(d.File.ValueText))
            If directiveNodes.Count = 0 Then
                Return ImmutableArray(Of ReferenceDirective).Empty
            End If

            Dim directives = ArrayBuilder(Of ReferenceDirective).GetInstance(directiveNodes.Count)
            For Each directiveNode In directiveNodes
                directives.Add(New ReferenceDirective(directiveNode.File.ValueText, New SourceLocation(directiveNode)))
            Next
            Return directives.ToImmutableAndFree()
        End Function

        Private Function CreateImplicitClass(parent As VisualBasicSyntaxNode, memberNames As ImmutableHashSet(Of String), children As ImmutableArray(Of SingleTypeDeclaration), declFlags As SingleTypeDeclaration.TypeDeclarationFlags) As SingleNamespaceOrTypeDeclaration
            Dim parentReference = _syntaxTree.GetReference(parent)

            Return New SingleTypeDeclaration(
                kind:=DeclarationKind.ImplicitClass,
                name:=TypeSymbol.ImplicitTypeName,
                arity:=0,
                modifiers:=DeclarationModifiers.Friend Or DeclarationModifiers.Partial Or DeclarationModifiers.NotInheritable,
                declFlags:=declFlags,
                syntaxReference:=parentReference,
                nameLocation:=parentReference.GetLocation(),
                memberNames:=memberNames,
                children:=children)
        End Function

        Private Function CreateScriptClass(parent As VisualBasicSyntaxNode, children As ImmutableArray(Of SingleTypeDeclaration), memberNames As ImmutableHashSet(Of String), declFlags As SingleTypeDeclaration.TypeDeclarationFlags) As SingleNamespaceOrTypeDeclaration
            Debug.Assert(parent.Kind = SyntaxKind.CompilationUnit AndAlso _syntaxTree.Options.Kind <> SourceCodeKind.Regular)

            ' script class is represented by the parent node:
            Dim parentReference = _syntaxTree.GetReference(parent)
            Dim fullName = _scriptClassName.Split("."c)

            ' Note: The symbol representing the merged declarations uses parentReference to enumerate non-type members.
            Dim decl As SingleNamespaceOrTypeDeclaration = New SingleTypeDeclaration(
                kind:=If(_isSubmission, DeclarationKind.Submission, DeclarationKind.Script),
                name:=fullName.Last(),
                arity:=0,
                modifiers:=DeclarationModifiers.Friend Or DeclarationModifiers.Partial Or DeclarationModifiers.NotInheritable,
                declFlags:=declFlags,
                syntaxReference:=parentReference,
                nameLocation:=parentReference.GetLocation(),
                memberNames:=memberNames,
                children:=children)

            For i = fullName.Length - 2 To 0 Step -1
                decl = New SingleNamespaceDeclaration(
                    name:=fullName(i),
                    hasImports:=False,
                    syntaxReference:=parentReference,
                    nameLocation:=parentReference.GetLocation(),
                    children:=ImmutableArray.Create(Of SingleNamespaceOrTypeDeclaration)(decl))

            Next

            Return decl
        End Function

        Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SingleNamespaceOrTypeDeclaration
            Dim children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
            Dim globalChildren As ImmutableArray(Of SingleNamespaceOrTypeDeclaration) = Nothing
            Dim nonGlobal As ImmutableArray(Of SingleNamespaceOrTypeDeclaration) = Nothing

            Dim syntaxRef = _syntaxTree.GetReference(node)
            Dim implicitClass As SingleNamespaceOrTypeDeclaration = Nothing

            Dim referenceDirectives As ImmutableArray(Of ReferenceDirective)
            If _syntaxTree.Options.Kind <> SourceCodeKind.Regular Then
                Dim childrenBuilder = ArrayBuilder(Of SingleNamespaceOrTypeDeclaration).GetInstance()
                Dim scriptChildren = ArrayBuilder(Of SingleTypeDeclaration).GetInstance()

                For Each member In node.Members
                    Dim decl = Visit(member)
                    If decl IsNot Nothing Then
                        ' Although namespaces are not allowed in script code process them 
                        ' here as if they were to improve error reporting.
                        If decl.Kind = DeclarationKind.Namespace Then
                            childrenBuilder.Add(decl)
                        Else
                            scriptChildren.Add(DirectCast(decl, SingleTypeDeclaration))
                        End If
                    End If
                Next

                'Script class is not static and contains no extensions.
                Dim declFlags As SingleTypeDeclaration.TypeDeclarationFlags = SingleTypeDeclaration.TypeDeclarationFlags.None
                Dim memberNames = GetNonTypeMemberNames(node.Members, declFlags)

                implicitClass = CreateScriptClass(node, scriptChildren.ToImmutableAndFree(), memberNames, declFlags)
                children = childrenBuilder.ToImmutableAndFree()
                referenceDirectives = GetReferenceDirectives(node)
            Else
                children = VisitNamespaceChildren(node, node.Members, implicitClass).ToImmutableAndFree()
                referenceDirectives = ImmutableArray(Of ReferenceDirective).Empty
            End If

            ' Find children within NamespaceGlobal separately
            FindGlobalDeclarations(children, implicitClass, globalChildren, nonGlobal)

            If _rootNamespace.Length = 0 Then
                ' No project-level root namespace specified. Both global and nested children within the root.
                Return New RootSingleNamespaceDeclaration(
                    hasImports:=True,
                    treeNode:=_syntaxTree.GetReference(node),
                    children:=globalChildren.Concat(nonGlobal),
                    referenceDirectives:=referenceDirectives,
                    hasAssemblyAttributes:=node.Attributes.Any)
            Else
                ' Project-level root namespace. All children without explicit global are children
                ' of the project-level root namespace. The root declaration has the project level namespace
                ' and global children within it.
                ' Note that we need to built the project level namespace even if it has no children [Bug 4879[
                Dim projectNs = BuildRootNamespace(node, nonGlobal)
                globalChildren = globalChildren.Add(projectNs)

                Dim newChildren = globalChildren.OfType(Of SingleNamespaceOrTypeDeclaration).AsImmutable()
                Return New RootSingleNamespaceDeclaration(
                    hasImports:=True,
                    treeNode:=_syntaxTree.GetReference(node),
                    children:=newChildren,
                    referenceDirectives:=referenceDirectives,
                    hasAssemblyAttributes:=node.Attributes.Any)
            End If
        End Function

        ' Given a set of single declarations, get the sets of global and non-global declarations.
        ' A regular declaration is put into "non-global declarations".
        ' A "Namespace Global" is not put in either place, but all its direct children are put into "global declarations".
        Private Sub FindGlobalDeclarations(declarations As ImmutableArray(Of SingleNamespaceOrTypeDeclaration),
                                           implicitClass As SingleNamespaceOrTypeDeclaration,
                                           ByRef globalDeclarations As ImmutableArray(Of SingleNamespaceOrTypeDeclaration),
                                           ByRef nonGlobal As ImmutableArray(Of SingleNamespaceOrTypeDeclaration))
            Dim globalBuilder = ArrayBuilder(Of SingleNamespaceOrTypeDeclaration).GetInstance()
            Dim nonGlobalBuilder = ArrayBuilder(Of SingleNamespaceOrTypeDeclaration).GetInstance()

            If implicitClass IsNot Nothing Then
                nonGlobalBuilder.Add(implicitClass)
            End If

            For Each decl In declarations
                Dim nsDecl As SingleNamespaceDeclaration = TryCast(decl, SingleNamespaceDeclaration)
                If nsDecl IsNot Nothing AndAlso nsDecl.IsGlobalNamespace Then
                    ' Namespace Global.
                    globalBuilder.AddRange(nsDecl.Children)
                Else
                    ' regular declaration
                    nonGlobalBuilder.Add(decl)
                End If
            Next

            globalDeclarations = globalBuilder.ToImmutableAndFree()
            nonGlobal = nonGlobalBuilder.ToImmutableAndFree()
        End Sub

        Private Function UnescapeIdentifier(identifier As String) As String
            If identifier(0) = "[" Then
                Debug.Assert(identifier(identifier.Length - 1) = "]")
                Return identifier.Substring(1, identifier.Length - 2)
            Else
                Return identifier
            End If
        End Function

        ' Build the declaration for the root (project-level) namespace. 
        Private Function BuildRootNamespace(node As CompilationUnitSyntax,
                                            children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)) As SingleNamespaceDeclaration
            Debug.Assert(_rootNamespace.Length > 0)

            Dim ns As SingleNamespaceDeclaration = Nothing

            ' The compilation node will count as a location for the innermost project level
            ' namespace.  i.e. if the project level namespace is "Goo.Bar", then each compilation unit
            ' is a location for "Goo.Bar".  "Goo" still has no syntax location though. 
            '
            ' By doing this we ensure that top level type and namespace declarations will have
            ' symbols whose parent namespace symbol points to the parent container CompilationUnit.
            ' This ensures parity with the case where their is no 'project-level' namespace and the
            ' global namespaces point to the compilation unit syntax.
            Dim syntaxReference = _syntaxTree.GetReference(node)
            Dim nameLocation = syntaxReference.GetLocation()

            ' traverse components from right to left
            For i = _rootNamespace.Length - 1 To 0 Step -1
                ' treat all root namespace parts as implicitly escaped.
                ' a root namespace with a name "global" will actually create "Global.[global]"
                ns = New SingleNamespaceDeclaration(
                    name:=UnescapeIdentifier(_rootNamespace(i)),
                    hasImports:=True,
                    syntaxReference:=syntaxReference,
                    nameLocation:=nameLocation,
                    children:=children,
                    isPartOfRootNamespace:=True)

                ' Only the innermost namespace will point at compilation unit.  All other outer
                ' namespaces will have no location.
                syntaxReference = Nothing
                nameLocation = Nothing

                ' This namespace is the child of the namespace to the left.
                children = ImmutableArray.Create(Of SingleNamespaceOrTypeDeclaration)(ns)
            Next

            Return ns
        End Function

        Public Overrides Function VisitNamespaceBlock(nsBlockSyntax As NamespaceBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Dim nsDeclSyntax As NamespaceStatementSyntax = nsBlockSyntax.NamespaceStatement
            Dim children = VisitNamespaceChildren(nsBlockSyntax, nsBlockSyntax.Members)
            Dim name As NameSyntax = nsDeclSyntax.Name

            While TypeOf name Is QualifiedNameSyntax
                Dim dotted = DirectCast(name, QualifiedNameSyntax)
                Dim ns = New SingleNamespaceDeclaration(
                    name:=dotted.Right.Identifier.ValueText,
                    hasImports:=True,
                    syntaxReference:=_syntaxTree.GetReference(dotted),
                    nameLocation:=_syntaxTree.GetLocation(dotted.Right.Span),
                    children:=children)

                children = {ns}.OfType(Of SingleNamespaceOrTypeDeclaration).AsImmutable()
                name = dotted.Left
            End While

            ' This is either the global namespace, or a regular namespace. Represent the global namespace
            ' with the empty string.
            If name.Kind = SyntaxKind.GlobalName Then
                If nsBlockSyntax.Parent.Kind = SyntaxKind.CompilationUnit Then
                    ' Namespace Global only allowed as direct child of compilation.
                    Return New GlobalNamespaceDeclaration(
                        hasImports:=True,
                        syntaxReference:=_syntaxTree.GetReference(name),
                        nameLocation:=_syntaxTree.GetLocation(name.Span),
                        children:=children)
                Else
                    ' Error for this will be diagnosed later. Create a namespace named "Global" for error recovery. (see corresponding code in BinderFactory)
                    Return New SingleNamespaceDeclaration(
                        name:="Global",
                        hasImports:=True,
                        syntaxReference:=_syntaxTree.GetReference(name),
                        nameLocation:=_syntaxTree.GetLocation(name.Span),
                        children:=children)
                End If
            Else
                Return New SingleNamespaceDeclaration(
                    name:=DirectCast(name, IdentifierNameSyntax).Identifier.ValueText,
                    hasImports:=True,
                    syntaxReference:=_syntaxTree.GetReference(name),
                    nameLocation:=_syntaxTree.GetLocation(name.Span),
                    children:=children)
            End If
        End Function

        Private Structure TypeBlockInfo
            Public ReadOnly TypeBlockSyntax As TypeBlockSyntax
            Public ReadOnly TypeDeclaration As SingleTypeDeclaration
            Public ReadOnly NestedTypes As ArrayBuilder(Of Integer)

            Public Sub New(typeBlockSyntax As TypeBlockSyntax)
                MyClass.New(typeBlockSyntax, Nothing, Nothing)
            End Sub

            Private Sub New(typeBlockSyntax As TypeBlockSyntax, declaration As SingleTypeDeclaration, nestedTypes As ArrayBuilder(Of Integer))
                Me.TypeBlockSyntax = typeBlockSyntax
                Me.TypeDeclaration = declaration
                Me.NestedTypes = nestedTypes
            End Sub

            Public Function WithNestedTypes(nested As ArrayBuilder(Of Integer)) As TypeBlockInfo
                Debug.Assert(Me.TypeDeclaration Is Nothing)
                Debug.Assert(Me.NestedTypes Is Nothing)
                Debug.Assert(nested IsNot Nothing)
                Return New TypeBlockInfo(Me.TypeBlockSyntax, Nothing, nested)
            End Function

            Public Function WithDeclaration(declaration As SingleTypeDeclaration) As TypeBlockInfo
                Debug.Assert(Me.TypeDeclaration Is Nothing)
                Debug.Assert(declaration IsNot Nothing)
                Return New TypeBlockInfo(Me.TypeBlockSyntax, declaration, Me.NestedTypes)
            End Function
        End Structure

        Private Function VisitTypeBlockNew(topTypeBlockSyntax As TypeBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Dim typeStack = ArrayBuilder(Of TypeBlockInfo).GetInstance
            typeStack.Add(New TypeBlockInfo(topTypeBlockSyntax))

            ' Fill the chain with types
            Dim index As Integer = 0
            While index < typeStack.Count
                Dim typeEntry As TypeBlockInfo = typeStack(index)
                Dim members As SyntaxList(Of StatementSyntax) = typeEntry.TypeBlockSyntax.Members

                If members.Count > 0 Then
                    Dim nestedTypeIndices As ArrayBuilder(Of Integer) = Nothing
                    For Each member In members
                        Select Case member.Kind
                            Case SyntaxKind.ModuleBlock,
                                 SyntaxKind.ClassBlock,
                                 SyntaxKind.StructureBlock,
                                 SyntaxKind.InterfaceBlock

                                If nestedTypeIndices Is Nothing Then
                                    nestedTypeIndices = ArrayBuilder(Of Integer).GetInstance()
                                End If

                                nestedTypeIndices.Add(typeStack.Count)
                                typeStack.Add(New TypeBlockInfo(DirectCast(member, TypeBlockSyntax)))
                        End Select
                    Next

                    If nestedTypeIndices IsNot Nothing Then
                        typeStack(index) = typeEntry.WithNestedTypes(nestedTypeIndices)
                    End If
                End If

                index += 1
            End While

            ' Process types
            Debug.Assert(index = typeStack.Count)
            Dim childrenBuilder = ArrayBuilder(Of SingleTypeDeclaration).GetInstance()
            While index > 0
                index -= 1
                Dim typeEntry As TypeBlockInfo = typeStack(index)

                Dim children = ImmutableArray(Of SingleTypeDeclaration).Empty
                Dim members As SyntaxList(Of StatementSyntax) = typeEntry.TypeBlockSyntax.Members
                If members.Count > 0 Then
                    childrenBuilder.Clear()

                    For Each member In members
                        Select Case member.Kind
                            Case SyntaxKind.ModuleBlock,
                                 SyntaxKind.ClassBlock,
                                 SyntaxKind.StructureBlock,
                                 SyntaxKind.InterfaceBlock
                                ' should be processed already

                            Case Else
                                Dim typeDecl = TryCast(Visit(member), SingleTypeDeclaration)
                                If typeDecl IsNot Nothing Then
                                    childrenBuilder.Add(typeDecl)
                                End If

                        End Select
                    Next

                    Dim nestedTypes As ArrayBuilder(Of Integer) = typeEntry.NestedTypes
                    If nestedTypes IsNot Nothing Then
                        For i = 0 To nestedTypes.Count - 1
                            childrenBuilder.Add(typeStack(nestedTypes(i)).TypeDeclaration)
                        Next
                        nestedTypes.Free()
                    End If

                    children = childrenBuilder.ToImmutable()
                End If

                Dim typeBlockSyntax As TypeBlockSyntax = typeEntry.TypeBlockSyntax
                Dim declarationSyntax As TypeStatementSyntax = typeBlockSyntax.BlockStatement

                ' Get the arity for things that can have arity.
                Dim typeArity As Integer = 0
                Select Case typeBlockSyntax.Kind
                    Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock
                        typeArity = GetArity(declarationSyntax.TypeParameterList)
                End Select

                Dim declFlags As SingleTypeDeclaration.TypeDeclarationFlags = If(declarationSyntax.AttributeLists.Any(),
                            SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes,
                            SingleTypeDeclaration.TypeDeclarationFlags.None)

                If (typeBlockSyntax.Inherits.Any) Then
                    declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.HasBaseDeclarations
                End If

                Dim memberNames = GetNonTypeMemberNames(typeBlockSyntax.Members, declFlags)

                typeStack(index) = typeEntry.WithDeclaration(
                    New SingleTypeDeclaration(
                        kind:=GetKind(declarationSyntax.Kind),
                        name:=declarationSyntax.Identifier.ValueText,
                        arity:=typeArity,
                        modifiers:=GetModifiers(declarationSyntax.Modifiers),
                        declFlags:=declFlags,
                        syntaxReference:=_syntaxTree.GetReference(typeBlockSyntax),
                        nameLocation:=_syntaxTree.GetLocation(typeBlockSyntax.BlockStatement.Identifier.Span),
                        memberNames:=memberNames,
                        children:=children))
            End While
            childrenBuilder.Free()

            Dim result As SingleNamespaceOrTypeDeclaration = typeStack(0).TypeDeclaration
            typeStack.Free()

            Return result
        End Function

        Public Overrides Function VisitModuleBlock(ByVal moduleBlockSyntax As ModuleBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Return VisitTypeBlockNew(moduleBlockSyntax)
        End Function

        Public Overrides Function VisitClassBlock(ByVal classBlockSyntax As ClassBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Return VisitTypeBlockNew(classBlockSyntax)
        End Function

        Public Overrides Function VisitStructureBlock(ByVal structureBlockSyntax As StructureBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Return VisitTypeBlockNew(structureBlockSyntax)
        End Function

        Public Overrides Function VisitInterfaceBlock(ByVal interfaceBlockSyntax As InterfaceBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Return VisitTypeBlockNew(interfaceBlockSyntax)
        End Function

        Public Overrides Function VisitEnumBlock(enumBlockSyntax As EnumBlockSyntax) As SingleNamespaceOrTypeDeclaration
            Dim declarationSyntax As EnumStatementSyntax = enumBlockSyntax.EnumStatement

            Dim declFlags As SingleTypeDeclaration.TypeDeclarationFlags = If(declarationSyntax.AttributeLists.Any(),
                SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes,
                SingleTypeDeclaration.TypeDeclarationFlags.None)

            If (declarationSyntax.UnderlyingType IsNot Nothing) Then
                declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.HasBaseDeclarations
            End If

            Dim memberNames = GetMemberNames(enumBlockSyntax, declFlags)

            Return New SingleTypeDeclaration(
                kind:=GetKind(declarationSyntax.Kind),
                name:=declarationSyntax.Identifier.ValueText,
                arity:=0,
                modifiers:=GetModifiers(declarationSyntax.Modifiers),
                declFlags:=declFlags,
                syntaxReference:=_syntaxTree.GetReference(enumBlockSyntax),
                nameLocation:=_syntaxTree.GetLocation(enumBlockSyntax.EnumStatement.Identifier.Span),
                memberNames:=memberNames,
                children:=VisitTypeChildren(enumBlockSyntax.Members))
        End Function

        Private Function VisitTypeChildren(members As SyntaxList(Of StatementSyntax)) As ImmutableArray(Of SingleTypeDeclaration)
            If members.Count = 0 Then
                Return ImmutableArray(Of SingleTypeDeclaration).Empty
            End If

            Dim children = ArrayBuilder(Of SingleTypeDeclaration).GetInstance()
            For Each member In members
                Dim typeDecl = TryCast(Visit(member), SingleTypeDeclaration)
                If typeDecl IsNot Nothing Then
                    children.Add(typeDecl)
                End If
            Next

            Return children.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Pool of builders used to create our member name sets.  Importantly, these use 
        ''' <see cref="CaseInsensitiveComparison.Comparer"/> so that name lookup happens in an
        ''' appropriate manner for VB identifiers. This allows fast member name O(log(n)) even if
        ''' the casing doesn't match.
        ''' </summary>
        Private Shared ReadOnly s_memberNameBuilderPool As New ObjectPool(Of ImmutableHashSet(Of String).Builder)(
            Function() ImmutableHashSet.CreateBuilder(IdentifierComparison.Comparer))

        Private Shared Function ToImmutableAndFree(builder As ImmutableHashSet(Of String).Builder) As ImmutableHashSet(Of String)
            Dim result = builder.ToImmutable()
            builder.Clear()
            s_memberNameBuilderPool.Free(builder)
            Return result
        End Function

        Private Function GetNonTypeMemberNames(members As SyntaxList(Of StatementSyntax), ByRef declFlags As SingleTypeDeclaration.TypeDeclarationFlags) As ImmutableHashSet(Of String)
            Dim anyMethodHadExtensionSyntax = False
            Dim anyMemberHasAttributes = False
            Dim anyNonTypeMembers = False

            Dim results = s_memberNameBuilderPool.Allocate()

            For Each statement In members
                Select Case statement.Kind
                    Case SyntaxKind.FieldDeclaration
                        anyNonTypeMembers = True
                        Dim field = DirectCast(statement, FieldDeclarationSyntax)
                        If field.AttributeLists.Any Then
                            anyMemberHasAttributes = True
                        End If

                        For Each decl In field.Declarators
                            For Each name In decl.Names
                                results.Add(name.Identifier.ValueText)
                            Next
                        Next

                    Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock, SyntaxKind.OperatorBlock
                        anyNonTypeMembers = True
                        Dim methodDecl = DirectCast(statement, MethodBlockBaseSyntax).BlockStatement
                        If methodDecl.AttributeLists.Any Then
                            anyMemberHasAttributes = True
                        End If
                        AddMemberNames(methodDecl, results)

                    Case SyntaxKind.PropertyBlock
                        anyNonTypeMembers = True
                        Dim propertyDecl = DirectCast(statement, PropertyBlockSyntax)
                        If propertyDecl.PropertyStatement.AttributeLists.Any Then
                            anyMemberHasAttributes = True
                        Else
                            For Each a In propertyDecl.Accessors
                                If a.BlockStatement.AttributeLists.Any Then
                                    anyMemberHasAttributes = True
                                End If
                            Next
                        End If
                        AddMemberNames(propertyDecl.PropertyStatement, results)

                    Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement,
                         SyntaxKind.SubNewStatement, SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement, SyntaxKind.OperatorStatement,
                         SyntaxKind.PropertyStatement

                        anyNonTypeMembers = True
                        Dim methodDecl = DirectCast(statement, MethodBaseSyntax)
                        If methodDecl.AttributeLists.Any Then
                            anyMemberHasAttributes = True
                        End If
                        AddMemberNames(methodDecl, results)

                    Case SyntaxKind.EventBlock
                        anyNonTypeMembers = True
                        Dim eventDecl = DirectCast(statement, EventBlockSyntax)
                        If eventDecl.EventStatement.AttributeLists.Any Then
                            anyMemberHasAttributes = True
                        Else
                            For Each a In eventDecl.Accessors
                                If a.BlockStatement.AttributeLists.Any Then
                                    anyMemberHasAttributes = True
                                End If
                            Next
                        End If
                        Dim name = eventDecl.EventStatement.Identifier.ValueText
                        results.Add(name)

                    Case SyntaxKind.EventStatement
                        anyNonTypeMembers = True
                        Dim eventDecl = DirectCast(statement, EventStatementSyntax)
                        If eventDecl.AttributeLists.Any Then
                            anyMemberHasAttributes = True
                        End If
                        Dim name = eventDecl.Identifier.ValueText
                        results.Add(name)
                End Select
            Next

            If (anyMemberHasAttributes) Then
                declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes
            End If

            If (anyNonTypeMembers) Then
                declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers
            End If

            Return ToImmutableAndFree(results)
        End Function

        Private Function GetMemberNames(enumBlockSyntax As EnumBlockSyntax, ByRef declFlags As SingleTypeDeclaration.TypeDeclarationFlags) As ImmutableHashSet(Of String)
            Dim members = enumBlockSyntax.Members

            If (members.Count <> 0) Then
                declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers
            End If

            Dim results = s_memberNameBuilderPool.Allocate()
            Dim anyMemberHasAttributes As Boolean = False

            For Each member In enumBlockSyntax.Members
                ' skip empty statements that represent invalid syntax in the Enum:
                If member.Kind = SyntaxKind.EnumMemberDeclaration Then
                    Dim enumMember = DirectCast(member, EnumMemberDeclarationSyntax)
                    results.Add(enumMember.Identifier.ValueText)

                    If Not anyMemberHasAttributes AndAlso enumMember.AttributeLists.Any Then
                        anyMemberHasAttributes = True
                    End If
                End If
            Next

            If (anyMemberHasAttributes) Then
                declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes
            End If

            Return ToImmutableAndFree(results)
        End Function

        Private Sub AddMemberNames(methodDecl As MethodBaseSyntax, results As ImmutableHashSet(Of String).Builder)
            Dim name = SourceMethodSymbol.GetMemberNameFromSyntax(methodDecl)
            results.Add(name)
        End Sub

        Public Overrides Function VisitDelegateStatement(node As DelegateStatementSyntax) As SingleNamespaceOrTypeDeclaration
            Dim declFlags As SingleTypeDeclaration.TypeDeclarationFlags = If(node.AttributeLists.Any(),
                    SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes,
                    SingleTypeDeclaration.TypeDeclarationFlags.None)

            declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers

            Return New SingleTypeDeclaration(
                kind:=DeclarationKind.Delegate,
                name:=node.Identifier.ValueText,
                arity:=GetArity(node.TypeParameterList),
                modifiers:=GetModifiers(node.Modifiers),
                declFlags:=declFlags,
                syntaxReference:=_syntaxTree.GetReference(node),
                nameLocation:=_syntaxTree.GetLocation(node.Identifier.Span),
                memberNames:=ImmutableHashSet(Of String).Empty,
                children:=ImmutableArray(Of SingleTypeDeclaration).Empty)
        End Function

        Public Overrides Function VisitEventStatement(node As EventStatementSyntax) As SingleNamespaceOrTypeDeclaration
            If node.AsClause IsNot Nothing OrElse node.ImplementsClause IsNot Nothing Then
                ' this event will not need a type
                Return Nothing
            End If

            Dim declFlags As SingleTypeDeclaration.TypeDeclarationFlags = If(node.AttributeLists.Any(),
                SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes,
                SingleTypeDeclaration.TypeDeclarationFlags.None)

            declFlags = declFlags Or SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers

            Return New SingleTypeDeclaration(
                kind:=DeclarationKind.EventSyntheticDelegate,
                name:=node.Identifier.ValueText,
                arity:=0,
                modifiers:=GetModifiers(node.Modifiers),
                declFlags:=declFlags,
                syntaxReference:=_syntaxTree.GetReference(node),
                nameLocation:=_syntaxTree.GetLocation(node.Identifier.Span),
                memberNames:=ImmutableHashSet(Of String).Empty,
                children:=ImmutableArray(Of SingleTypeDeclaration).Empty)
        End Function

        ' Public because BinderCache uses it also.
        Public Shared Function GetKind(kind As SyntaxKind) As DeclarationKind
            Select Case kind
                Case SyntaxKind.ClassStatement : Return DeclarationKind.Class
                Case SyntaxKind.InterfaceStatement : Return DeclarationKind.Interface
                Case SyntaxKind.StructureStatement : Return DeclarationKind.Structure
                Case SyntaxKind.NamespaceStatement : Return DeclarationKind.Namespace
                Case SyntaxKind.ModuleStatement : Return DeclarationKind.Module
                Case SyntaxKind.EnumStatement : Return DeclarationKind.Enum
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement : Return DeclarationKind.Delegate
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function

        ' Public because BinderCache uses it also.
        Public Shared Function GetArity(typeParamsSyntax As TypeParameterListSyntax) As Integer
            If typeParamsSyntax Is Nothing Then
                Return 0
            Else
                Return typeParamsSyntax.Parameters.Count
            End If
        End Function

        Private Shared Function GetModifiers(modifiers As SyntaxTokenList) As DeclarationModifiers
            Dim result As DeclarationModifiers = DeclarationModifiers.None

            For Each modifier In modifiers
                Dim bit As DeclarationModifiers = 0
                Select Case modifier.Kind
                    Case SyntaxKind.MustInheritKeyword : bit = DeclarationModifiers.MustInherit
                    Case SyntaxKind.NotInheritableKeyword : bit = DeclarationModifiers.NotInheritable
                    Case SyntaxKind.PartialKeyword : bit = DeclarationModifiers.Partial
                    Case SyntaxKind.ShadowsKeyword : bit = DeclarationModifiers.Shadows
                    Case SyntaxKind.PublicKeyword : bit = DeclarationModifiers.Public
                    Case SyntaxKind.ProtectedKeyword : bit = DeclarationModifiers.Protected
                    Case SyntaxKind.FriendKeyword : bit = DeclarationModifiers.Friend
                    Case SyntaxKind.PrivateKeyword : bit = DeclarationModifiers.Private
                    Case SyntaxKind.ShadowsKeyword : bit = DeclarationModifiers.Shadows
                    Case SyntaxKind.MustInheritKeyword : bit = DeclarationModifiers.MustInherit
                    Case SyntaxKind.NotInheritableKeyword : bit = DeclarationModifiers.NotInheritable
                    Case SyntaxKind.PartialKeyword : bit = DeclarationModifiers.Partial
                    Case SyntaxKind.SharedKeyword : bit = DeclarationModifiers.Shared
                    Case SyntaxKind.ReadOnlyKeyword : bit = DeclarationModifiers.ReadOnly
                    Case SyntaxKind.WriteOnlyKeyword : bit = DeclarationModifiers.WriteOnly
                    Case SyntaxKind.OverridesKeyword : bit = DeclarationModifiers.Overrides
                    Case SyntaxKind.OverridableKeyword : bit = DeclarationModifiers.Overridable
                    Case SyntaxKind.MustOverrideKeyword : bit = DeclarationModifiers.MustOverride
                    Case SyntaxKind.NotOverridableKeyword : bit = DeclarationModifiers.NotOverridable
                    Case SyntaxKind.OverloadsKeyword : bit = DeclarationModifiers.Overloads
                    Case SyntaxKind.WithEventsKeyword : bit = DeclarationModifiers.WithEvents
                    Case SyntaxKind.DimKeyword : bit = DeclarationModifiers.Dim
                    Case SyntaxKind.ConstKeyword : bit = DeclarationModifiers.Const
                    Case SyntaxKind.DefaultKeyword : bit = DeclarationModifiers.Default
                    Case SyntaxKind.StaticKeyword : bit = DeclarationModifiers.Static
                    Case SyntaxKind.WideningKeyword : bit = DeclarationModifiers.Widening
                    Case SyntaxKind.NarrowingKeyword : bit = DeclarationModifiers.Narrowing
                    Case SyntaxKind.AsyncKeyword : bit = DeclarationModifiers.Async
                    Case SyntaxKind.IteratorKeyword : bit = DeclarationModifiers.Iterator

                    Case Else
                        ' It is possible to run into other tokens here, but only in error conditions.
                        ' We are going to ignore them here.
                        If Not modifier.GetDiagnostics().Any(Function(d) d.Severity = DiagnosticSeverity.Error) Then
                            Throw ExceptionUtilities.UnexpectedValue(modifier.Kind)
                        End If
                End Select

                result = result Or bit
            Next

            Return result
        End Function
    End Class
End Namespace
