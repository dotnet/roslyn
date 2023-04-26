' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Allows asking semantic questions about a tree of syntax nodes that did not appear in the original source code.
    ''' Typically, an instance is obtained by a call to SemanticModel.TryGetSpeculativeSemanticModel. 
    ''' </summary>
    Friend NotInheritable Class SpeculativeSyntaxTreeSemanticModel
        Inherits SyntaxTreeSemanticModel

        Private ReadOnly _parentSemanticModel As SyntaxTreeSemanticModel
        Private ReadOnly _root As ExpressionSyntax
        Private ReadOnly _rootBinder As Binder
        Private ReadOnly _position As Integer
        Private ReadOnly _bindingOption As SpeculativeBindingOption

        Public Shared Function Create(parentSemanticModel As SyntaxTreeSemanticModel, root As ExpressionSyntax, binder As Binder, position As Integer, bindingOption As SpeculativeBindingOption) As SpeculativeSyntaxTreeSemanticModel
            Debug.Assert(parentSemanticModel IsNot Nothing)
            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            Return New SpeculativeSyntaxTreeSemanticModel(parentSemanticModel, root, binder, position, bindingOption)
        End Function

        Private Sub New(parentSemanticModel As SyntaxTreeSemanticModel, root As ExpressionSyntax, binder As Binder, position As Integer, bindingOption As SpeculativeBindingOption)
            MyBase.New(parentSemanticModel.Compilation, DirectCast(parentSemanticModel.Compilation.SourceModule, SourceModuleSymbol), root.SyntaxTree, ignoreAccessibility:=parentSemanticModel.IgnoresAccessibility)

            _parentSemanticModel = parentSemanticModel
            _root = root
            _rootBinder = binder
            _position = position
            _bindingOption = bindingOption
        End Sub

        Public Overrides ReadOnly Property IsSpeculativeSemanticModel As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalPositionForSpeculation As Integer
            Get
                Return Me._position
            End Get
        End Property

        Public Overrides ReadOnly Property ParentModel As SemanticModel
            Get
                Return Me._parentSemanticModel
            End Get
        End Property

        Friend Overrides ReadOnly Property Root As SyntaxNode
            Get
                Return _root
            End Get
        End Property

        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _root.SyntaxTree
            End Get
        End Property

        Friend Overrides Function Bind(binder As Binder, node As SyntaxNode, diagnostics As BindingDiagnosticBag) As BoundNode
            Return _parentSemanticModel.Bind(binder, node, diagnostics)
        End Function

        Friend Overrides Function GetEnclosingBinder(position As Integer) As Binder
            Return _rootBinder
        End Function

        Private Function GetSpeculativeBindingOption(node As ExpressionSyntax) As SpeculativeBindingOption
            If SyntaxFacts.IsInNamespaceOrTypeContext(node) Then
                Return SpeculativeBindingOption.BindAsTypeOrNamespace
            End If

            Return _bindingOption
        End Function

        Friend Overrides Function GetExpressionSymbolInfo(node As ExpressionSyntax, options As VBSemanticModel.SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            If (options And VBSemanticModel.SymbolInfoOptions.PreserveAliases) <> 0 Then
                Debug.Assert(TypeOf node Is IdentifierNameSyntax)
                Dim aliasSymbol = _parentSemanticModel.GetSpeculativeAliasInfo(_position, DirectCast(node, IdentifierNameSyntax), Me.GetSpeculativeBindingOption(node))
                Return SymbolInfoFactory.Create(ImmutableArray.Create(Of ISymbol)(aliasSymbol), If(aliasSymbol IsNot Nothing, LookupResultKind.Good, LookupResultKind.Empty))
            End If

            Return _parentSemanticModel.GetSpeculativeSymbolInfo(_position, node, Me.GetSpeculativeBindingOption(node))
        End Function

        Friend Overrides Function GetExpressionTypeInfo(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            Return _parentSemanticModel.GetSpeculativeTypeInfoWorker(_position, node, Me.GetSpeculativeBindingOption(node))
        End Function

        Friend Overrides Function GetExpressionMemberGroup(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            Return _parentSemanticModel.GetExpressionMemberGroup(node, cancellationToken)
        End Function

        Friend Overrides Function GetExpressionConstantValue(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ConstantValue
            Return _parentSemanticModel.GetExpressionConstantValue(node, cancellationToken)
        End Function

        Public Overrides Function GetSyntaxDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        Public Overrides Function GetDeclarationDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        Public Overrides Function GetDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
