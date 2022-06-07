' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ' Note: Namespace Global has empty string as a name, as well as namespaces with errors
    Friend Class SingleNamespaceDeclaration
        Inherits SingleNamespaceOrTypeDeclaration

        Private ReadOnly _children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
        Public Property HasImports As Boolean
        Public ReadOnly IsPartOfRootNamespace As Boolean

        Public Sub New(name As String,
                       hasImports As Boolean,
                       syntaxReference As SyntaxReference,
                       nameLocation As Location,
                       children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration),
                       Optional isPartOfRootNamespace As Boolean = False)
            MyBase.New(name, syntaxReference, nameLocation)
            Me._children = children
            Me.HasImports = hasImports
            Me.IsPartOfRootNamespace = isPartOfRootNamespace
        End Sub

        ' Is this representing the global namespace ("Namespace Global")
        Public Overridable ReadOnly Property IsGlobalNamespace As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Kind As DeclarationKind
            Get
                Return DeclarationKind.Namespace
            End Get
        End Property

        Protected Overrides Function GetNamespaceOrTypeDeclarationChildren() As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
            Return Me._children
        End Function

        ' If this declaration was part of a namespace block, return it, otherwise return nothing.
        Public Function GetNamespaceBlockSyntax() As NamespaceBlockSyntax
            If SyntaxReference Is Nothing Then
                Return Nothing
            Else
                Return SyntaxReference.GetSyntax().AncestorsAndSelf().OfType(Of NamespaceBlockSyntax)().FirstOrDefault()
            End If
        End Function

        Private Class Comparer
            Implements IEqualityComparer(Of SingleNamespaceDeclaration)

            Private Shadows Function Equals(decl1 As SingleNamespaceDeclaration, decl2 As SingleNamespaceDeclaration) As Boolean Implements IEqualityComparer(Of SingleNamespaceDeclaration).Equals
                Return IdentifierComparison.Equals(decl1.Name, decl2.Name)
            End Function

            Private Shadows Function GetHashCode(decl1 As SingleNamespaceDeclaration) As Integer Implements IEqualityComparer(Of SingleNamespaceDeclaration).GetHashCode
                Return IdentifierComparison.GetHashCode(decl1.Name)
            End Function
        End Class

        Public Shared ReadOnly EqualityComparer As IEqualityComparer(Of SingleNamespaceDeclaration) = New Comparer()

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
        Public Overloads Shared Function BestName(Of T As SingleNamespaceDeclaration)(singleDeclarations As ImmutableArray(Of T), ByRef multipleSpellings As Boolean) As String
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

                    If singleDeclarations(0).IsPartOfRootNamespace Then
                        Return bestDeclarationName
                    End If

                    If singleDeclarations(declarationIndex).IsPartOfRootNamespace Then
                        Return otherName
                    End If

                    If comp > 0 Then
                        bestDeclarationName = otherName
                    End If
                End If
            Next

            Return bestDeclarationName
        End Function
    End Class
End Namespace
