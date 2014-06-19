Imports System.Collections.Generic
Imports System.Globalization
Imports System.Threading
Imports Roslyn.Compilers.Common
Imports System.Runtime.InteropServices

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class SourceFieldSymbol
        ''' <summary>
        ''' A source field with an explicit initializer. In a declaration declaring multiple
        ''' fields, such as "Dim a, b, c = d", this class is used for the fields other than
        ''' the first. (The first field is an instance of SourceFieldSymbolWithInitializer.)
        ''' An instance of this class holds a reference to the first field in the declaration
        ''' and reuses the bound initializer from that field.
        ''' </summary>
        Private NotInheritable Class SourceFieldSymbolSiblingInitializer
            Inherits SourceFieldSymbol

            ' Sibling field symbol with common initializer (used to
            ' avoid binding constant initializer multiple times).
            Private ReadOnly m_sibling As SourceFieldSymbol

            Public Sub New(container As SourceNamedTypeSymbol,
                declRef As SyntaxReference,
                syntaxRef As SyntaxReference,
                name As String,
                type As TypeSymbol,
                memberFlags As SourceMemberFlags,
                sibling As SourceFieldSymbol)
                MyBase.New(container, declRef, syntaxRef, name, type, memberFlags)
                m_sibling = sibling
            End Sub

            Friend Overrides ReadOnly Property EqualsValueOrAsNewInitOpt As SyntaxNode
                Get
                    Return m_sibling.EqualsValueOrAsNewInitOpt
                End Get
            End Property

            Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
                Return m_sibling.GetConstantValue(inProgress)
            End Function

            Protected Overrides Function GetInferredConstantType() As TypeSymbol
                Return m_sibling.GetInferredConstantType()
            End Function
        End Class
    End Class
End Namespace