' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Instances of this type represent user-facing speculative semantic models that are backed by 
    ''' internal <see cref="MemberSemanticModel"/>.
    ''' </summary>
    Friend NotInheritable Class SpeculativeSemanticModelWithMemberModel
        Inherits PublicSemanticModel

        Private ReadOnly _parentSemanticModel As SyntaxTreeSemanticModel
        Private ReadOnly _speculatedPosition As Integer
        Private ReadOnly _memberModel As MemberSemanticModel

        Private Sub New(parentSemanticModel As SyntaxTreeSemanticModel,
                        speculatedPosition As Integer)
            Debug.Assert(Not parentSemanticModel.IsSpeculativeSemanticModel, VBResources.ChainingSpeculativeModelIsNotSupported)

            _parentSemanticModel = parentSemanticModel
            _speculatedPosition = speculatedPosition
        End Sub

        Friend Sub New(parentSemanticModel As SyntaxTreeSemanticModel, position As Integer, root As AttributeSyntax, binder As Binder)
            Me.New(parentSemanticModel, position)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            _memberModel = New AttributeSemanticModel(root, binder, containingPublicSemanticModel:=Me)
        End Sub

        Friend Sub New(parentSemanticModel As SyntaxTreeSemanticModel, position As Integer, root As EqualsValueSyntax, binder As Binder)
            Me.New(parentSemanticModel, position)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            _memberModel = New InitializerSemanticModel(root, binder, containingPublicSemanticModel:=Me)
        End Sub

        Friend Sub New(parentSemanticModel As SyntaxTreeSemanticModel, position As Integer, root As VisualBasicSyntaxNode, binder As Binder)
            Me.New(parentSemanticModel, position)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            _memberModel = New MethodBodySemanticModel(root, binder, containingPublicSemanticModel:=Me)
        End Sub

        Friend Sub New(parentSemanticModel As SyntaxTreeSemanticModel, position As Integer, root As TypeSyntax, binder As Binder)
            Me.New(parentSemanticModel, position)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            _memberModel = New SpeculativeMemberSemanticModel(Me, root, binder)
        End Sub

        Friend Sub New(parentSemanticModel As SyntaxTreeSemanticModel, position As Integer, root As RangeArgumentSyntax, binder As Binder)
            Me.New(parentSemanticModel, position)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            _memberModel = New SpeculativeMemberSemanticModel(Me, root, binder)
        End Sub

        Friend Overrides ReadOnly Property Root As SyntaxNode
            Get
                Return _memberModel.Root
            End Get
        End Property

        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _memberModel.SyntaxTree
            End Get
        End Property

        Public Overrides ReadOnly Property IsSpeculativeSemanticModel As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalPositionForSpeculation As Integer
            Get
                Return Me._speculatedPosition
            End Get
        End Property

        Public Overrides ReadOnly Property ParentModel As SemanticModel
            Get
                Return Me._parentSemanticModel
            End Get
        End Property

        Public Overrides ReadOnly Property IgnoresAccessibility As Boolean
            Get
                Return Me._parentSemanticModel.IgnoresAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return _memberModel.Compilation
            End Get
        End Property

        Friend Overloads Overrides Function GetEnclosingBinder(position As Integer) As Binder
            Return _memberModel.GetEnclosingBinder(position)
        End Function

        Public Overrides Function ClassifyConversion(expression As ExpressionSyntax, destination As ITypeSymbol) As Conversion
            Return _memberModel.ClassifyConversion(expression, destination)
        End Function

        Friend Overrides Function GetInvokeSummaryForRaiseEvent(node As RaiseEventStatementSyntax) As BoundNodeSummary
            Return _memberModel.GetInvokeSummaryForRaiseEvent(node)
        End Function

        Public Overrides Function GetSyntaxDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        Public Overrides Function GetDeclarationDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        Public Overrides Function GetMethodBodyDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        Public Overrides Function GetDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As TypeStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            Return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As EnumStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            Return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As NamespaceStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamespaceSymbol
            Return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        Friend Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As MethodBaseSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            Return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        Public Overloads Overrides Function GetDeclaredSymbol(parameter As ParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As IParameterSymbol
            Return _memberModel.GetDeclaredSymbol(parameter, cancellationToken)
        End Function

        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As SimpleImportsClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As IAliasSymbol
            Return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        Public Overloads Overrides Function GetDeclaredSymbol(typeParameter As TypeParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As ITypeParameterSymbol
            Return _memberModel.GetDeclaredSymbol(typeParameter, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(declarationSyntax As EnumMemberDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As IFieldSymbol
            Return _memberModel.GetDeclaredSymbol(declarationSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(identifierSyntax As ModifiedIdentifierSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            Return _memberModel.GetDeclaredSymbol(identifierSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax As AnonymousObjectCreationExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            Return _memberModel.GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(fieldInitializerSyntax As FieldInitializerSyntax, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As IPropertySymbol
            Return _memberModel.GetDeclaredSymbol(fieldInitializerSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            Return _memberModel.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            Return _memberModel.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As AggregationRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            Return _memberModel.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        Friend Overrides Function GetDeclaredSymbols(declarationSyntax As FieldDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            Return _memberModel.GetDeclaredSymbols(declarationSyntax, cancellationToken)
        End Function

        Friend Overrides Function GetForEachStatementInfoWorker(node As ForEachBlockSyntax) As ForEachStatementInfo
            Return _memberModel.GetForEachStatementInfoWorker(node)
        End Function

        Friend Overrides Function GetAttributeSymbolInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetAttributeSymbolInfo(attribute, cancellationToken)
        End Function

        Friend Overrides Function GetAttributeTypeInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            Return _memberModel.GetAttributeTypeInfo(attribute, cancellationToken)
        End Function

        Friend Overrides Function GetAttributeMemberGroup(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            Return _memberModel.GetAttributeMemberGroup(attribute, cancellationToken)
        End Function

        Friend Overrides Function GetExpressionSymbolInfo(node As ExpressionSyntax, options As SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetExpressionSymbolInfo(node, options, cancellationToken)
        End Function

        Friend Overrides Function GetOperationWorker(node As VisualBasicSyntaxNode, cancellationToken As CancellationToken) As IOperation
            Return _memberModel.GetOperationWorker(node, cancellationToken)
        End Function

        Friend Overrides Function GetExpressionTypeInfo(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            Return _memberModel.GetExpressionTypeInfo(node, cancellationToken)
        End Function

        Friend Overrides Function GetExpressionMemberGroup(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            Return _memberModel.GetExpressionMemberGroup(node, cancellationToken)
        End Function

        Friend Overrides Function GetExpressionConstantValue(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ConstantValue
            Return _memberModel.GetExpressionConstantValue(node, cancellationToken)
        End Function

        Friend Overrides Function GetCollectionInitializerAddSymbolInfo(collectionInitializer As ObjectCreationExpressionSyntax, node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetCollectionInitializerAddSymbolInfo(collectionInitializer, node, cancellationToken)
        End Function

        Friend Overrides Function GetCrefReferenceSymbolInfo(crefReference As CrefReferenceSyntax, options As VBSemanticModel.SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetCrefReferenceSymbolInfo(crefReference, options, cancellationToken)
        End Function

        Friend Overrides Function GetQueryClauseSymbolInfo(node As QueryClauseSyntax, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetQueryClauseSymbolInfo(node, cancellationToken)
        End Function

        Friend Overrides Function GetLetClauseSymbolInfo(node As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetLetClauseSymbolInfo(node, cancellationToken)
        End Function

        Friend Overrides Function GetOrderingSymbolInfo(node As OrderingSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return _memberModel.GetOrderingSymbolInfo(node, cancellationToken)
        End Function

        Friend Overrides Function GetAggregateClauseSymbolInfoWorker(node As AggregateClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As AggregateClauseSymbolInfo
            Return _memberModel.GetAggregateClauseSymbolInfoWorker(node, cancellationToken)
        End Function

        Friend Overrides Function GetCollectionRangeVariableSymbolInfoWorker(node As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As CollectionRangeVariableSymbolInfo
            Return _memberModel.GetCollectionRangeVariableSymbolInfoWorker(node, cancellationToken)
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, type As TypeSyntax, bindingOption As SpeculativeBindingOption, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, rangeArgument As RangeArgumentSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Overrides Function GetAwaitExpressionInfoWorker(awaitExpression As AwaitExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As AwaitExpressionInfo
            Return _memberModel.GetAwaitExpressionInfoWorker(awaitExpression, cancellationToken)
        End Function

        Friend Overrides Function Bind(binder As Binder, node As SyntaxNode, diagnostics As BindingDiagnosticBag) As BoundNode
            Return _memberModel.Bind(binder, node, diagnostics)
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function
    End Class
End Namespace
