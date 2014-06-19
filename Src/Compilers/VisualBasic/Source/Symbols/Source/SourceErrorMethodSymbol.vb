Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Roslyn.Compilers.Collections
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' An error type, used to represent the type of a type binding
    ''' operation when binding fails.
    ''' </summary>
    Friend Class SourceErrorMethodSymbol
        Inherits ErrorMethodSymbol

        Private ReadOnly _name As String
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _diagnostic As DiagnosticInfo

        Friend Sub New(ByVal name As String, ByVal returnType As TypeSymbol, ByVal diagnostic As DiagnosticInfo)
            Me._name = name
            Me._returnType = returnType
            Me._diagnostic = diagnostic
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._name
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return Me._returnType
            End Get
        End Property

        Public Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return Me._diagnostic
            End Get
        End Property
    End Class

End Namespace

