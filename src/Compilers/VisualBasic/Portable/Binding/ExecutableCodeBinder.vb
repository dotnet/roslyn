' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A ExecutableCodeBinder provides context for looking up labels within a context represented by a syntax node, 
    ''' and also implementation of GetBinder. 
    ''' </summary>
    Friend MustInherit Class ExecutableCodeBinder
        Inherits Binder

        Private ReadOnly _syntaxRoot As SyntaxNode
        Private ReadOnly _descendantBinderFactory As DescendantBinderFactory
        Private _labelsMap As MultiDictionary(Of String, SourceLabelSymbol)
        Private _labels As ImmutableArray(Of SourceLabelSymbol) = Nothing

        Public Sub New(root As SyntaxNode, containingBinder As Binder)
            MyBase.New(containingBinder)

            _syntaxRoot = root
            _descendantBinderFactory = New DescendantBinderFactory(Me, root)
        End Sub

        Friend ReadOnly Property Labels As ImmutableArray(Of SourceLabelSymbol)
            Get
                If _labels.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_labels, BuildLabels(), Nothing)
                End If

                Return _labels
            End Get
        End Property

        ' Build a read only array of all the local variables declared in this statement list.
        Private Function BuildLabels() As ImmutableArray(Of SourceLabelSymbol)
            Dim labels = ArrayBuilder(Of SourceLabelSymbol).GetInstance()

            Dim syntaxVisitor = New LabelVisitor(labels, DirectCast(ContainingMember, MethodSymbol), Me)

            Select Case _syntaxRoot.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    syntaxVisitor.Visit(DirectCast(_syntaxRoot, SingleLineLambdaExpressionSyntax).Body)

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    syntaxVisitor.VisitList(DirectCast(_syntaxRoot, MultiLineLambdaExpressionSyntax).Statements)

                Case Else
                    syntaxVisitor.Visit(_syntaxRoot)
            End Select

            If labels.Count > 0 Then
                Return labels.ToImmutableAndFree()
            Else
                labels.Free()
                Return ImmutableArray(Of SourceLabelSymbol).Empty
            End If
        End Function

        Private ReadOnly Property LabelsMap As MultiDictionary(Of String, SourceLabelSymbol)
            Get
                If Me._labelsMap Is Nothing Then
                    Interlocked.CompareExchange(Me._labelsMap, BuildLabelsMap(Me.Labels), Nothing)
                End If
                Return Me._labelsMap
            End Get
        End Property

        Private Shared ReadOnly s_emptyLabelMap As MultiDictionary(Of String, SourceLabelSymbol) = New MultiDictionary(Of String, SourceLabelSymbol)(0, IdentifierComparison.Comparer)

        Private Shared Function BuildLabelsMap(labels As ImmutableArray(Of SourceLabelSymbol)) As MultiDictionary(Of String, SourceLabelSymbol)
            If Not labels.IsEmpty Then
                Dim map = New MultiDictionary(Of String, SourceLabelSymbol)(labels.Length, IdentifierComparison.Comparer)
                For Each label In labels
                    map.Add(label.Name, label)
                Next
                Return map
            Else
                ' Return an empty map if there aren't any labels.
                ' LookupLabelByNameToken and other methods assumes a non null map 
                ' is returned from the LabelMap property.
                Return s_emptyLabelMap
            End If
        End Function

        Friend Overrides Function LookupLabelByNameToken(labelName As SyntaxToken) As LabelSymbol
            Dim name As String = labelName.ValueText

            For Each labelSymbol As LabelSymbol In Me.LabelsMap(name)
                If labelSymbol.LabelName = labelName Then
                    Return labelSymbol
                End If
            Next

            Return MyBase.LookupLabelByNameToken(labelName)
        End Function

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                      name As String,
                                                      arity As Integer,
                                                      options As LookupOptions,
                                                      originalBinder As Binder,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            If (options And LookupOptions.LabelsOnly) = LookupOptions.LabelsOnly AndAlso LabelsMap IsNot Nothing Then
                Dim labels = Me.LabelsMap(name)

                Select Case labels.Count
                    Case 0
                        ' Not found

                    Case 1
                        lookupResult.SetFrom(SingleLookupResult.Good(labels.Single()))

                    Case Else
                        ' There are several labels with the same name, so we are going through the list 
                        ' of labels and pick one with the smallest location to make the choice deterministic
                        Dim bestSymbol As SourceLabelSymbol = Nothing
                        Dim bestLocation As Location = Nothing
                        For Each symbol In labels
                            Debug.Assert(symbol.Locations.Length = 1)
                            Dim sourceLocation As Location = symbol.Locations(0)
                            If bestSymbol Is Nothing OrElse Me.Compilation.CompareSourceLocations(bestLocation, sourceLocation) > 0 Then
                                bestSymbol = symbol
                                bestLocation = sourceLocation
                            End If
                        Next

                        lookupResult.SetFrom(SingleLookupResult.Good(bestSymbol))
                End Select
            End If
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            ' UNDONE: additional filtering based on options?
            If Not Labels.IsEmpty AndAlso (options And LookupOptions.LabelsOnly) = LookupOptions.LabelsOnly Then
                Dim labels = Me.Labels
                For Each labelSymbol In labels
                    nameSet.AddSymbol(labelSymbol, labelSymbol.Name, 0)
                Next
            End If
        End Sub

        Public Overrides Function GetBinder(stmtList As SyntaxList(Of StatementSyntax)) As Binder
            Return _descendantBinderFactory.GetBinder(stmtList)
        End Function

        Public Overrides Function GetBinder(node As SyntaxNode) As Binder
            Return _descendantBinderFactory.GetBinder(node)
        End Function

        Public ReadOnly Property Root As SyntaxNode
            Get
                Return _descendantBinderFactory.Root
            End Get
        End Property

        ' Get the map that maps from syntax nodes to binders.
        Public ReadOnly Property NodeToBinderMap As ImmutableDictionary(Of SyntaxNode, BlockBaseBinder)
            Get
                Return _descendantBinderFactory.NodeToBinderMap
            End Get
        End Property

        ' Get the map that maps from statement lists to binders.
        Friend ReadOnly Property StmtListToBinderMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)
            Get
                Return _descendantBinderFactory.StmtListToBinderMap
            End Get
        End Property

#If DEBUG Then
        ' Implicit variable declaration (Option Explicit Off) relies on identifiers
        ' being bound in order. Also, most of our tests run with Option Explicit On. To test that 
        ' we bind identifiers in order even with Option Explicit On, in DEBUG we check the order or
        ' binding of simple names when compiling a whole method body (i.e., during batch compilation, 
        ' not SemanticModel services). 
        '
        ' We check lambda separately from method bodies (even though in theory they should be checked
        ' together) because there are cases where lambda are bound out of order.
        '
        ' See SourceMethodSymbol.GetBoundMethodBody for where this is enabled. 
        '
        ' See BindSimpleName for where CheckSimpleNameBinderOrder is called.
        ' We just store the positions of simple names that have been checked. 

        Private _checkSimpleNameBindingOrder As Boolean = False

        ' The set of offsets of simple names that have already been bound.
        Private _boundSimpleNames As HashSet(Of Integer)

        ' The largest position that has been bound.
        Private _lastBoundSimpleName As Integer = -1

        Public Overrides Sub CheckSimpleNameBindingOrder(node As SimpleNameSyntax)
            If _checkSimpleNameBindingOrder Then
                Dim position = node.SpanStart

                ' There are cases where we bind the same name multiple times -- for example, For loop
                ' variables, and debug checks with local type inference. Hence we only check the first time we 
                ' see a simple name.
                If Not _boundSimpleNames.Contains(position) Then
                    ' If this assert fires, it indicates that simple names are not being bound in order.
                    ' This indicates that binding with Option Explicit Off likely will exhibit a bug.
                    Debug.Assert(position >= _lastBoundSimpleName, "Did not bind simple names in order. Option Explicit Off probably will not behave correctly.")

                    _boundSimpleNames.Add(position)
                    _lastBoundSimpleName = Math.Max(_lastBoundSimpleName, position)
                End If
            End If
        End Sub

        ' We require simple name binding order checks to be enabled, because the SemanticModel APIs
        ' will bind things not in order. We only enable them during binding of a full method body or lambda body.
        Public Overrides Sub EnableSimpleNameBindingOrderChecks(enable As Boolean)
            If enable Then
                Debug.Assert(Not _checkSimpleNameBindingOrder)
                Debug.Assert(_boundSimpleNames Is Nothing)
                _boundSimpleNames = New HashSet(Of Integer)
                _checkSimpleNameBindingOrder = True
            Else
                _boundSimpleNames = Nothing
                _checkSimpleNameBindingOrder = False
            End If
        End Sub
#End If

        Public Class LabelVisitor
            Inherits StatementSyntaxWalker

            Private ReadOnly _labels As ArrayBuilder(Of SourceLabelSymbol)
            Private ReadOnly _containingMethod As MethodSymbol
            Private ReadOnly _binder As Binder

            Public Sub New(labels As ArrayBuilder(Of SourceLabelSymbol), containingMethod As MethodSymbol, binder As Binder)
                Me._labels = labels
                Me._containingMethod = containingMethod
                Me._binder = binder
            End Sub

            Public Overrides Sub VisitLabelStatement(node As LabelStatementSyntax)
                _labels.Add(New SourceLabelSymbol(node.LabelToken, _containingMethod, _binder))
            End Sub
        End Class
    End Class

End Namespace
