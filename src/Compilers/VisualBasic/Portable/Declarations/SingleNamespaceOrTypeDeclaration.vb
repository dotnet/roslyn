' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend MustInherit Class SingleNamespaceOrTypeDeclaration
        Inherits Declaration

        Public ReadOnly SyntaxReference As SyntaxReference
        Public ReadOnly NameLocation As Location

        Protected Sub New(name As String, syntaxReference As SyntaxReference, nameLocation As Location)
            MyBase.New(name)
            Me.SyntaxReference = syntaxReference
            Me.NameLocation = nameLocation
        End Sub

        Public ReadOnly Property Location As Location
            Get
                Return Me.SyntaxReference.GetLocation()
            End Get
        End Property

        Protected MustOverride Function GetNamespaceOrTypeDeclarationChildren() As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)

        Protected Overrides Function GetDeclarationChildren() As ImmutableArray(Of Declaration)
            Return StaticCast(Of Declaration).From(Me.GetNamespaceOrTypeDeclarationChildren())
        End Function

        Public Overloads ReadOnly Property Children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
            Get
                Return Me.GetNamespaceOrTypeDeclarationChildren()
            End Get
        End Property

        ''' <summary>
        ''' This function is used to determine the best name of a type or namespace when there are multiple declarations that
        ''' have the same name but with different spellings.
        ''' If this declaration is part of the rootnamespace (specified by /rootnamespace:&lt;nsname&gt; this is considered the best name.
        ''' Otherwise the best name of a type or namespace is the one that String.Compare considers to be less using a Ordinal.
        ''' In practice this prefers uppercased or camelcased identifiers.
        ''' </summary>
        ''' <typeparam name="T"></typeparam>
        ''' <param name="singleDeclarations">The single declarations.</param>
        ''' <param name="multipleSpellings">Set to true if there were multiple distinct spellings.</param>
        Public Shared Function BestName(Of T As SingleNamespaceOrTypeDeclaration)(singleDeclarations As ImmutableArray(Of T), ByRef multipleSpellings As Boolean) As String
            Debug.Assert(Not singleDeclarations.IsEmpty)
            multipleSpellings = False

            Dim bestDeclarationName = singleDeclarations(0).Name
            For declarationIndex = 1 To singleDeclarations.Length - 1
                Dim otherName = singleDeclarations(declarationIndex).Name
                Dim comp = String.Compare(bestDeclarationName, otherName, StringComparison.Ordinal)
                If comp <> 0 Then
                    multipleSpellings = True

                    ' We detected multiple spellings. If one of the namespaces is part of the rootnamespace
                    ' we can already return from this loop.

                    If comp > 0 Then
                        bestDeclarationName = otherName
                    End If
                End If
            Next

            Return bestDeclarationName
        End Function

        Public Shared Function BestName(Of T As SingleNamespaceOrTypeDeclaration)(singleDeclarations As ImmutableArray(Of T)) As String
            Dim multipleSpellings As Boolean = False
            Return BestName(Of T)(singleDeclarations, multipleSpellings)
        End Function

    End Class
End Namespace
