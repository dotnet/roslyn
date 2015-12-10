' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Allows asking semantic questions about any node in a SyntaxTree within a Compilation.
    ''' </summary>
    Friend Class SyntaxTreeSemanticModel
        Inherits VBSemanticModel

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _sourceModule As SourceModuleSymbol
        Private ReadOnly _syntaxTree As SyntaxTree
        Private ReadOnly _binderFactory As BinderFactory
        Private ReadOnly _ignoresAccessibility As Boolean

        ' maps from a higher-level binder to an appropriate SemanticModel for the construct (such as a method, or initializer).
        Private ReadOnly _semanticModelCache As New ConcurrentDictionary(Of Tuple(Of Binder, Boolean), MemberSemanticModel)()

        Friend Sub New(compilation As VisualBasicCompilation, sourceModule As SourceModuleSymbol, syntaxTree As SyntaxTree, Optional ignoreAccessibility As Boolean = False)
            _compilation = compilation
            _sourceModule = sourceModule
            _syntaxTree = syntaxTree
            _ignoresAccessibility = ignoreAccessibility
            _binderFactory = New BinderFactory(sourceModule, syntaxTree)
        End Sub

        ''' <summary> 
        ''' The compilation associated with this binding.
        ''' </summary> 
        Public Overrides ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return _compilation
            End Get
        End Property

        ''' <summary> 
        ''' The root node of the syntax tree that this binding is based on.
        ''' </summary> 
        Friend Overrides ReadOnly Property Root As VisualBasicSyntaxNode
            Get
                Return DirectCast(_syntaxTree.GetRoot(), VisualBasicSyntaxNode)
            End Get
        End Property

        ''' <summary> 
        ''' The SyntaxTree that is bound
        ''' </summary> 
        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return Me._syntaxTree
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this Is a SemanticModel that ignores accessibility rules when answering semantic questions.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property IgnoresAccessibility As Boolean
            Get
                Return Me._ignoresAccessibility
            End Get
        End Property

        ''' <summary>
        ''' Get all the errors within the syntax tree associated with this object. Includes errors involving compiling
        ''' method bodies or initializers, in addition to the errors returned by GetDeclarationDiagnostics and parse errors.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the process of obtaining the
        ''' diagnostics.</param>
        ''' <remarks>
        ''' Because this method must semantically analyze all method bodies and initializers to check for diagnostics, it may
        ''' take a significant amount of time. Unlike GetDeclarationDiagnostics, diagnostics for method bodies and
        ''' initializers are not cached, the any semantic information used to obtain the diagnostics is discarded.
        ''' </remarks>
        Public Overrides Function GetDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return _compilation.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, _syntaxTree, span, includeEarlierStages:=True, cancellationToken:=cancellationToken)
        End Function

        ''' <summary>
        ''' Get all of the syntax errors within the syntax tree associated with this
        ''' object. Does not get errors involving declarations or compiling method bodies or initializers.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the
        ''' process of obtaining the diagnostics.</param>
        Public Overrides Function GetSyntaxDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return _compilation.GetDiagnosticsForSyntaxTree(CompilationStage.Parse, _syntaxTree, span, includeEarlierStages:=False, cancellationToken:=cancellationToken)
        End Function

        ''' <summary>
        ''' Get all the syntax and declaration errors within the syntax tree associated with this object. Does not get
        ''' errors involving compiling method bodies or initializers.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the process of obtaining the
        ''' diagnostics.</param>
        ''' <remarks>The declaration errors for a syntax tree are cached. The first time this method is called, a ll
        ''' declarations are analyzed for diagnostics. Calling this a second time will return the cached diagnostics.
        ''' </remarks>
        Public Overrides Function GetDeclarationDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return _compilation.GetDiagnosticsForSyntaxTree(CompilationStage.Declare, _syntaxTree, span, includeEarlierStages:=False, cancellationToken:=cancellationToken)
        End Function

        ''' <summary>
        ''' Get all the syntax and declaration errors within the syntax tree associated with this object. Does not get
        ''' errors involving compiling method bodies or initializers.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the process of obtaining the
        ''' diagnostics.</param>
        ''' <remarks>The declaration errors for a syntax tree are cached. The first time this method is called, a ll
        ''' declarations are analyzed for diagnostics. Calling this a second time will return the cached diagnostics.
        ''' </remarks>
        Public Overrides Function GetMethodBodyDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return _compilation.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, _syntaxTree, span, includeEarlierStages:=False, cancellationToken:=cancellationToken)
        End Function

        ' PERF: These shared variables avoid repeated allocation of Func(Of Binder, MemberSemanticModel) in GetMemberSemanticModel
        Private Shared ReadOnly s_methodBodySemanticModelCreator As Func(Of Tuple(Of Binder, Boolean), MemberSemanticModel) = Function(key As Tuple(Of Binder, Boolean)) MethodBodySemanticModel.Create(DirectCast(key.Item1, MethodBodyBinder), key.Item2)
        Private Shared ReadOnly s_initializerSemanticModelCreator As Func(Of Tuple(Of Binder, Boolean), MemberSemanticModel) = Function(key As Tuple(Of Binder, Boolean)) InitializerSemanticModel.Create(DirectCast(key.Item1, DeclarationInitializerBinder), key.Item2)
        Private Shared ReadOnly s_attributeSemanticModelCreator As Func(Of Tuple(Of Binder, Boolean), MemberSemanticModel) = Function(key As Tuple(Of Binder, Boolean)) AttributeSemanticModel.Create(DirectCast(key.Item1, AttributeBinder), key.Item2)
        Private Shared ReadOnly s_topLevelCodeSemanticModelCreator As Func(Of Tuple(Of Binder, Boolean), MemberSemanticModel) = Function(key As Tuple(Of Binder, Boolean)) New TopLevelCodeSemanticModel(DirectCast(key.Item1, TopLevelCodeBinder), key.Item2)

        Public Function GetMemberSemanticModel(binder As Binder) As MemberSemanticModel

            If TypeOf binder Is MethodBodyBinder Then
                Return _semanticModelCache.GetOrAdd(Tuple.Create(binder, IgnoresAccessibility), s_methodBodySemanticModelCreator)
            End If

            If TypeOf binder Is DeclarationInitializerBinder Then
                Return _semanticModelCache.GetOrAdd(Tuple.Create(binder, IgnoresAccessibility), s_initializerSemanticModelCreator)
            End If

            If TypeOf binder Is AttributeBinder Then
                Return _semanticModelCache.GetOrAdd(Tuple.Create(binder, IgnoresAccessibility), s_attributeSemanticModelCreator)
            End If

            If TypeOf binder Is TopLevelCodeBinder Then
                Return _semanticModelCache.GetOrAdd(Tuple.Create(binder, IgnoresAccessibility), s_topLevelCodeSemanticModelCreator)
            End If

            Return Nothing
        End Function

        Friend Function GetMemberSemanticModel(position As Integer) As MemberSemanticModel
            Dim binder As binder = _binderFactory.GetBinderForPosition(FindInitialNodeFromPosition(position), position)
            Dim model = GetMemberSemanticModel(binder) ' Depends on the runtime type, so don't wrap in a SemanticModelBinder.
            Debug.Assert(model Is Nothing OrElse model.RootBinder.IsSemanticModelBinder)
            Return model
        End Function

        Friend Function GetMemberSemanticModel(node As VisualBasicSyntaxNode) As MemberSemanticModel
            Return GetMemberSemanticModel(node.SpanStart)
        End Function

        Friend Overrides Function GetEnclosingBinder(position As Integer) As Binder
            ' special case if node is from interior of a member declaration (method body, etc)
            Dim model As MemberSemanticModel = GetMemberSemanticModel(position)
            If model IsNot Nothing Then
                ' If the node is from the interior of a member declaration, then we need to go further
                ' to find more nested binders.
                Return model.GetEnclosingBinder(position)
            Else
                Dim binder As binder = _binderFactory.GetBinderForPosition(FindInitialNodeFromPosition(position), position)
                Return SemanticModelBinder.Mark(binder, IgnoresAccessibility)
            End If
        End Function

        Friend Overrides Function GetInvokeSummaryForRaiseEvent(node As RaiseEventStatementSyntax) As BoundNodeSummary
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)
            If model IsNot Nothing Then
                Return model.GetInvokeSummaryForRaiseEvent(node)
            Else
                Return Nothing
            End If
        End Function

        Friend Overrides Function GetCrefReferenceSymbolInfo(crefReference As CrefReferenceSyntax, options As VBSemanticModel.SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            ValidateSymbolInfoOptions(options)
            Debug.Assert(IsInCrefOrNameAttributeInterior(crefReference))
            Return GetSymbolInfoForCrefOrNameAttributeReference(crefReference, options)
        End Function

        Friend Overrides Function GetExpressionSymbolInfo(node As ExpressionSyntax, options As SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            ValidateSymbolInfoOptions(options)

            node = SyntaxFactory.GetStandaloneExpression(DirectCast(node, ExpressionSyntax))

            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)
            Dim result As SymbolInfo

            If model IsNot Nothing Then
                result = model.GetExpressionSymbolInfo(node, options, cancellationToken)

                ' If we didn't get anything and were in Type/Namespace only context, let's bind normally and see
                ' if any symbol comes out.
                If result.IsEmpty AndAlso SyntaxFacts.IsInNamespaceOrTypeContext(node) Then
                    Dim tryAsExpression = TryBindNamespaceOrTypeAsExpression(node, options)
                    If Not tryAsExpression.IsEmpty Then
                        result = tryAsExpression
                    End If
                End If
            Else
                ' We don't have a bound tree to examine outside of a member semantic model. Instead, we just
                ' rebind the appropriate syntax as if we were binding it as part of symbol creation.
                '
                ' if expression is not part of a member semantic model then 
                '   a) it may be a reference to a type or namespace name
                '   b) it may be a reference to a interface member in an Implements clause
                '   c) it may be a reference to a field in an Handles clause
                '   d) it may be a reference to an event in an Handles clause

                If SyntaxFacts.IsImplementedMember(node) Then
                    result = GetImplementedMemberSymbolInfo(DirectCast(node, QualifiedNameSyntax), options)
                ElseIf SyntaxFacts.IsHandlesEvent(node) Then
                    result = GetHandlesEventSymbolInfo(DirectCast(node.Parent, HandlesClauseItemSyntax), options)
                ElseIf SyntaxFacts.IsHandlesContainer(node) Then
                    Dim parent = node.Parent
                    If parent.Kind <> SyntaxKind.HandlesClauseItem Then
                        parent = parent.Parent
                    End If

                    result = GetHandlesContainerSymbolInfo(DirectCast(parent, HandlesClauseItemSyntax), options)
                ElseIf SyntaxFacts.IsHandlesProperty(node) Then
                    result = GetHandlesPropertySymbolInfo(DirectCast(node.Parent.Parent, HandlesClauseItemSyntax), options)
                ElseIf IsInCrefOrNameAttributeInterior(node) Then
                    result = GetSymbolInfoForCrefOrNameAttributeReference(node, options)
                ElseIf SyntaxFacts.IsInNamespaceOrTypeContext(node) Then
                    ' Bind the type or namespace name.
                    result = GetTypeOrNamespaceSymbolInfoNotInMember(DirectCast(node, TypeSyntax), options)
                Else
                    result = SymbolInfo.None
                End If
            End If

            Return result
        End Function

        Friend Overrides Function GetCollectionInitializerAddSymbolInfo(collectionInitializer As ObjectCreationExpressionSyntax, node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(collectionInitializer)

            If model IsNot Nothing Then
                Return model.GetCollectionInitializerAddSymbolInfo(collectionInitializer, node, cancellationToken)
            End If

            Return SymbolInfo.None
        End Function

        Private Function TryBindNamespaceOrTypeAsExpression(node As ExpressionSyntax, options As SymbolInfoOptions) As SymbolInfo
            ' Let's bind expression normally and use what comes back as candidate symbols.
            Dim binder As Binder = GetEnclosingBinder(node.SpanStart)

            If binder IsNot Nothing Then
                Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()
                Dim bound As BoundExpression = binder.BindExpression(node, diagnostics)
                diagnostics.Free()

                Dim newSymbolInfo = GetSymbolInfoForNode(options, New BoundNodeSummary(bound, bound, Nothing), binderOpt:=Nothing)

                If Not newSymbolInfo.GetAllSymbols().IsDefaultOrEmpty Then
                    Return SymbolInfoFactory.Create(newSymbolInfo.GetAllSymbols(), LookupResultKind.NotATypeOrNamespace)
                End If
            End If

            Return SymbolInfo.None
        End Function

        Friend Overrides Function GetExpressionTypeInfo(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            node = SyntaxFactory.GetStandaloneExpression(DirectCast(node, ExpressionSyntax))

            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetExpressionTypeInfo(node, cancellationToken)
            Else
                ' We don't have a bound tree to examine outside of a member semantic model. Instead, we just
                ' rebind the appropriate syntax as if we were binding it as part of symbol creation.
                '
                ' if expression is not part of a member semantic model then 
                '   a) it may be a reference to a type or namespace name
                '   b) it may be a reference to a interface member in an Implements clause
                '   c) it may be a reference to a field in an Handles clause
                '   d) it may be a reference to an event in an Handles clause

                If SyntaxFacts.IsImplementedMember(node) Then
                    Return GetImplementedMemberTypeInfo(DirectCast(node, QualifiedNameSyntax))
                ElseIf SyntaxFacts.IsHandlesEvent(node) Then
                    Return GetHandlesEventTypeInfo(DirectCast(node, IdentifierNameSyntax))
                ElseIf SyntaxFacts.IsHandlesContainer(node) Then
                    Dim parent = node.Parent
                    If parent.Kind <> SyntaxKind.HandlesClauseItem Then
                        parent = parent.Parent
                    End If

                    Return GetHandlesContainerTypeInfo(DirectCast(parent, HandlesClauseItemSyntax))
                ElseIf SyntaxFacts.IsHandlesProperty(node) Then
                    Return GetHandlesPropertyTypeInfo(DirectCast(node.Parent.Parent, HandlesClauseItemSyntax))
                ElseIf IsInCrefOrNameAttributeInterior(node) Then
                    Dim typeSyntax = TryCast(node, TypeSyntax)
                    If typeSyntax IsNot Nothing Then
                        Return GetTypeInfoForCrefOrNameAttributeReference(typeSyntax)
                    End If
                ElseIf SyntaxFacts.IsInNamespaceOrTypeContext(node) Then
                    ' Bind the type or namespace name.
                    Return GetTypeOrNamespaceTypeInfoNotInMember(DirectCast(node, TypeSyntax))
                End If

                Return VisualBasicTypeInfo.None
            End If
        End Function

        Friend Overrides Function GetExpressionMemberGroup(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            node = SyntaxFactory.GetStandaloneExpression(DirectCast(node, ExpressionSyntax))

            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetExpressionMemberGroup(node, cancellationToken)
            Else
                Return ImmutableArray(Of Symbol).Empty
            End If
        End Function

        Friend Overrides Function GetExpressionConstantValue(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ConstantValue
            node = SyntaxFactory.GetStandaloneExpression(DirectCast(node, ExpressionSyntax))

            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetExpressionConstantValue(node, cancellationToken)
            Else
                Return Nothing
            End If
        End Function

        Friend Overrides Function GetOperationWorker(node As VisualBasicSyntaxNode, options As GetOperationOptions, cancellationToken As CancellationToken) As IOperation
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetOperationWorker(node, options, cancellationToken)
            Else
                Return Nothing
            End If
        End Function

        Friend Overrides Function GetAttributeSymbolInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(attribute)

            If model IsNot Nothing Then
                Return model.GetAttributeSymbolInfo(attribute, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend Overrides Function GetQueryClauseSymbolInfo(node As QueryClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetQueryClauseSymbolInfo(node, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend Overrides Function GetLetClauseSymbolInfo(node As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetLetClauseSymbolInfo(node, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend Overrides Function GetOrderingSymbolInfo(node As OrderingSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetOrderingSymbolInfo(node, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend Overrides Function GetAggregateClauseSymbolInfoWorker(node As AggregateClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As AggregateClauseSymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetAggregateClauseSymbolInfoWorker(node, cancellationToken)
            Else
                Return New AggregateClauseSymbolInfo(SymbolInfo.None, SymbolInfo.None)
            End If
        End Function

        Friend Overrides Function GetCollectionRangeVariableSymbolInfoWorker(node As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As CollectionRangeVariableSymbolInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetCollectionRangeVariableSymbolInfoWorker(node, cancellationToken)
            Else
                Return CollectionRangeVariableSymbolInfo.None
            End If
        End Function

        Friend Overrides Function GetAttributeTypeInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(attribute)

            If model IsNot Nothing Then
                Return model.GetAttributeTypeInfo(attribute, cancellationToken)
            Else
                Return VisualBasicTypeInfo.None
            End If
        End Function

        Friend Overrides Function GetAttributeMemberGroup(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(attribute)

            If model IsNot Nothing Then
                Return model.GetAttributeMemberGroup(attribute, cancellationToken)
            Else
                Return ImmutableArray(Of Symbol).Empty
            End If
        End Function

        Private Function GetTypeOrNamespaceSymbolNotInMember(expression As TypeSyntax) As Symbol
            Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()
            Try
                ' Set up the binding context.
                Dim binder As Binder = GetEnclosingBinder(expression.SpanStart)

                ' Attempt to bind the type or namespace
                Dim resultSymbol As Symbol
                If SyntaxFacts.IsInTypeOnlyContext(expression) Then
                    resultSymbol = binder.BindTypeOrAliasSyntax(expression, diagnostics)
                Else
                    resultSymbol = binder.BindNamespaceOrTypeOrAliasSyntax(expression, diagnostics)
                End If

                ' Create the result.
                Return resultSymbol
            Finally
                diagnostics.Free()
            End Try
        End Function

        ' Get the symbol info of reference from 'cref' or 'name' attribute value
        Private Function GetSymbolInfoForCrefOrNameAttributeReference(node As VisualBasicSyntaxNode, options As SymbolInfoOptions) As SymbolInfo
            Dim typeParameters As ImmutableArray(Of Symbol) = Nothing
            Dim result As ImmutableArray(Of Symbol) = GetCrefOrNameAttributeReferenceSymbols(node, (options And SymbolInfoOptions.ResolveAliases) = 0, typeParameters)

            If result.IsDefaultOrEmpty Then
                If typeParameters.IsDefaultOrEmpty Then
                    Return SymbolInfo.None
                Else
                    Return SymbolInfoFactory.Create(typeParameters, LookupResultKind.NotReferencable)
                End If
            End If

            If result.Length = 1 Then
                Dim retValue As SymbolInfo = GetSymbolInfoForSymbol(result(0), options)
                If retValue.CandidateReason = CandidateReason.None Then
                    Return retValue
                End If

                result = ImmutableArray(Of Symbol).Empty
            End If

            Dim symbolsBuilder = ArrayBuilder(Of Symbol).GetInstance()
            symbolsBuilder.AddRange(result)
            Dim symbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(symbolsBuilder, options)
            symbolsBuilder.Free()

            If symbols.Length = 0 Then
                Return SymbolInfoFactory.Create(symbols, LookupResultKind.Empty)
            End If

            Return SymbolInfoFactory.Create(symbols, If(symbols.Length = 1, LookupResultKind.Good, LookupResultKind.Ambiguous))
        End Function

        ' Get the type info of reference from 'cref' or 'name' attribute value
        Private Function GetTypeInfoForCrefOrNameAttributeReference(name As TypeSyntax) As VisualBasicTypeInfo
            Dim typeParameters As ImmutableArray(Of Symbol) = Nothing
            Dim result As ImmutableArray(Of Symbol) = GetCrefOrNameAttributeReferenceSymbols(name, preserveAlias:=False, typeParameters:=typeParameters)

            If result.IsDefaultOrEmpty Then
                result = typeParameters
                If result.IsDefaultOrEmpty Then
                    Return VisualBasicTypeInfo.None
                End If
            End If

            If result.Length > 1 Then
                Return VisualBasicTypeInfo.None
            End If

            Dim resultSymbol As Symbol = result(0)

            Select Case resultSymbol.Kind
                Case SymbolKind.ArrayType,
                     SymbolKind.TypeParameter,
                     SymbolKind.NamedType
                    Return GetTypeInfoForSymbol(resultSymbol)

            End Select

            Return VisualBasicTypeInfo.None
        End Function

        ''' <summary>
        ''' Get symbols referenced from 'cref' or 'name' attribute value.
        ''' </summary>
        ''' <param name="node">Node to bind.</param>
        ''' <param name="preserveAlias">True to leave <see cref="AliasSymbol"/>s, False to unwrap them.</param>
        ''' <param name="typeParameters">Out: symbols that would have been in the return value but improperly refer to type parameters.</param>
        ''' <returns>Referenced symbols, less type parameters.</returns>
        Private Function GetCrefOrNameAttributeReferenceSymbols(node As VisualBasicSyntaxNode,
                                                                preserveAlias As Boolean,
                                                                <Out> ByRef typeParameters As ImmutableArray(Of Symbol)) As ImmutableArray(Of Symbol)
            typeParameters = ImmutableArray(Of Symbol).Empty

            ' We only allow a certain list of node kinds to be processed here
            If node.Kind = SyntaxKind.XmlString Then
                Return Nothing
            End If
            Debug.Assert(node.Kind = SyntaxKind.IdentifierName OrElse
                         node.Kind = SyntaxKind.GenericName OrElse
                         node.Kind = SyntaxKind.PredefinedType OrElse
                         node.Kind = SyntaxKind.QualifiedName OrElse
                         node.Kind = SyntaxKind.GlobalName OrElse
                         node.Kind = SyntaxKind.QualifiedCrefOperatorReference OrElse
                         node.Kind = SyntaxKind.CrefOperatorReference OrElse
                         node.Kind = SyntaxKind.CrefReference)

            ' We need to find trivia's enclosing binder first
            Dim parent As VisualBasicSyntaxNode = node.Parent
            Debug.Assert(parent IsNot Nothing)

            Dim attributeNode As BaseXmlAttributeSyntax = Nothing
            Do
                Debug.Assert(parent IsNot Nothing)
                Select Case parent.Kind
                    Case SyntaxKind.XmlCrefAttribute,
                         SyntaxKind.XmlNameAttribute
                        attributeNode = DirectCast(parent, BaseXmlAttributeSyntax)

                    Case SyntaxKind.DocumentationCommentTrivia
                        Exit Do
                End Select
                parent = parent.Parent
            Loop
            Debug.Assert(parent IsNot Nothing)

            If attributeNode Is Nothing Then
                Return Nothing
            End If

            Dim isCrefAttribute As Boolean = attributeNode.Kind = SyntaxKind.XmlCrefAttribute
            Debug.Assert(isCrefAttribute OrElse attributeNode.Kind = SyntaxKind.XmlNameAttribute)

            Dim trivia As SyntaxTrivia = DirectCast(parent, DocumentationCommentTriviaSyntax).ParentTrivia
            If trivia.Kind = SyntaxKind.None Then
                Return Nothing
            End If

            Dim token As SyntaxToken = CType(trivia.Token, SyntaxToken)
            If token.Kind = SyntaxKind.None Then
                Return Nothing
            End If

            Dim docCommentBinder = Me._binderFactory.GetBinderForPosition(node, node.SpanStart)
            docCommentBinder = SemanticModelBinder.Mark(docCommentBinder, IgnoresAccessibility)

            If isCrefAttribute Then
                Dim symbols As ImmutableArray(Of Symbol)
                Dim isTopLevel As Boolean
                If node.Kind = SyntaxKind.CrefReference Then
                    isTopLevel = True
                    symbols = docCommentBinder.BindInsideCrefAttributeValue(DirectCast(node, CrefReferenceSyntax), preserveAlias, Nothing, Nothing)
                Else
                    isTopLevel = node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.CrefReference
                    symbols = docCommentBinder.BindInsideCrefAttributeValue(DirectCast(node, TypeSyntax), preserveAlias, Nothing, Nothing)
                End If

                If isTopLevel Then
                    Dim symbolsBuilder As ArrayBuilder(Of Symbol) = Nothing
                    Dim typeParametersBuilder As ArrayBuilder(Of Symbol) = Nothing

                    For i = 0 To symbols.Length - 1
                        Dim symbol = symbols(i)
                        If symbol.Kind = SymbolKind.TypeParameter Then
                            If symbolsBuilder Is Nothing Then
                                symbolsBuilder = ArrayBuilder(Of Symbol).GetInstance(i)
                                typeParametersBuilder = ArrayBuilder(Of Symbol).GetInstance()
                                symbolsBuilder.AddRange(symbols, i)
                            End If
                            typeParametersBuilder.Add(DirectCast(symbol, TypeParameterSymbol))
                        ElseIf symbolsBuilder IsNot Nothing Then
                            symbolsBuilder.Add(symbol)
                        End If
                    Next

                    If symbolsBuilder IsNot Nothing Then
                        symbols = symbolsBuilder.ToImmutableAndFree()
                        typeParameters = typeParametersBuilder.ToImmutableAndFree()
                    End If
                End If

                Return symbols
            Else
                Return docCommentBinder.BindXmlNameAttributeValue(DirectCast(node, IdentifierNameSyntax), useSiteDiagnostics:=Nothing)
            End If
        End Function

        ' Get the symbol info of type or namespace syntax that is outside a member body
        Private Function GetTypeOrNamespaceSymbolInfoNotInMember(expression As TypeSyntax, options As SymbolInfoOptions) As SymbolInfo
            Dim resultSymbol As Symbol = GetTypeOrNamespaceSymbolNotInMember(expression)

            ' Deal with the case of a namespace group. We may need to bind more in order to see if the ambiguity can be resolved.
            If resultSymbol.Kind = SymbolKind.Namespace AndAlso
               expression.Parent IsNot Nothing AndAlso
               expression.Parent.Kind = SyntaxKind.QualifiedName AndAlso
               DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression Then
                Dim ns = DirectCast(resultSymbol, NamespaceSymbol)

                If ns.NamespaceKind = NamespaceKindNamespaceGroup Then
                    Dim parentInfo As SymbolInfo = GetTypeOrNamespaceSymbolInfoNotInMember(DirectCast(expression.Parent, QualifiedNameSyntax), Nothing)

                    If Not parentInfo.IsEmpty Then
                        Dim namespaces = New SmallDictionary(Of NamespaceSymbol, Boolean)()

                        If parentInfo.Symbol IsNot Nothing Then
                            If Not Binder.AddReceiverNamespaces(namespaces, DirectCast(parentInfo.Symbol, Symbol), Compilation) Then
                                namespaces = Nothing
                            End If
                        Else
                            For Each candidate In parentInfo.CandidateSymbols
                                If Not Binder.AddReceiverNamespaces(namespaces, DirectCast(candidate, Symbol), Compilation) Then
                                    namespaces = Nothing
                                    Exit For
                                End If
                            Next
                        End If

                        If namespaces IsNot Nothing AndAlso namespaces.Count < ns.ConstituentNamespaces.Length Then
                            resultSymbol = DirectCast(ns, MergedNamespaceSymbol).Shrink(namespaces.Keys)
                        End If
                    End If
                End If
            End If

            ' Create the result.
            Dim result = GetSymbolInfoForSymbol(resultSymbol, options)

            ' If we didn't get anything and were in Type/Namespace only context, let's bind normally and see
            ' if any symbol comes out.
            If result.IsEmpty Then
                Dim tryAsExpression = TryBindNamespaceOrTypeAsExpression(expression, options)
                If Not tryAsExpression.IsEmpty Then
                    result = tryAsExpression
                End If
            End If

            Return result
        End Function

        ' Get the symbol info of type or namespace syntax that is outside a member body
        Private Function GetTypeOrNamespaceTypeInfoNotInMember(expression As TypeSyntax) As VisualBasicTypeInfo
            Dim resultSymbol As Symbol = GetTypeOrNamespaceSymbolNotInMember(expression)

            ' Create the result.
            Return GetTypeInfoForSymbol(resultSymbol)
        End Function

        Private Function GetImplementedMemberAndResultKind(symbolBuilder As ArrayBuilder(Of Symbol), memberName As QualifiedNameSyntax) As LookupResultKind
            Debug.Assert(symbolBuilder.Count = 0)

            Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()
            Dim resultKind As LookupResultKind = LookupResultKind.Good
            Try

                ' Set up the binding context.
                Dim binder As Binder = GetEnclosingBinder(memberName.SpanStart)

                ' Figure out the symbol this implements clause is on, and bind the syntax for it.
                Dim implementingMemberSyntax = TryCast(memberName.Parent.Parent, MethodBaseSyntax)
                If implementingMemberSyntax IsNot Nothing Then
                    Dim implementingMember = GetDeclaredSymbol(implementingMemberSyntax)

                    If implementingMember IsNot Nothing Then
                        Select Case implementingMember.Kind
                            Case SymbolKind.Method
                                ImplementsHelper.FindExplicitlyImplementedMember(Of MethodSymbol)(
                                                        DirectCast(implementingMember, MethodSymbol),
                                                        DirectCast(implementingMember, MethodSymbol).ContainingType,
                                                        memberName,
                                                        binder,
                                                        diagnostics,
                                                        symbolBuilder,
                                                        resultKind)

                            Case SymbolKind.Property
                                ImplementsHelper.FindExplicitlyImplementedMember(Of PropertySymbol)(
                                                        DirectCast(implementingMember, PropertySymbol),
                                                        DirectCast(implementingMember, PropertySymbol).ContainingType,
                                                        memberName,
                                                        binder,
                                                        diagnostics,
                                                        symbolBuilder,
                                                        resultKind)

                            Case SymbolKind.Event
                                ImplementsHelper.FindExplicitlyImplementedMember(Of EventSymbol)(
                                                        DirectCast(implementingMember, EventSymbol),
                                                        DirectCast(implementingMember, EventSymbol).ContainingType,
                                                        memberName,
                                                        binder,
                                                        diagnostics,
                                                        symbolBuilder,
                                                        resultKind)

                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(implementingMember.Kind)
                        End Select
                    End If
                End If

                Return resultKind
            Finally
                diagnostics.Free()
            End Try
        End Function

        Private Function GetHandledEventOrContainerSymbolsAndResultKind(eventSymbolBuilder As ArrayBuilder(Of Symbol),
                                                                       containerSymbolBuilder As ArrayBuilder(Of Symbol),
                                                                       propertySymbolBuilder As ArrayBuilder(Of Symbol),
                                                                       handlesClause As HandlesClauseItemSyntax) As LookupResultKind

            Dim resultKind As LookupResultKind = LookupResultKind.Good

            ' Set up the binding context.
            Dim binder As Binder = GetEnclosingBinder(handlesClause.SpanStart)

            ' Figure out the symbol this handles clause is on, and bind the syntax for it.
            Dim handlingMethodSyntax = TryCast(handlesClause.Parent.Parent, MethodStatementSyntax)
            If handlingMethodSyntax IsNot Nothing Then
                Dim implementingMember = GetDeclaredSymbol(handlingMethodSyntax)

                If implementingMember IsNot Nothing Then
                    Dim methodSym = DirectCast(implementingMember, SourceMemberMethodSymbol)

                    Dim diagbag = DiagnosticBag.GetInstance()
                    methodSym.BindSingleHandlesClause(handlesClause,
                                                      binder,
                                                      diagbag,
                                                      eventSymbolBuilder,
                                                      containerSymbolBuilder,
                                                      propertySymbolBuilder,
                                                      resultKind)

                    diagbag.Free()
                End If
            End If

            Return resultKind
        End Function

        ' Get the symbol info of an implemented member in an implements clause.
        Private Function GetImplementedMemberSymbolInfo(memberName As QualifiedNameSyntax, options As SymbolInfoOptions) As SymbolInfo
            Dim implementedMemberBuilder As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
            Dim resultKind As LookupResultKind = GetImplementedMemberAndResultKind(implementedMemberBuilder, memberName)
            Dim symbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(implementedMemberBuilder, options)
            implementedMemberBuilder.Free()

            Return SymbolInfoFactory.Create(symbols, resultKind)
        End Function

        ' Get the symbol info of a handled event in a handles clause.
        Private Function GetHandlesEventSymbolInfo(handlesClause As HandlesClauseItemSyntax, options As SymbolInfoOptions) As SymbolInfo
            Dim builder As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
            Dim resultKind As LookupResultKind = GetHandledEventOrContainerSymbolsAndResultKind(eventSymbolBuilder:=builder,
                                                                                                containerSymbolBuilder:=Nothing,
                                                                                                propertySymbolBuilder:=Nothing,
                                                                                                handlesClause:=handlesClause)

            Dim symbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(builder, options)
            builder.Free()

            Return SymbolInfoFactory.Create(symbols, resultKind)
        End Function

        ' Get the symbol info of an identifier event container in a handles clause.
        Private Function GetHandlesContainerSymbolInfo(handlesClause As HandlesClauseItemSyntax, options As SymbolInfoOptions) As SymbolInfo
            Dim builder As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
            Dim resultKind As LookupResultKind = GetHandledEventOrContainerSymbolsAndResultKind(eventSymbolBuilder:=Nothing,
                                                                                                containerSymbolBuilder:=builder,
                                                                                                propertySymbolBuilder:=Nothing,
                                                                                                handlesClause:=handlesClause)

            Dim symbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(builder, options)
            builder.Free()

            Return SymbolInfoFactory.Create(symbols, resultKind)
        End Function

        ' Get the symbol info of an withevents sourcing property in a handles clause.
        Private Function GetHandlesPropertySymbolInfo(handlesClause As HandlesClauseItemSyntax, options As SymbolInfoOptions) As SymbolInfo
            Dim builder As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
            Dim resultKind As LookupResultKind = GetHandledEventOrContainerSymbolsAndResultKind(eventSymbolBuilder:=Nothing,
                                                                                                containerSymbolBuilder:=Nothing,
                                                                                                propertySymbolBuilder:=builder,
                                                                                                handlesClause:=handlesClause)

            Dim symbols As ImmutableArray(Of Symbol) = RemoveErrorTypesAndDuplicates(builder, options)
            builder.Free()

            Return SymbolInfoFactory.Create(symbols, resultKind)
        End Function

        ' Get the type info of a implemented member in a implements clause.
        Private Function GetImplementedMemberTypeInfo(memberName As QualifiedNameSyntax) As VisualBasicTypeInfo
            ' Implemented members have no type.
            Return VisualBasicTypeInfo.None
        End Function

        ' Get the type info of a implemented member in a implements clause.
        Private Function GetHandlesEventTypeInfo(memberName As IdentifierNameSyntax) As VisualBasicTypeInfo
            ' Handled events have no type.
            Return VisualBasicTypeInfo.None
        End Function

        ' Get the type info of a implemented member in a implements clause.
        Private Function GetHandlesContainerTypeInfo(memberName As HandlesClauseItemSyntax) As VisualBasicTypeInfo
            ' Handled events have no type.
            Return VisualBasicTypeInfo.None
        End Function

        ' Get the type info of a implemented member in a implements clause.
        Private Function GetHandlesPropertyTypeInfo(memberName As HandlesClauseItemSyntax) As VisualBasicTypeInfo
            ' Handled events have no type.
            Return VisualBasicTypeInfo.None
        End Function

        ''' <summary>
        ''' Checks all symbol locations against the syntax provided and return symbol if any of the locations is 
        ''' inside the syntax span. Returns Nothing otherwise.
        ''' </summary>
        Private Function CheckSymbolLocationsAgainstSyntax(symbol As NamedTypeSymbol, nodeToCheck As VisualBasicSyntaxNode) As NamedTypeSymbol
            For Each location In symbol.Locations
                If location.SourceTree Is Me.SyntaxTree AndAlso nodeToCheck.Span.Contains(location.SourceSpan.Start) Then
                    Return symbol
                End If
            Next
            Return Nothing
        End Function

        ''' <summary>
        ''' Given a delegate declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a type.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Function GetDeclaredSymbol(declarationSyntax As DelegateStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As NamedTypeSymbol
            If declarationSyntax Is Nothing Then Throw New ArgumentNullException(NameOf(declarationSyntax))
            If Not IsInTree(declarationSyntax) Then Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)

            ' Don't need to wrap in a SemanticModelBinder, since we're not binding.
            Dim binder As Binder = _binderFactory.GetNamedTypeBinder(declarationSyntax)

            If binder IsNot Nothing AndAlso TypeOf binder Is NamedTypeBinder Then
                Return CheckSymbolLocationsAgainstSyntax(DirectCast(binder.ContainingType, NamedTypeSymbol), declarationSyntax)
            Else
                Return Nothing  ' Can this happen? Maybe in some edge case error cases.
            End If
        End Function


        ''' <summary>
        ''' Given a type declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a type.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As TypeStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            If declarationSyntax Is Nothing Then Throw New ArgumentNullException(NameOf(declarationSyntax))
            If Not IsInTree(declarationSyntax) Then Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)

            ' Don't need to wrap in a SemanticModelBinder, since we're not binding.
            Dim binder As Binder = _binderFactory.GetNamedTypeBinder(declarationSyntax)

            If binder IsNot Nothing AndAlso TypeOf binder Is NamedTypeBinder Then
                Return CheckSymbolLocationsAgainstSyntax(DirectCast(binder.ContainingType, NamedTypeSymbol), declarationSyntax)
            Else
                Return Nothing  ' Can this happen? Maybe in some edge case error cases.
            End If
        End Function

        ''' <summary>
        ''' Given a enum declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares an enum.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As EnumStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            If declarationSyntax Is Nothing Then Throw New ArgumentNullException(NameOf(declarationSyntax))
            If Not IsInTree(declarationSyntax) Then Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)

            ' Don't need to wrap in a SemanticModelBinder, since we're not binding.
            Dim binder As Binder = _binderFactory.GetNamedTypeBinder(declarationSyntax)

            If binder IsNot Nothing AndAlso TypeOf binder Is NamedTypeBinder Then
                Return CheckSymbolLocationsAgainstSyntax(DirectCast(binder.ContainingType, NamedTypeSymbol), declarationSyntax)
            Else
                Return Nothing  ' Can this happen? Maybe in some edge case with errors
            End If
        End Function

        ''' <summary>
        ''' Given a namespace declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a namespace.</param>
        ''' <returns>The namespace symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As NamespaceStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamespaceSymbol
            If declarationSyntax Is Nothing Then Throw New ArgumentNullException(NameOf(declarationSyntax))
            If Not IsInTree(declarationSyntax) Then Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)

            Dim parentBlock = TryCast(declarationSyntax.Parent, NamespaceBlockSyntax)
            If parentBlock IsNot Nothing Then
                ' Don't need to wrap in a SemanticModelBinder, since we're not binding.
                Dim binder As Binder = _binderFactory.GetNamespaceBinder(parentBlock)

                If binder IsNot Nothing AndAlso TypeOf binder Is NamespaceBinder Then
                    Return DirectCast(binder.ContainingNamespaceOrType, NamespaceSymbol)
                End If
            End If

            Return Nothing ' Edge case with errors
        End Function

        ''' <summary>
        ''' Given a method, property, or event declaration, get the corresponding symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a method, property, or event.</param>
        ''' <returns>The method, property, or event symbol that was declared.</returns>
        Friend Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As MethodBaseSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            If declarationSyntax Is Nothing Then Throw New ArgumentNullException(NameOf(declarationSyntax))
            If Not IsInTree(declarationSyntax) Then
                Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)
            End If

            ' Delegate declarations are a subclass of MethodBaseSyntax syntax-wise, but they are
            ' more like a type declaration, so we need to special case here.
            If declarationSyntax.Kind = SyntaxKind.DelegateFunctionStatement OrElse
                declarationSyntax.Kind = SyntaxKind.DelegateSubStatement Then
                Return GetDeclaredSymbol(DirectCast(declarationSyntax, DelegateStatementSyntax), cancellationToken)
            End If

            Dim statementSyntax = TryCast(declarationSyntax.Parent, StatementSyntax)
            If statementSyntax IsNot Nothing Then

                '  get parent type block
                Dim parentTypeBlock As TypeBlockSyntax = Nothing
                Select Case statementSyntax.Kind
                    Case SyntaxKind.ClassBlock, SyntaxKind.EnumBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                        parentTypeBlock = TryCast(statementSyntax, TypeBlockSyntax)

                    Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock, SyntaxKind.OperatorBlock, SyntaxKind.PropertyBlock, SyntaxKind.EventBlock
                        parentTypeBlock = TryCast(statementSyntax.Parent, TypeBlockSyntax)

                        ' EDMAURER maybe this is a top-level decl in which case the parent is a CompilationUnitSyntax
                        If parentTypeBlock Is Nothing AndAlso statementSyntax.Parent IsNot Nothing Then
                            Dim namespaceToLookInForImplicitType As INamespaceSymbol = Nothing
                            Select Case statementSyntax.Parent.Kind
                                Case SyntaxKind.CompilationUnit
                                    namespaceToLookInForImplicitType = Me._sourceModule.RootNamespace
                                Case SyntaxKind.NamespaceBlock
                                    namespaceToLookInForImplicitType = GetDeclaredSymbol(DirectCast(statementSyntax.Parent, NamespaceBlockSyntax))
                            End Select

                            If namespaceToLookInForImplicitType IsNot Nothing Then
                                Dim implicitType = DirectCast(namespaceToLookInForImplicitType.GetMembers(TypeSymbol.ImplicitTypeName).SingleOrDefault(), NamedTypeSymbol)

                                If implicitType IsNot Nothing Then
                                    Return SourceMethodSymbol.FindSymbolFromSyntax(declarationSyntax, _syntaxTree, implicitType)
                                End If
                            End If
                        End If
                    Case SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock, SyntaxKind.AddHandlerAccessorBlock, SyntaxKind.RemoveHandlerAccessorBlock, SyntaxKind.RaiseEventAccessorBlock
                        '  redirect to upper property or event symbol
                        If statementSyntax.Parent IsNot Nothing Then
                            parentTypeBlock = TryCast(statementSyntax.Parent.Parent, TypeBlockSyntax)
                        End If

                    Case SyntaxKind.AddHandlerAccessorBlock, SyntaxKind.RemoveHandlerAccessorBlock
                        '  redirect to upper event symbol
                        If statementSyntax.Parent IsNot Nothing Then
                            parentTypeBlock = TryCast(statementSyntax.Parent.Parent, TypeBlockSyntax)
                        End If

                    Case Else
                        ' broken code scenarios end up here

                        ' to end up here, a methodbasesyntax's parent must be a statement and not be one of the above. 
                        ' The parser does e.g. not generate an enclosing block for accessors statements,
                        ' but for Operators, conversions and constructors.

                        ' The case where an invalid accessor is contained in e.g. an interface is handled further down in "FindSymbolFromSyntax".

                        ' TODO: consider always creating a (missing) block around the statements in the parser

                        ' We are asserting what we know so far. If this assert fails, this is not a bug, we either need to remove this assert or relax the assert. 
                        Debug.Assert(statementSyntax.Kind = SyntaxKind.NamespaceBlock AndAlso
                                     (TypeOf (declarationSyntax) Is AccessorStatementSyntax OrElse
                                      TypeOf (declarationSyntax) Is EventStatementSyntax OrElse
                                      TypeOf (declarationSyntax) Is MethodStatementSyntax OrElse
                                      TypeOf (declarationSyntax) Is PropertyStatementSyntax))

                        Return Nothing
                End Select

                If parentTypeBlock IsNot Nothing Then
                    Dim containingType = DirectCast(GetDeclaredSymbol(parentTypeBlock.BlockStatement, cancellationToken), NamedTypeSymbol)
                    If containingType IsNot Nothing Then
                        Return SourceMethodSymbol.FindSymbolFromSyntax(declarationSyntax, _syntaxTree, containingType)
                    End If
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a parameter declaration, get the corresponding parameter symbol.
        ''' </summary>
        ''' <param name="parameter">The syntax node that declares a parameter.</param>
        ''' <returns>The parameter symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(parameter As ParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As IParameterSymbol
            If parameter Is Nothing Then
                Throw New ArgumentNullException(NameOf(parameter))
            End If

            Dim paramList As ParameterListSyntax = TryCast(parameter.Parent, ParameterListSyntax)
            If paramList IsNot Nothing Then
                Dim declarationSyntax As MethodBaseSyntax = TryCast(paramList.Parent, MethodBaseSyntax)
                If declarationSyntax IsNot Nothing Then
                    Dim symbol = GetDeclaredSymbol(declarationSyntax, cancellationToken)
                    If symbol IsNot Nothing Then
                        Select Case symbol.Kind
                            Case SymbolKind.Method
                                Return GetParameterSymbol(DirectCast(symbol, MethodSymbol).Parameters, parameter)
                            Case SymbolKind.Event
                                Return GetParameterSymbol(DirectCast(symbol, EventSymbol).DelegateParameters, parameter)
                            Case SymbolKind.Property
                                Return GetParameterSymbol(DirectCast(symbol, PropertySymbol).Parameters, parameter)
                            Case SymbolKind.NamedType
                                '  check for being delegate 
                                Dim typeSymbol = DirectCast(symbol, NamedTypeSymbol)
                                Debug.Assert(typeSymbol.TypeKind = TypeKind.Delegate)
                                If typeSymbol.DelegateInvokeMethod IsNot Nothing Then
                                    Return GetParameterSymbol(typeSymbol.DelegateInvokeMethod.Parameters, parameter)
                                End If
                        End Select

                    ElseIf TypeOf declarationSyntax Is LambdaHeaderSyntax Then
                        ' This could be a lambda parameter.
                        Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(declarationSyntax)

                        If model IsNot Nothing Then
                            Return model.GetDeclaredSymbol(parameter, cancellationToken)
                        End If
                    End If
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a type parameter declaration, get the corresponding type parameter symbol.
        ''' </summary>
        ''' <param name="typeParameter">The syntax node that declares a type parameter.</param>
        ''' <returns>The type parameter symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(typeParameter As TypeParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As ITypeParameterSymbol
            If typeParameter Is Nothing Then
                Throw New ArgumentNullException(NameOf(typeParameter))
            End If
            If Not IsInTree(typeParameter) Then
                Throw New ArgumentException(VBResources.TypeParameterNotWithinTree)
            End If

            Dim symbol As ISymbol = Nothing
            Dim typeParamList = TryCast(typeParameter.Parent, TypeParameterListSyntax)
            If typeParamList IsNot Nothing AndAlso typeParamList.Parent IsNot Nothing Then
                If TypeOf typeParamList.Parent Is MethodStatementSyntax Then
                    symbol = GetDeclaredSymbol(DirectCast(typeParamList.Parent, MethodStatementSyntax), cancellationToken)
                ElseIf TypeOf typeParamList.Parent Is TypeStatementSyntax Then
                    symbol = GetDeclaredSymbol(DirectCast(typeParamList.Parent, TypeStatementSyntax), cancellationToken)
                ElseIf TypeOf typeParamList.Parent Is DelegateStatementSyntax Then
                    symbol = GetDeclaredSymbol(DirectCast(typeParamList.Parent, DelegateStatementSyntax), cancellationToken)
                End If

                If symbol IsNot Nothing Then
                    Dim typeSymbol = TryCast(symbol, NamedTypeSymbol)
                    If typeSymbol IsNot Nothing Then
                        Return Me.GetTypeParameterSymbol(typeSymbol.TypeParameters, typeParameter)
                    End If

                    Dim methodSymbol = TryCast(symbol, MethodSymbol)
                    If methodSymbol IsNot Nothing Then
                        Return Me.GetTypeParameterSymbol(methodSymbol.TypeParameters, typeParameter)
                    End If
                End If
            End If

            Return Nothing
        End Function

        ' Get a type parameter symbol from a ROA of TypeParametersSymbols and the syntax for one.
        Private Function GetTypeParameterSymbol(parameters As ImmutableArray(Of TypeParameterSymbol), parameter As TypeParameterSyntax) As TypeParameterSymbol
            For Each symbol In parameters
                For Each location In symbol.Locations
                    If location.IsInSource AndAlso location.SourceTree Is _syntaxTree AndAlso parameter.Span.Contains(location.SourceSpan) Then
                        Return symbol
                    End If
                Next
            Next

            Return Nothing
        End Function

        Public Overrides Function GetDeclaredSymbol(declarationSyntax As EnumMemberDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As IFieldSymbol
            If declarationSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(declarationSyntax))
            End If

            If Not IsInTree(declarationSyntax) Then
                Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)
            End If

            Dim enumBlock As EnumBlockSyntax = DirectCast(declarationSyntax.Parent, EnumBlockSyntax)

            If enumBlock IsNot Nothing Then
                Dim containingType = DirectCast(GetDeclaredSymbol(enumBlock.EnumStatement, cancellationToken), NamedTypeSymbol)
                If containingType IsNot Nothing Then
                    Return DirectCast(SourceFieldSymbol.FindFieldOrWithEventsSymbolFromSyntax(declarationSyntax.Identifier, _syntaxTree, containingType), FieldSymbol)
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a variable declaration, get the corresponding  symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a variable.</param>
        ''' <returns>The symbol that was declared.</returns>
        Public Overrides Function GetDeclaredSymbol(declarationSyntax As ModifiedIdentifierSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            If declarationSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(declarationSyntax))
            End If

            If Not IsInTree(declarationSyntax) Then
                Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)
            End If

            Dim declarationParent = declarationSyntax.Parent

            ' Possibility 1: Field syntax, could be a Field or WithEvent property
            Dim fieldSyntax As FieldDeclarationSyntax = Nothing
            If declarationParent IsNot Nothing Then
                fieldSyntax = TryCast(declarationParent.Parent, FieldDeclarationSyntax)
            End If

            Dim parentTypeBlock As TypeBlockSyntax = Nothing
            If fieldSyntax IsNot Nothing Then
                parentTypeBlock = TryCast(fieldSyntax.Parent, TypeBlockSyntax)
            Else : End If

            If parentTypeBlock IsNot Nothing Then
                Dim containingType = DirectCast(GetDeclaredSymbol(parentTypeBlock.BlockStatement, cancellationToken), NamedTypeSymbol)
                If containingType IsNot Nothing Then
                    Return SourceFieldSymbol.FindFieldOrWithEventsSymbolFromSyntax(declarationSyntax.Identifier, _syntaxTree, containingType)
                End If
            End If

            ' Possibility 2: Parameter
            Dim parameterSyntax As ParameterSyntax = TryCast(declarationParent, ParameterSyntax)

            If parameterSyntax IsNot Nothing Then
                Return GetDeclaredSymbol(parameterSyntax, cancellationToken)
            End If

            ' Possibility 3: Local variable
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(declarationSyntax)

            If model IsNot Nothing Then
                Return model.GetDeclaredSymbol(declarationSyntax, cancellationToken)
            End If

            Return MyBase.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        ''' <summary>
        ''' Given an FieldInitializerSyntax, get the corresponding symbol of anonymous type creation.
        ''' </summary>
        ''' <param name="fieldInitializerSyntax">The anonymous object creation field initializer syntax.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overrides Function GetDeclaredSymbol(fieldInitializerSyntax As FieldInitializerSyntax, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As IPropertySymbol
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(fieldInitializerSyntax)

            If model IsNot Nothing Then
                Return model.GetDeclaredSymbol(fieldInitializerSyntax, cancellationToken)
            End If

            Return MyBase.GetDeclaredSymbol(fieldInitializerSyntax, cancellationToken)
        End Function

        ''' <summary>
        ''' Given an AnonymousObjectCreationExpressionSyntax, get the corresponding symbol of anonymous type.
        ''' </summary>
        ''' <param name="anonymousObjectCreationExpressionSyntax">The anonymous object creation syntax.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overrides Function GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax As AnonymousObjectCreationExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(anonymousObjectCreationExpressionSyntax)

            If model IsNot Nothing Then
                Return model.GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax, cancellationToken)
            End If

            Return MyBase.GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax, cancellationToken)
        End Function

        ''' <summary>
        ''' Given an ExpressionRangeVariableSyntax, get the corresponding symbol.
        ''' </summary>
        ''' <param name="rangeVariableSyntax">The range variable syntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(rangeVariableSyntax)

            If model IsNot Nothing Then
                Return model.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
            End If

            Return MyBase.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        ''' <summary>
        ''' Given an CollectionRangeVariableSyntax, get the corresponding symbol.
        ''' </summary>
        ''' <param name="rangeVariableSyntax">The range variable syntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(rangeVariableSyntax)

            If model IsNot Nothing Then
                Return model.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
            End If

            Return MyBase.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        ''' <summary>
        ''' Given an AggregationRangeVariableSyntax, get the corresponding symbol.
        ''' </summary>
        ''' <param name="rangeVariableSyntax">The range variable syntax that declares a variable.</param>
        ''' <returns>The symbol that was declared, or Nothing if no such symbol exists.</returns>
        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As AggregationRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(rangeVariableSyntax)

            If model IsNot Nothing Then
                Return model.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
            End If

            Return MyBase.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        ''' <summary>
        ''' Given an import clause get the corresponding symbol for the import alias that was introduced.
        ''' </summary>
        ''' <param name="declarationSyntax">The import statement syntax node.</param>
        ''' <returns>The alias symbol that was declared or Nothing if no alias symbol was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As SimpleImportsClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As IAliasSymbol
            If declarationSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(declarationSyntax))
            End If

            If Not IsInTree(declarationSyntax) Then
                Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)
            End If

            If declarationSyntax.Alias Is Nothing Then
                Return Nothing
            End If

            Dim aliasName As String = declarationSyntax.Alias.Identifier.ValueText

            If Not String.IsNullOrEmpty(aliasName) Then
                Dim sourceFile = Me._sourceModule.GetSourceFile(Me.SyntaxTree)

                Dim aliasImports As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition) = sourceFile.AliasImportsOpt
                Dim symbol As AliasAndImportsClausePosition = Nothing

                If aliasImports IsNot Nothing AndAlso aliasImports.TryGetValue(aliasName, symbol) Then
                    '  make sure the symbol is declared inside declarationSyntax node
                    For Each location In symbol.Alias.Locations
                        If location.IsInSource AndAlso location.SourceTree Is _syntaxTree AndAlso declarationSyntax.Span.Contains(location.SourceSpan) Then
                            Return symbol.Alias
                        End If
                    Next

                    ' If the alias name was in the map but the location didn't match, then the syntax declares a duplicate alias.
                    ' We'll return a new AliasSymbol to improve the API experience.
                    Dim binder As Binder = GetEnclosingBinder(declarationSyntax.SpanStart)
                    Dim discardedDiagnostics = DiagnosticBag.GetInstance()
                    Dim targetSymbol As NamespaceOrTypeSymbol = binder.BindNamespaceOrTypeSyntax(declarationSyntax.Name, discardedDiagnostics)
                    discardedDiagnostics.Free()
                    If targetSymbol IsNot Nothing Then
                        Return New AliasSymbol(binder.Compilation, binder.ContainingNamespaceOrType, aliasName, targetSymbol, declarationSyntax.GetLocation())
                    End If
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a field declaration syntax, get the corresponding symbols.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares one or more fields.</param>
        ''' <returns>The field symbols that were declared.</returns>
        Friend Overrides Function GetDeclaredSymbols(declarationSyntax As FieldDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            If declarationSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(declarationSyntax))
            End If

            If Not IsInTree(declarationSyntax) Then
                Throw New ArgumentException(VBResources.DeclarationSyntaxNotWithinTree)
            End If

            Dim builder = New ArrayBuilder(Of ISymbol)

            For Each declarator In declarationSyntax.Declarators
                For Each identifier In declarator.Names
                    Dim field = TryCast(Me.GetDeclaredSymbol(identifier, cancellationToken), IFieldSymbol)
                    If field IsNot Nothing Then
                        builder.Add(field)
                    End If
                Next
            Next

            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Determines what type of conversion, if any, would be used if a given expression was converted to a given
        ''' type.
        ''' </summary>
        ''' <param name="expression">An expression which much occur within the syntax tree associated with this
        ''' object.</param>
        ''' <param name="destination">The type to attempt conversion to.</param>
        ''' <returns>Returns a Conversion object that summarizes whether the conversion was possible, and if so, what
        ''' kind of conversion it was. If no conversion was possible, a Conversion object with a false "Exists "
        ''' property is returned.</returns>
        ''' <remarks>To determine the conversion between two types (instead of an expression and a type), use
        ''' Compilation.ClassifyConversion.</remarks>
        Public Overrides Function ClassifyConversion(expression As ExpressionSyntax, destination As ITypeSymbol) As Conversion
            CheckSyntaxNode(expression)
            If destination Is Nothing Then
                Throw New ArgumentNullException(NameOf(destination))
            End If

            Dim vbdestination = destination.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(destination))

            ' TODO(cyrusn): Check arguments.  This is a public entrypoint, so we must do appropriate
            ' checks here.  However, no other methods in this type do any checking currently.  So I'm
            ' going to hold off on this until we do a full sweep of the API.
            Dim binding = Me.GetMemberSemanticModel(expression)
            If binding Is Nothing Then
                Return New Conversion(Nothing)  'NoConversion
            End If

            Return binding.ClassifyConversion(expression, vbdestination)
        End Function

        Public Overrides ReadOnly Property IsSpeculativeSemanticModel As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalPositionForSpeculation As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property ParentModel As SemanticModel
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim memberModel = Me.GetMemberSemanticModel(position)
            If memberModel IsNot Nothing Then
                Return memberModel.TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel, position, method, speculativeModel)
            End If

            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, type As TypeSyntax, bindingOption As SpeculativeBindingOption, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim memberModel = Me.GetMemberSemanticModel(position)
            If memberModel IsNot Nothing Then
                Return memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, type, bindingOption, speculativeModel)
            End If

            Dim binder As Binder = Me.GetSpeculativeBinderForExpression(position, type, bindingOption)
            If binder IsNot Nothing Then
                speculativeModel = SpeculativeSyntaxTreeSemanticModel.Create(Me, type, binder, position, bindingOption)
                Return True
            End If

            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, rangeArgument As RangeArgumentSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim memberModel = Me.GetMemberSemanticModel(position)
            If memberModel IsNot Nothing Then
                Return memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, rangeArgument, speculativeModel)
            End If

            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim memberModel = Me.GetMemberSemanticModel(position)
            If memberModel IsNot Nothing Then
                Return memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, statement, speculativeModel)
            End If

            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim memberModel = Me.GetMemberSemanticModel(position)
            If memberModel IsNot Nothing Then
                Return memberModel.TryGetSpeculativeSemanticModelCore(parentModel, position, initializer, speculativeModel)
            End If

            speculativeModel = Nothing
            Return False
        End Function

        ''' <summary>
        ''' Analyze control-flow within a part of a method body.
        ''' </summary>
        ''' <param name="firstStatement">The first statement to be included in the analysis.</param>
        ''' <param name="lastStatement">The last statement to be included in the analysis.</param>
        ''' <returns>An object that can be used to obtain the result of the control flow analysis.</returns>
        ''' <exception cref="ArgumentException">The two statements are not contained within the same statement list.</exception>
        Public Overrides Function AnalyzeControlFlow(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As ControlFlowAnalysis
            Dim context As RegionAnalysisContext = If(ValidateRegionDefiningStatementsRange(firstStatement, lastStatement),
                                                      CreateRegionAnalysisContext(firstStatement, lastStatement),
                                                      CreateFailedRegionAnalysisContext())

            Dim result = New VisualBasicControlFlowAnalysis(context)

            ' we assume the analysis should only fail if the original context is invalid
            Debug.Assert(result.Succeeded OrElse context.Failed)

            Return result
        End Function

        ''' <summary>
        ''' The first statement to be included in the analysis.
        ''' </summary>
        ''' <param name="firstStatement">The first statement to be included in the analysis.</param>
        ''' <param name="lastStatement">The last statement to be included in the analysis.</param>
        ''' <returns>An object that can be used to obtain the result of the data flow analysis.</returns>
        ''' <exception cref="ArgumentException">The two statements are not contained within the same statement list.</exception>
        Public Overrides Function AnalyzeDataFlow(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As DataFlowAnalysis
            Dim context As RegionAnalysisContext = If(ValidateRegionDefiningStatementsRange(firstStatement, lastStatement),
                                                      CreateRegionAnalysisContext(firstStatement, lastStatement),
                                                      CreateFailedRegionAnalysisContext())

            Dim result = New VisualBasicDataFlowAnalysis(context)

            ' we assume the analysis should only fail if the original context is invalid
            Debug.Assert(result.Succeeded OrElse result.InvalidRegionDetectedInternal OrElse context.Failed)

            Return result
        End Function

        ''' <summary>
        ''' Analyze data-flow within an expression. 
        ''' </summary>
        ''' <param name="expression">The expression within the associated SyntaxTree to analyze.</param>
        ''' <returns>An object that can be used to obtain the result of the data flow analysis.</returns>
        Public Overrides Function AnalyzeDataFlow(expression As ExpressionSyntax) As DataFlowAnalysis
            Dim context As RegionAnalysisContext = If(ValidateRegionDefiningExpression(expression),
                                                      CreateRegionAnalysisContext(expression),
                                                      CreateFailedRegionAnalysisContext())

            Dim result = New VisualBasicDataFlowAnalysis(context)

            ' Assert that we either correctly precalculated succeeded 
            ' flag or we know for sure why we failed to precalculate it
            CheckSucceededFlagInAnalyzeDataFlow(expression, result, context)

            Return result
        End Function

        <Conditional("DEBUG")>
        Private Sub CheckSucceededFlagInAnalyzeDataFlow(expression As ExpressionSyntax, result As VisualBasicDataFlowAnalysis, context As RegionAnalysisContext)
            If result.Succeeded OrElse result.InvalidRegionDetectedInternal OrElse context.Failed Then
                Return
            End If

            ' Some cases of unsucceeded result that cannot be precalculated properly are handled below

            ' CASE 1: If the region flow analysis is performed on the left part of member access like 
            '         on 'a' part of 'a.b.c()' expression AND 'a.b' is a type, we don't create a bound node
            '         to be linked with syntax node 'a'. In this case FirstInRegion/LastInRegion nodes are 
            '         calculated by trying to bind 'a' itself, and in most cases it binds into Namespace
            '         or Type expression. This case is handles in RegionAnalysisContext..ctor.
            '
            '         But in Color/Color case it binds into other symbol kinds and we suppress 
            '         assertion for this case here
            Dim expressionParent As VisualBasicSyntaxNode = expression.Parent
            If expression.Kind = SyntaxKind.IdentifierName AndAlso
                    expressionParent IsNot Nothing AndAlso expressionParent.Kind = SyntaxKind.SimpleMemberAccessExpression AndAlso
                    DirectCast(expressionParent, MemberAccessExpressionSyntax).Expression Is expression Then

                ' Color/Color confusion may only be possible if expression is an IdentifierName
                ' and is nested in member access. This is too wide definition, but it's 
                ' difficult to improve this without doing semantic analysis
                Return
            End If

            ' CASE 2: If the region flow analysis is performed on the arguments of field declaration of array
            '         data type having explicit initializer, like 'Public AnArray(2) = {0, 1}'; 
            '         VB semantics generates an error about specifying both bounds and initializer and ignores them
            If expression.Kind = SyntaxKind.NumericLiteralExpression AndAlso
                    expressionParent IsNot Nothing AndAlso (expressionParent.Kind = SyntaxKind.SimpleArgument AndAlso Not DirectCast(expressionParent, SimpleArgumentSyntax).IsNamed) Then

                '           VariableDeclarator
                '          |                  |
                '  ModifiedIdentifier     EqualsValue
                '          |
                '  ArgumentList
                '      |...|...|
                '  SimpleArgument
                '          |
                '  NumericalLiteral

                Dim argList As VisualBasicSyntaxNode = expressionParent.Parent
                If argList IsNot Nothing AndAlso argList.Kind = SyntaxKind.ArgumentList Then
                    Dim modIdentifier As VisualBasicSyntaxNode = argList.Parent
                    If modIdentifier IsNot Nothing AndAlso modIdentifier.Kind = SyntaxKind.ModifiedIdentifier Then
                        Dim varDeclarator As VisualBasicSyntaxNode = modIdentifier.Parent
                        If varDeclarator IsNot Nothing AndAlso varDeclarator.Kind = SyntaxKind.VariableDeclarator AndAlso
                                DirectCast(varDeclarator, VariableDeclaratorSyntax).Initializer IsNot Nothing Then
                            Return
                        End If
                    End If
                End If
            End If

            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' Checks if the node is inside the attribute arguments 
        ''' </summary>
        Private Shared Function IsNodeInsideAttributeArguments(node As VisualBasicSyntaxNode) As Boolean
            While node IsNot Nothing
                If node.Kind = SyntaxKind.Attribute Then
                    Return True
                End If

                node = node.Parent
            End While
            Return False
        End Function

        ''' <summary>
        ''' Check Expression for being in right context, for example 'For ... Next [x]' 
        ''' is not correct context
        ''' </summary>
        Private Shared Function IsExpressionInValidContext(expression As ExpressionSyntax) As Boolean

            Dim currentNode As VisualBasicSyntaxNode = expression
            Do
                Dim parent As VisualBasicSyntaxNode = currentNode.Parent
                If parent Is Nothing Then Return True

                Dim expressionParent = TryCast(parent, ExpressionSyntax)
                If expressionParent Is Nothing Then
                    Select Case parent.Kind

                        Case SyntaxKind.NextStatement
                            Return False

                        Case SyntaxKind.EqualsValue
                            ' One cannot perform flow analysis on an expression from Enum member declaration
                            parent = parent.Parent
                            If parent Is Nothing Then
                                Return True
                            End If

                            Select Case parent.Kind
                                Case SyntaxKind.EnumMemberDeclaration,
                                     SyntaxKind.Parameter
                                    Return False

                                Case SyntaxKind.VariableDeclarator
                                    Dim localDeclSyntax = TryCast(parent.Parent, LocalDeclarationStatementSyntax)
                                    If localDeclSyntax IsNot Nothing Then
                                        For Each modifier In localDeclSyntax.Modifiers
                                            Select Case modifier.Kind
                                                Case SyntaxKind.ConstKeyword
                                                    Return False
                                            End Select
                                        Next
                                    End If
                                    Return True

                                Case Else
                                    Return True
                            End Select

                        Case SyntaxKind.RaiseEventStatement
                            Return False

                        Case SyntaxKind.NamedFieldInitializer
                            If DirectCast(parent, NamedFieldInitializerSyntax).Name Is currentNode Then
                                Return False
                            End If
                        ' else proceed to the upper-level node

                        Case SyntaxKind.NameColonEquals
                            Return False

                        Case SyntaxKind.RangeArgument
                            If DirectCast(parent, RangeArgumentSyntax).LowerBound Is currentNode Then
                                Return False
                            End If
                        ' proceed to the upper-level node

                        Case SyntaxKind.ArgumentList,
                             SyntaxKind.SimpleArgument,
                             SyntaxKind.ObjectMemberInitializer
                            ' proceed to the upper-level node

                        Case SyntaxKind.GoToStatement
                            Return False

                        Case SyntaxKind.XmlDeclarationOption
                            Return False

                        Case Else
                            Return True

                    End Select

                Else
                    Select Case parent.Kind
                        Case SyntaxKind.XmlElementEndTag
                            Return False
                    End Select
                End If

                ' up one level
                currentNode = parent
            Loop

        End Function

        Private Sub AssertNodeInTree(node As VisualBasicSyntaxNode, argName As String)
            If node Is Nothing Then
                Throw New ArgumentNullException(argName)
            End If

            If Not IsInTree(node) Then
                Throw New ArgumentException(argName & VBResources.NotWithinTree)
            End If
        End Sub

        Private Function ValidateRegionDefiningExpression(expression As ExpressionSyntax) As Boolean
            AssertNodeInTree(expression, NameOf(expression))

            If expression.Kind = SyntaxKind.PredefinedType OrElse SyntaxFacts.IsInNamespaceOrTypeContext(expression) Then
                Return False
            End If

            If SyntaxFactory.GetStandaloneExpression(expression) IsNot expression Then
                Return False
            End If

            ' Check for pseudo-expressions
            Select Case expression.Kind
                Case SyntaxKind.CollectionInitializer
                    Dim parent As VisualBasicSyntaxNode = expression.Parent

                    If parent IsNot Nothing Then
                        Select Case parent.Kind
                            Case SyntaxKind.ObjectCollectionInitializer
                                If DirectCast(parent, ObjectCollectionInitializerSyntax).Initializer Is expression Then
                                    Return False
                                End If

                            Case SyntaxKind.ArrayCreationExpression
                                If DirectCast(parent, ArrayCreationExpressionSyntax).Initializer Is expression Then
                                    Return False
                                End If

                            Case SyntaxKind.CollectionInitializer
                                ' Nested collection initializer is not an expression from the language point of view.
                                ' However, third level collection initializer under ObjectCollectionInitializer should
                                ' be treated as a stand alone expression.
                                Dim possibleSecondLevelInitializer As VisualBasicSyntaxNode = parent
                                parent = parent.Parent

                                If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.CollectionInitializer Then
                                    Dim possibleFirstLevelInitializer As VisualBasicSyntaxNode = parent
                                    parent = parent.Parent

                                    If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.ObjectCollectionInitializer AndAlso
                                       DirectCast(parent, ObjectCollectionInitializerSyntax).Initializer Is possibleFirstLevelInitializer Then
                                        Exit Select
                                    End If
                                End If

                                Return False
                        End Select
                    End If

                Case SyntaxKind.NumericLabel,
                     SyntaxKind.IdentifierLabel,
                     SyntaxKind.NextLabel
                    Return False
            End Select

            If Not IsExpressionInValidContext(expression) OrElse IsNodeInsideAttributeArguments(expression) Then
                Return False
            End If

            Return True
        End Function

        Private Function ValidateRegionDefiningStatementsRange(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As Boolean
            AssertNodeInTree(firstStatement, NameOf(firstStatement))
            AssertNodeInTree(lastStatement, NameOf(lastStatement))

            If firstStatement.Parent Is Nothing OrElse firstStatement.Parent IsNot lastStatement.Parent Then
                Throw New ArgumentException("statements not within the same statement list")
            End If

            If firstStatement.SpanStart > lastStatement.SpanStart Then
                Throw New ArgumentException("first statement does not precede last statement")
            End If

            If Not TypeOf firstStatement Is ExecutableStatementSyntax OrElse Not TypeOf lastStatement Is ExecutableStatementSyntax Then
                Return False
            End If

            ' Test for |For ... Next x, y|
            If IsNotUppermostForBlock(firstStatement) Then
                Return False
            End If

            If firstStatement IsNot lastStatement AndAlso IsNotUppermostForBlock(lastStatement) Then
                Return False
            End If

            If IsNodeInsideAttributeArguments(firstStatement) OrElse (firstStatement IsNot lastStatement AndAlso IsNodeInsideAttributeArguments(lastStatement)) Then
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Check ForBlockSyntax for being the uppermost For block. By uppermost 
        ''' For block we mean that if Next clause contains several control variables,
        ''' the uppermost block is the one which includes all the For blocks ending with 
        ''' the same Next clause
        ''' </summary>
        Private Function IsNotUppermostForBlock(forBlockOrStatement As VisualBasicSyntaxNode) As Boolean
            Debug.Assert(forBlockOrStatement.Kind <> SyntaxKind.ForStatement)
            Debug.Assert(forBlockOrStatement.Kind <> SyntaxKind.ForEachStatement)

            Dim forBlock = TryCast(forBlockOrStatement, ForOrForEachBlockSyntax)
            If forBlock Is Nothing Then
                Return False
            End If

            Dim endNode As NextStatementSyntax = forBlock.NextStatement

            If endNode IsNot Nothing Then
                ' The only case where the statement is valid is this case is 
                ' that the Next clause contains one single control variable (or none)
                Return endNode.ControlVariables.Count > 1
            End If

            ' go down the For statements chain until the last and ensure it has as many 
            ' variables as there were nested For statements
            Dim nesting As Integer = 1
            Do
                If forBlock.Statements.Count = 0 Then
                    Return True
                End If

                Dim lastStatement = TryCast(forBlock.Statements.Last(), ForOrForEachBlockSyntax)
                If lastStatement Is Nothing Then
                    Return True
                End If

                nesting += 1
                endNode = lastStatement.NextStatement

                If endNode IsNot Nothing Then
                    Return endNode.ControlVariables.Count <> nesting
                End If

                ' Else - next level
                forBlock = lastStatement
            Loop
        End Function

        ''' <summary>
        ''' Gets the semantic information of a for each statement.
        ''' </summary>
        ''' <param name="node">The for each syntax node.</param>
        Friend Overrides Function GetForEachStatementInfoWorker(node As ForEachBlockSyntax) As ForEachStatementInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(node)

            If model IsNot Nothing Then
                Return model.GetForEachStatementInfoWorker(node)
            Else
                Return Nothing
            End If
        End Function

        Friend Overrides Function GetAwaitExpressionInfoWorker(awaitExpression As AwaitExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As AwaitExpressionInfo
            Dim model As MemberSemanticModel = Me.GetMemberSemanticModel(awaitExpression)

            If model IsNot Nothing Then
                Return model.GetAwaitExpressionInfoWorker(awaitExpression, cancellationToken)
            Else
                Return Nothing
            End If
        End Function

#Region "Region Analysis Context Creation"

        ''' <summary> Used to create a region analysis context 
        ''' with failed flag set to be used in 'failed' scenarios </summary>
        Private Function CreateFailedRegionAnalysisContext() As RegionAnalysisContext
            Return New RegionAnalysisContext(Me.Compilation)
        End Function

        Private Function CreateRegionAnalysisContext(expression As ExpressionSyntax) As RegionAnalysisContext
            Dim region As TextSpan = expression.Span

            Dim memberModel As MemberSemanticModel = GetMemberSemanticModel(expression)
            If memberModel Is Nothing Then
                ' Recover from error cases
                Dim node As BoundBadStatement = New BoundBadStatement(expression, ImmutableArray(Of BoundNode).Empty)
                Return New RegionAnalysisContext(Compilation, Nothing, node, node, node, region)
            End If

            Dim boundNode As BoundNode = memberModel.GetBoundRoot()
            Dim boundExpression As BoundNode = memberModel.GetUpperBoundNode(expression)

            Return New RegionAnalysisContext(Compilation, memberModel.MemberSymbol, boundNode, boundExpression, boundExpression, region)
        End Function

        Private Function CreateRegionAnalysisContext(firstStatement As StatementSyntax, lastStatement As StatementSyntax) As RegionAnalysisContext
            Dim region As TextSpan = TextSpan.FromBounds(firstStatement.SpanStart, lastStatement.Span.End)

            Dim memberModel As MemberSemanticModel = GetMemberSemanticModel(firstStatement)
            If memberModel Is Nothing Then
                ' Recover from error cases
                Dim node As BoundBadStatement = New BoundBadStatement(firstStatement, ImmutableArray(Of BoundNode).Empty)
                Return New RegionAnalysisContext(Compilation, Nothing, node, node, node, region)
            End If

            Dim boundNode As BoundNode = memberModel.GetBoundRoot()
            Dim firstBoundNode As BoundNode = memberModel.GetUpperBoundNode(firstStatement)
            Dim lastBoundNode As BoundNode = memberModel.GetUpperBoundNode(lastStatement)

            Return New RegionAnalysisContext(Compilation, memberModel.MemberSymbol, boundNode, firstBoundNode, lastBoundNode, region)
        End Function

#End Region

    End Class
End Namespace
