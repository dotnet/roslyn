' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ' An invariant of a merged declaration is that all of its children
    ' are also merged declarations.
    Friend NotInheritable Class MergedNamespaceDeclaration
        Inherits MergedNamespaceOrTypeDeclaration

        Private ReadOnly _declarations As ImmutableArray(Of SingleNamespaceDeclaration)
        Private ReadOnly _multipleSpellings As Boolean  ' true if the namespace is spelling with multiple different case-insensitive spellings ("Namespace GOO" and "Namespace goo")
        Private _children As ImmutableArray(Of MergedNamespaceOrTypeDeclaration)

        Private Sub New(declarations As ImmutableArray(Of SingleNamespaceDeclaration))
            MyBase.New(String.Empty)
            If declarations.Any() Then
                Me.Name = SingleNamespaceDeclaration.BestName(Of SingleNamespaceDeclaration)(declarations, _multipleSpellings)
            End If

            Me._declarations = declarations
        End Sub

        Public Shared Function Create(declarations As IEnumerable(Of SingleNamespaceDeclaration)) As MergedNamespaceDeclaration
            Return New MergedNamespaceDeclaration(ImmutableArray.CreateRange(Of SingleNamespaceDeclaration)(declarations))
        End Function

        Public Shared Function Create(ParamArray declarations As SingleNamespaceDeclaration()) As MergedNamespaceDeclaration
            Return New MergedNamespaceDeclaration(declarations.AsImmutableOrNull)
        End Function

        Public Overrides ReadOnly Property Kind As DeclarationKind
            Get
                Return DeclarationKind.Namespace
            End Get
        End Property

        Public ReadOnly Property Declarations As ImmutableArray(Of SingleNamespaceDeclaration)
            Get
                Return _declarations
            End Get
        End Property

        Public Function GetLexicalSortKey(compilation As VisualBasicCompilation) As LexicalSortKey
            ' Return first sort key from all declarations.
            Dim sortKey As LexicalSortKey = New LexicalSortKey(_declarations(0).NameLocation, compilation)
            For i = 1 To _declarations.Length - 1
                sortKey = LexicalSortKey.First(sortKey, New LexicalSortKey(_declarations(i).NameLocation, compilation))
            Next
            Return sortKey
        End Function

        Public ReadOnly Property NameLocations As ImmutableArray(Of Location)
            Get
                If _declarations.Length = 1 Then
                    Dim loc = _declarations(0).NameLocation
                    If loc Is Nothing Then
                        Return ImmutableArray(Of Location).Empty
                    End If
                    Return ImmutableArray.Create(loc)
                End If

                Dim builder = ArrayBuilder(Of Location).GetInstance()

                For Each decl In _declarations
                    Dim loc = decl.NameLocation
                    If loc IsNot Nothing Then
                        builder.Add(loc)
                    End If
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Public ReadOnly Property SyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Dim references = ArrayBuilder(Of SyntaxReference).GetInstance()

                For Each decl In _declarations
                    If decl.SyntaxReference IsNot Nothing Then
                        references.Add(decl.SyntaxReference)
                    End If
                Next

                Return references.ToImmutableAndFree()
            End Get
        End Property

        Protected Overrides Function GetDeclarationChildren() As ImmutableArray(Of Declaration)
            Return StaticCast(Of Declaration).From(Me.Children)
        End Function

        Private Function MakeChildren() As ImmutableArray(Of MergedNamespaceOrTypeDeclaration)

            Dim childNamespaces = ArrayBuilder(Of SingleNamespaceDeclaration).GetInstance()
            Dim singleTypeDeclarations = ArrayBuilder(Of SingleTypeDeclaration).GetInstance()

            ' Distribute declarations into the two lists
            For Each d As SingleNamespaceDeclaration In _declarations
                For Each child As SingleNamespaceOrTypeDeclaration In d.Children
                    Dim singleNamespaceDeclaration As SingleNamespaceDeclaration = TryCast(child, SingleNamespaceDeclaration)
                    If singleNamespaceDeclaration IsNot Nothing Then
                        childNamespaces.Add(singleNamespaceDeclaration)
                    Else
                        singleTypeDeclarations.Add(DirectCast(child, SingleTypeDeclaration))
                    End If
                Next
            Next

            Dim result As ArrayBuilder(Of MergedNamespaceOrTypeDeclaration) = ArrayBuilder(Of MergedNamespaceOrTypeDeclaration).GetInstance()

            ' Merge and add the namespaces
            Select Case childNamespaces.Count
                Case 0
                    ' Do nothing
                Case 1
                    ' Add a single element
                    result.Add(MergedNamespaceDeclaration.Create(childNamespaces))
                Case 2
                    ' Could be one group or two
                    If SingleNamespaceDeclaration.EqualityComparer.Equals(childNamespaces(0), childNamespaces(1)) Then
                        result.Add(MergedNamespaceDeclaration.Create(childNamespaces))
                    Else
                        result.Add(MergedNamespaceDeclaration.Create(childNamespaces(0)))
                        result.Add(MergedNamespaceDeclaration.Create(childNamespaces(1)))
                    End If
                Case Else
                    ' Three or more. Use GroupBy to add by groups.
                    For Each group In childNamespaces.GroupBy(Function(n) n, SingleNamespaceDeclaration.EqualityComparer)
                        result.Add(MergedNamespaceDeclaration.Create(group))
                    Next
            End Select

            childNamespaces.Free()

            ' Merge and add the types
            If singleTypeDeclarations.Count <> 0 Then
                result.AddRange(MergedTypeDeclaration.MakeMergedTypes(singleTypeDeclarations))
            End If

            singleTypeDeclarations.Free()

            Return result.ToImmutableAndFree()

        End Function

        Public Overloads ReadOnly Property Children As ImmutableArray(Of MergedNamespaceOrTypeDeclaration)
            Get
                If Me._children.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(Me._children, MakeChildren())
                End If

                Return Me._children
            End Get
        End Property

        ' Is this declaration merged from declarations with different case-sensitive spellings
        ' (i.e., "Namespace GOO" and "Namespace goo".
        Public ReadOnly Property HasMultipleSpellings As Boolean
            Get
                Return _multipleSpellings
            End Get
        End Property
    End Class
End Namespace
