' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private ReadOnly m_parentSemanticModel As SyntaxTreeSemanticModel
        Private ReadOnly m_Root As ExpressionSyntax
        Private ReadOnly m_RootBinder As Binder
        Private ReadOnly m_position As Integer
        Private ReadOnly m_bindingOption As SpeculativeBindingOption

        Public Shared Function Create(parentSemanticModel As SyntaxTreeSemanticModel, root As ExpressionSyntax, binder As Binder, position As Integer, bindingOption As SpeculativeBindingOption) As SpeculativeSyntaxTreeSemanticModel
            Debug.Assert(parentSemanticModel IsNot Nothing)
            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            Return New SpeculativeSyntaxTreeSemanticModel(parentSemanticModel, root, binder, position, bindingOption)
        End Function

        Private Sub New(parentSemanticModel As SyntaxTreeSemanticModel, root As ExpressionSyntax, binder As Binder, position As Integer, bindingOption As SpeculativeBindingOption)
            MyBase.New(parentSemanticModel.Compilation, DirectCast(parentSemanticModel.Compilation.SourceModule, SourceModuleSymbol), root.SyntaxTree)

            m_parentSemanticModel = parentSemanticModel
            m_Root = root
            m_RootBinder = binder
            m_position = position
            m_bindingOption = bindingOption
        End Sub

        Public Overrides ReadOnly Property IsSpeculativeSemanticModel As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalPositionForSpeculation As Integer
            Get
                Return Me.m_position
            End Get
        End Property

        Public Overrides ReadOnly Property ParentModel As SemanticModel
            Get
                Return Me.m_parentSemanticModel
            End Get
        End Property

        Friend Overrides ReadOnly Property Root As VisualBasicSyntaxNode
            Get
                Return m_Root
            End Get
        End Property

        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return m_Root.SyntaxTree
            End Get
        End Property

        Friend Overrides Function Bind(binder As Binder, node As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As BoundNode
            Return m_parentSemanticModel.Bind(binder, node, diagnostics)
        End Function

        Friend Overrides Function GetEnclosingBinder(position As Integer) As Binder
            Return m_RootBinder
        End Function

        Private Function GetSpeculativeBindingOption(node As ExpressionSyntax) As SpeculativeBindingOption
            If SyntaxFacts.IsInNamespaceOrTypeContext(node) Then
                Return SpeculativeBindingOption.BindAsTypeOrNamespace
            End If

            Return m_bindingOption
        End Function

        Friend Overrides Function GetExpressionSymbolInfo(node As ExpressionSyntax, options As VBSemanticModel.SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            If (options And VBSemanticModel.SymbolInfoOptions.PreserveAliases) <> 0 Then
                Debug.Assert(TypeOf node Is IdentifierNameSyntax)
                Dim aliasSymbol = m_parentSemanticModel.GetSpeculativeAliasInfo(m_position, DirectCast(node, IdentifierNameSyntax), Me.GetSpeculativeBindingOption(node))
                Return SymbolInfoFactory.Create(ImmutableArray.Create(Of ISymbol)(aliasSymbol), If(aliasSymbol IsNot Nothing, LookupResultKind.Good, LookupResultKind.Empty))
            End If

            Return m_parentSemanticModel.GetSpeculativeSymbolInfo(m_position, node, Me.GetSpeculativeBindingOption(node))
        End Function

        Friend Overrides Function GetExpressionTypeInfo(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            Return m_parentSemanticModel.GetSpeculativeTypeInfoWorker(m_position, node, Me.GetSpeculativeBindingOption(node))
        End Function

        Friend Overrides Function GetExpressionMemberGroup(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            Return m_parentSemanticModel.GetExpressionMemberGroup(node, cancellationToken)
        End Function

        Friend Overrides Function GetExpressionConstantValue(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ConstantValue
            Return m_parentSemanticModel.GetExpressionConstantValue(node, cancellationToken)
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