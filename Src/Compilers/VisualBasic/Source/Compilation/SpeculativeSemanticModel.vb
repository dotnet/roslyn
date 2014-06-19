Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Friend Class SpeculativeSemanticModel
        Inherits MemberSemanticModel

        Private _rootBinder As Binder
        Private _boundTree As BoundNode
        Private _nodeMap As ImmutableMap(Of SyntaxNode, BlockBaseBinder)
        Private _stmtListMap As ImmutableMap(Of SeparatedSyntaxList(Of StatementSyntax), BlockBaseBinder)
        Private _boundNodeMap As Dictionary(Of SyntaxNode, BoundNode)
        Private _diagnostics As DiagnosticBag

        Private ReadOnly _compilation As Compilation
        Private ReadOnly _memberSymbol As Symbol
        Private ReadOnly _root As SyntaxNode

        Public Sub New(compilation As Compilation,
                       root As SyntaxNode,
                       member As Symbol,
                       containingBinder As Binder,
                       blockMap As ImmutableMap(Of SyntaxNode, BlockBaseBinder),
                       stmtListMap As ImmutableMap(Of SeparatedSyntaxList(Of StatementSyntax), BlockBaseBinder))

            _compilation = compilation
            _root = root
            _memberSymbol = MemberSymbol

            Dim binder = SpeculativeBinder.Create(containingBinder, Me)

            ' Add new entries to the binder maps for local binders within the statement.
            Dim builder As New LocalBinderBuilder(DirectCast(Me.MemberSymbol, SourceMethodSymbol), blockMap, stmtListMap)
            builder.MakeBinder(root, binder)

            _rootBinder = binder
            _nodeMap = builder.NodeToBinderMap
            _stmtListMap = builder.StmtListToBinderMap
            'TODO: if need to use ObjectPool for these, then there must be a way to release them.
            _diagnostics = New DiagnosticBag
            _boundTree = Bind(binder, root, _diagnostics)
            _boundNodeMap = BoundNodeMapBuilder.Build(_boundTree)

            binder.DisallowFurtherImplicitVariableDeclaration()
        End Sub

        Friend Overrides ReadOnly Property RootBinder As Binder
            Get
                Return Me._rootBinder
            End Get
        End Property

        Public Overrides ReadOnly Property Compilation As Compilation
            Get
                Return _compilation
            End Get
        End Property

        Friend Overrides ReadOnly Property MemberSymbol As Symbol
            Get
                Return _memberSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property Root As SyntaxNode
            Get
                Return _root
            End Get
        End Property

        Friend Function GetBinder(node As SyntaxNode) As Binder
            Dim binder As BlockBaseBinder = Nothing
            Me._nodeMap.TryGetValue(node, binder)
            Return binder
        End Function

        Friend Function GetBinder(statementList As SeparatedSyntaxList(Of StatementSyntax)) As Binder
            Dim binder As BlockBaseBinder = Nothing
            Me._stmtListMap.TryGetValue(statementList, binder)
            Return binder
        End Function

        Protected Overrides Function GetEnclosingBinder(node As SyntaxNode, position As Integer) As Binder
            Dim binder As Binder
            Dim current As SyntaxNode = node

            Do
                If current Is Nothing OrElse current Is _root Then
                    Return Me.RootBinder
                End If

                Dim body As SeparatedSyntaxList(Of StatementSyntax) = Nothing

                If InBlockInterior(current, position, body) Then
                    ' We are in the interior of a block statement.
                    binder = Me.GetBinder(body)
                    If binder IsNot Nothing Then
                        Return binder
                    End If
                End If

                binder = Me.GetBinder(current)
                If binder IsNot Nothing Then
                    Return binder
                End If

                current = current.Parent
            Loop
        End Function

        Friend Overrides Function GetBoundNode(node As SyntaxNode) As BoundNode
            If node Is _root Then
                Return _boundTree
            End If

            Dim bound As BoundNode = Nothing
            If Not Me._boundNodeMap.TryGetValue(node, bound) Then
                ' TODO: bind the node if it is not found in the map which happens in 
                '       some cases, for example for everyting inside type 
                '       syntax bound with BindTypeSyntax(...) call
            End If
            Return bound
        End Function

        Friend Overrides Function GetBoundNodeSummary(node As SyntaxNode) As BoundNodeSummary
            Dim upperBound = GetUpperBoundNodeInContext(node)
            Dim lowerBound = GetLowerBoundNodeInContext(node)
            Dim parentSyntax As SyntaxNode = If(node.Parent Is Nothing, Nothing, GetBindableSyntaxNode(node.Parent))
            Dim lowerBoundOfParent = If(parentSyntax Is Nothing, Nothing, GetLowerBoundNodeInContext(parentSyntax))

            Return New BoundNodeSummary(lowerBound, upperBound, lowerBoundOfParent)
        End Function

        Friend Overrides Function GetSpeculativeSemanticModel(position As Integer, expression As ExpressionSyntax) As SemanticModel
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function GetSpeculativeSemanticModel(position As Integer, statement As StatementSyntax) As SemanticModel
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetDeclarationDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
            ' Since there are no diagnostics in a SpeculativeSemanticModel, just syntax errors suffices.

            ' TODO: we don't have a syntax tree, so we can't get the syntax diagnostics correctly.
            ' return syntaxTree.GetDiagnostics(Root, cancellationToken);
            Return SpecializedCollections.EmptyEnumerable(Of Diagnostic)()
        End Function

        Public Overrides Function GetDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
            ' Get all the syntax errors.
            Dim result As IEnumerable(Of Diagnostic) = GetDeclarationDiagnostics(cancellationToken)

            ' Add semantic errors.
            result = result.Concat(_diagnostics.Cast(Of Diagnostic)())

            Return result
        End Function

        NotInheritable Class BoundNodeMapBuilder
            Inherits BoundTreeWalker

            Private _map As Dictionary(Of SyntaxNode, BoundNode) = New Dictionary(Of SyntaxNode, BoundNode)()

            Public Shared Function Build(root As BoundNode) As Dictionary(Of SyntaxNode, BoundNode)
                Dim builder = New BoundNodeMapBuilder()
                builder.Visit(root)
                Return builder._map
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If node Is Nothing Then
                    Return Nothing
                Else
                    Dim syntax = node.Syntax
                    If syntax IsNot Nothing AndAlso Not node.WasCompilerGenerated Then
                        ' NOTE: this will override current mapping leaving in the map 
                        '       the last node walker happened to visit
                        ' TODO: revise
                        _map(syntax) = node
                    End If
                    Return MyBase.Visit(node)
                End If
            End Function
        End Class

    End Class
End Namespace

