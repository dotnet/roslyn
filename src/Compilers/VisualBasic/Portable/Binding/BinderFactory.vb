' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The <see cref="BinderFactory"/> class finds the correct Binder to use for a node in a syntax
    ''' tree, down to method level. Within a method, the <see cref="ExecutableCodeBinder"/> has a
    ''' cache of further binders within the method.
    ''' 
    ''' The <see cref="BinderFactory"/> caches results so that binders are efficiently reused between queries.
    ''' </summary>
    Partial Friend Class BinderFactory
        Private ReadOnly _sourceModule As SourceModuleSymbol
        Private ReadOnly _tree As SyntaxTree

        ' Some syntax nodes, like method blocks, have multiple different binders associated with them
        ' for different parts of the method (the body, where parameters are available, and the header, where
        ' parameters aren't). To be able to cache a few different binders for each syntax node, we use
        ' a NodeUsage to sub-distinguish binders associated with nodes. Each kind of syntax node must have its
        ' associated usage value(s), because the usage is used when creating the binder (if not found in the cache).
        Private ReadOnly _cache As ConcurrentDictionary(Of ValueTuple(Of VisualBasicSyntaxNode, Byte), Binder)
        Private ReadOnly _binderFactoryVisitorPool As ObjectPool(Of BinderFactoryVisitor)

        Private ReadOnly Property InScript As Boolean
            Get
                Return _tree.Options.Kind = SourceCodeKind.Script
            End Get
        End Property

        Public Sub New(sourceModule As SourceModuleSymbol, tree As SyntaxTree)
            Me._sourceModule = sourceModule
            Me._tree = tree
            Me._cache = New ConcurrentDictionary(Of ValueTuple(Of VisualBasicSyntaxNode, Byte), Binder)

            Me._binderFactoryVisitorPool = New ObjectPool(Of BinderFactoryVisitor)(Function() New BinderFactoryVisitor(Me))
        End Sub

        Private Function MakeBinder(node As VisualBasicSyntaxNode, position As Integer) As Binder
            If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(node, position) OrElse node.Kind = SyntaxKind.CompilationUnit Then
                Dim visitor = _binderFactoryVisitorPool.Allocate()
                visitor.Position = position
                Dim result = node.Accept(visitor)
                _binderFactoryVisitorPool.Free(visitor)
                Return result
            Else
                Return Nothing
            End If
        End Function

        ' Get binder for interior of a namespace block
        Public Function GetNamespaceBinder(node As NamespaceBlockSyntax) As Binder
            Return GetBinderForNodeAndUsage(node, NodeUsage.NamespaceBlockInterior, node.Parent, node.SpanStart)
        End Function

        ' Get binder for a type
        Public Function GetNamedTypeBinder(node As TypeStatementSyntax) As Binder
            Dim possibleParentBlock = TryCast(node.Parent, TypeBlockSyntax)
            Dim parentForEnclosingBinder As VisualBasicSyntaxNode = If(possibleParentBlock IsNot Nothing, possibleParentBlock.Parent, node.Parent)

            Return GetBinderForNodeAndUsage(node, NodeUsage.TypeBlockFull, parentForEnclosingBinder, node.SpanStart)
        End Function

        ' Get binder for an enum
        Public Function GetNamedTypeBinder(node As EnumStatementSyntax) As Binder
            Dim possibleParentBlock = TryCast(node.Parent, EnumBlockSyntax)
            Dim parentForEnclosingBinder As VisualBasicSyntaxNode = If(possibleParentBlock IsNot Nothing, possibleParentBlock.Parent, node.Parent)

            Return GetBinderForNodeAndUsage(node, NodeUsage.EnumBlockFull, parentForEnclosingBinder, node.SpanStart)
        End Function

        ' Get binder for a delegate
        Public Function GetNamedTypeBinder(node As DelegateStatementSyntax) As Binder
            Return GetBinderForNodeAndUsage(node, NodeUsage.DelegateDeclaration, node.Parent, node.SpanStart)
        End Function

        ' Find the binder to use for a position in the tree. The position should have been adjusted
        ' already to be at the start of a token.
        Public Function GetBinderForPosition(node As VisualBasicSyntaxNode, position As Integer) As Binder
            Return GetBinderAtOrAbove(node, position)
        End Function

        ' Find the binder for a node or above at a given position
        Private Function GetBinderAtOrAbove(node As VisualBasicSyntaxNode, position As Integer) As Binder
            ' Go up the tree until we find a node that has a corresponding binder.
            Do
                Dim binder As Binder = MakeBinder(node, position)
                If binder IsNot Nothing Then
                    Return binder
                End If

                If node.Kind = SyntaxKind.DocumentationCommentTrivia Then
                    node = DirectCast(DirectCast(node, StructuredTriviaSyntax).ParentTrivia.Token.Parent, VisualBasicSyntaxNode)
                Else
                    node = node.Parent
                End If

                ' We should always find a binder, because the compilation unit should always give a binder,
                ' and going up the parent node chain should always get us a compilation unit.
                Debug.Assert(node IsNot Nothing, "We should always get a binder")
            Loop
        End Function

        ' Given a node and usage, find the correct binder to use. Use the cache first, if not in the cache, then
        ' create a new binder. The parent node and position are used if we need to find an enclosing binder unless specified explicitly.
        Private Function GetBinderForNodeAndUsage(node As VisualBasicSyntaxNode,
                                                  usage As NodeUsage,
                                                  Optional parentNode As VisualBasicSyntaxNode = Nothing,
                                                  Optional position As Integer = -1,
                                                  Optional containingBinder As Binder = Nothing) As Binder

            ' either parentNode and position is specified or the containingBinder is specified
            Debug.Assert((parentNode Is Nothing) = (position < 0))
            Debug.Assert(containingBinder Is Nothing OrElse parentNode Is Nothing)

            Dim binder As Binder = Nothing
            Dim nodeUsagePair = ValueTuple.Create(node, CByte(usage))

            If Not _cache.TryGetValue(nodeUsagePair, binder) Then
                ' Didn't find it in the cache, so we need to create it. But we need the containing binder first.
                If containingBinder Is Nothing AndAlso parentNode IsNot Nothing Then
                    containingBinder = GetBinderAtOrAbove(parentNode, position)
                End If

                binder = CreateBinderForNodeAndUsage(node, usage, containingBinder)
                _cache.TryAdd(nodeUsagePair, binder)
            End If

            Return binder
        End Function

        ' Given a node, usage, and containing binder, create the binder for this node and usage. This is called
        ' only when we've found the given node & usage are not cached (and the caller will cache the result).
        Private Function CreateBinderForNodeAndUsage(node As VisualBasicSyntaxNode,
                                                     usage As NodeUsage,
                                                     containingBinder As Binder) As Binder
            Select Case usage
                Case NodeUsage.CompilationUnit
                    ' Get the binder associated with the default project namespace
                    Return BinderBuilder.CreateBinderForNamespace(_sourceModule, _tree, _sourceModule.RootNamespace)

                Case NodeUsage.ImplicitClass
                    Dim implicitType As NamedTypeSymbol
                    If node.Kind <> SyntaxKind.CompilationUnit OrElse _tree.Options.Kind = SourceCodeKind.Regular Then
                        implicitType = DirectCast(containingBinder.ContainingNamespaceOrType.GetMembers(TypeSymbol.ImplicitTypeName).Single(), NamedTypeSymbol)
                    Else
                        implicitType = _sourceModule.ContainingSourceAssembly.DeclaringCompilation.SourceScriptClass
                    End If
                    Return New NamedTypeBinder(containingBinder, implicitType)

                Case NodeUsage.ScriptCompilationUnit
                    Dim rootNamespaceBinder = GetBinderForNodeAndUsage(node, NodeUsage.CompilationUnit)
                    Debug.Assert(TypeOf rootNamespaceBinder Is NamespaceBinder)

                    ' TODO (tomat): this is just a simple temporary solution, we'll need to plug-in submissions, interactive imports and host object members:
                    Return New NamedTypeBinder(rootNamespaceBinder, _sourceModule.ContainingSourceAssembly.DeclaringCompilation.SourceScriptClass)

                Case NodeUsage.TopLevelExecutableStatement
                    Debug.Assert(TypeOf containingBinder Is NamedTypeBinder AndAlso containingBinder.ContainingType.IsScriptClass)
                    Return New TopLevelCodeBinder(containingBinder.ContainingType.InstanceConstructors.Single(), containingBinder)

                Case NodeUsage.ImportsStatement
                    Return BinderBuilder.CreateBinderForSourceFileImports(_sourceModule, _tree)

                Case NodeUsage.NamespaceBlockInterior
                    Dim nsBlockSyntax = DirectCast(node, NamespaceBlockSyntax)

                    Dim containingNamespaceBinder = TryCast(containingBinder, NamespaceBinder)
                    If containingNamespaceBinder Is Nothing Then
                        ' If the containing binder is a script class binder use its namespace as a containing binder.
                        ' It is an error 
                        Dim containingNamedTypeBinder = TryCast(containingBinder, NamedTypeBinder)
                        If containingNamedTypeBinder IsNot Nothing AndAlso containingNamedTypeBinder.ContainingType.IsScriptClass Then
                            Dim rootNamespaceBinder = GetBinderForNodeAndUsage(node, NodeUsage.CompilationUnit)
                            containingNamespaceBinder = DirectCast(rootNamespaceBinder, NamespaceBinder)
                        End If
                    End If

                    If containingNamespaceBinder IsNot Nothing Then
                        Return BuildNamespaceBinder(containingNamespaceBinder, nsBlockSyntax.NamespaceStatement.Name, nsBlockSyntax.Parent.Kind = SyntaxKind.CompilationUnit)
                    End If

                    Return containingBinder   ' This occurs is some edge case error, like declaring a namespace inside a class.

                Case NodeUsage.TypeBlockFull
                    Dim declarationSyntax = DirectCast(node, TypeStatementSyntax)

                    Dim symbol = SourceNamedTypeSymbol.FindSymbolFromSyntax(declarationSyntax,
                                                                            containingBinder.ContainingNamespaceOrType,
                                                                            _sourceModule)

                    '  if symbol is invalid, we might be dealing with different error cases like class/namespace/class declaration
                    Return If(symbol IsNot Nothing, New NamedTypeBinder(containingBinder, symbol), containingBinder)

                Case NodeUsage.EnumBlockFull
                    Dim declarationSyntax = DirectCast(node, EnumStatementSyntax)

                    Dim symbol = SourceNamedTypeSymbol.FindSymbolFromSyntax(declarationSyntax,
                                                                            containingBinder.ContainingNamespaceOrType,
                                                                            _sourceModule)

                    '  if symbol is invalid, we might be dealing with different error cases like class/namespace/class declaration
                    Return If(symbol IsNot Nothing, New NamedTypeBinder(containingBinder, symbol), containingBinder)

                Case NodeUsage.DelegateDeclaration
                    Dim delegateSyntax = DirectCast(node, DelegateStatementSyntax)

                    Dim symbol = SourceNamedTypeSymbol.FindSymbolFromSyntax(delegateSyntax,
                                                                            containingBinder.ContainingNamespaceOrType,
                                                                            _sourceModule)

                    '  if symbol is invalid, we might be dealing with different error cases like class/namespace/class declaration
                    Return If(symbol IsNot Nothing, New NamedTypeBinder(containingBinder, symbol), containingBinder)

                Case NodeUsage.InheritsStatement
                    Dim containingNamedTypeBinder = TryCast(containingBinder, NamedTypeBinder)

                    If containingNamedTypeBinder IsNot Nothing Then
                        ' When binding the inherits clause, we don't want to look to base types of our own type, to follow how actual
                        ' determination of the base type is done. This is done by using a BasesBeingResolvedBinder.
                        Debug.Assert(containingNamedTypeBinder.ContainingType IsNot Nothing)
                        Return New BasesBeingResolvedBinder(containingBinder, ConsList(Of Symbol).Empty.Prepend(containingNamedTypeBinder.ContainingType))
                    Else
                        Return containingBinder
                    End If

                Case NodeUsage.PropertyFull
                    Return GetContainingNamedTypeBinderForMemberNode(DirectCast(node, PropertyStatementSyntax).Parent.Parent, containingBinder)

                Case NodeUsage.MethodFull, NodeUsage.MethodInterior
                    Dim methodBase = DirectCast(node, MethodBaseSyntax)

                    Dim containingNamedTypeBinder = GetContainingNamedTypeBinderForMemberNode(node.Parent.Parent, containingBinder)
                    If containingNamedTypeBinder Is Nothing Then
                        Return containingBinder
                    End If

                    ' UNDONE: Remove this once we can create MethodSymbols for other kinds of declarations.
                    Select Case methodBase.Kind
                        Case SyntaxKind.FunctionStatement,
                            SyntaxKind.SubStatement,
                            SyntaxKind.SubNewStatement,
                            SyntaxKind.GetAccessorStatement,
                            SyntaxKind.SetAccessorStatement,
                            SyntaxKind.AddHandlerAccessorStatement,
                            SyntaxKind.RemoveHandlerAccessorStatement,
                            SyntaxKind.RaiseEventAccessorStatement,
                            SyntaxKind.OperatorStatement
                        Case Else
                            Return containingBinder
                    End Select

                    Return BuildMethodBinder(containingNamedTypeBinder, methodBase, (usage = NodeUsage.MethodInterior))

                Case NodeUsage.FieldOrPropertyInitializer
                    Dim fieldOrProperty As Symbol = Nothing
                    Dim containingNamedTypeBinder As NamedTypeBinder

                    Select Case node.Kind
                        Case SyntaxKind.VariableDeclarator
                            Dim declarator = DirectCast(node, VariableDeclaratorSyntax)
                            ' Declaration should have initializer or AsNew and exactly one variable name.
                            Debug.Assert(declarator.Initializer IsNot Nothing OrElse TryCast(declarator.AsClause, AsNewClauseSyntax) IsNot Nothing)
                            ' more than one name may happen if there is a syntax error
                            Debug.Assert(declarator.Names.Count > 0)

                            containingNamedTypeBinder = GetContainingNamedTypeBinderForMemberNode(node.Parent.Parent, containingBinder)
                            If containingNamedTypeBinder Is Nothing Then
                                Return Nothing
                            End If

                            Dim identifier = declarator.Names(0).Identifier
                            fieldOrProperty = containingNamedTypeBinder.ContainingType.FindFieldOrProperty(identifier.ValueText, identifier.Span, _tree)

                        Case SyntaxKind.EnumMemberDeclaration
                            Dim enumDeclaration = DirectCast(node, EnumMemberDeclarationSyntax)
                            Debug.Assert(enumDeclaration.Initializer IsNot Nothing)

                            containingNamedTypeBinder = DirectCast(containingBinder, NamedTypeBinder)

                            Dim identifier = enumDeclaration.Identifier
                            fieldOrProperty = containingNamedTypeBinder.ContainingType.FindMember(identifier.ValueText, SymbolKind.Field, identifier.Span, _tree)

                        Case SyntaxKind.PropertyStatement
                            Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                            Debug.Assert(propertyStatement.Initializer IsNot Nothing OrElse TryCast(propertyStatement.AsClause, AsNewClauseSyntax) IsNot Nothing)

                            containingNamedTypeBinder = GetContainingNamedTypeBinderForMemberNode(node.Parent, containingBinder)
                            If containingNamedTypeBinder Is Nothing Then
                                Return Nothing
                            End If

                            Dim identifier = propertyStatement.Identifier
                            fieldOrProperty = containingNamedTypeBinder.ContainingType.FindMember(identifier.ValueText, SymbolKind.Property, identifier.Span, _tree)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(node.Kind)

                    End Select

                    If fieldOrProperty IsNot Nothing Then
                        Return BuildInitializerBinder(containingNamedTypeBinder, fieldOrProperty)
                    End If

                    Return Nothing

                Case NodeUsage.FieldArrayBounds
                    Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
                    Dim containingNamedTypeBinder = TryCast(containingBinder, NamedTypeBinder)

                    If containingNamedTypeBinder IsNot Nothing Then
                        Dim containingType = containingNamedTypeBinder.ContainingType
                        Dim identifier = modifiedIdentifier.Identifier
                        Dim field = containingType.FindMember(identifier.ValueText, SymbolKind.Field, identifier.Span, _tree)
                        If field IsNot Nothing Then
                            Return BuildInitializerBinder(containingNamedTypeBinder, field)
                        End If
                    End If

                    Return Nothing

                Case NodeUsage.Attribute
                    Return BuildAttributeBinder(containingBinder, node)

                Case NodeUsage.ParameterDefaultValue
                    Dim parameterSyntax = DirectCast(node, ParameterSyntax)

                    If parameterSyntax.Default IsNot Nothing Then
                        Dim parameterListSyntax = DirectCast(parameterSyntax.Parent, ParameterListSyntax)
                        Dim methodSyntax = DirectCast(parameterListSyntax.Parent, MethodBaseSyntax)
                        Dim parameterSymbol As ParameterSymbol = Nothing

                        Select Case methodSyntax.Kind
                            Case SyntaxKind.SubNewStatement,
                                SyntaxKind.FunctionStatement,
                                SyntaxKind.SubStatement,
                                SyntaxKind.DeclareFunctionStatement,
                                SyntaxKind.DeclareSubStatement
                                Dim containingType = GetParameterDeclarationContainingType(containingBinder)
                                If containingType IsNot Nothing Then
                                    Dim methodSymbol = DirectCast(SourceMethodSymbol.FindSymbolFromSyntax(methodSyntax, _tree, containingType), SourceMethodSymbol)
                                    If methodSymbol IsNot Nothing Then
                                        parameterSymbol = GetParameterSymbol(methodSymbol.Parameters, parameterSyntax)
                                    End If
                                End If

                            Case SyntaxKind.DelegateFunctionStatement,
                                SyntaxKind.DelegateSubStatement
                                Dim containingType = GetParameterDeclarationContainingType(containingBinder)
                                If containingType IsNot Nothing AndAlso
                                    containingType.TypeKind = TypeKind.Delegate Then
                                    Dim invokeSymbol = containingType.DelegateInvokeMethod
                                    Debug.Assert(invokeSymbol IsNot Nothing, "Delegate should always have an invoke method.")
                                    parameterSymbol = GetParameterSymbol(invokeSymbol.Parameters, parameterSyntax)
                                End If

                            Case SyntaxKind.EventStatement
                                Dim containingType = GetParameterDeclarationContainingType(containingBinder)
                                If containingType IsNot Nothing Then
                                    Dim eventSymbol = DirectCast(SourceMethodSymbol.FindSymbolFromSyntax(methodSyntax, _tree, containingType), SourceEventSymbol)
                                    If eventSymbol IsNot Nothing Then
                                        parameterSymbol = GetParameterSymbol(eventSymbol.DelegateParameters, parameterSyntax)
                                    End If
                                End If

                            Case SyntaxKind.PropertyStatement
                                Dim containingType = GetParameterDeclarationContainingType(containingBinder)
                                If containingType IsNot Nothing Then
                                    Dim propertySymbol = DirectCast(SourceMethodSymbol.FindSymbolFromSyntax(methodSyntax, _tree, containingType), SourcePropertySymbol)
                                    If propertySymbol IsNot Nothing Then
                                        parameterSymbol = GetParameterSymbol(propertySymbol.Parameters, parameterSyntax)
                                    End If
                                End If

                            Case SyntaxKind.FunctionLambdaHeader,
                                 SyntaxKind.SubLambdaHeader,
                                 SyntaxKind.SetAccessorStatement,
                                 SyntaxKind.GetAccessorStatement,
                                 SyntaxKind.AddHandlerAccessorStatement,
                                 SyntaxKind.RemoveHandlerAccessorStatement,
                                 SyntaxKind.RaiseEventAccessorStatement
                                ' Default values are not valid (and not bound) for lambda parameters or property accessors
                                Return Nothing

                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(methodSyntax.Kind)

                        End Select

                        If parameterSymbol IsNot Nothing Then
                            Return BinderBuilder.CreateBinderForParameterDefaultValue(parameterSymbol, containingBinder, parameterSyntax)
                        End If
                    End If

                    Return Nothing

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(usage)
            End Select
        End Function

        Private Function CreateDocumentationCommentBinder(node As DocumentationCommentTriviaSyntax, binderType As DocumentationCommentBinder.BinderType) As Binder
            Debug.Assert(binderType <> DocumentationCommentBinder.BinderType.None)

            ' Now we need to find a symbol for class/structure, method, event, or property
            ' Those may be needed to bind parameters and/or type parameters
            ' Note that we actually don't need field/module/enum symbols, because they 
            ' do not have type parameters or parameters
            Dim trivia As SyntaxTrivia = node.ParentTrivia
            Dim token As SyntaxToken = CType(trivia.Token, SyntaxToken)
            Dim parent = DirectCast(token.Parent, VisualBasicSyntaxNode)
            Debug.Assert(parent IsNot Nothing)

            ' This is a binder for commented symbol's containing type or namespace 
            Dim nodeForOuterBinder As VisualBasicSyntaxNode = Nothing
lAgain:
            Select Case parent.Kind
                Case SyntaxKind.ClassStatement,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.ModuleStatement

                    ' BREAK: Roslyn uses the type binder, whereas Dev11 uses the scope strictly above the type declaration.
                    ' This change was made to improve the consistency with C#.  In particular, it allows unqualified references
                    ' to members of the type to which the doc comment has been applied.
                    nodeForOuterBinder = parent

                Case SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.OperatorStatement


                    ' Delegates don't have user-defined members, so it makes more sense to treat
                    ' them like methods.

                    nodeForOuterBinder = parent.Parent
                    If nodeForOuterBinder IsNot Nothing AndAlso TypeOf (nodeForOuterBinder) Is MethodBlockBaseSyntax Then
                        nodeForOuterBinder = nodeForOuterBinder.Parent
                    End If

                Case SyntaxKind.PropertyStatement
                    nodeForOuterBinder = parent.Parent
                    If nodeForOuterBinder IsNot Nothing AndAlso nodeForOuterBinder.Kind = SyntaxKind.PropertyBlock Then
                        nodeForOuterBinder = nodeForOuterBinder.Parent
                    End If

                Case SyntaxKind.EventStatement
                    nodeForOuterBinder = parent.Parent
                    If nodeForOuterBinder IsNot Nothing AndAlso nodeForOuterBinder.Kind = SyntaxKind.EventStatement Then
                        nodeForOuterBinder = nodeForOuterBinder.Parent
                    End If

                Case SyntaxKind.FieldDeclaration,
                     SyntaxKind.EnumMemberDeclaration

                    nodeForOuterBinder = parent.Parent

                Case SyntaxKind.AttributeList
                    nodeForOuterBinder = parent.Parent
                    If nodeForOuterBinder IsNot Nothing Then
                        parent = nodeForOuterBinder
                        nodeForOuterBinder = Nothing
                        GoTo lAgain
                    End If

            End Select

            If nodeForOuterBinder Is Nothing Then
                Return GetBinderAtOrAbove(parent, parent.SpanStart)
            End If

            Dim containingBinder As Binder = GetBinderAtOrAbove(nodeForOuterBinder, parent.SpanStart)
            Dim symbol As Symbol = Nothing


            Select Case parent.Kind
                Case SyntaxKind.ClassStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.StructureStatement

                    symbol = containingBinder.ContainingNamespaceOrType

                Case SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.EventStatement

                    If containingBinder.ContainingType IsNot Nothing Then
                        symbol = SourceMethodSymbol.FindSymbolFromSyntax(
                            DirectCast(parent, MethodBaseSyntax), _tree, containingBinder.ContainingType)
                    End If

                Case SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement

                    If containingBinder.ContainingType IsNot Nothing Then
                        symbol = SourceMethodSymbol.FindSymbolFromSyntax(
                            DirectCast(parent, MethodBaseSyntax), _tree, containingBinder.ContainingType)
                    Else
                        symbol = SourceNamedTypeSymbol.FindSymbolFromSyntax(
                            DirectCast(parent, DelegateStatementSyntax), containingBinder.ContainingNamespaceOrType, _sourceModule)
                    End If

                Case SyntaxKind.FieldDeclaration,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.EnumMemberDeclaration,
                     SyntaxKind.ModuleStatement

                    ' we are not using field, enum, module symbols for params or type params resolution

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(parent.Kind)
            End Select

            Return BinderBuilder.CreateBinderForDocumentationComment(containingBinder, symbol, binderType)
        End Function

        Private Function GetContainingNamedTypeBinderForMemberNode(node As VisualBasicSyntaxNode, containingBinder As Binder) As NamedTypeBinder
            Dim containingNamedTypeBinder = TryCast(containingBinder, NamedTypeBinder)
            If containingNamedTypeBinder IsNot Nothing Then
                Return containingNamedTypeBinder
            End If

            ' member declared on top-level or in a namespace is enclosed in an implicit type:
            If node IsNot Nothing AndAlso (node.Kind = SyntaxKind.NamespaceBlock OrElse node.Kind = SyntaxKind.CompilationUnit) Then
                Return DirectCast(
                    GetBinderForNodeAndUsage(node, NodeUsage.ImplicitClass,
                                             containingBinder:=containingBinder), NamedTypeBinder)
            End If

            Return Nothing
        End Function

        Private Shared Function GetParameterDeclarationContainingType(containingBinder As Binder) As NamedTypeSymbol
            ' Method declarations are bound using either a NamedTypeBinder or a MethodTypeParametersBinder
            Dim namedTypeBinder = TryCast(containingBinder, NamedTypeBinder)
            If namedTypeBinder Is Nothing Then
                ' Must be a MethodTypeParametersBinder unless the member
                ' containing the parameter is outside of a type (in invalid code).
                Dim methodDeclarationBinder = TryCast(containingBinder, MethodTypeParametersBinder)
                If methodDeclarationBinder Is Nothing Then
                    Return Nothing
                End If

                namedTypeBinder = DirectCast(methodDeclarationBinder.ContainingBinder, NamedTypeBinder)
            End If

            Return namedTypeBinder.ContainingType
        End Function

        ' Given the name of a child namespace, build up a binder from a containing binder.
        Private Function BuildNamespaceBinder(containingBinder As NamespaceBinder, childName As NameSyntax, globalNamespaceAllowed As Boolean) As NamespaceBinder
            Dim name As String

            Select Case childName.Kind
                Case SyntaxKind.GlobalName
                    If globalNamespaceAllowed Then
                        Return DirectCast(BinderBuilder.CreateBinderForNamespace(_sourceModule, _tree, _sourceModule.GlobalNamespace), NamespaceBinder)
                    Else
                        ' Global namespace isn't allowed here. Use a namespace named "Global" as error recovery (see corresponding code in DeclarationTreeBuilder)
                        name = "Global"
                    End If
                Case SyntaxKind.QualifiedName
                    Dim dotted = DirectCast(childName, QualifiedNameSyntax)
                    containingBinder = BuildNamespaceBinder(containingBinder, dotted.Left, globalNamespaceAllowed)
                    name = dotted.Right.Identifier.ValueText
                Case SyntaxKind.IdentifierName
                    name = DirectCast(childName, IdentifierNameSyntax).Identifier.ValueText
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(childName.Kind)
            End Select

            For Each symbol As NamespaceOrTypeSymbol In containingBinder.NamespaceSymbol.GetMembers(name)
                Dim nsChild = TryCast(symbol, NamespaceSymbol)

                If nsChild IsNot Nothing Then
                    Return New NamespaceBinder(containingBinder, nsChild)
                End If
            Next

            ' Namespace is expected to be found by name in parent binder.
            Throw ExceptionUtilities.Unreachable
        End Function

        ' Given the name of a method, and the span of the name, and the containing type binder, get the 
        ' binder for the method. We just search all the method symbols with the given name.
        Private Function BuildMethodBinder(containingBinder As NamedTypeBinder,
                                            methodSyntax As MethodBaseSyntax,
                                            forBody As Boolean) As Binder
            Dim containingType = containingBinder.ContainingType

            Dim symbol = SourceMethodSymbol.FindSymbolFromSyntax(methodSyntax, _tree, containingType)

            If (symbol IsNot Nothing) AndAlso
                (symbol.Kind = SymbolKind.Method) Then
                Dim methodSymbol = DirectCast(symbol, SourceMethodSymbol)
                If forBody Then
                    Return BinderBuilder.CreateBinderForMethodBody(methodSymbol, methodSymbol.Syntax, containingBinder)
                Else
                    Return BinderBuilder.CreateBinderForMethodDeclaration(methodSymbol, containingBinder)
                End If
            Else
                ' Not sure if there's a case where we get here. For now, fail. Maybe if a declarations
                ' is so malformed there isn't a symbol for it?

                ' This can happen if property has multiple accessors. 
                ' Parser allows multiple accessors, but binder will accept only one of a kind
                Return containingBinder
            End If
        End Function

        Private Function BuildInitializerBinder(containingBinder As Binder, fieldOrProperty As Symbol) As Binder
            Return BinderBuilder.CreateBinderForInitializer(containingBinder, fieldOrProperty)
        End Function

        Private Function BuildAttributeBinder(containingBinder As Binder, node As VisualBasicSyntaxNode) As Binder
            Debug.Assert(node.Kind = SyntaxKind.Attribute)

            If containingBinder IsNot Nothing AndAlso node.Parent IsNot Nothing Then

                ' Go to attribute block
                Dim attributeBlock = node.Parent

                ' Go to statement that owns the attribute
                If attributeBlock.Parent IsNot Nothing Then

                    Select Case attributeBlock.Parent.Kind
                        Case SyntaxKind.ClassStatement, SyntaxKind.ModuleStatement, SyntaxKind.StructureStatement, SyntaxKind.InterfaceStatement, SyntaxKind.EnumStatement
                            ' Attributes on a class, module, structure, interface, or enum are contained within those nodes.  However, the members of those
                            ' blocks should not be in scope when evaluating expressions within the attribute. Therefore, remove the named type binder from 
                            ' the binder hierarchy.
                            If TypeOf containingBinder Is NamedTypeBinder Then
                                containingBinder = containingBinder.ContainingBinder
                            End If
                    End Select

                End If

            End If

            Return BinderBuilder.CreateBinderForAttribute(_tree, containingBinder, node)
        End Function

    End Class
End Namespace
