Imports System
Imports Roslyn.Services.Formatting
Imports Roslyn.Utilities

Namespace Roslyn.Services.VisualBasic.Formatting

    ''' <summary>
    ''' formatting options
    ''' </summary>
    Public Class VisualBasicFormattingOptions
        Inherits FormattingOptions

        Private ReadOnly _useTab As Boolean
        Private ReadOnly _tabSize As Integer
        Private ReadOnly _indentationSize As Integer
        Private ReadOnly _debugMode As Boolean

        Public Sub New(useTab As Boolean, tabSize As Integer, indentationSize As Integer, debugMode As Boolean)
            Me._useTab = useTab
            Me._tabSize = tabSize
            Me._indentationSize = indentationSize
            Me._debugMode = debugMode
        End Sub

        Public Overrides ReadOnly Property UseTab As Boolean
            Get
                Return Me._useTab
            End Get
        End Property

        Public Overrides ReadOnly Property TabSize As Integer
            Get
                Return Me._tabSize
            End Get
        End Property

        Public Overrides ReadOnly Property IndentationSize As Integer
            Get
                Return Me._indentationSize
            End Get
        End Property

        Friend Overrides ReadOnly Property DebugMode As Boolean
            Get
                Return Me._debugMode
            End Get
        End Property
    End Class
End Namespace

